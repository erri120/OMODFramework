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

        private static Stream SevenZipDecompress(Stream compressedStream, long outSize)
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

        private static Stream ZipDecompress(Stream compressedStream, long outSize)
        {
            var zip = new ZipFile(compressedStream);
            using var inputStream = zip.GetInputStream(0);
            var stream = new MemoryStream((int)outSize);

            inputStream.CopyTo(stream);

            stream.Seek(0, SeekOrigin.Begin);
            if(stream.Length != outSize)
                throw new Exception($"Expected stream length to be {outSize} but is {stream.Length}!");

            return stream;
        }
    }
}
