using HybridCast_ServerRelay.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;

namespace HybridCast_ServerRelay.Controllers
{
    [Route("[controller]")]
    public class GameController : Controller
    {
        private static readonly ConcurrentDictionary<Room, ConcurrentBag<Player>> Rooms = new();
        [Route("/new")]
        public async Task NewGame()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                string code = string.Empty;
                Room room = null!;
                do
                {
                    code = Utility.RandomUtility.GenerateRoomCode(4);

                    room = new Room() { Code = code };
                }
                while (Rooms.Keys.Any(x => x.Code == code));


            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }
    }
}
