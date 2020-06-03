#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
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

    internal static partial class Utils
    {
        internal static string ToFileString(this OMODEntryFileType entryFileType)
        {
            return entryFileType switch
            {
                OMODEntryFileType.DataCRC => "data.crc",
                OMODEntryFileType.PluginsCRC => "plugins.crc",
                OMODEntryFileType.Config => "config",
                OMODEntryFileType.Readme => "readme",
                OMODEntryFileType.Script => "script",
                OMODEntryFileType.Image => "image",
                OMODEntryFileType.Data => "data",
                OMODEntryFileType.Plugins => "plugins",
                _ => throw new ArgumentOutOfRangeException(nameof(entryFileType), entryFileType, "Should not be possible!")
            };
        }
    }

    [PublicAPI]
    public class Config
    {
        public string Name { get; set; } = string.Empty;
        private int _majorVersion, _minorVersion, _buildVersion;
        public Version Version => new Version(_majorVersion, _minorVersion, _buildVersion);
        public string Description { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;

        public DateTime CreationTime { get; set; }
        public byte FileVersion { get; set; }
        public CompressionType CompressionType { get; set; }

        internal static Config ParseConfig(Stream stream)
        {
            var config = new Config();
            using var br = new BinaryReader(stream);

            config.FileVersion = br.ReadByte();
            config.Name = br.ReadString();
            config._majorVersion = br.ReadInt32();
            config._minorVersion = br.ReadInt32();
            config.Author = br.ReadString();
            config.Email = br.ReadString();
            config.Website = br.ReadString();
            config.Description = br.ReadString();

            if (config.FileVersion >= 2)
                config.CreationTime = DateTime.FromBinary(br.ReadInt64());
            else
            {
                var sCreationTime = br.ReadString();
                config.CreationTime = !DateTime.TryParseExact(sCreationTime, "dd/MM/yyyy HH:mm", null, DateTimeStyles.None,
                    out var creationTime) ? new DateTime(2006, 1, 1) : creationTime;
            }

            config.CompressionType = (CompressionType) br.ReadByte();
            if (config.FileVersion >= 1)
                config._buildVersion = br.ReadInt32();

            if (config._majorVersion < 0)
                config._majorVersion = 0;
            if (config._minorVersion < 0)
                config._minorVersion = 0;
            if (config._buildVersion < 0)
                config._buildVersion = 0;

            return config;
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
                    (MemoryStream) CompressionHandler.DecompressStream(DataList, ExtractFile(OMODEntryFileType.Data), CompressionType);
            }
            else
            {
                PluginsList ??= GetCRCList(false);

                _decompressedPluginStream ??=
                    (MemoryStream) CompressionHandler.DecompressStream(PluginsList, ExtractFile(OMODEntryFileType.Plugins), CompressionType);
            }
        }

        internal void ExtractAllDecompressedFiles(DirectoryInfo output, bool data)
        {
            var decompressedStream = data ? _decompressedDataStream : _decompressedPluginStream;
            IEnumerable<OMODCompressedEntry>? enumerable = data ? DataList : PluginsList;

            if (decompressedStream == null)
                throw new NotImplementedException();
            if (enumerable == null)
                throw new NotImplementedException();

            var list = enumerable.ToList();

            foreach (var current in list)
            {
                decompressedStream.Seek(current.Offset, SeekOrigin.Begin);

                var file = new FileInfo(current.GetFullPath(output));
                if(file.Directory == null)
                    throw new NullReferenceException("Directory is null!");
                if(!file.Directory.Exists)
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
                throw new NotImplementedException();

            decompressedStream.Seek(entry.Offset, SeekOrigin.Begin);
            byte[] buffer = new byte[entry.Length];

            decompressedStream.Read(buffer, 0, (int)entry.Length);
            var stream = new MemoryStream(buffer, 0, (int)entry.Length, false);
            return stream;
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
                list.Add(new OMODCompressedEntry(name, crc, length){Offset = offset});
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

    [PublicAPI]
    public class OMOD : IDisposable
    {
        private readonly FrameworkSettings _frameworkSettings = null!;
        internal readonly OMODFile OMODFile;

        public readonly Config Config = null!;

        public OMOD(FileInfo path, FrameworkSettings? settings = null, bool checkIntegrity = true)
        {
            if (!path.Exists)
                throw new ArgumentException($"File at {path} does not exists!", nameof(path));

            if (settings == null)
                _frameworkSettings = FrameworkSettings.DefaultFrameworkSettings;

            OMODFile = new OMODFile(path);

            if (checkIntegrity)
            {
                if (!OMODFile.CheckIntegrity())
                    return;
            }

            Config = OMODFile.ReadConfig();

            if(Config.FileVersion > _frameworkSettings.CurrentOMODVersion)
                throw new OMODInvalidConfigException(Config, $"The file version in the config: {Config.FileVersion} is greater than the set OMOD version in the Framework Settings: {_frameworkSettings.CurrentOMODVersion}!");

            OMODFile.CompressionType = Config.CompressionType;
        }

        /// <summary>
        /// Checks if the OMOD contains the given file
        /// </summary>
        /// <param name="entryFileType">The file</param>
        /// <returns></returns>
        public bool HasFile(OMODEntryFileType entryFileType) => OMODFile.HasFile(entryFileType);

        /// <summary>
        /// Extract the given file from the OMOD and returns a <see cref="Stream"/> with the data
        /// instead of writing to file.
        /// </summary>
        /// <param name="entryFileType">The file to extract</param>
        /// <returns></returns>
        /// <exception cref="ZipFileEntryNotFoundException"></exception>
        public Stream ExtractFile(OMODEntryFileType entryFileType)
        {
            return OMODFile.ExtractFile(entryFileType);
        }

        private string ExtractStringFile(OMODEntryFileType entryFileType)
        {
            using var stream = ExtractFile(entryFileType);
            using var br = new BinaryReader(stream);
            return br.ReadString();
        }

        /// <summary>
        /// Returns the readme of the OMOD
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ZipFileEntryNotFoundException"></exception>
        public string ExtractReadme()
        {
            return ExtractStringFile(OMODEntryFileType.Readme);
        }

        /// <summary>
        /// Returns the script of the OMOD
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ZipFileEntryNotFoundException"></exception>
        public string ExtractScript()
        {
            return ExtractStringFile(OMODEntryFileType.Script);
        }

        /// <summary>
        /// Extracts the Image in the OMOD
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ZipFileEntryNotFoundException"></exception>
        public Bitmap ExtractImage()
        {
            using var stream = ExtractFile(OMODEntryFileType.Image);
            var image = Image.FromStream(stream);
            return (Bitmap) image;
        }

        /// <summary>
        /// Returns an enumerable of all data files.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<OMODCompressedEntry> GetDataFileList()
        {
            return OMODFile.GetDataFileList();
        }

        /// <summary>
        /// Returns an enumerable for all plugins. Use <see cref="HasFile"/> beforehand
        /// so you don't get a <see cref="ZipFileEntryNotFoundException"/>!
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ZipFileEntryNotFoundException"></exception>
        public IEnumerable<OMODCompressedEntry> GetPlugins()
        {
            return OMODFile.GetPlugins();

        }
        
        /// <summary>
        /// Extracts all plugins to a given directory.
        /// </summary>
        /// <param name="outputDirectory"></param>
        public void ExtractPluginFiles(DirectoryInfo outputDirectory)
        {
            ExtractCompressedData(OMODEntryFileType.Data, outputDirectory);
        }

        /// <summary>
        /// Extracts all data files to a given directory.
        /// </summary>
        /// <param name="outputDirectory"></param>
        public void ExtractDataFiles(DirectoryInfo outputDirectory)
        {
            ExtractCompressedData(OMODEntryFileType.Data, outputDirectory);
        }

        private void ExtractCompressedData(OMODEntryFileType entryFileType, DirectoryInfo outputDirectory)
        {
            if(entryFileType != OMODEntryFileType.Data && entryFileType != OMODEntryFileType.Plugins)
                throw new ArgumentException($"Provided OMODFile can only be Data or Plugins but is {entryFileType}!", nameof(entryFileType));

            if(!outputDirectory.Exists)
                outputDirectory.Create();

            OMODFile.Decompress(entryFileType);

            OMODFile.ExtractAllDecompressedFiles(outputDirectory, entryFileType == OMODEntryFileType.Data);
        }

        /// <summary>
        /// Disposes the OMOD and closes the underlying zip file.
        /// </summary>
        public void Dispose()
        {
            OMODFile.Dispose();
        }
    }
}
