namespace HybridCast_ServerRelay.Models
{
    public class Room
    {
        public Guid Id { get; private set; }
        public string Code { get; set; }

        public Room(string code)
        {
            Id = Guid.NewGuid();
            Code = code;
        }
    }
}
