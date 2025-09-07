using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace HybridCast_ServerRelay.Models
{
    public class Player
    {
        public Guid Id { get; private set; }
        public string Name { get; set; } = string.Empty;
        public bool IsHost { get; set; }
        public WebSocket? WebSocket { get; set; }

        public Player()
        {
            Id = Guid.NewGuid();
        }
    }
}
