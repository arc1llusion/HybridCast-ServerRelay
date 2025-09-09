using System.Text.Json;
using System.Text.Json.Serialization;

namespace HybridCast_ServerRelay.Models
{
    public enum RelayMessageType
    {
        ServerMessage,
        GameMessage
    }

    public enum ServerMessageType
    {
        None,
        RoomCode,
        PlayerAdded,
        PlayerRemoved
    }
    public class ServerRelayMessage
    {
        [JsonConverter(typeof(JsonStringEnumConverter<RelayMessageType>))]
        public RelayMessageType RelayMessageType { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter<ServerMessageType>))]
        public ServerMessageType ServerMessageType { get; set; }

        public Guid? GameMessagePlayerFromId { get; set; } = null;

        public string? GameMessagePlayerFromName { get; set; } = null;

        public object? Payload { get; set; } = JsonSerializer.Serialize("{}");
    }
}
