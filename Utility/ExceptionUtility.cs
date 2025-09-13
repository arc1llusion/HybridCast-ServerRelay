using System.Text;

namespace HybridCast_ServerRelay.Utility
{
    public static class ExceptionUtility
    {
        public static string Unwind(this Exception exception)
        {
            StringBuilder builder = new();

            builder.AppendLine(exception.Message);
            builder.AppendLine(exception.StackTrace);

            Exception? current = exception.InnerException;
            int max = 2;
            int currentCount = 0;
            while(current != null)
            {
                builder.AppendLine(current.Message);
                builder.AppendLine(current.StackTrace);

                ++currentCount;
                if(currentCount >= max)
                {
                    break;
                }
            }

            return builder.ToString();
        }
    }
}
