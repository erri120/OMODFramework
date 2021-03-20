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
    [PublicAPI]
    public sealed class OMODCreationOptions : OMODConfig
    {
        public string? Readme { get; set; }

        public OMODScriptType ScriptType { get; set; } = OMODScriptType.OBMMScript;
        
        public string? Script { get; set; }

        public Bitmap? Image { get; set; }

        public SevenZipCompressionLevel SevenZipCompressionLevel { get; set; } = SevenZipCompressionLevel.Medium;

        public CompressionLevel ZipCompressionLevel { get; set; } = CompressionLevel.Optimal;
        
        public CompressionLevel OMODCompressionLevel { get; set; } = CompressionLevel.Optimal;

        public List<OMODCreationFile>? DataFiles { get; set; }

        public List<OMODCreationFile>? PluginFiles { get; set; }
        
        public OMODCreationOptions(Version version) : base(string.Empty, version, string.Empty, string.Empty,
            string.Empty, string.Empty, DateTime.Now, 2, CompressionType.SevenZip) { }
    }

    [PublicAPI]
    public readonly struct OMODCreationFile
    {
        public readonly string From;
        public readonly string To;

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
    
    [PublicAPI]
    public static class OMODCreation
    {
        public static void CreateOMOD(OMODCreationOptions options, string output)
        {
            using var ms = CreateOMOD(options);
            using var fs = File.Open(output, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            ms.CopyTo(fs);
        }

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
