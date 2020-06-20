#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using JetBrains.Annotations;
using OMODFramework.Exceptions;

namespace OMODFramework
{
    /// <summary>
    /// Enum for all possible files in an omod file
    /// </summary>
    [PublicAPI]
    public enum OMODEntryFileType : byte
    {
        /// <summary>
        /// File containing information about the data files
        /// </summary>
        DataCRC,
        /// <summary>
        /// Raw compressed data
        /// </summary>
        Data,
        /// <summary>
        /// File containing information about the plugins
        /// </summary>
        PluginsCRC,
        /// <summary>
        /// Raw compressed plugins (optional)
        /// </summary>
        Plugins,
        /// <summary>
        /// Config file (optional)
        /// </summary>
        Config,
        /// <summary>
        /// Readme file
        /// </summary>
        Readme,
        /// <summary>
        /// Script file (optional)
        /// </summary>
        Script,
        /// <summary>
        /// Image file (optional)
        /// </summary>
        Image
    }

    /// <summary>
    /// Files inside .crc entries
    /// </summary>
    [PublicAPI]
    public class OMODCompressedEntry
    {
        /// <summary>
        /// Path and Name of the file
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// CRC32 of the file
        /// </summary>
        public readonly uint CRC;

        /// <summary>
        /// Length (in bytes) of the file
        /// </summary>
        public readonly long Length;

        /// <summary>
        /// Offset in the decompressed data
        /// </summary>
        internal long Offset { get; set; }

        public OMODCompressedEntry(string name, uint crc, long length)
        {
            Name = name;
            CRC = crc;
            Length = length;
        }

        internal string GetFullPath(DirectoryInfo directory)
        {
            return Path.Combine(directory.FullName, Name);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is OMODCompressedEntry entry))
                return false;

            return CRC == entry.CRC && Path.GetFullPath(Name).Equals(Path.GetFullPath(entry.Name), StringComparison.InvariantCultureIgnoreCase);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode(StringComparison.InvariantCultureIgnoreCase);
        }

        public override string ToString()
        {
            return $"{Name} {Length} bytes ({CRC:x8})";
        }
    }

    internal class OMODCompressedEntryComparer : IComparer<OMODCompressedEntry>
    {
        public int Compare(OMODCompressedEntry x, OMODCompressedEntry y)
        {
            return (int) (x.Offset - y.Offset);
        }
    }

    internal class SplitMemoryStream : MemoryStream
    {
        internal readonly int StartIndex;
        internal readonly int EndIndex;

        internal SplitMemoryStream(byte[] buffer, int index, int count, int startIndex, int endIndex) :
            base(buffer, index, count, false)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
        }
    }

    internal class OMODFile : IDisposable
    {
        private readonly ZipFile _zipFile;
        private readonly FrameworkSettings _settings;
        private MemoryStream? _decompressedDataStream;
        private MemoryStream? _decompressedPluginStream;

        private HashSet<OMODCompressedEntry>? _dataFiles;
        internal HashSet<OMODCompressedEntry> DataFiles
        {
            get
            {
                _dataFiles ??= GetCRCSet(true)!;
                if(_dataFiles == null)
                    throw new OMODException("DataFiles were requested but DataFiles are null!");
                return _dataFiles;
            }
        }

        private HashSet<OMODCompressedEntry>? _plugins;
        internal HashSet<OMODCompressedEntry> Plugins
        {
            get
            {
                _plugins ??= GetCRCSet(false);
                if(_plugins == null)
                    throw new OMODException("Plugins were requested but Plugins are null!");
                return _plugins;
            }
        }

        internal CompressionType CompressionType { get; set; }

        internal OMODFile(FileInfo path, FrameworkSettings? settings = null)
        {
            _zipFile = new ZipFile(path.OpenRead());
            _settings = settings ?? FrameworkSettings.DefaultFrameworkSettings;
        }

        internal bool CheckIntegrity()
        {
            return _zipFile.CheckIntegrity();
        }

        internal void Decompress(OMODEntryFileType entryFileType)
        {
            if (entryFileType == OMODEntryFileType.Data)
            {
                _decompressedDataStream ??= (MemoryStream)CompressionHandler.DecompressStream(DataFiles, ExtractEntryFile(OMODEntryFileType.Data), CompressionType, _settings.CodeProgress);
            }
            else if (entryFileType == OMODEntryFileType.Plugins)
            {
                _decompressedPluginStream ??= (MemoryStream)CompressionHandler.DecompressStream(Plugins, ExtractEntryFile(OMODEntryFileType.Plugins), CompressionType, _settings.CodeProgress);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(entryFileType));
            }
        }

        internal async Task ExtractAllDecompressedFilesAsync(DirectoryInfo output, bool data, int threads = 4)
        {
            var decompressedStream = data ? _decompressedDataStream : _decompressedPluginStream;
            IEnumerable<OMODCompressedEntry>? enumerable = data ? DataFiles : Plugins;

            if (decompressedStream == null)
                throw new Exception($"Decompressed Stream ({(data ? "data" : "plugins")}) is null!");
            if (enumerable == null)
                throw new Exception($"Enumerable for ({(data ? "data" : "plugins")}) is null!");

            var comparer = new OMODCompressedEntryComparer();
            var files = enumerable.ToList();
            files.Sort(comparer);


            var totalLength = decompressedStream.Length;
            var lengthEach = totalLength / threads;
            long usedLength = 0;
            var lastIndex = 0;

            var streams = new List<SplitMemoryStream>(threads);
            for (var i = 0; i < threads; i++)
            {
                if (i == threads - 1)
                {
                    var length = totalLength - usedLength;
                    var stream = new SplitMemoryStream(decompressedStream.GetBuffer(), (int) usedLength, (int) length, lastIndex, files.Count - 1);
                    streams.Add(stream);
                }
                else
                {
                    var length = lengthEach + usedLength;
                    //get the last entry in the enumerable whose offset is smaller than the length
                    //so we dont end up cutting files in half
                    var lastEntry = files.Last(x => x.Offset < length);
                    var endIndex = files.IndexOf(lastEntry);
                    length = lastEntry.Offset - usedLength;
                    var stream = new SplitMemoryStream(decompressedStream.GetBuffer(), (int) usedLength, (int) length,
                        lastIndex, endIndex);
                    streams.Add(stream);

                    lastIndex = endIndex + 1;
                    usedLength += length;
                }
            }

            var streamsLength = streams.Select(x => x.Length).Aggregate((x, y) => x + y);
            if(totalLength != streamsLength)
                throw new OMODException($"Stream creation failed, length of all streams does not equal length of all files: {streamsLength} != {totalLength}");

            await Task.WhenAll(streams.Select(x => Task.Run(() =>
            {
                for (var i = x.StartIndex; i <= x.EndIndex; i++)
                {
                    var current = files.ElementAtOrDefault(i);
                    if(current == null)
                        throw new OMODException($"Unable to get OMODCompressedEntry at position {i}");
                    ExtractDecompressedFile(x, current, output);
                }
            })));

            streams.Do(x => x.Dispose());
        }

        internal void ExtractAllDecompressedFiles(DirectoryInfo output, bool data)
        {
            var decompressedStream = data ? _decompressedDataStream : _decompressedPluginStream;
            HashSet<OMODCompressedEntry>? enumerable = data ? DataFiles : Plugins;

            if (decompressedStream == null)
                throw new Exception($"Decompressed Stream ({(data ? "data" : "plugins")}) is null!");
            if (enumerable == null)
                throw new Exception($"Enumerable for ({(data ? "data" : "plugins")}) is null!");

            foreach (var current in enumerable)
            {
                ExtractDecompressedFile(decompressedStream, current, output);
            }
        }

        private static void ExtractDecompressedFile(MemoryStream stream, OMODCompressedEntry entry, DirectoryInfo output)
        {
            stream.Seek(entry.Offset, SeekOrigin.Begin);

            var file = new FileInfo(entry.GetFullPath(output));
            if (file.Directory == null)
                throw new NullReferenceException("Directory is null!");
            if (!file.Directory.Exists)
                file.Directory.Create();

            if (file.Exists)
            {
                if (file.Length == entry.Length)
                    return;
                file.Delete();
            }

            using var fileStream = file.Create();

            byte[] buffer = new byte[entry.Length];
            stream.Read(buffer, 0, (int)entry.Length);

            fileStream.Write(buffer, 0, (int)entry.Length);
        }

        internal Stream ExtractDecompressedFile(OMODCompressedEntry entry, bool data = true)
        {
            var decompressedStream = data ? _decompressedDataStream : _decompressedPluginStream;

            if (decompressedStream == null)
                throw new Exception($"Decompressed Stream ({(data ? "data" : "plugins")}) is null!");

            decompressedStream.Seek(entry.Offset, SeekOrigin.Begin);
            byte[] buffer = new byte[entry.Length];

            decompressedStream.Read(buffer, 0, (int)entry.Length);
            var stream = new MemoryStream(buffer, 0, (int)entry.Length, false);
            return stream;
        }

        internal FileStream ExtractDecompressedFile(OMODCompressedEntry entry, FileInfo output, bool data = true)
        {
            var decompressedStream = data ? _decompressedDataStream : _decompressedPluginStream;

            if (decompressedStream == null)
                throw new Exception($"Decompressed Stream ({(data ? "data" : "plugins")}) is null!");

            decompressedStream.Seek(entry.Offset, SeekOrigin.Begin);

            var fs = File.Create(output.FullName, (int) entry.Length);
            byte[] buffer = new byte[entry.Length];

            decompressedStream.Read(buffer, 0, (int)entry.Length);
            fs.Write(buffer, 0, (int)entry.Length);

            return fs;
        }

        internal bool HasEntryFile(OMODEntryFileType entryFileType) => _zipFile.HasFile(entryFileType.ToFileString());

        internal Stream ExtractEntryFile(OMODEntryFileType entryFileType)
        {
            return _zipFile.ExtractFile(entryFileType.ToFileString());
        }

        internal Config ReadConfig()
        {
            return Config.ParseConfig(ExtractEntryFile(OMODEntryFileType.Config));
        }

        private HashSet<OMODCompressedEntry>? GetCRCSet(bool data)
        {
            var entry = data ? OMODEntryFileType.DataCRC : OMODEntryFileType.PluginsCRC;

            if (!HasEntryFile(entry))
                return null;

            using var stream = ExtractEntryFile(entry);
            using var br = new BinaryReader(stream);

            var set = new HashSet<OMODCompressedEntry>();
            long offset = 0;
            while (br.PeekChar() != -1)
            {
                var name = br.ReadString();
                var crc = br.ReadUInt32();
                var length = br.ReadInt64();
                var res = set.Add(new OMODCompressedEntry(name, crc, length) { Offset = offset });
                //TODO: check res
                offset += length;
            }

            return set;
        }

        public void Dispose()
        {
            _decompressedDataStream?.Dispose();
            _decompressedPluginStream?.Dispose();
            _zipFile.Close();
        }
    }
}
