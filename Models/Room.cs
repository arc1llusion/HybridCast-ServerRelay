namespace HybridCast_ServerRelay.Models
{
    public class Room
    {
        public Guid Id { get; private set; }
        public string Code { get; set; }
        public DateTimeOffset? EmptiedTime { get; private set; } = null;

        public Room(string code)
        {
            Id = Guid.NewGuid();
            Code = code;
        }

        public void SetEmptyTime()
        {
            EmptiedTime = DateTimeOffset.UtcNow;
        }

        public void RemoveEmptyFlagIfExists()
        {
            EmptiedTime = null;
        }
    }
}
