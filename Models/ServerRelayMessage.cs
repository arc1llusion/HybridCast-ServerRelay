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
        PlayerList,
        PlayerInformation,
        PlayerAdded,
        PlayerRemoved,
        PlayerDisconnected,
        PlayerReconnected
    }
    public class ServerRelayMessage
    {
        [JsonConverter(typeof(JsonStringEnumConverter<RelayMessageType>))]
        public RelayMessageType RelayMessageType { get; set; }

        public Guid? GameMessagePlayerFromId { get; set; } = null;

        public string? GameMessagePlayerFromName { get; set; } = null;

        public object? Payload { get; set; } = JsonSerializer.Serialize("{}");
    }

    public class ServerMessage
    {
        [JsonConverter(typeof(JsonStringEnumConverter<ServerMessageType>))]
        public ServerMessageType ServerMessageType { get; set; }

        public object? SubPayload { get; set; } = JsonSerializer.Serialize("{}");
    }
}
