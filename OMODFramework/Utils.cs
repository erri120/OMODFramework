/*
 * This file contains parts of the Oblivion Mod Manager licensed under GPLv2
 * and has been modified for use in this OMODFramework
 * Original source: https://www.nexusmods.com/oblivion/mods/2097
 * GPLv2: https://opensource.org/licenses/gpl-2.0.php
 */

using System;
using System.Collections.Generic;
using System.IO;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using File = Alphaleonis.Win32.Filesystem.File;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace OMODFramework
{
    public static class Utils
    {
        internal static bool IsSafeFileName(string s)
        {
            s = s.Replace('/', '\\');
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
                var s = Path.Combine(Framework.TempDir, "tmp_" + i);
                if (File.Exists(s))
                    continue;

                path = s;
                return File.Create(s);
            }
            throw new OMODFrameworkException("Could not create a new temp file because the directory is full!");
        }

        internal static string CreateTempDirectory()
        {
            for (var i = 0; i < 32000; i++)
            {
                var path = Path.Combine(Framework.TempDir, i.ToString());
                if (Directory.Exists(path))
                    continue;

                Directory.CreateDirectory(path);
                return path;
            }

            throw new OMODFrameworkException("Could not create a new temp folder because the directory is full!");
        }

        internal static void Do<T>(this IEnumerable<T> coll, Action<T> f)
        {
            foreach (var i in coll) f(i);
        }
    }
}
