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

    /// <summary>
    /// Options used for OMOD Creation, inherits <see cref="Config"/>
    /// </summary>
    [PublicAPI]
    public class CreationOptions : Config
    {
        /// <summary>
        /// Optional, entire Readme as a string
        /// </summary>
        public string? Readme { get; set; }
        /// <summary>
        /// Optional, entire Script as a string
        /// </summary>
        public string? Script { get; set; }
        /// <summary>
        /// Optional but has to be set when <see cref="Script"/> is not null
        /// </summary>
        public ScriptType ScriptType { get; set; }
        /// <summary>
        /// Optional, path to the image
        /// </summary>
        public FileInfo? ImagePath { get; set; }

        /// <summary>
        /// Level of compression used for the .omod file. Must not be <see cref="CompressionLevel.None"/>
        /// </summary>
        public CompressionLevel OMODCompressionLevel { get; set; }
        /// <summary>
        /// Level of compression used for the data and plugin files. Must not be <see cref="CompressionLevel.None"/>
        /// </summary>
        public CompressionLevel DataCompressionLevel { get; set; }

        /// <summary>
        /// Required, List of all data files
        /// </summary>
        public HashSet<CreationOptionFile>? DataFiles { get; set; }
        /// <summary>
        /// Optional, List of all plugin files
        /// </summary>
        public HashSet<CreationOptionFile>? PluginFiles { get; set; }

        /// <summary>
        /// Utility function to verify whether the given Options are valid.
        /// </summary>
        /// <param name="throwException">Whether to throw an Exception instead of returning false if an option
        /// is not valid. Default: true</param>
        /// <returns></returns>
        public bool VerifyOptions(bool throwException = true)
        {
            if (string.IsNullOrEmpty(Name))
            {
                if (throwException)
                    throw new ArgumentException("Name must not be empty!", nameof(Name));
                return false;
            }

            if (DataFiles == null)
            {
                if (throwException)
                    throw new ArgumentException("DataFiles list must not be null!", nameof(DataFiles));
                return false;
            }

            if (DataFiles.Count == 0)
            {
                if (throwException)
                    throw new ArgumentException("DataFiles list must have at least 1 entry!", nameof(DataFiles));
                return false;
            }

            if (OMODCompressionLevel == CompressionLevel.None)
            {
                if (throwException)
                    throw new ArgumentException("OMODCompressionLevel must not be None!", nameof(OMODCompressionLevel));
                return false;
            }

            if (DataCompressionLevel == CompressionLevel.None)
            {
                if (throwException)
                    throw new ArgumentException("DataCompressionLevel must not be None!", nameof(DataCompressionLevel));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Struct for files to be used in <see cref="DataFiles"/> and <see cref="PluginFiles"/>
        /// </summary>
        [PublicAPI]
        public struct CreationOptionFile
        {
            /// <summary>
            /// File on disk to include
            /// </summary>
            public FileInfo From { get; }
            /// <summary>
            /// Path of the file to go to in the omod. Do note that plugins
            /// must not be in a directory.
            /// </summary>
            public string To { get; }

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

            public override bool Equals(object obj)
            {
                if (!(obj is CreationOptionFile file))
                    return false;

                return To.Equals(file.To, StringComparison.InvariantCultureIgnoreCase) && From.FullName.Equals(file.From.FullName, StringComparison.InvariantCultureIgnoreCase);
            }

            public override int GetHashCode()
            {
                return From.FullName.GetHashCode(StringComparison.InvariantCultureIgnoreCase);
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

        /// <summary>
        /// Create an OMOD
        /// </summary>
        /// <param name="options">Options</param>
        /// <param name="output">Output file</param>
        /// <param name="settings">Optional, <see cref="FrameworkSettings"/> to use</param>
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
