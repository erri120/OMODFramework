/*
    Copyright (C) 2019  erri120

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

/*
 * This file contains parts of the Oblivion Mod Manager licensed under GPLv2
 * and has been modified for use in this OMODFramework
 * Original source: https://www.nexusmods.com/oblivion/mods/2097
 * GPLv2: https://opensource.org/licenses/gpl-2.0.php
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using File = Alphaleonis.Win32.Filesystem.File;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace OMODFramework
{
    internal class FileLogger : ILogger
    {
        private const string LogFile = "OMODFramework.log";
        private static string LogFilePath => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), LogFile);

        private bool _hasInit;

        public void Init()
        {
            if (_hasInit)
                return;

            _hasInit = true;

            if (File.Exists(LogFilePath))
            {
                try
                {
                    File.Delete(LogFilePath);
                }
                catch (Exception e)
                {
                    throw new OMODFrameworkException($"FileLogger could not initialize.\n{e}");
                }
            }

            try
            {
                File.Create(LogFilePath).Close();
                Log(LoggingLevel.INFO, "FileLogger started", DateTime.Now);
            }
            catch (Exception e)
            {
                throw new OMODFrameworkException($"Could not create logging file at {LogFilePath}\n{e}");
            }

        }

        public void Log(LoggingLevel level, string message, DateTime time)
        {
            if (!Framework.Settings.LoggingSettings.LogToFile)
                return;

            if (Framework.Settings.LoggingSettings.LowestLoggingLevel > level)
                return;

            if(!File.Exists(LogFilePath))
                throw new OMODFrameworkException($"FileLogger file at {LogFilePath} does not exist anymore!");

            var msg = $"[{time:HH:mm:ss:fff}][{level.ToString()}]: {message}";

            try
            {
                File.AppendAllText(LogFilePath, $"\n{msg}", Encoding.UTF8);
            }
            catch (Exception e)
            {
                throw new OMODFrameworkException($"Could not write to file {LogFilePath}\n{e}");
            }
        }
    }

    public static class Utils
    {
        static Utils()
        {
            Logger = new FileLogger();
            Logger.Init();
        }

        internal static ILogger Logger;

        private static void Log(LoggingLevel level, string s)
        {
            if (!Framework.Settings.LoggingSettings.UseLogger)
                return;

            Logger?.Log(level, s, DateTime.Now);
        }

        internal static void Script(string s)
        {
            Log(LoggingLevel.SCRIPT, s);
        }

        internal static void Debug(string s)
        {
            Log(LoggingLevel.DEBUG, s);
        }

        internal static void Info(string s)
        {
            Log(LoggingLevel.INFO, s);
        }

        internal static void Warn(string s)
        {
            Log(LoggingLevel.WARNING, s);
        }

        internal static void Error(string s)
        {
            Log(LoggingLevel.ERROR, s);
        }

        internal static string MakeValidFolderPath(string s)
        {
            s = s.Replace('/', '\\');
            if (s.StartsWith("\\")) s = s.Substring(1);
            // if (!s.EndsWith("\\")) s += "\\";
            if (s.Contains("\\\\")) s = s.Replace("\\\\", "\\");
            return s;
        }

        internal static bool IsSafeFileName(string s)
        {
            s = s.Replace('/', '\\');
            if (s.StartsWith("\\")) s = s.Substring(1);
            if (s.IndexOfAny(Path.GetInvalidPathChars()) != -1) return false;
            if (Path.IsPathRooted(s)) return false;
            if (s.StartsWith(".") || Array.IndexOf(Path.GetInvalidFileNameChars(), s[0]) != -1) return false;
            if (s.Contains("\\..\\")) return false;
            if (s.EndsWith(".") || Array.IndexOf(Path.GetInvalidFileNameChars(), s[s.Length - 1]) != -1) return false;
            return true;
        }

        internal static bool IsSafeFolderName(string s)
        {
            if (s.Length == 0) return true;
            s = s.Replace('/', '\\');
            if (s.StartsWith("\\")) s = s.Substring(1);
            if (s.IndexOfAny(Path.GetInvalidPathChars()) != -1) return false;
            if (Path.IsPathRooted(s)) return false;
            if (s.StartsWith(".") || Array.IndexOf(Path.GetInvalidFileNameChars(), s[0]) != -1) return false;
            if (s.Contains("\\..\\")) return false;
            if (s.EndsWith(".")) return false;
            return true;
        }

        internal static FileStream CreateTempFile() { return CreateTempFile(out _); }
        internal static FileStream CreateTempFile(out string path)
        {
            for (var i = 0; i < 32000; i++)
            {
                var s = Path.Combine(Framework.Settings.TempPath, "tmp_" + i);
                if (File.Exists(s))
                    continue;

                path = s;
                if (!Directory.Exists(Framework.Settings.TempPath))
                    Directory.CreateDirectory(Framework.Settings.TempPath);
                return File.Create(s);
            }
            throw new OMODFrameworkException("Could not create a new temp file because the directory is full!");
        }

        internal static string CreateTempDirectory()
        {
            for (var i = 0; i < 32000; i++)
            {
                var path = Path.Combine(Framework.Settings.TempPath, i.ToString());
                if (Directory.Exists(path))
                    continue;

                if (!Directory.Exists(Framework.Settings.TempPath))
                    Directory.CreateDirectory(Framework.Settings.TempPath);
                Directory.CreateDirectory(path);
                return path;
            }

            throw new OMODFrameworkException("Could not create a new temp folder because the directory is full!");
        }

        internal static void Do<T>(this IEnumerable<T> coll, Action<T> f)
        {
            foreach (var i in coll) f(i);
        }

        internal static void Do(this IEnumerable coll, Action<object> f)
        {
            foreach (var i in coll) f(i);
        }
    }
}
