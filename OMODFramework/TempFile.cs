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
using System.IO;
using System.Text;

namespace OMODFramework
{
    internal class TempFile : IDisposable
    {
        public readonly FileStream FileStream;
        public readonly string Path;

        private BinaryReader? _binaryReader;
        
        public TempFile(string path, FileMode mode, FileAccess access, FileShare share, string? copyFile = null)
        {
            if (copyFile != null)
                File.Copy(copyFile, path, true);

            if (File.Exists(path))
            {
                if (mode == FileMode.CreateNew)
                    mode = FileMode.OpenOrCreate;
            }
            FileStream = File.Open(path, mode, access, share);
            Path = path;
        }

        public BinaryReader GetBinaryReader()
        {
            _binaryReader = new BinaryReader(FileStream, Encoding.UTF8, true);
            return _binaryReader;
        }
        
        public void Dispose()
        {
            _binaryReader?.Dispose();

            FileStream.Dispose();
            
            File.Delete(Path);
        }
    }
}
