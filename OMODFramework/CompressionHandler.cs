#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using JetBrains.Annotations;
using SevenZip;
using Decoder = SevenZip.Compression.LZMA.Decoder;
using Encoder = SevenZip.Compression.LZMA.Encoder;

namespace OMODFramework
{
    [PublicAPI]
    public enum CompressionType : byte
    {
        SevenZip,
        Zip
    }

    [PublicAPI]
    public enum CompressionLevel
    {
        VeryHigh = 9, 
        High = 7, 
        Medium = 5,
        Low = 3,
        VeryLow = 1,
        None = 0
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

        internal static void CompressFiles(IEnumerable<CreationOptions.CreationOptionFile> files, CompressionType type,
            CompressionLevel level, out Stream compressedStream, out Stream crcStream)
        {
            IEnumerable<CreationOptions.CreationOptionFile> creationOptionFiles = files.ToList();
            crcStream = GenerateCRCStream(creationOptionFiles);
            compressedStream = type switch
            {
                CompressionType.SevenZip => SevenZipOMODCompress(creationOptionFiles, level),
                CompressionType.Zip => ZipOMODCompress(creationOptionFiles, level),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        private static Stream GenerateCRCStream(IEnumerable<CreationOptions.CreationOptionFile> files)
        {
            var stream = new MemoryStream();
            using var bw = new BinaryWriter(stream, Encoding.Default, true);

            //data structure:
            //  path      (string)
            //  CRC       (uint)
            //  length    (long)

            foreach (var file in files)
            {
                bw.Write(file.To);
                bw.Write(Utils.CRC32(file.From));
                bw.Write(file.From.Length);
            }

            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        private static Stream CreateDecompressedStream(IEnumerable<CreationOptions.CreationOptionFile> files)
        {
            var list = files.ToList();
            var length = list.Select(x => x.From.Length).Aggregate((x, y) => x + y);

            var decompressedStream = new MemoryStream((int)length);
            foreach (var file in list.Select(x => x.From))
            {
                using var fs = file.OpenRead();
                fs.CopyTo(decompressedStream);
            }

            decompressedStream.Position = 0;

            return decompressedStream;
        }

        #region SevenZip

        private static Stream SevenZipOMODCompress(IEnumerable<CreationOptions.CreationOptionFile> files,
            CompressionLevel level)
        {
            using var decompressedStream = CreateDecompressedStream(files);
            return SevenZipCompress(decompressedStream, level);
        }

        internal static Stream SevenZipCompress(Stream decompressedStream, CompressionLevel level)
        {
            var encoder = new Encoder();
            var dictionarySize = level switch
            {
                CompressionLevel.VeryHigh => 1 << 26,
                CompressionLevel.High => 1 << 25,
                CompressionLevel.Medium => 1 << 23,
                CompressionLevel.Low => 1 << 21,
                CompressionLevel.VeryLow => 1 << 19,
                CompressionLevel.None => 0,
                _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
            };

            encoder.SetCoderProperties(new[] { CoderPropID.DictionarySize }, new object[] { dictionarySize });

            var compressedStream = new MemoryStream();
            encoder.WriteCoderProperties(compressedStream);

            encoder.Code(decompressedStream, compressedStream, decompressedStream.Length, -1, null);

            compressedStream.Position = 0;

            return compressedStream;
        }

        internal static Stream SevenZipDecompress(Stream compressedStream, long outSize)
        {
            var buffer = new byte[5];
            var decoder = new Decoder();
            compressedStream.Read(buffer, 0, 5);
            decoder.SetDecoderProperties(buffer);

            var inSize = compressedStream.Length - compressedStream.Position;
            var stream = new MemoryStream((int)outSize);

            decoder.Code(compressedStream, stream, inSize, outSize, null);

            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        #endregion

        #region Zip

        private static Stream ZipOMODCompress(IEnumerable<CreationOptions.CreationOptionFile> files, CompressionLevel level)
        {
            using var decompressedStream = CreateDecompressedStream(files);
            return ZipCompress(decompressedStream, level);
        }

        internal static Stream ZipCompress(Stream decompressedStream, CompressionLevel level)
        {
            var ms = new MemoryStream();
            using var baseOutputStream = new MemoryStream();
            var zipStream = new ZipOutputStream(baseOutputStream);

            zipStream.SetLevel((int)level);

            var entry = new ZipEntry("a");
            zipStream.PutNextEntry(entry);

            decompressedStream.CopyTo(zipStream);

            zipStream.Finish();

            baseOutputStream.Position = 0;
            baseOutputStream.CopyTo(ms);
            ms.Position = 0;

            zipStream.Close();

            return ms;
        }

        internal static Stream ZipDecompress(Stream compressedStream, long outSize)
        {
            var zip = new ZipFile(compressedStream);
            using var inputStream = zip.GetInputStream(0);
            var stream = new MemoryStream((int)outSize);

            inputStream.CopyTo(stream);

            stream.Seek(0, SeekOrigin.Begin);
            if (stream.Length != outSize)
                throw new Exception($"Expected stream length to be {outSize} but is {stream.Length}!");

            return stream;
        }

        #endregion
    }
}
