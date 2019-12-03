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
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using File = Alphaleonis.Win32.Filesystem.File;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace OMODFramework
{
    public static class Utils
    {
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
