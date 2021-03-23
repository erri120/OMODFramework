using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Text;
using JetBrains.Annotations;
using OMODFramework.Compression;

namespace OMODFramework
{
    /// <summary>
    /// Represents creation options for an OMOD.
    /// </summary>
    [PublicAPI]
    public sealed class OMODCreationOptions : OMODConfig
    {
        /// <summary>
        /// Readme of the OMOD.
        /// </summary>
        public string? Readme { get; set; }

        /// <summary>
        /// Type of the script.
        /// </summary>
        public OMODScriptType ScriptType { get; set; } = OMODScriptType.OBMMScript;
        
        /// <summary>
        /// Script of the OMOD.
        /// </summary>
        public string? Script { get; set; }

        /// <summary>
        /// Image of the OMOD.
        /// </summary>
        public Bitmap? Image { get; set; }

        /// <summary>
        /// Compression-level used for LZMA compression.
        /// </summary>
        public SevenZipCompressionLevel SevenZipCompressionLevel { get; set; } = SevenZipCompressionLevel.Medium;

        /// <summary>
        /// Compression-level used for ZIP compression.
        /// </summary>
        public CompressionLevel ZipCompressionLevel { get; set; } = CompressionLevel.Optimal;
        
        /// <summary>
        /// Compression-level used for the OMOD.
        /// </summary>
        public CompressionLevel OMODCompressionLevel { get; set; } = CompressionLevel.Optimal;

        /// <summary>
        /// List of all data files to be added to the OMOD.
        /// </summary>
        public List<OMODCreationFile>? DataFiles { get; set; }

        /// <summary>
        /// List of all plugin files to be added to the OMOD.
        /// </summary>
        public List<OMODCreationFile>? PluginFiles { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="OMODCreationOptions"/> class.
        /// </summary>
        /// <param name="version"><see cref="Version"/></param>
        public OMODCreationOptions(Version version) : base(string.Empty, version, string.Empty, string.Empty,
            string.Empty, string.Empty, DateTime.Now, 2, CompressionType.SevenZip) { }
    }

    /// <summary>
    /// Represents a data or plugin file to be added to the OMOD.
    /// </summary>
    [PublicAPI]
    public readonly struct OMODCreationFile
    {
        /// <summary>
        /// Path to the file on disk.
        /// </summary>
        public readonly string From;
        
        /// <summary>
        /// Relative file of the file in the OMOD.
        /// </summary>
        public readonly string To;

        /// <summary>
        /// Initializes a new instance of the <see cref="OMODCreationFile"/> structure.
        /// </summary>
        /// <param name="from"><see cref="From"/></param>
        /// <param name="to"><see cref="To"/></param>
        /// <exception cref="ArgumentException">File <paramref name="from"/> does not exist.</exception>
        public OMODCreationFile(string from, string to)
        {
            if (!File.Exists(from))
                throw new ArgumentException($"Input file does not exist! {from}", nameof(from));

            var isRelative = !Path.IsPathRooted(to) && !Path.IsPathFullyQualified(to);
            if (!isRelative)
                throw new ArgumentException($"Path is not relative! {to}", nameof(to));
            
            From = from;
            To = to.Normalize();
        }
    }
    
    /// <summary>
    /// Provides static functions for OMOD creation.
    /// </summary>
    [PublicAPI]
    public static class OMODCreation
    {
        /// <summary>
        /// Create a new OMOD and write to disk.
        /// </summary>
        /// <param name="options">Creation options to use.</param>
        /// <param name="output">Output path.</param>
        public static void CreateOMOD(OMODCreationOptions options, string output)
        {
            using var ms = CreateOMOD(options);
            using var fs = File.Open(output, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            ms.CopyTo(fs);
        }
        
        /// <summary>
        /// Create a new OMOD as a <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="options">Creation options to use.</param>
        /// <returns></returns>
        public static MemoryStream CreateOMOD(OMODCreationOptions options)
        {
            var ms = new MemoryStream();

            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true, Encoding.UTF8))
            {
                void WriteStringToArchive(string name, string content)
                {
                    var entry = archive.CreateEntry(name, options.OMODCompressionLevel);
                    using var entryStream = entry.Open();
                    using var bw = new BinaryWriter(entryStream);
                    bw.Write(content);
                }
                
                if (options.Readme != null)
                {
                    WriteStringToArchive("readme", options.Readme);
                }

                if (options.Script != null)
                {
                    WriteStringToArchive("script", $"{(char) options.ScriptType}{options.Script}");
                }

                if (options.Image != null)
                {
                    var entry = archive.CreateEntry("image", options.OMODCompressionLevel);
                    using var entryStream = entry.Open();
                    options.Image.Save(entryStream, options.Image.RawFormat);
                }
                
                {
                    var config = archive.CreateEntry("config", options.OMODCompressionLevel);
                    using var configStream = config.Open();
                    using var bw = new BinaryWriter(configStream);

                    bw.Write((byte) 4);
                    bw.Write(options.Name);
                    bw.Write(options.Version.Major);
                    bw.Write(options.Version.Minor);
                    bw.Write(options.Author);
                    bw.Write(options.Email);
                    bw.Write(options.Website);
                    bw.Write(options.Description);
                    bw.Write(DateTime.Now.ToBinary());
                    bw.Write((byte) options.CompressionType);
                    bw.Write(options.Version.Build);
                }

                void WriteFilesToArchive(string name, IEnumerable<OMODCreationFile> files)
                {
                    CompressionHandler.CompressFiles(files, options.CompressionType,
                        options.SevenZipCompressionLevel, options.ZipCompressionLevel, out var crcStream,
                        out var compressedStream);

                    //Entries cannot be created while previously created entries are still open
                    {
                        var crcEntry = archive.CreateEntry($"{name}.crc", options.OMODCompressionLevel);
                        using var crcEntryStream = crcEntry.Open();
                        crcStream.CopyTo(crcEntryStream);
                        crcStream.Dispose();
                    }

                    {
                        var dataEntry = archive.CreateEntry(name, CompressionLevel.NoCompression);
                        using var dataEntryStream = dataEntry.Open();
                        compressedStream.CopyTo(dataEntryStream);
                        compressedStream.Dispose();
                    }
                }
                
                if (options.DataFiles != null)
                {
                    WriteFilesToArchive("data", options.DataFiles);
                }

                if (options.PluginFiles != null)
                {
                    WriteFilesToArchive("plugins", options.PluginFiles);
                }
            }

            ms.Position = 0;
            return ms;
        }
    }
}
