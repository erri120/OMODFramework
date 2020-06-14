using System;
using OMODFramework.Logging;

namespace OMODFramework
{
    internal static partial class Utils
    {
        internal static ILogger? Logger { get; set; }

        static Utils()
        {
            Logger = new FileLogger();
            Info("FileLogger initialized.");
        }

        private static void Log(LoggingLevel level, string message)
        {
            Logger?.Log(level, message, DateTime.Now);
        }

        internal static void Debug(string message)
        {
            Log(LoggingLevel.Debug, message);
        }

        internal static void Info(string message)
        {
            Log(LoggingLevel.Debug, message);
        }
    }
}
