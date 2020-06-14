using System;
using System.IO;
using System.Reflection;
using System.Text;
using OMODFramework.Exceptions;

namespace OMODFramework.Logging
{
    internal sealed class FileLogger : ILogger
    {
        public LoggingLevel LowestLevel { get; set; } = LoggingLevel.Debug;

        private const string kLogFile = "OMODFramework.log";
        private static string LogFilePath => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", kLogFile);

        private readonly object _lockObject = new object();

        public FileLogger()
        {
            if (!File.Exists(LogFilePath)) return;
            try
            {
                File.Delete(LogFilePath);
            }
            catch (Exception e)
            {
                throw new OMODException($"Unable to delete log file at {LogFilePath}!", e);
            }
        }

        public void Log(LoggingLevel level, string message, DateTime time)
        {
            lock (_lockObject)
            {
                var msg = $"[{time:HH:mm:ss:fff}][{level}]: {message}";
                File.AppendAllText(LogFilePath, msg);
            }
        }

        public void Dispose()
        {
            Log(LoggingLevel.Debug, "FileLogger disposed", DateTime.Now);
        }
    }
}
