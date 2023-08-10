using Common.Models;
using log4net;

namespace Common.Logging
{
    public class Logger
    {
        public static ILog Log { get; private set; }

        public static void SetLogger(LoggerType loggerName)
        {
            Log = LogManager.GetLogger(loggerName.ToString());
        }
    }
}
