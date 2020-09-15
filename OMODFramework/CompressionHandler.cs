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
    /// <summary>
    /// CompressionType enum for the OMOD
    /// </summary>
    [PublicAPI]
    public enum CompressionType : byte
    {
        /// <summary>
        /// LZMA
        /// </summary>
        SevenZip,

        /// <summary>
        /// Zip
        /// </summary>
        Zip
    }

    /// <summary>
    /// Level of compression. 
    /// </summary>
    [PublicAPI]
    public enum CompressionLevel
    {
        /// <summary>
        /// SevenZip: 1 left shift 26 dictionary size
        /// </summary>
        VeryHigh = 9,
        /// <summary>
        /// SevenZip: 1 left shift 25 dictionary size
        /// </summary>
        High = 7,
        /// <summary>
        /// SevenZip: 1 left shift 23 dictionary size
        /// </summary>
        Medium = 5,
        /// <summary>
        /// SevenZip: 1 left shift 21 dictionary size
        /// </summary>
        Low = 3,
        /// <summary>
        /// SevenZip: 1 left shift 19 dictionary size
        /// </summary>
        VeryLow = 1,
        /// <summary>
        /// SevenZip: 0 dictionary size
        /// </summary>
        None = 0
    }

    /// <summary>
    /// Progress Reporter interface for compression and decompression of SevenZip archives
    /// </summary>
    [PublicAPI]
    public interface ICodeProgress : SevenZip.ICodeProgress, IDisposable
    {
        /// <summary>
        /// Init function that is called before compression/decompression starts. Return
        /// whether or not this progress reporter should be used.
        /// </summary>
        /// <param name="totalSize">Total size to be compressed/decompressed</param>
        /// <param name="compressing">Whether we are compression or decompression</param>
        /// <returns></returns>
        bool Init(long totalSize, bool compressing);
    }

    internal static class CompressionHandler
    {
        internal static Stream DecompressStream(IEnumerable<OMODCompressedFile> entries, Stream compressedStream, CompressionType compressionType, ICodeProgress? progress = null)
        {
            var outSize = entries.Select(x => x.Length).Aggregate((x, y) => x + y);
            return compressionType switch
            {
                CompressionType.SevenZip => SevenZipDecompress(compressedStream, outSize, progress),
                CompressionType.Zip => ZipDecompress(compressedStream, outSize),
                _ => throw new ArgumentOutOfRangeException(nameof(compressionType), compressionType, null)
            };
        }

        internal static void CompressFiles(HashSet<OMODCreationFile> files, CompressionType type,
            CompressionLevel level, out Stream compressedStream, out Stream crcStream, ICodeProgress? progress = null)
        {
            crcStream = GenerateCRCStream(files);
            compressedStream = type switch
            {
                CompressionType.SevenZip => SevenZipOMODCompress(files, level, progress),
                CompressionType.Zip => ZipOMODCompress(files, level),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        private static Stream GenerateCRCStream(IEnumerable<OMODCreationFile> files)
        {
            var stream = new MemoryStream();
            using var bw = new BinaryWriter(stream, Encoding.Default, true);

            //data structure:
            //  path      (string)
            //  CRC       (uint)
            //  length    (long)

            var crc = new CRC32();

            foreach (var file in files)
            {
                var fi = new FileInfo(file.From);

                bw.Write(file.To);
                bw.Write(crc.FromFile(file.From));
                bw.Write(fi.Length);
            }

            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        private static Stream CreateDecompressedStream(HashSet<OMODCreationFile> files)
        {
            var length = files.Select(x => x.From.Length).Aggregate((x, y) => x + y);

            var decompressedStream = new MemoryStream(length);
            foreach (var file in files.Select(x => x.From))
            {
                using var fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                fs.CopyTo(decompressedStream);
            }

            decompressedStream.Position = 0;

            return decompressedStream;
        }

        #region SevenZip

        private static Stream SevenZipOMODCompress(HashSet<OMODCreationFile> files,
            CompressionLevel level, ICodeProgress? progress)
        {
            using var decompressedStream = CreateDecompressedStream(files);
            return SevenZipCompress(decompressedStream, level, progress);
        }

        internal static Stream SevenZipCompress(Stream decompressedStream, CompressionLevel level, ICodeProgress? progress)
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

            var res = false;
            if (progress != null)
                res = progress.Init(decompressedStream.Length, true);

            encoder.Code(decompressedStream, compressedStream, decompressedStream.Length, -1, res ? progress : null);

            compressedStream.Position = 0;

            return compressedStream;
        }

        internal static Stream SevenZipDecompress(Stream compressedStream, long outSize, ICodeProgress? progress)
        {
            var buffer = new byte[5];
            var decoder = new Decoder();
            compressedStream.Read(buffer, 0, 5);
            decoder.SetDecoderProperties(buffer);

            var inSize = compressedStream.Length - compressedStream.Position;
            var stream = new MemoryStream((int)outSize);

            var res = false;
            if (progress != null)
                res = progress.Init(outSize, false);

            decoder.Code(compressedStream, stream, inSize, outSize, res ? progress : null);

            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        #endregion

        #region Zip

        private static Stream ZipOMODCompress(HashSet<OMODCreationFile> files, CompressionLevel level)
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
