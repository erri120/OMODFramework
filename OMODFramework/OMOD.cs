using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Force.Crc32;
using JetBrains.Annotations;
using OMODFramework.Compression;

namespace OMODFramework
{
    [PublicAPI]
    public sealed class OMOD : IDisposable
    {
        private ZipArchive _zipArchive;

        public OMODConfig Config;

        private HashSet<OMODCompressedFile>? _dataFiles;
        private HashSet<OMODCompressedFile>? _pluginFiles;
        
        /// <summary>
        /// Create new OMOD instance from File.
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="ArgumentException"></exception>
        public OMOD(string path)
        {
            if (!File.Exists(path))
                throw new ArgumentException($"File at {path} does not exist!", nameof(path));

            _zipArchive = ZipFile.Open(path, ZipArchiveMode.Read, Encoding.UTF8);

            OMODArchiveValidation.ValidateArchive(_zipArchive);

            using var configStream = GetEntryFileStream(OMODEntryFileType.Config);
            Config = OMODConfig.ParseConfig(configStream);
        }

        /// <summary>
        /// Create new OMOD instance from Stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="leaveOpen"></param>
        /// <exception cref="ArgumentException"></exception>
        public OMOD(Stream stream, bool leaveOpen = false)
        {
            if (!stream.CanRead)
                throw new ArgumentException("Stream does not support reading!", nameof(stream));
            if (!stream.CanSeek)
                throw new ArgumentException("Stream does not support seeking!", nameof(stream));

            _zipArchive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen, Encoding.UTF8);
            
            OMODArchiveValidation.ValidateArchive(_zipArchive);

            using var configStream = GetEntryFileStream(OMODEntryFileType.Config);
            Config = OMODConfig.ParseConfig(configStream);
        }

        /// <summary>
        /// Determines if the <see cref="OMODEntryFileType"/> is present in the archive.
        /// </summary>
        /// <param name="entryFileType"></param>
        /// <returns></returns>
        public bool HasEntryFile(OMODEntryFileType entryFileType)
        {
            var entry = _zipArchive.GetEntry(entryFileType.ToFileString());
            return entry != null;
        }

        /// <summary>
        /// Returns the <see cref="ZipArchiveEntry"/> from the archive.
        /// </summary>
        /// <param name="entryFileType"></param>
        /// <returns></returns>
        /// <exception cref="OMODEntryNotFoundException"></exception>
        public ZipArchiveEntry GetArchiveEntry(OMODEntryFileType entryFileType)
        {
            var entry = _zipArchive.GetEntry(entryFileType.ToFileString());
            if (entry == null)
                throw new OMODEntryNotFoundException(entryFileType);

            return entry;
        }

        /// <summary>
        /// Tries to return the <see cref="ZipArchiveEntry"/> from the archive.
        /// </summary>
        /// <param name="entryFileType"></param>
        /// <param name="archiveEntry"></param>
        /// <returns></returns>
        public bool TryGetArchiveEntry(OMODEntryFileType entryFileType,
            [MaybeNullWhen(false)] out ZipArchiveEntry archiveEntry)
        {
            archiveEntry = _zipArchive.GetEntry(entryFileType.ToFileString());
            return archiveEntry != null;
        }
        
        /// <summary>
        /// Returns the compressed Stream of the entry.
        /// </summary>
        /// <param name="entryFileType"></param>
        /// <returns></returns>
        public Stream GetEntryFileStream(OMODEntryFileType entryFileType)
        {
            return GetArchiveEntry(entryFileType).Open();
        }

        /// <summary>
        /// Tries to return the compressed Stream of the entry.
        /// </summary>
        /// <param name="entryFileType"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public bool TryGetEntryFileStream(OMODEntryFileType entryFileType, [MaybeNullWhen(false)] out Stream stream)
        {
            stream = null;
            if (!TryGetArchiveEntry(entryFileType, out var archiveEntry))
                return false;
            stream = archiveEntry.Open();
            return true;
        }
        
        /// <summary>
        /// Returns the decompressed entry as a <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="entryFileType"></param>
        /// <returns></returns>
        public MemoryStream GetDecompressedEntryFileStream(OMODEntryFileType entryFileType)
        {
            var entry = GetArchiveEntry(entryFileType);
            using var stream = entry.Open();
            var ms = new MemoryStream((int) entry.Length);
            stream.CopyTo(ms);
            ms.Position = 0;
            return ms;
        }
        
        /// <summary>
        /// Extracts the entry to disk.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="entryFileType"></param>
        public void ExtractEntryFile(string path, OMODEntryFileType entryFileType)
        {
            using var stream = GetEntryFileStream(entryFileType);
            using var fs = File.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            stream.CopyTo(fs);
        }

        /// <summary>
        /// Extracts the entry to disk asynchronous.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="entryFileType"></param>
        /// <param name="cancellationToken"></param>
        public async Task ExtractEntryFileAsync(string path, OMODEntryFileType entryFileType, CancellationToken? cancellationToken = null)
        {
            var stream = GetEntryFileStream(entryFileType);
            await using (stream.ConfigureAwait(false))
            {
                var fs = File.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                await using (fs.ConfigureAwait(false))
                {
                    await stream.CopyToAsync(fs, cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                }
            }
        }

        private string GetStringFromEntryFile(OMODEntryFileType entryFileType)
        {
            using var stream = GetEntryFileStream(entryFileType);
            using var br = new BinaryReader(stream);
            return br.ReadString();
        }
        
        /// <summary>
        /// Extracts and returns the readme if present.
        /// </summary>
        /// <returns></returns>
        public string GetReadme()
        {
            return GetStringFromEntryFile(OMODEntryFileType.Readme);
        }

        /// <summary>
        /// Extracts and returns the script if present.
        /// </summary>
        /// <param name="removeType">Remove type byte at the start of the file</param>
        /// <returns></returns>
        public string GetScript(bool removeType = true)
        {
            var script = GetStringFromEntryFile(OMODEntryFileType.Script);
            if (!removeType) return script;

            var span = script.AsSpan();
            
            //script byte can only be 0 <= x <= 3, maybe throw if this is not the case
            if ((byte) span[0] < 4)
                span = span[1..];
            return span.ToString();
        }

        /// <summary>
        /// Extracts and returns the image if present. Remember to dispose of the image using <see cref="Image.Dispose"/>.
        /// </summary>
        /// <returns></returns>
        public Bitmap GetImage()
        {
            //stream has to be kept open for the lifetime of the image
            //see https://docs.microsoft.com/en-us/dotnet/api/system.drawing.image.fromstream#System_Drawing_Image_FromStream_System_IO_Stream_
            var stream = GetEntryFileStream(OMODEntryFileType.Image);
            var image = Image.FromStream(stream);
            return (Bitmap) image;
        }
        
        /// <summary>
        /// Returns the file info for all compressed files.
        /// </summary>
        /// <param name="data">read data.crc or plugins.crc</param>
        /// <returns></returns>
        public HashSet<OMODCompressedFile> GetFilesFromCRC(bool data)
        {
            switch (data)
            {
                case true when _dataFiles != null:
                    return _dataFiles;
                case false when _pluginFiles != null:
                    return _pluginFiles;
            }

            var entryFileType = data ? OMODEntryFileType.DataCRC : OMODEntryFileType.PluginsCRC;

            var result = new HashSet<OMODCompressedFile>();
            
            using var decompressedStream = GetDecompressedEntryFileStream(entryFileType);
            using var br = new BinaryReader(decompressedStream);

            var offset = 0L;
            
            while(br.PeekChar() != -1)
            {
                var name = br.ReadString();
                var crc = br.ReadUInt32();
                var length = br.ReadInt64();

                var file = new OMODCompressedFile(name, crc, length, offset);
                
                result.Add(file);
                offset += length;
            }

            if (data)
                _dataFiles = result;
            else
                _pluginFiles = result;
            
            return result;
        }

        /// <summary>
        /// Returns the file information for of all data files.
        /// </summary>
        /// <returns></returns>
        public HashSet<OMODCompressedFile> GetDataFiles()
        {
            return GetFilesFromCRC(true);
        }

        /// <summary>
        /// Returns the file information for all plugin files.
        /// </summary>
        /// <returns></returns>
        public HashSet<OMODCompressedFile> GetPluginFiles()
        {
            return GetFilesFromCRC(false);
        }

        #region Data Extraction
        
        private static void ExtractDecompressedFile(Stream decompressedStream, OMODCompressedFile compressedFile,
            string path, bool verify = false)
        {
            var outputPath = Path.Combine(path, compressedFile.Name);
            var directory = Path.GetDirectoryName(outputPath);
            if (directory == null)
                throw new DirectoryNotFoundException($"Unable to get directory name for path {outputPath}");
            Directory.CreateDirectory(directory);
            
            decompressedStream.Seek(compressedFile.Offset, SeekOrigin.Begin);
            
            //TODO: dynamic buffer size
            var buffer = new byte[compressedFile.Length];
            decompressedStream.Read(buffer, 0, buffer.Length);

            if (verify)
            {
                var crc = Crc32Algorithm.Compute(buffer, 0, buffer.Length);
                if (crc != compressedFile.CRC)
                    throw new OMODValidationException($"CRC does not match: {crc:X} != {compressedFile.CRC:X}");
            }
            
            using var fs = File.Open(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            fs.Write(buffer, 0, buffer.Length);
        }
        
        private class GenericSplitMemoryStream<T> : MemoryStream
        {
            public readonly Stack<T> Stack;

            public GenericSplitMemoryStream(MemoryStream parent) : base(parent.GetBuffer(), 0, (int) parent.Length,
                false)
            {
                Stack = new Stack<T>();
            }
        }
        
        private static List<GenericSplitMemoryStream<OMODCompressedFile>> CreateGenericSplitStreams(
            MemoryStream decompressedStream, List<OMODCompressedFile> files, byte numStreams)
        {
            /*
             * Stream splitting:
             *
             * We have one massive MemoryStream containing the entire decompressed data and want to create
             * x amount of "Sub-Streams/Split-Streams" so we can extract more than one file at a time.
             *
             * We first calculate how much data each Stream should handle (lengthEach) but since we are dealing
             * with files and don't want to cut a file in half, this value is not set in stone. We get the last
             * entry in our list where the offset is smaller than the length of our Stream.
             *
             * The GenericSplitMemoryStream has a Stack where we can push all items on there that
             * are within its range, meaning every file with an offset: usedLength <= offset <= LastEntry.Offset
             */
            
            var totalLength = decompressedStream.Length;
            var lengthEach = totalLength / numStreams;
            var usedLength = 0L;
            
            var streams = new List<GenericSplitMemoryStream<OMODCompressedFile>>(numStreams);

            for (var i = 0; i < numStreams; i++)
            {
                if (i == numStreams - 1)
                {
                    var stream = new GenericSplitMemoryStream<OMODCompressedFile>(decompressedStream);
                    var innerLength = usedLength;
                    foreach (var x in files.Where(x => x.Offset >= innerLength))
                    {
                        stream.Stack.Push(x);
                    }
                    streams.Add(stream);
                }
                else
                {
                    var length = lengthEach + usedLength;
                    var lastEntry = files.Last(x => x.Offset < length);
                    length = lastEntry.Offset - usedLength;
                    var stream = new GenericSplitMemoryStream<OMODCompressedFile>(decompressedStream);
                    var innerLength = usedLength;
                    //TODO: whack solution, find a better one
                    if (i == 0)
                        stream.Stack.Push(files.First());
                    //used to be x.Offset >= innerLength && x.Offset <= lastEntry.Offset but this resulted in multiple streams
                    //having the same file in the stack
                    foreach (var x in files.Where(x => x.Offset > innerLength && x.Offset <= lastEntry.Offset))
                    {
                        stream.Stack.Push(x);
                    }
                
                    streams.Add(stream);
                    usedLength += length;
                }
            }
            
            return streams;
        }
        
        /// <summary>
        /// Extracts the data files in parallel using split memory-streams to disk.
        /// </summary>
        /// <param name="path">Output directory</param>
        /// <param name="numStreams">Number of streams to create</param>
        /// <param name="degreeOfParallelism">see <see cref="ParallelEnumerable.WithDegreeOfParallelism{TSource}"/></param>
        /// <param name="verify">to verify the decompressed bytes using CRC32</param>
        /// <param name="cancellationToken">Cancellation token for <see cref="ParallelEnumerable.WithCancellation{TSource}"/></param>
        /// <exception cref="ArgumentException"></exception>
        public void ExtractFilesParallel(string path, byte numStreams, int degreeOfParallelism = 0, bool verify = false, CancellationToken? cancellationToken = null)
        {
            switch (numStreams)
            {
                case 0:
                    throw new ArgumentException("Can't extract Files with 0 streams!");
                case 1:
                    ExtractFiles(true, path, verify);
                    return;
            }

            if (File.Exists(path))
                throw new ArgumentException("Path can not be a file!", nameof(path));
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            Directory.CreateDirectory(path);

            const OMODEntryFileType entryFileType = OMODEntryFileType.Data;
            var fileSet = GetDataFiles(); 

            using var compressedStream = GetEntryFileStream(entryFileType);
            var outputSize = fileSet.Select(x => x.Length).Aggregate((x, y) => x + y);
            using var decompressedStream = CompressionHandler.SevenZipDecompress(compressedStream, outputSize);

            var files = fileSet.ToList();
            files.Sort((a, b) => (int)(a.Offset - b.Offset));

            var streams = CreateGenericSplitStreams(decompressedStream, files, numStreams);
            var query = streams.AsParallel()
                .WithCancellation(cancellationToken ?? CancellationToken.None)
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism);

            if (degreeOfParallelism != 0)
                query = query.WithDegreeOfParallelism(degreeOfParallelism);

            query.ForAll(x =>
                {
                    while (x.Stack.TryPop(out var current))
                    {
                        ExtractDecompressedFile(x, current, path, verify);
                    }
                });

            foreach (var stream in streams)
            {
                stream.Dispose();
            }
        }

        /// <summary>
        /// Extracts all files to disk.
        /// </summary>
        /// <param name="data">extract data or plugin files</param>
        /// <param name="path">output folder</param>
        /// <param name="verify">to verify the decompressed bytes using CRC32</param>
        /// <exception cref="ArgumentException"></exception>
        public void ExtractFiles(bool data, string path, bool verify = false)
        {
            if (File.Exists(path))
                throw new ArgumentException("Path can not be a file!", nameof(path));
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            Directory.CreateDirectory(path);
            
            var entryFileType = data ? OMODEntryFileType.Data : OMODEntryFileType.Plugins;
            var files = data ? GetDataFiles() : GetPluginFiles();

            using var compressedStream = GetEntryFileStream(entryFileType);
            var outputSize = files.Select(x => x.Length).Aggregate((x, y) => x + y);
            using var decompressedStream = CompressionHandler.SevenZipDecompress(compressedStream, outputSize);
            
            foreach (var compressedFile in files)
            { 
                ExtractDecompressedFile(decompressedStream, compressedFile, path, verify);
            }
        }
        
        #endregion
        
        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Config.Name}";
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _zipArchive.Dispose();
        }
    }
}
