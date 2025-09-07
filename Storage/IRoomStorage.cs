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

        public Task<ConcurrentBag<Player>> GetRoomPlayers(string roomCode);

    }

    public class RoomStorage : IRoomStorage
    {
        private readonly ConcurrentDictionary<Room, ConcurrentBag<Player>> Rooms = new();
        private readonly SemaphoreSlim Slim = new(1, 1);

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
            if (!Rooms.TryAdd(room, new ConcurrentBag<Player>()))
            {
                throw new InvalidOperationException("Couldn't add new room");
            }

            var addedPlayer = new Player()
            {
                IsHost = true,
                Name = playerName,
                WebSocket = hostSocket
            };

            Rooms[room].Add(addedPlayer);

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

                    Rooms[room].Add(player);

                    return (room, player);
                }

                return (null, null);
            }
            finally
            {
                Slim.Release();
            }
        }

        public async Task<ConcurrentBag<Player>> GetRoomPlayers(string roomCode)
        {
            await Slim.WaitAsync();

            try
            {
                var room = Rooms.Keys.FirstOrDefault(x => x.Code == roomCode);

                if (room != null)
                {
                    return Rooms[room];
                }

                return [];
            }
            finally
            {
                Slim.Release();
            }
        }
    }
}
