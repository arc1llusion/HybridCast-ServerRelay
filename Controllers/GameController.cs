using HybridCast_ServerRelay.Models;
using HybridCast_ServerRelay.Storage;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace HybridCast_ServerRelay.Controllers
{
    [Route("[controller]")]
    public class GameController : Controller
    {
        private readonly IRoomStorage roomStorage;

        private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

        public GameController(IRoomStorage roomStorage)
        {
            this.roomStorage = roomStorage ?? throw new InvalidOperationException(nameof(IRoomStorage));
        }

        [HttpGet("CheckRoomCode")]
        public async Task CheckRoomCode(string roomCode)
        {
            bool result = await roomStorage.CheckRoomCode(roomCode);

            if (result)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status200OK;
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            }
        }

        [Route("newgame")]
        public async Task NewGame(string playerName, CancellationToken cancellationToken = default)
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                var room = await roomStorage.CreateNewRoom(playerName, webSocket);

                if (room.Room != null && room.AddedPlayer != null)
                {

                    await SendRoomCode(room.Room, webSocket, cancellationToken);
                    await WebSocketLoop(room.Room, room.AddedPlayer, webSocket, cancellationToken);
                }
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        [Route("connect")]
        public async Task Connect(string roomCode, string playerName, CancellationToken cancellationToken = default)
        {
            bool result = await roomStorage.CheckRoomCode(roomCode);

            if(!result)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                var room = await roomStorage.AddPlayer(roomCode, playerName, webSocket);

                if (room.Room != null && room.AddedPlayer != null)
                {
                    await SendPlayerAddedEvent(room.Room, room.AddedPlayer, webSocket, cancellationToken);
                    await WebSocketLoop(room.Room, room.AddedPlayer, webSocket, cancellationToken);
                }
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        private async Task SendRoomCode(Room room, WebSocket webSocket, CancellationToken cancellationToken = default)
        {
            if (webSocket.State == WebSocketState.Open)
            {
                var message = JsonSerializer.Serialize<ServerRelayMessage>(new ServerRelayMessage
                {
                    RelayMessageType = RelayMessageType.ServerMessage,
                    ServerMessageType = ServerMessageType.RoomCode,
                    Payload = JsonSerializer.Serialize(new { RoomCode = room.Code }),
                }, jsonSerializerOptions);

                var buffer = System.Text.Encoding.UTF8.GetBytes(message);

                var arraySegment = new ArraySegment<byte>(buffer, 0, buffer.Length);
                await webSocket.SendAsync(arraySegment, System.Net.WebSockets.WebSocketMessageType.Text, true, default);
            }
        }

        private async Task SendPlayerAddedEvent(Room room, Player addedPlayer, WebSocket webSocket, CancellationToken cancellationToken = default)
        {
            if(webSocket.State == WebSocketState.Open)
            {
                foreach (var player in await this.roomStorage.GetRoomPlayers(room.Code))
                {
                    if (player.WebSocket != webSocket)
                    {
                        var message = JsonSerializer.Serialize<ServerRelayMessage>(new ServerRelayMessage
                        {
                            RelayMessageType = RelayMessageType.ServerMessage,
                            ServerMessageType = ServerMessageType.PlayerAdded,
                            Payload = JsonSerializer.Serialize(new { addedPlayer.Id, addedPlayer.Name }),
                        }, jsonSerializerOptions);

                        var buffer = Encoding.UTF8.GetBytes(message);

                        await player.WebSocket!.SendAsync(
                            new ArraySegment<byte>(buffer, 0, buffer.Length),
                            WebSocketMessageType.Text,
                            true,
                            cancellationToken);
                    }
                }
            }
        }

        private async Task WebSocketLoop(Room room, Player newPlayer, WebSocket webSocket, CancellationToken cancellationToken = default)
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var buffer = new byte[1024 * 4];
                var receiveResult = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), cancellationToken);

                foreach (var player in await this.roomStorage.GetRoomPlayers(room.Code))
                {
                    if (player.WebSocket != webSocket)
                    {
                        var message = JsonSerializer.Serialize<ServerRelayMessage>(new ServerRelayMessage
                        {
                            RelayMessageType = RelayMessageType.GameMessage,
                            ServerMessageType = ServerMessageType.None,
                            GameMessagePlayerFromId = newPlayer.Id,
                            GameMessagePlayerFromName = newPlayer.Name,
                            Payload = String.Join(string.Empty, Encoding.UTF8.GetString(buffer.Where(x => x != 0).ToArray()))
                        }, jsonSerializerOptions);

                        var responseBuffer = Encoding.UTF8.GetBytes(message);

                        await player.WebSocket!.SendAsync(
                            new ArraySegment<byte>(responseBuffer, 0, responseBuffer.Length),
                            WebSocketMessageType.Text,
                            true,
                            cancellationToken);
                    }
                }
            }
        }
    }
}
