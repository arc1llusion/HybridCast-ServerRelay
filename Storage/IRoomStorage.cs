using HybridCast_ServerRelay.Models;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace HybridCast_ServerRelay.Storage
{
    public interface IRoomStorage
    {
        public Task<bool> CheckRoomCode(string roomCode);
        public Task<(Room? Room, Player? AddedPlayer)> CreateNewRoom(string playerName, WebSocket hostSocket);
        public Task<(Room? Room, Player? AddedPlayer)> AddPlayer(string roomCode, string playerName, WebSocket playerSocket);
        public Task RemovePlayer(string roomCode, Guid playerId);

        public Task<Player[]> GetRoomPlayers(string roomCode);

        public Task CleanRooms();

    }

    public class RoomStorage : IRoomStorage
    {
        private readonly ConcurrentDictionary<Room, ConcurrentDictionary<Guid, Player>> Rooms = new();
        private readonly SemaphoreSlim Slim = new(1, 1);
        private readonly ILogger<RoomStorage> Logger;

        public RoomStorage(ILogger<RoomStorage> logger)
        {
            Logger = logger ?? throw new InvalidOperationException(nameof(ILogger<RoomStorage>));
        }

        public async Task<bool> CheckRoomCode(string roomCode)
        {
            await Slim.WaitAsync();

            try
            {
                return Rooms.Keys.Any(x => x.Code == roomCode);
            }
            finally
            {
                Slim.Release();
            }
        }

        public async Task<(Room? Room, Player? AddedPlayer)> CreateNewRoom(string playerName, WebSocket hostSocket)
        {
            string code = Utility.RandomUtility.GenerateRoomCode(4);
            var roomExists = true;

            do
            {
                roomExists = await CheckRoomCode(code);
            }
            while (roomExists);

            var room = new Room(code);
            if (!Rooms.TryAdd(room, new ConcurrentDictionary<Guid, Player>()))
            {
                return (null, null);
            }

            var addedPlayer = new Player()
            {
                IsHost = true,
                Name = playerName,
                WebSocket = hostSocket
            };

            if(!Rooms[room].TryAdd(addedPlayer.Id, addedPlayer))
            {
                return (null, null);
            }

            return (room, addedPlayer);
        }

        public async Task<(Room? Room, Player? AddedPlayer)> AddPlayer(string roomCode, string playerName, WebSocket playerSocket)
        {
            await Slim.WaitAsync();

            try
            {
                var room = Rooms.Keys.FirstOrDefault(x => x.Code == roomCode);

                if (room != null)
                {
                    var player = new Player()
                    {
                        Name = playerName,
                        WebSocket = playerSocket,
                        IsHost = false
                    };

                    Rooms[room].TryAdd(player.Id, player);

                    return (room, player);
                }

                return (null, null);
            }
            finally
            {
                Slim.Release();
            }
        }

        public async Task RemovePlayer(string roomCode, Guid playerId)
        {
            await Slim.WaitAsync();

            try
            {
                var room = Rooms.Keys.FirstOrDefault(x => x.Code == roomCode);

                if (room != null)
                {
                    if(Rooms[room].TryGetValue(playerId, out var player))
                    {
                        Rooms[room].TryRemove(new KeyValuePair<Guid, Player>(playerId, player));
                    }                    
                }
            }
            finally
            {
                Slim.Release();
            }
        }

        public async Task<Player[]> GetRoomPlayers(string roomCode)
        {
            await Slim.WaitAsync();

            try
            {
                var room = Rooms.Keys.FirstOrDefault(x => x.Code == roomCode);

                if (room != null)
                {
                    return Rooms[room].Values.ToArray();
                }

                return [];
            }
            finally
            {
                Slim.Release();
            }
        }

        public async Task CleanRooms()
        {
            await Slim.WaitAsync();

            try
            {
                List<KeyValuePair<Room, ConcurrentDictionary<Guid, Player>>> roomsToRemove = new();

                foreach (var room in Rooms)
                {
                    if (Rooms[room.Key].Count == 0)
                    {
                        roomsToRemove.Add(room);
                    }
                }

                foreach(var room in roomsToRemove)
                {
                    Logger.LogInformation($"Removed room: {room.Key.Code}");
                    Rooms.TryRemove(room);
                }
            }
            finally
            {
                Slim.Release();
            }
        }
    }
}
