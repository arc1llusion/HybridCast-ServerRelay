using System.Text;

namespace HybridCast_ServerRelay.Utility
{
    public class RandomUtility
    {
        private static Random random = new Random(); // Initialize Random once for better randomness

        public static string GenerateRoomCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            StringBuilder sb = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                sb.Append(chars[random.Next(chars.Length)]);
            }

            return sb.ToString();
        }
    }
}
