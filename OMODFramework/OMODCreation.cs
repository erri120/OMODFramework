// /*
//     Copyright (C) 2020  erri120
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <https://www.gnu.org/licenses/>.
// */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using JetBrains.Annotations;
using OMODFramework.Logging;

namespace OMODFramework
{
    [PublicAPI]
    public enum ScriptType : byte
    {
        /// <summary>
        /// Classic OBMMScript, see (http://timeslip.chorrol.com/obmmm/functionlist.htm)
        /// </summary>
        OBMMScript,
        /// <summary>
        /// Python using IronPython (not supported)
        /// </summary>
        [Obsolete("Not supported, use C# or OBMMScript instead.")]
        Python,
        /// <summary>
        /// C#
        /// </summary>
        CSharp,
        /// <summary>
        /// Visual Basic (not supported)
        /// </summary>
        [Obsolete("Not supported, use C# or OBMMScript instead.")]
        VB
    }

    /// <summary>
    /// Struct for data and plugin files in <see cref="OMODCreationOptions"/>
    /// </summary>
    [PublicAPI]
    public struct OMODCreationFile
    {
        public readonly string From;
        public readonly string To;

        public OMODCreationFile(string from, string to)
        {
            if (!File.Exists(from))
                throw new ArgumentException($"File at {from} does not exist!", nameof(from));
            
            var isAbsoluteFrom = Path.IsPathRooted(from) && Path.IsPathFullyQualified(from);
            if (!isAbsoluteFrom)
                throw new ArgumentException($"Path \"{from}\" is not absolute!", nameof(from));

            var isAbsoluteTo = Path.IsPathRooted(to) && Path.IsPathFullyQualified(to);
            if (isAbsoluteTo)
                throw new ArgumentException($"Path \"{to}\" is not relative!", nameof(to));
            
            //TODO: make valid path
            
            From = from;
            To = to;
        }
    }

    public class OMODCreationFileComparer : IEqualityComparer<OMODCreationFile>
    {
        public bool Equals(OMODCreationFile x, OMODCreationFile y)
        {
            return string.Equals(x.From, y.From, StringComparison.OrdinalIgnoreCase) && string.Equals(x.To, y.To, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(OMODCreationFile obj)
        {
            var hashCode = new HashCode();
            hashCode.Add(obj.From, StringComparer.OrdinalIgnoreCase);
            hashCode.Add(obj.To, StringComparer.OrdinalIgnoreCase);
            return hashCode.ToHashCode();
        }
    } 
    
    [PublicAPI]
    public class OMODCreationOptions : OMODConfig
    {
        /// <summary>
        /// Optional, Readme or path to Readme.
        /// </summary>
        public string? Readme { get; set; }
        
        /// <summary>
        /// Optional, Script or path to Script.
        /// </summary>
        public string? Script { get; set; }
        
        /// <summary>
        /// Required when <see cref="Script"/> is not null.
        /// </summary>
        public ScriptType ScriptType { get; set; }
        
        /// <summary>
        /// Optional, path to Image if you don't want to use <see cref="Image"/>. <see cref="Image"/> must be null!
        /// </summary>
        public string? ImagePath { get; set; }
        
        /// <summary>
        /// Optional, Image if you don't want to use <see cref="ImagePath"/>. <see cref="ImagePath"/> must be null!
        /// </summary>
        public Bitmap? Image { get; set; }
        
        /// <summary>
        /// Level of compression used for the .omod file. Must not be <see cref="CompressionLevel.None"/>.
        /// </summary>
        public CompressionLevel OMODCompressionLevel { get; set; }
        
        /// <summary>
        /// Level of compression used for the data and plugin files. Must not be <see cref="CompressionLevel.None"/>.
        /// </summary>
        public CompressionLevel DataCompressionLevel { get; set; }
        
        /// <summary>
        /// Required, HashSet of all Data Files. Use <see cref="HashSet{T}.Add"/> to add the files you want to use. Make sure you check the return value of <see cref="HashSet{T}.Add"/>!
        /// </summary>
        public readonly HashSet<OMODCreationFile> DataFiles = new HashSet<OMODCreationFile>(new OMODCreationFileComparer());
        
        /// <summary>
        /// Optional, HashSet of all Plugin Files. Use <see cref="HashSet{T}.Add"/> to add the files you want to use. Make sure you check the return value of <see cref="HashSet{T}.Add"/>!
        /// </summary>
        public readonly HashSet<OMODCreationFile> PluginFiles = new HashSet<OMODCreationFile>(new OMODCreationFileComparer());

        public bool VerifyOptions(bool throwException = true)
        {
            var report = new Action<string, string>((msg, name) =>
            {
                if (throwException)
                    throw new ArgumentException(msg, name);
            });

            if (string.IsNullOrEmpty(Name))
            {
                report("Name must not be empty!", nameof(Name));
                return false;
            }

            if (DataFiles.Count == 0)
            {
                report("Data Files must not be empty!", nameof(DataFiles));
                return false;
            }

            if (OMODCompressionLevel == CompressionLevel.None)
            {
                report("OMOD Compression Level must not be None!", nameof(OMODCompressionLevel));
                return false;
            }

            if (DataCompressionLevel == CompressionLevel.None)
            {
                report("Data Compression Level must not be None!", nameof(DataCompressionLevel));
                return false;
            }

            if (ImagePath != null && !File.Exists(ImagePath))
            {
                report($"Image Path {ImagePath} does not exist!", nameof(ImagePath));
                return false;
            }
            
            return true;
        }
    }

    public partial class OMOD
    {
        public static void CreateOMOD(OMODCreationOptions options, string output, FrameworkSettings? settings = null)
        {
            var logger = OMODFrameworkLogging.GetLogger("OMODCreation");
            
            settings ??= FrameworkSettings.DefaultFrameworkSettings;

            if (!options.VerifyOptions())
                logger.ErrorThrow(new ArgumentException("Creation Options are not valid!", nameof(options)));
            
            var isDirectory = Directory.Exists(output);
            var outputFile = isDirectory ? $"{options.Name}.omod" : output;
            
            logger.Info($"Output is {outputFile}");
            
            if (File.Exists(outputFile))
                File.Delete(outputFile);

            var fs = File.Open(output, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
            using var zipStream = new ZipOutputStream(fs);
            using var bw = new BinaryWriter(zipStream);
            
            zipStream.SetLevel((int) options.OMODCompressionLevel);

            if (options.Readme != null)
            {
                var readme = options.Readme!;
                try
                {
                    var path = Path.GetFullPath(options.Readme);
                    if (File.Exists(path))
                    {
                        logger.Debug($"Reading Readme from {options.Readme}");
                        readme = File.ReadAllText(options.Readme, Encoding.UTF8);
                    }
                }
                catch
                {
                    readme = options.Readme!;
                }
                
                var entry = new ZipEntry("readme");
                zipStream.PutNextEntry(entry);
                bw.Write(readme);
                bw.Flush();
            }

            if (options.Script != null)
            {
                var script = options.Script!;
                try
                {
                    var path = Path.GetFullPath(options.Script);
                    if (File.Exists(path))
                    {
                        logger.Debug($"Reading Script from {options.Script}");
                        script = File.ReadAllText(options.Script, Encoding.UTF8);
                    }
                }
                catch
                {
                    script = options.Script!;
                }

                script = script.Insert(0, $"{(char) options.ScriptType}");
                
                var entry = new ZipEntry("script");
                zipStream.PutNextEntry(entry);
                bw.Write(script);
                bw.Flush();
            }

            if (options.ImagePath != null || options.Image != null)
            {
                var entry = new ZipEntry("image");
                zipStream.PutNextEntry(entry);

                if (options.ImagePath != null)
                {
                    logger.Debug($"Reading Image from path {options.ImagePath}");
                    using var imageFs = File.Open(options.ImagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    fs.CopyTo(zipStream);
                } else if (options.Image != null)
                {
                    logger.Debug("Reading Image from Bitmap");
                    options.Image.Save(zipStream, options.Image.RawFormat);
                }
                else
                {
                    throw new NotImplementedException();
                }
                
                bw.Flush();
            }
            
            logger.Debug("Writing config info");
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
            bw.Write((byte) options.CompressionType);
            bw.Write(options.Version.Build);
            bw.Flush();

            if (options.PluginFiles.Count > 0)
            {
                logger.Debug($"Including {options.PluginFiles.Count} Plugin Files");
                var entry = new ZipEntry("plugins.crc");
                zipStream.PutNextEntry(entry);
                
                CompressionHandler.CompressFiles(options.PluginFiles, options.CompressionType, options.DataCompressionLevel, 
                    out var pluginsCompressed, out var pluginsCRC, settings.CodeProgress);

                pluginsCRC.CopyTo(zipStream);
                zipStream.Flush();
                
                zipStream.SetLevel(0);
                entry = new ZipEntry("plugins");
                zipStream.PutNextEntry(entry);
                
                pluginsCompressed.CopyTo(zipStream);
                zipStream.Flush();
                zipStream.SetLevel((int) options.OMODCompressionLevel);
                
                pluginsCompressed.Dispose();
                pluginsCRC.Dispose();
            }
            
            if (options.DataFiles.Count > 0)
            {
                logger.Debug($"Including {options.DataFiles.Count} Data Files");
                var entry = new ZipEntry("data.crc");
                zipStream.PutNextEntry(entry);
                
                CompressionHandler.CompressFiles(options.DataFiles, options.CompressionType, options.DataCompressionLevel, 
                    out var dataCompressed, out var dataCRC, settings.CodeProgress);
                
                dataCRC.CopyTo(zipStream);
                zipStream.Flush();

                zipStream.SetLevel(0);
                entry = new ZipEntry("data");
                zipStream.PutNextEntry(entry);
                
                dataCompressed.CopyTo(zipStream);
                zipStream.Flush();
                zipStream.SetLevel((int) options.OMODCompressionLevel);
                
                dataCompressed.Dispose();
                dataCRC.Dispose();
            }
            
            logger.Info($"Finished OMOD Creation to {outputFile}");
        }
    }
}
