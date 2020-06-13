#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;
using JetBrains.Annotations;

namespace OMODFramework
{
    /// <summary>
    /// Enum for all possible files in an omod file
    /// </summary>
    [PublicAPI]
    public enum OMODEntryFileType : byte
    {
        DataCRC,
        Data,
        PluginsCRC,
        Plugins,
        Config,
        Readme,
        Script,
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

    internal class OMODFile : IDisposable
    {
        private readonly ZipFile _zipFile;
        private readonly FrameworkSettings _settings;
        private MemoryStream? _decompressedDataStream;
        private MemoryStream? _decompressedPluginStream;

        internal HashSet<OMODCompressedEntry>? DataFiles { get; private set; }
        internal HashSet<OMODCompressedEntry>? Plugins { get; private set; }

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
                DataFiles ??= GetCRCSet(true);

                _decompressedDataStream ??=
                    (MemoryStream)CompressionHandler.DecompressStream(DataFiles, ExtractFile(OMODEntryFileType.Data), CompressionType, _settings.CodeProgress);
            }
            else
            {
                Plugins ??= GetCRCSet(false);

                _decompressedPluginStream ??=
                    (MemoryStream)CompressionHandler.DecompressStream(Plugins, ExtractFile(OMODEntryFileType.Plugins), CompressionType, _settings.CodeProgress);
            }
        }

        internal void ExtractAllDecompressedFiles(DirectoryInfo output, bool data)
        {
            var decompressedStream = data ? _decompressedDataStream : _decompressedPluginStream;
            IEnumerable<OMODCompressedEntry>? enumerable = data ? DataFiles : Plugins;

            if (decompressedStream == null)
                throw new Exception($"Decompressed Stream ({(data ? "data" : "plugins")}) is null!");
            if (enumerable == null)
                throw new Exception($"Enumerable for ({(data ? "data" : "plugins")}) is null!");

            var list = enumerable.ToList();

            foreach (var current in list)
            {
                decompressedStream.Seek(current.Offset, SeekOrigin.Begin);

                var file = new FileInfo(current.GetFullPath(output));
                if (file.Directory == null)
                    throw new NullReferenceException("Directory is null!");
                if (!file.Directory.Exists)
                    file.Directory.Create();

                if (file.Exists)
                {
                    if (file.Length == current.Length)
                        return;
                    file.Delete();
                }

                using var fileStream = file.Create();

                byte[] buffer = new byte[current.Length];
                decompressedStream.Read(buffer, 0, (int)current.Length);

                fileStream.Write(buffer, 0, (int)current.Length);
            }
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

        internal bool HasFile(OMODEntryFileType entryFileType) => _zipFile.HasFile(entryFileType.ToFileString());

        internal Stream ExtractFile(OMODEntryFileType entryFileType)
        {
            return _zipFile.ExtractFile(entryFileType.ToFileString());
        }

        internal Config ReadConfig()
        {
            return Config.ParseConfig(ExtractFile(OMODEntryFileType.Config));
        }

        internal IEnumerable<OMODCompressedEntry> GetDataFiles()
        {
            if (DataFiles != null)
                return DataFiles;

            DataFiles ??= GetCRCSet(true);
            return DataFiles;
        }

        internal IEnumerable<OMODCompressedEntry> GetPlugins()
        {
            if (Plugins != null)
                return Plugins;

            Plugins ??= GetCRCSet(false);
            return Plugins;
        }

        private HashSet<OMODCompressedEntry> GetCRCSet(bool data)
        {
            var entry = data ? OMODEntryFileType.DataCRC : OMODEntryFileType.PluginsCRC;

            using var stream = ExtractFile(entry);
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
