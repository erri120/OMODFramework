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
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip;
using SevenZip;
using SevenZip.Compression.LZMA;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using Path = Alphaleonis.Win32.Filesystem.Path;
using File = Alphaleonis.Win32.Filesystem.File;

namespace OMODFramework.Classes
{
    public enum CompressionType : byte { SevenZip, Zip }
    public enum CompressionLevel : byte { VeryHigh, High, Medium, Low, VeryLow, None }

    internal class SparseFileWriterStream : Stream
    {
        private long _length;

        private BinaryReader _fileList;

        private string _currentFile;
        private long _fileLength;
        private long _written;
        private FileStream _currentOutputStream;

        internal string BaseDirectory
        {
            get;
        }

        internal SparseFileWriterStream(Stream fileList)
        {
            _fileList = new BinaryReader(fileList);
            BaseDirectory = Utils.CreateTempDirectory();
            CreateDirectoryStructure();
            NextFile();
        }

        private void CreateDirectoryStructure()
        {
            long totalLength = 0;
            while (_fileList.PeekChar() != -1)
            {
                string path = _fileList.ReadString();
                _fileList.ReadInt32();
                totalLength += _fileList.ReadInt64();
                int upTo = 0;
                while (true)
                {
                    int i = path.IndexOf("\\", upTo, StringComparison.Ordinal);
                    if (i == -1) break;
                    string dir = path.Substring(0, i);
                    if (!Directory.Exists(Path.Combine(BaseDirectory, dir)))
                        Directory.CreateDirectory(Path.Combine(BaseDirectory, dir));
                    upTo = i + 1;
                }
            }

            _length = totalLength;
            _fileList.BaseStream.Position = 0;
        }

        private void NextFile()
        {
            _currentFile = _fileList.ReadString();
            _fileList.ReadUInt32(); //CRC
            _fileLength = _fileList.ReadInt64();
            _currentOutputStream?.Close();

            _currentOutputStream = File.Create(!Utils.IsSafeFileName(_currentFile)
                ? Path.Combine(Framework.TempDir, "IllegalFile")
                : Path.Combine(BaseDirectory, _currentFile));
            _written = 0;
        }

        public override long Position
        {
            get => 0;
            set { throw new NotImplementedException("The SparseFileStream does not support seeking"); }
        }

        public override long Length => _length;
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException("The SparseFileStream does not support reading");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            while (_written + count > _fileLength)
            {
                _currentOutputStream.Write(buffer, offset, (int)(_fileLength - _written));
                offset += (int)(_fileLength - _written);
                count -= (int)(_fileLength - _written);
                NextFile();
            }

            if (count <= 0) return;

            _currentOutputStream.Write(buffer, offset, count);
            _written += count;
        }

        public override void SetLength(long length)
        {
            throw new NotImplementedException("The SparseFileStream does not support length");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException("The SparseFileStream does not support seeking");
        }

        public override void Flush()
        {
            _currentOutputStream?.Flush();
        }

        public override void Close()
        {
            Flush();

            while (_fileList.BaseStream.Position < _fileList.BaseStream.Length)
            {
                _currentFile = _fileList.ReadString();
                _fileList.ReadUInt32(); //CRC
                _fileLength = _fileList.ReadInt64();

                if (_fileLength > 0)
                    throw new OMODFrameworkException(
                        "Compressed data file stream didn't contain enough information to fill all files");

                _currentOutputStream?.Close();

                _currentOutputStream = File.Create(!Utils.IsSafeFileName(_currentFile)
                    ? Path.Combine(Framework.TempDir, "IllegalFile")
                    : Path.Combine(BaseDirectory, _currentFile));
            }

            _fileList?.Close();
            _fileList = null;

            if (_currentOutputStream == null) return;

            _currentOutputStream.Close();
            _currentOutputStream = null;
        }
    }

    internal class SparseFileReaderStream : Stream
    {
        private long _position ;
        private long _length;

        private readonly List<string> _filePaths;
        private int _fileCount;
        private FileStream _currentInputStream ;
        private long _currentFileEnd ;
        private bool _finished;

        //internal string CurrentFile => _filePaths[_fileCount - 1];

        internal SparseFileReaderStream(List<string> filePaths)
        {
            _length = 0;
            filePaths.Do(s => _length += new FileInfo(s).Length);
            _filePaths = filePaths;
            NextFile();
        }

        private bool NextFile()
        {
            _currentInputStream?.Close();
            if (_fileCount >= _filePaths.Count)
            {
                _currentInputStream = null;
                _finished = true;
                return false;
            }

            _currentInputStream = File.OpenRead(_filePaths[_fileCount++]);
            _currentFileEnd += _currentInputStream.Length;
            return true;
        }

        public override long Position
        {
            get => _position;
            set { throw  new NotImplementedException("The SparseFileReaderStream does not support seeking");}
        }

        public override long Length => _length;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_finished) return 0;
            int read = 0;
            while (count > _currentFileEnd - _position)
            {
                _currentInputStream.Read(buffer, offset, (int)(_currentFileEnd - _position));
                offset += (int)(_currentFileEnd - _position);
                count -= (int)(_currentFileEnd - _position);
                read += (int)(_currentFileEnd - _position);
                _position += _currentFileEnd - _position;
                if(!NextFile()) return read;
            }

            if (count <= 0) return read;

            _currentInputStream.Read(buffer, offset, count);
            _position += count;
            read += count;

            return read;
        }

        public override void Write(byte[] buffer, int offset, int count) {
            throw new NotImplementedException("The SparseFileReaderStream does not support writing");
        }

        public override void SetLength(long length) {
            throw new NotImplementedException("The SparseFileReaderStream does not support setting length");
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotImplementedException("The SparseFileReaderStream does not support seeking");
        }

        public override void Flush()
        {
            _currentInputStream?.Close();
        }

        public override void Close()
        {
            Flush();
            if (_currentInputStream == null) return;
            _currentInputStream.Close();
            _currentInputStream = null;
        }
    }

    internal abstract class CompressionHandler
    {
        private static readonly Crc32 CRC32 = new Crc32();
        private static readonly SevenZipHandler SevenZip = new SevenZipHandler();
        private static readonly ZipHandler Zip = new ZipHandler();

        internal static void CompressFiles(List<string> files, List<string> folderStructure,
            out FileStream compressedStream, out Stream fileList, CompressionType compType,
            CompressionLevel compLevel)
        {
            fileList = GenerateFileList(files, folderStructure);
            switch (compType)
            {
                case CompressionType.SevenZip:
                    compressedStream = SevenZip.CompressAll(files, compLevel);
                    break;
                case CompressionType.Zip:
                    compressedStream = Zip.CompressAll(files, compLevel);
                    break;
                default:
                    throw new OMODFrameworkException("Unrecognized compression type.");
            }
        }

        internal static string DecompressFiles(Stream fileList, Stream compressedStream, CompressionType type)
        {
            switch (type)
            {
                case CompressionType.SevenZip: return SevenZip.DecompressAll(fileList, compressedStream);
                case CompressionType.Zip: return Zip.DecompressAll(fileList, compressedStream);
                default: throw new OMODFrameworkException("Unrecognized compression type.");
            }
        }

        private static Stream GenerateFileList(IReadOnlyList<string> files, IReadOnlyList<string> folderStructure)
        {
            var fileList = new MemoryStream();
            var bw = new BinaryWriter(fileList);

            for (int i = 0; i < files.Count; i++)
            {
                bw.Write(folderStructure[i]);
                bw.Write(CRC(files[i]));
                bw.Write(new FileInfo(files[i]).Length);
            }
            bw.Flush();

            fileList.Position = 0;
            return fileList;
        }

        internal static uint CRC(string s)
        {
            var fs = File.OpenRead(s);
            uint i = CRC(fs);
            fs.Close();
            return i;
        }

        internal static uint CRC(Stream inputStream)
        {
            byte[] buffer = new byte[4096];
            CRC32.Reset();
            while (inputStream.Position + 4096 < inputStream.Length)
            {
                inputStream.Read(buffer, 0, 4096);
                CRC32.Update(buffer);
            }

            if (inputStream.Position >= inputStream.Length) 
                return (uint)CRC32.Value;

            int i = (int)(inputStream.Length - inputStream.Position);
            inputStream.Read(buffer, 0, i);
            CRC32.Update(buffer);

            return (uint)CRC32.Value;
        }

        internal static uint CRC(byte[] b)
        {
            CRC32.Reset();
            CRC32.Update(b);
            return (uint)CRC32.Value;
        }

        public static void WriteStreamToZip(BinaryWriter bw, Stream input)
        {
            input.Position = 0;
            byte[] buffer = new byte[4096];
            int upTo = 0;

            while (input.Length - upTo > 4096)
            {
                input.Read(buffer, 0, 4096);
                bw.Write(buffer, 0, 4096);
                upTo += 4096;
            }

            if (input.Length - upTo <= 0)
                return;

            input.Read(buffer, 0, (int)(input.Length - upTo));
            bw.Write(buffer, 0, (int)(input.Length - upTo));
        }

        protected abstract string DecompressAll(Stream fileList, Stream compressedStream);
        protected abstract FileStream CompressAll(List<string> filePaths, CompressionLevel level);
    }

    internal class SevenZipHandler : CompressionHandler
    {
        protected override string DecompressAll(Stream fileList, Stream compressedStream)
        {
            var sfs = new SparseFileWriterStream(fileList);
            byte[] buffer = new byte[5];
            var decoder = new Decoder();

            compressedStream.Read(buffer, 0, 5);
            decoder.SetDecoderProperties(buffer);

            try
            {
                decoder.Code(compressedStream, sfs, compressedStream.Length - compressedStream.Position, sfs.Length,
                    null);
            }
            finally
            {
                sfs.Close();
            }

            return sfs.BaseDirectory;
        }

        protected override FileStream CompressAll(List<string> filePaths, CompressionLevel level)
        {
            var sfs = new SparseFileReaderStream(filePaths);
            var coder = new Encoder();
            int size = 0;

            switch (level)
            {
                case CompressionLevel.VeryHigh:
                    size = 1 << 26;
                    break;
                case CompressionLevel.High:
                    size = 1 << 25;
                    break;
                case CompressionLevel.Medium:
                    size = 1 << 23;
                    break;
                case CompressionLevel.Low:
                    size = 1 << 21;
                    break;
                case CompressionLevel.VeryLow:
                    size = 1 << 19;
                    break;
                case CompressionLevel.None:
                    break;
                default:
                    throw new OMODFrameworkException("Unrecognized compression level");
            }

            coder.SetCoderProperties(new[]
            {
                CoderPropID.DictionarySize
            }, new object[] { size });

            var fs = Utils.CreateTempFile();
            coder.WriteCoderProperties(fs);

            try
            {
                coder.Code(sfs, fs, sfs.Length, -1, null);
            }
            catch
            {
                fs.Close();
                throw;
            }
            finally
            {
                sfs.Close();
            }

            fs.Flush();
            fs.Position = 0;
            return fs;
        }
    }

    internal class ZipHandler : CompressionHandler
    {
        internal static int GetCompressionLevel(CompressionLevel level)
        {
            switch (level)
            {
                case CompressionLevel.VeryHigh:
                    return 9;
                case CompressionLevel.High:
                    return 7;
                case CompressionLevel.Medium:
                    return 5;
                case CompressionLevel.Low:
                    return 3;
                case CompressionLevel.VeryLow:
                    return 1;
                case CompressionLevel.None:
                    return 0;
                default:
                    throw new OMODFrameworkException("Unrecognized compression level.");
            }
        }

        protected override string DecompressAll(Stream fileList, Stream compressedStream)
        {
            var sfs = new SparseFileWriterStream(fileList);
            using (var zip = new ZipFile(compressedStream))
            {
                var file = zip.GetInputStream(0);
                byte[] buffer = new byte[4096];
                int i;

                while ((i = file.Read(buffer, 0, 4096)) > 0)
                {
                    sfs.Write(buffer, 0, i);
                }

                sfs.Close();
            }

            return sfs.BaseDirectory;
        }

        protected override FileStream CompressAll(List<string> filePaths, CompressionLevel level)
        {
            var fs = Utils.CreateTempFile(out var tempFileName);
            using (var sfs = new SparseFileReaderStream(filePaths))
            using (var zipStream = new ZipOutputStream(fs))
            {
                zipStream.SetLevel(GetCompressionLevel(level));
                var ze = new ZipEntry("a");
                zipStream.PutNextEntry(ze);
                
                byte[] buffer = new byte[4096];
                int upTo = 0;

                while (sfs.Length - upTo > 4096)
                {
                    sfs.Read(buffer, 0, 4096);
                    zipStream.Write(buffer, 0, 4096);
                    upTo += 4096;
                }

                if (sfs.Length - upTo > 0)
                {
                    sfs.Read(buffer, 0, (int)(sfs.Length - upTo));
                    zipStream.Write(buffer, 0, (int)(sfs.Length - upTo));
                }

                zipStream.Finish();
            }

            return File.OpenRead(tempFileName);
        }
    }
}
