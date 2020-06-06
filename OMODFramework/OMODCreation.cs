#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using JetBrains.Annotations;

namespace OMODFramework
{
    [PublicAPI]
    public enum ScriptType : byte
    {
        OBMMScript,
        Python,
        CSharp,
        VB
    }

    [PublicAPI]
    public class CreationOptions : Config
    {
        public string? Readme { get; set; }
        public string? Script { get; set; }
        public ScriptType ScriptType { get; set; }
        public FileInfo? ImagePath { get; set; }

        public CompressionLevel OMODCompressionLevel { get; set; }
        public CompressionLevel DataCompressionLevel { get; set; }

        public List<CreationOptionFile>? DataFiles { get; set; }
        public List<CreationOptionFile>? PluginFiles { get; set; }

        internal bool VerifyOptions()
        {
            if (string.IsNullOrEmpty(Name))
                throw new ArgumentException("Name must not be empty!", nameof(Name));

            if (DataFiles == null)
                throw new ArgumentException("DataFiles list must not be null!", nameof(DataFiles));

            if (DataFiles.Count == 0)
                throw new ArgumentException("DataFiles list must have at least 1 entry!", nameof(DataFiles));

            return true;
        }

        [PublicAPI]
        public struct CreationOptionFile
        {
            public FileInfo From { get; set; }
            public string To { get; set; }

            public CreationOptionFile(FileInfo from, string to)
            {
                if (!from.Exists)
                    throw new ArgumentException($"The given path: {from} does not exist!", nameof(from));

                From = from;
                To = to;
            }

            public override string ToString()
            {
                return $"{From.FullName} to {To}";
            }
        }
    }

    public partial class OMOD
    {
        private static void WriteStreamToZip(BinaryWriter bw, Stream stream)
        {
            stream.Position = 0;
            var buffer = new byte[4096];
            var upTo = 0;
            while (stream.Length - upTo > 4096)
            {
                stream.Read(buffer, 0, 4096);
                bw.Write(buffer, 0, 4096);
                upTo += 4096;
            }

            if (stream.Length - upTo > 0)
            {
                stream.Read(buffer, 0, (int)stream.Length - upTo);
                bw.Write(buffer, 0, (int)stream.Length - upTo);
            }
        }

        public static void CreateOMOD(CreationOptions options, FileInfo output, FrameworkSettings? settings = null)
        {
            settings ??= FrameworkSettings.DefaultFrameworkSettings;

            if(output.Exists)
                output.Delete();

            if(output.Extension != ".omod")
                throw new ArgumentException("Output file has to have the .omod extension!", nameof(output));

            using var zipStream = new ZipOutputStream(output.Open(FileMode.CreateNew, FileAccess.ReadWrite));
            using var bw = new BinaryWriter(zipStream);

            zipStream.SetLevel((int)options.OMODCompressionLevel);

            if (options.Readme != null && !string.IsNullOrEmpty(options.Readme))
            {
                var entry = new ZipEntry("readme");
                zipStream.PutNextEntry(entry);
                bw.Write(options.Readme);
                bw.Flush();
            }

            if (options.Script != null && !string.IsNullOrEmpty(options.Script))
            {
                var entry = new ZipEntry("script");
                zipStream.PutNextEntry(entry);

                var script = new char[options.Script.Length+1];
                script[0] = (char) options.ScriptType;

                for (var i = 0; i < options.Script.Length; i++)
                {
                    var c = options.Script[i];
                    script[i + 1] = c;
                }

                var sScript = new string(script);
                bw.Write(sScript);
                bw.Flush();
            }

            if (options.ImagePath != null && options.ImagePath.Exists)
            {
                var entry = new ZipEntry("image");
                zipStream.PutNextEntry(entry);

                using var fs = options.ImagePath.OpenRead();
                //TODO: check if this actually works
                fs.CopyTo(zipStream);
                bw.Flush();
            }

            var config = new ZipEntry("config");
            zipStream.PutNextEntry(config);

            bw.Write(settings.CurrentOMODVersion);
            bw.Write(options.Name);
            bw.Write(options.Version.Major);
            bw.Write(options.Version.Minor);
            bw.Write(options.Author);
            bw.Write(options.Email);
            bw.Write(options.Website);
            bw.Write(options.Description);
            bw.Write(DateTime.Now.ToBinary());
            bw.Write((byte)options.CompressionType);
            bw.Write(options.Version.Build);
            bw.Flush();

            if (options.PluginFiles != null && options.PluginFiles.Count > 0)
            {
                var entry = new ZipEntry("plugins.crc");
                zipStream.PutNextEntry(entry);
                CompressionHandler.CompressFiles(options.PluginFiles, options.CompressionType, options.DataCompressionLevel, out var pluginsCompressed, out var pluginsCRC);
                WriteStreamToZip(bw, pluginsCRC);
                bw.Flush();

                zipStream.SetLevel(0);
                entry = new ZipEntry("plugins");
                zipStream.PutNextEntry(entry);
                WriteStreamToZip(bw, pluginsCompressed);
                bw.Flush();
                zipStream.SetLevel((int)options.OMODCompressionLevel);

                pluginsCompressed.Close();
                pluginsCRC.Close();
            }

            if (options.DataFiles != null && options.DataFiles.Count > 0)
            {
                var entry = new ZipEntry("data.crc");
                zipStream.PutNextEntry(entry);
                CompressionHandler.CompressFiles(options.DataFiles, options.CompressionType, options.DataCompressionLevel, out var dataCompressed, out var dataCRC);
                WriteStreamToZip(bw, dataCRC);
                bw.Flush();

                zipStream.SetLevel(0);
                entry = new ZipEntry("data");
                zipStream.PutNextEntry(entry);
                WriteStreamToZip(bw, dataCompressed);
                bw.Flush();
                zipStream.SetLevel((int)options.OMODCompressionLevel);

                dataCompressed.Close();
                dataCRC.Close();
            }
        }
    }
}
