#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using OMODFramework.Exceptions;

namespace OMODFramework
{
    /// <summary>
    /// Config containing general information like Name, Author and Description about
    /// an OMOD
    /// </summary>
    [PublicAPI]
    public class Config
    {
        /// <summary>
        /// Name of the OMOD
        /// </summary>
        public string Name { get; set; } = string.Empty;
        private int _majorVersion, _minorVersion, _buildVersion;
        /// <summary>
        /// Version of the OMOD
        /// </summary>
        public Version Version => new Version(_majorVersion, _minorVersion, _buildVersion);
        /// <summary>
        /// Description of the OMOD, can be empty
        /// </summary>
        public string Description { get; set; } = string.Empty;
        /// <summary>
        /// Email of the author, can be empty
        /// </summary>
        public string Email { get; set; } = string.Empty;
        /// <summary>
        /// Website of the OMOD, can be empty
        /// </summary>
        public string Website { get; set; } = string.Empty;
        /// <summary>
        /// Author of the OMOD, can be empty
        /// </summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// <see cref="DateTime"/> of the creation
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// File Version which is checked against <see cref="FrameworkSettings.CurrentOMODVersion"/>
        /// </summary>
        public byte FileVersion { get; set; }

        /// <summary>
        /// <see cref="CompressionType"/> of the OMOD
        /// </summary>
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

    /// <summary>
    /// OMOD class, implements <see cref="IDisposable"/>
    /// </summary>
    [PublicAPI]
    public partial class OMOD : IDisposable
    {
        private readonly FrameworkSettings _frameworkSettings = null!;
        internal readonly OMODFile OMODFile;

        /// <summary>
        /// <see cref="Config"/> of the OMOD
        /// </summary>
        public readonly Config Config;

        /// <summary>
        /// Loads the OMOD file and reads the config.
        /// </summary>
        /// <param name="path">Path to the .omod file</param>
        /// <param name="settings">Optional, custom <see cref="FrameworkSettings"/>. Default is <see cref="FrameworkSettings.DefaultFrameworkSettings"/></param>
        /// <param name="checkIntegrity">Optional, whether to check verify the integrity of the .omod file. Default is <c>true</c></param>
        public OMOD(FileInfo path, FrameworkSettings? settings = null, bool checkIntegrity = true)
        {
            if (!path.Exists)
                throw new ArgumentException($"File at {path} does not exists!", nameof(path));

            if (settings == null)
                _frameworkSettings = FrameworkSettings.DefaultFrameworkSettings;

            Utils.Info($"Opening OMOD from {path.FullName}");

            OMODFile = new OMODFile(path, _frameworkSettings);

            if (checkIntegrity)
            {
                Utils.Debug("Verifying integrity of the OMOD.");
                if (!OMODFile.CheckIntegrity())
                    throw new OMODException("OMOD failed the integrity check!");
            }

            Utils.Debug("Reading Config from OMOD.");
            Config = OMODFile.ReadConfig();

            if(Config.FileVersion > _frameworkSettings.CurrentOMODVersion)
                throw new OMODInvalidConfigException(Config, $"The file version in the config: {Config.FileVersion} is greater than the set OMOD version in the Framework Settings: {_frameworkSettings.CurrentOMODVersion}!");

            OMODFile.CompressionType = Config.CompressionType;

            Utils.Info($"Successfully loaded OMOD {Config.Name}");
        }

        /// <summary>
        /// Checks if the OMOD contains the given <see cref="OMODEntryFileType"/>
        /// </summary>
        /// <param name="entryFileType">The file</param>
        /// <returns></returns>
        public bool HasFile(OMODEntryFileType entryFileType) => OMODFile.HasEntryFile(entryFileType);

        /// <summary>
        /// Extract the given file from the OMOD and returns a <see cref="Stream"/> with the data
        /// instead of writing to file.
        /// </summary>
        /// <param name="entryFileType">The file to extract</param>
        /// <returns></returns>
        /// <exception cref="ZipFileEntryNotFoundException"></exception>
        public Stream ExtractFile(OMODEntryFileType entryFileType)
        {
            return OMODFile.ExtractEntryFile(entryFileType);
        }

        /// <summary>
        /// Extracts the given file from the OMOD to a specified output location.
        /// If the output already exists but has different lengths than the file
        /// to be extracted, it will be deleted. The directory will also be created
        /// for you.
        /// </summary>
        /// <param name="entryFileType">File to extract</param>
        /// <param name="output">Output location</param>
        public void ExtractFile(OMODEntryFileType entryFileType, FileInfo output)
        {
            using var stream = ExtractFile(entryFileType);
            if(output.Directory != null && !output.Directory.Exists)
                output.Directory.Create();
            
            if (output.Exists)
            {
                if (output.Length == stream.Length)
                    return;
                output.Delete();
            }

            using var fs = File.Create(output.FullName);
            stream.CopyTo(fs);
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
        public string GetReadme()
        {
            return ExtractStringFile(OMODEntryFileType.Readme);
        }

        /// <summary>
        /// Returns the script of the OMOD
        /// </summary>
        /// <param name="removeType">Whether to remove the script type identifier from the script.
        /// This identifier is one byte at the start of the script.</param>
        /// <returns></returns>
        /// <exception cref="ZipFileEntryNotFoundException"></exception>
        public string GetScript(bool removeType = true)
        {
            var script = ExtractStringFile(OMODEntryFileType.Script);

            if (!removeType)
                return script;

            if ((byte) script[0] < 4)
                script = script.Substring(1);

            return script;
        }

        /// <summary>
        /// Extracts the Image in the OMOD and returns a <see cref="Bitmap"/>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ZipFileEntryNotFoundException"></exception>
        public Bitmap GetImage()
        {
            //stream has be kept open for the lifetime of the image
            //see https://docs.microsoft.com/en-us/dotnet/api/system.drawing.image.fromstream?view=dotnet-plat-ext-3.1
            var stream = ExtractFile(OMODEntryFileType.Image);
            var image = Image.FromStream(stream);
            return (Bitmap) image;
        }

        /// <summary>
        /// Returns an enumerable of all data files.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<OMODCompressedEntry> GetDataFiles()
        {
            return OMODFile.DataFiles;
        }

        /// <summary>
        /// Returns an enumerable for all plugins. Do note that plugins
        /// are optional so check if this is null before doing anything with it.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<OMODCompressedEntry>? GetPlugins()
        {
            return HasFile(OMODEntryFileType.PluginsCRC) && HasFile(OMODEntryFileType.Plugins) 
                ? OMODFile.Plugins 
                : null;
        }

        /// <summary>
        /// Extracts all plugins to a given directory.
        /// </summary>
        /// <param name="outputDirectory">Output directory</param>
        public void ExtractPluginFiles(DirectoryInfo outputDirectory)
        {
            ExtractCompressedData(OMODEntryFileType.Data, outputDirectory);
        }

        /// <summary>
        /// Extracts all data files to a given directory.
        /// </summary>
        /// <param name="outputDirectory">Output directory</param>
        public void ExtractDataFiles(DirectoryInfo outputDirectory)
        {
            ExtractCompressedData(OMODEntryFileType.Data, outputDirectory);
        }

        /// <summary>
        /// Extracts all plugins to a given directory asynchronously with a variable amount of threads.
        /// </summary>
        /// <param name="outputDirectory">Output directory</param>
        /// <param name="threads">Number of threads to use. Default is 2</param>
        /// <returns></returns>
        public async Task ExtractPluginFilesAsync(DirectoryInfo outputDirectory, int threads = 2)
        {
            await ExtractCompressedDataAsync(OMODEntryFileType.Plugins, outputDirectory, threads);
        }

        /// <summary>
        /// Extracts all data files to a given directory.
        /// </summary>
        /// <param name="outputDirectory">Output directory</param>
        /// <param name="threads">Number of threads to use. Default is 4</param>
        /// <returns></returns>
        public async Task ExtractDataFilesAsync(DirectoryInfo outputDirectory, int threads = 4)
        {
            await ExtractCompressedDataAsync(OMODEntryFileType.Data, outputDirectory, threads);
        }

        private async Task ExtractCompressedDataAsync(OMODEntryFileType entryFileType, DirectoryInfo outputDirectory,
            int threads = 4)
        {
            if (entryFileType != OMODEntryFileType.Data && entryFileType != OMODEntryFileType.Plugins)
                throw new ArgumentException($"Provided OMODFile can only be Data or Plugins but is {entryFileType}!", nameof(entryFileType));

            if (!outputDirectory.Exists)
                outputDirectory.Create();

            OMODFile.Decompress(entryFileType);

            await OMODFile.ExtractAllDecompressedFilesAsync(outputDirectory, entryFileType == OMODEntryFileType.Data,
                threads);
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
            Utils.Debug("Disposing OMOD.");
            OMODFile.Dispose();
        }
    }
}
