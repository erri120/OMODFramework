// /*
//     Copyright (C) 2020  erri120
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <https://www.gnu.org/licenses/>.
// */

using System;
using JetBrains.Annotations;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace OMODFramework.Logging
{
    [PublicAPI]
    public static class OMODFrameworkLogging
    {
        public static readonly Logger MainLogger;
        
        static OMODFrameworkLogging()
        {
            MainLogger = SetupLogging();
        }

        private static LoggingConfiguration GetDefaultConfiguration()
        {
            var config = new LoggingConfiguration();
            var logFile = new FileTarget("logFile")
            {
                FileName = "OMODFramework.log"
            };
            var logConsole = new ConsoleTarget("logConsole");
            
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logConsole);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logFile);

            return config;
        }
        
        private static Logger SetupLogging()
        {
            var config = GetDefaultConfiguration();
            
            LogManager.Configuration = config;

            return LogManager.GetLogger("OMODFramework");
        }

        public static void AddLogTarget(string name, Target logTarget)
        {
            var config = GetDefaultConfiguration();
            
            config.AddTarget(name, logTarget);

            LogManager.Configuration = config;
        }
        
        public static Logger GetNullLogger()
        {
            return LogManager.CreateNullLogger();
        }

        public static Logger GetLogger(string name)
        {
            return LogManager.GetLogger(name);
        }
        
        [ContractAnnotation("=> halt")]
        public static void ErrorThrow(this Logger logger, Exception e)
        {
            logger.Error(e);
            throw e;
        }
    }
}
