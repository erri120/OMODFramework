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

using System;
using System.IO;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace OMODFramework
{
    public class Framework
    {
        public static string Version = "1.1.12";
        public static byte MajorVersion = 1;
        public static byte MinorVersion = 1;
        public static byte BuildNumber = 12;
        public static byte CurrentOmodVersion = 4;

        public static bool IgnoreVersion = false;

        public static int MaxMemoryStreamSize => 67108864;

        public static string TempDir { get; set; } = Path.Combine(Path.GetTempPath(), "OMODFramework");

        public static bool EnableWarnings { get; set; } = false;

        public static void CleanTempDir(bool deleteRoot = false)
        {
            if(!Directory.Exists(TempDir))
                return;

            var dInfo = new DirectoryInfo(TempDir);
            dInfo.GetFiles().Do(f => {if(f.Exists && !f.IsReadOnly) f.Delete();});
            dInfo.GetDirectories().Do(d => {if(d.Exists && !d.Attributes.HasFlag(FileAttributes.ReadOnly)) d.Delete(true);});

            if(deleteRoot)
                Directory.Delete(TempDir);
        }
    }

    public class OMODFrameworkException : ApplicationException
    {
        public OMODFrameworkException(string s) : base(s)
        {

        }
    }
}
