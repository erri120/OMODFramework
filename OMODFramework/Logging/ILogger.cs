using System;
using JetBrains.Annotations;

namespace OMODFramework.Logging
{
    /// <summary>
    /// Logging levels. Lower values mean more verbose output.
    /// </summary>
    [PublicAPI]
    public enum LoggingLevel
    {
        /// <summary>
        /// Lowest value. This is very verbose and will even report
        /// on every function call during script execution. 
        /// </summary>
        Debug = -1,
        /// <summary>
        /// Default value.
        /// </summary>
        Info = 0
    }

    /// <summary>
    /// Interface for a custom Logger
    /// </summary>
    [PublicAPI]
    public interface ILogger : IDisposable
    {
        /// <summary>
        /// Lowest level that will be reported. <see cref="Log"/> won't be called
        /// if the level of the log is below this LowestLevel.
        /// </summary>
        LoggingLevel LowestLevel { get; set; }

        /// <summary>
        /// Log a message
        /// </summary>
        /// <param name="level">Level of the message, will always be higher than <see cref="LowestLevel"/></param>
        /// <param name="message">The message (can be multiline)</param>
        /// <param name="time">Time of the log</param>
        void Log(LoggingLevel level, string message, DateTime time);
    }
}
