#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;
using JetBrains.Annotations;
using SevenZip.Compression.LZMA;

namespace OMODFramework
{
    [PublicAPI]
    public enum CompressionType : byte
    {
        SevenZip,
        Zip
    }

    [PublicAPI]
    public enum CompressionLevel : byte
    {
        VeryHigh, 
        High, 
        Medium,
        Low,
        VeryLow,
        None
    }

    internal class DecompressedStreamWriter : Stream
    {
        private readonly List<OMODCompressedEntry> _entryList;
        private readonly DirectoryInfo _outputDirectory;

        private long _written;
        private int _entryListPos;
        private OMODCompressedEntry _currentEntry => _entryList[_entryListPos];

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length { get; }

        public override long Position { get; set; }

        internal DecompressedStreamWriter(IEnumerable<OMODCompressedEntry> entryList, DirectoryInfo outputDirectory)
        {
            _entryList = entryList.ToList();
            _outputDirectory = outputDirectory;

            Length = _entryList.Select(x => x.Length).Aggregate((x, y) => x + y);
            _entryListPos = -1;
        }

        private bool NextFile()
        {
            _entryListPos++;
            if (_entryListPos > _entryList.Count)
                throw new IndexOutOfRangeException();

            var file = new FileInfo(_currentEntry.GetFullPath(_outputDirectory));
            if(file.Directory == null)
                throw new NullReferenceException("Directory is null!");
            if (!file.Directory.Exists)
            {
                file.Directory.Create();
            }

            if (!file.Exists) return true;
            if (file.Length == _currentEntry.Length)
                return false;
            file.Delete();

            return true;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            while (count > _written)
            {
                if(NextFile())
                {
                    using var fileStream = File.Create(_currentEntry.GetFullPath(_outputDirectory));
                    fileStream.Write(buffer, offset, (int)_currentEntry.Length);
                }

                offset += (int) _currentEntry.Length;
                _written += _currentEntry.Length;
            }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        #region Not Implemented

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    internal static class CompressionHandler
    {
        internal static Stream DecompressStream(IEnumerable<OMODCompressedEntry> entryList, Stream compressedStream, CompressionType compressionType)
        {
            var outSize = entryList.Select(x => x.Length).Aggregate((x, y) => x + y);
            return compressionType switch
            {
                CompressionType.SevenZip => SevenZipDecompress(compressedStream, outSize),
                CompressionType.Zip => ZipDecompress(compressedStream, outSize),
                _ => throw new ArgumentOutOfRangeException(nameof(compressionType), compressionType, null)
            };
        }

        internal static void WriteDecompressedStream(IEnumerable<OMODCompressedEntry> entryList,
            Stream decompressedStream, DirectoryInfo outputDirectory)
        {
            using var stream = new DecompressedStreamWriter(entryList, outputDirectory);
            decompressedStream.CopyTo(stream);
        }

        private static Stream SevenZipDecompress(Stream compressedStream, long outSize)
        {
            var buffer = new byte[5];
            var decoder = new Decoder();
            compressedStream.Read(buffer, 0, 5);
            decoder.SetDecoderProperties(buffer);

            var stream = new MemoryStream();
            var inSize = compressedStream.Length - compressedStream.Position;

            decoder.Code(compressedStream, stream, inSize, outSize, null);

            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        private static Stream ZipDecompress(Stream compressedStream, long outSize)
        {
            var zip = new ZipFile(compressedStream);
            using var inputStream = zip.GetInputStream(0);
            var stream = new MemoryStream();

            inputStream.CopyTo(stream);

            stream.Seek(0, SeekOrigin.Begin);
            if(stream.Length != outSize)
                throw new Exception($"Expected stream length to be {outSize} but is {stream.Length}!");

            return stream;
        }
    }
}
