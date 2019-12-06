using System;

namespace OMODFramework
{
    /// <summary>
    ///     Logging levels, script is the lowest and is will print out every function call within a script
    /// </summary>
    public enum LoggingLevel { SCRIPT, DEBUG, INFO, WARNING, ERROR }

    public interface ILogger
    {
        /// <summary>
        ///     Initialization of your logger, this gets called after you have set <see cref="LoggingSettings.Logger"/>
        /// </summary>
        void Init();

        /// <summary>
        ///     Logging a message
        /// </summary>
        /// <param name="level">Level of the message</param>
        /// <param name="message">The message (can be multiline)</param>
        /// <param name="time">Time when the message was fired</param>
        void Log(LoggingLevel level, string message, DateTime time);
    }
}
