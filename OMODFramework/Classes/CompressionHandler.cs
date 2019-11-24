/*
 * This file contains parts of the Oblivion Mod Manager licensed under GPLv2
 * and has been modified for use in this OMODFramework
 * Original source: https://www.nexusmods.com/oblivion/mods/2097
 * GPLv2: https://opensource.org/licenses/gpl-2.0.php
 */

using System;
using System.Collections.Generic;
using System.IO;
using SevenZip.Compression.LZMA;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using Path = Alphaleonis.Win32.Filesystem.Path;
using File = Alphaleonis.Win32.Filesystem.File;

namespace OMODFramework.Classes
{
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
                    string dir = path.Substring(0, 1);
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

        internal string CurrentFile => _filePaths[_fileCount - 1];

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
}
