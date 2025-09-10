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

        private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = false };

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
        public async Task<IActionResult> NewGame(string playerName, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(playerName))
            {
                return BadRequest("Player name must have a value");
            }

            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                var room = await roomStorage.CreateNewRoom(playerName, webSocket);

                if (room.Room != null && room.AddedPlayer != null)
                {
                    await SendRoomCode(room.Room, webSocket, cancellationToken);
                    await SendPlayerInformation(room.Room, room.AddedPlayer, webSocket, cancellationToken);
                    await WebSocketLoop(room.Room, room.AddedPlayer, webSocket, cancellationToken);
                }
                else
                {
                    return BadRequest("Couldn't create room");
                }
            }
            else
            {
                return BadRequest("Must be a web socket request");
            }

            return Ok();
        }

        [Route("connect")]
        public async Task<IActionResult> Connect(string roomCode, string playerName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                return BadRequest("Player name must have a value");
            }

            bool result = await roomStorage.CheckRoomCode(roomCode);

            if(!result)
            {
                return NotFound("Room code not found");
            }

            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                var room = await roomStorage.AddPlayer(roomCode, playerName, webSocket);

                if (room.Room != null && room.AddedPlayer != null)
                {
                    await SendPlayerInformation(room.Room, room.AddedPlayer, webSocket, cancellationToken);
                    await SendCurrentPlayerList(room.Room, webSocket, cancellationToken);
                    await SendPlayerAddedEvent(room.Room, room.AddedPlayer, webSocket, cancellationToken);
                    await WebSocketLoop(room.Room, room.AddedPlayer, webSocket, cancellationToken);
                }
                else
                {
                    return BadRequest("Couldn't add player to room");
                }
            }
            else
            {
                return BadRequest("Must be a web socket request");
            }

            return Ok();
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

        private async Task SendCurrentPlayerList(Room room, WebSocket webSocket, CancellationToken cancellationToken = default)
        {
            if (webSocket.State == WebSocketState.Open)
            {
                var players = await roomStorage.GetRoomPlayers(room.Code);

                var message = JsonSerializer.Serialize<ServerRelayMessage>(new ServerRelayMessage
                {
                    RelayMessageType = RelayMessageType.ServerMessage,
                    ServerMessageType = ServerMessageType.PlayerList,
                    Payload = JsonSerializer.Serialize(new { PlayerList = players.Select(x => new {x.Id, x.Name }) }),
                }, jsonSerializerOptions);

                var buffer = System.Text.Encoding.UTF8.GetBytes(message);

                var arraySegment = new ArraySegment<byte>(buffer, 0, buffer.Length);
                await webSocket.SendAsync(arraySegment, System.Net.WebSockets.WebSocketMessageType.Text, true, default);
            }
        }

        private async Task SendPlayerInformation(Room room, Player player, WebSocket webSocket, CancellationToken cancellationToken = default)
        {
            if (webSocket.State == WebSocketState.Open)
            {
                var message = JsonSerializer.Serialize<ServerRelayMessage>(new ServerRelayMessage
                {
                    RelayMessageType = RelayMessageType.ServerMessage,
                    ServerMessageType = ServerMessageType.PlayerInformation,
                    Payload = JsonSerializer.Serialize(new { player.Id, player.Name }),
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

        private async Task SendPlayerRemovedEvent(Room room, Player removedPlayer, CancellationToken cancellationToken = default)
        {
            foreach (var player in await this.roomStorage.GetRoomPlayers(room.Code))
            {
                if(player.WebSocket!.State != WebSocketState.Open)
                {
                    continue;
                }

                var message = JsonSerializer.Serialize<ServerRelayMessage>(new ServerRelayMessage
                {
                    RelayMessageType = RelayMessageType.ServerMessage,
                    ServerMessageType = ServerMessageType.PlayerRemoved,
                    Payload = JsonSerializer.Serialize(new { removedPlayer.Id, removedPlayer.Name }),
                }, jsonSerializerOptions);

                var buffer = Encoding.UTF8.GetBytes(message);

                await player.WebSocket!.SendAsync(
                    new ArraySegment<byte>(buffer, 0, buffer.Length),
                    WebSocketMessageType.Text,
                    true,
                    cancellationToken);
            }
        }

        private async Task WebSocketLoop(Room room, Player newPlayer, WebSocket webSocket, CancellationToken cancellationToken = default)
        {
            bool closeStatusReceived = false;
            while (webSocket.State == WebSocketState.Open)
            {
                var buffer = new byte[1024 * 4];
                var receiveResult = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), cancellationToken);

                if(receiveResult.CloseStatus != null)
                {
                    closeStatusReceived = true;
                    await roomStorage.RemovePlayer(room.Code, newPlayer.Id);
                    await SendPlayerRemovedEvent(room, newPlayer, cancellationToken);
                    break;
                }

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

            if(!closeStatusReceived)
            {
                await roomStorage.RemovePlayer(room.Code, newPlayer.Id);
                await SendPlayerRemovedEvent(room, newPlayer, cancellationToken);
            }
        }
    }
}
