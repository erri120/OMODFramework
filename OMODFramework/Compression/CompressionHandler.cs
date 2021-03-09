using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Force.Crc32;
using JetBrains.Annotations;
using SharpCompress.Compressors.LZMA;

namespace OMODFramework.Compression
{
    /// <summary>
    /// Level of compression. 
    /// </summary>
    [PublicAPI]
    public enum SevenZipCompressionLevel
    {
        /// <summary>
        /// SevenZip: 1 left shift 26 dictionary size
        /// </summary>
        VeryHigh = 1 << 26,
        /// <summary>
        /// SevenZip: 1 left shift 25 dictionary size
        /// </summary>
        High = 1 << 25,
        /// <summary>
        /// SevenZip: 1 left shift 23 dictionary size
        /// </summary>
        Medium = 1 << 23,
        /// <summary>
        /// SevenZip: 1 left shift 21 dictionary size
        /// </summary>
        Low = 1 << 21,
        /// <summary>
        /// SevenZip: 1 left shift 19 dictionary size
        /// </summary>
        VeryLow = 1 << 19,
        /// <summary>
        /// SevenZip: 0 dictionary size
        /// </summary>
        None = 0
    }
    
    internal static class CompressionHandler
    {
        internal static void CompressFiles(IEnumerable<OMODCreationFile> files, CompressionType compressionType,
            SevenZipCompressionLevel sevenZipCompressionLevel, CompressionLevel zipCompressionLevel,
            out MemoryStream crcStream, out MemoryStream compressedStream)
        {
            GenerateCRCStream(files, out crcStream, out var contentStream);
            compressedStream = compressionType switch
            {
                CompressionType.SevenZip => SevenZipCompress(contentStream, sevenZipCompressionLevel),
                CompressionType.Zip => ZipCompress(contentStream, zipCompressionLevel),
                _ => throw new ArgumentOutOfRangeException(nameof(compressionType), compressionType, null)
            };
        }

        private static void GenerateCRCStream(IEnumerable<OMODCreationFile> files, out MemoryStream crcStream,
            out MemoryStream contentStream)
        {
            crcStream = new MemoryStream();
            contentStream = new MemoryStream();
            using var bw = new BinaryWriter(crcStream, Encoding.UTF8, true);

            foreach (var file in files)
            {
                var fi = new FileInfo(file.From);

                var bytes = File.ReadAllBytes(file.From);
                
                bw.Write(file.To);
                bw.Write(Crc32Algorithm.Compute(bytes, 0, bytes.Length));
                bw.Write(fi.Length);

                contentStream.Write(bytes, 0, bytes.Length);
            }

            crcStream.Position = 0;
            contentStream.Position = 0;
        }
        
        #region SevenZip
        
        internal static MemoryStream SevenZipCompress(Stream decompressedStream, SevenZipCompressionLevel sevenZipCompressionLevel)
        {
            //256 starting capacity so we can write the LZMA properties
            var ms = new MemoryStream(256);
            var dictionarySize = (int) sevenZipCompressionLevel;
            
            //explicit using-scope so we can readjust the position of the output stream before returning.
            //the dispose function of the LzmaStream changes the output stream and we don't want to call it after changing
            //the stream position and returning
            using (var lzmaStream = new LzmaStream(new LzmaEncoderProperties(false, dictionarySize), false, ms))
            {
                //the LzmaStream does not write the properties to the output stream and only writes the compressed data
                //meaning we have to manually write the properties to the output streams so it can be decompressed later
                ms.Write(lzmaStream.Properties, 0, lzmaStream.Properties.Length);
                decompressedStream.CopyTo(lzmaStream);
            }
            
            ms.Position = 0;
            return ms;
        }
        
        internal static MemoryStream SevenZipDecompress(Stream compressedStream, long outputSize)
        {
            //manually read the LZMA properties
            var props = new byte[5];
            compressedStream.Read(props, 0, 5);
            
            var ms = new MemoryStream((int) outputSize);

            using (var lzmaStream = new LzmaStream(props, compressedStream, -1, outputSize))
            {
                lzmaStream.CopyTo(ms);
            }
            
            ms.Position = 0;
            return ms;
        }
        
        #endregion

        #region Zip

        internal static MemoryStream ZipCompress(Stream decompressedStream, CompressionLevel compressionLevel)
        {
            var ms = new MemoryStream();

            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true, Encoding.UTF8))
            {
                //yes, we create an entry called "a". OBMM does this and I don't know why
                var entry = archive.CreateEntry("a", compressionLevel);
                using var entryStream = entry.Open();
                decompressedStream.CopyTo(entryStream);
            }

            ms.Position = 0;
            return ms;
        }
        
        internal static MemoryStream ZipDecompress(Stream compressedStream, long outputSize)
        {
            var ms = new MemoryStream((int) outputSize);

            using (var archive = new ZipArchive(compressedStream, ZipArchiveMode.Read, true, Encoding.UTF8))
            {
                var entry = archive.GetEntry("a");
                if (entry == null)
                    throw new OMODException("Unable to find main zip entry trying to decompress zip archive!");
                
                var entryStream = entry.Open();
                entryStream.CopyTo(ms);
            }

            ms.Position = 0;
            return ms;
        }

        #endregion
    }
}
