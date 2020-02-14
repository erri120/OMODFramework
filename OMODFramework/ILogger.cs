/*
    Copyright (C) 2019-2020  erri120

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

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
