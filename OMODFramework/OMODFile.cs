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

            return CRC == entry.CRC;
        }

        public override int GetHashCode()
        {
            return (int)CRC;
        }

        public override string ToString()
        {
            return $"{Name} {Length} bytes ({CRC:x8})";
        }
    }

    internal class OMODFile : IDisposable
    {
        private readonly ZipFile _zipFile;
        private MemoryStream? _decompressedDataStream;
        private MemoryStream? _decompressedPluginStream;

        internal IEnumerable<OMODCompressedEntry>? DataList { get; private set; }
        internal IEnumerable<OMODCompressedEntry>? PluginsList { get; private set; }

        internal CompressionType CompressionType { get; set; }

        internal OMODFile(FileInfo path)
        {
            _zipFile = new ZipFile(path.OpenRead());
        }

        internal bool CheckIntegrity()
        {
            return _zipFile.CheckIntegrity();
        }

        internal void Decompress(OMODEntryFileType entryFileType)
        {
            if (entryFileType == OMODEntryFileType.Data)
            {
                DataList ??= GetCRCList(true);

                _decompressedDataStream ??=
                    (MemoryStream)CompressionHandler.DecompressStream(DataList, ExtractFile(OMODEntryFileType.Data), CompressionType);
            }
            else
            {
                PluginsList ??= GetCRCList(false);

                _decompressedPluginStream ??=
                    (MemoryStream)CompressionHandler.DecompressStream(PluginsList, ExtractFile(OMODEntryFileType.Plugins), CompressionType);
            }
        }

        internal void ExtractAllDecompressedFiles(DirectoryInfo output, bool data)
        {
            var decompressedStream = data ? _decompressedDataStream : _decompressedPluginStream;
            IEnumerable<OMODCompressedEntry>? enumerable = data ? DataList : PluginsList;

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

        internal IEnumerable<OMODCompressedEntry> GetDataFileList()
        {
            if (DataList != null)
                return DataList;

            DataList ??= GetCRCList(true);
            return DataList;
        }

        internal IEnumerable<OMODCompressedEntry> GetPlugins()
        {
            if (PluginsList != null)
                return PluginsList;

            PluginsList ??= GetCRCList(false);
            return PluginsList;
        }

        private IEnumerable<OMODCompressedEntry> GetCRCList(bool data)
        {
            var entry = data ? OMODEntryFileType.DataCRC : OMODEntryFileType.PluginsCRC;

            using var stream = ExtractFile(entry);
            using var br = new BinaryReader(stream);

            var list = new List<OMODCompressedEntry>();
            long offset = 0;
            while (br.PeekChar() != -1)
            {
                var name = br.ReadString();
                var crc = br.ReadUInt32();
                var length = br.ReadInt64();
                list.Add(new OMODCompressedEntry(name, crc, length) { Offset = offset });
                offset += length;
            }

            return list;
        }

        public void Dispose()
        {
            _decompressedDataStream?.Dispose();
            _decompressedPluginStream?.Dispose();
            _zipFile.Close();
        }
    }
}
