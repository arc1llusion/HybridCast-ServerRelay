using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;

namespace HybridCast_ServerRelay.Controllers
{
    [Route("[controller]")]
    public class PingController : Controller
    {
        private static List<WebSocket> sockets = new();
        private static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);


        [Route("/ping")]
        public async Task Get(CancellationToken token = default)
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                var buffer = System.Text.Encoding.Unicode.GetBytes("Pong");

                var arraySegment = new ArraySegment<byte>(buffer, 0, buffer.Length);
                await webSocket.SendAsync(arraySegment, System.Net.WebSockets.WebSocketMessageType.Text, true, token);

                await webSocket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Normal close", token);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        [Route("connect")]
        public async Task Connect(CancellationToken token = default)
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                await semaphore.WaitAsync();

                try
                { 
                    sockets.Add(webSocket);
                }
                finally
                {
                    semaphore.Release();
                }
                await Echo(webSocket, token);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        private async Task Echo(WebSocket webSocket, CancellationToken token = default)
        {
            var buffer = new byte[1024 * 4];
            var receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), token);

            while (!receiveResult.CloseStatus.HasValue)
            {
                await webSocket.SendAsync(
                    new ArraySegment<byte>(buffer, 0, receiveResult.Count),
                    receiveResult.MessageType,
                    receiveResult.EndOfMessage,
                    token);

                await semaphore.WaitAsync(token);

                try
                {
                    foreach (var ws in sockets)
                    {
                        if (ws != webSocket)
                        {
                            await ws.SendAsync(
                                new ArraySegment<byte>(buffer, 0, receiveResult.Count),
                                receiveResult.MessageType,
                                receiveResult.EndOfMessage,
                                token);
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }

                receiveResult = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), token);
            }

            await webSocket.CloseAsync(
                receiveResult.CloseStatus.Value,
                receiveResult.CloseStatusDescription,
                token);
        }
    }
}
