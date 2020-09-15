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
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NLog;
using OMODFramework.Exceptions;
using OMODFramework.Logging;

namespace OMODFramework
{
    [PublicAPI]
    public sealed partial class OMOD : IDisposable
    {
        private readonly Logger _logger;
        
        private readonly FrameworkSettings _frameworkSettings;
        internal readonly OMODFile OMODFile;
        
        public readonly OMODConfig OMODConfig;

        public OMOD(string path, FrameworkSettings? settings = null, bool checkIntegrity = true)
        {
            _logger = OMODFrameworkLogging.GetLogger("OMOD");
            
            if (!File.Exists(path))
                _logger.ErrorThrow(new ArgumentException($"OMOD {path} does not exist!", nameof(path)));

            _frameworkSettings = settings ?? FrameworkSettings.DefaultFrameworkSettings;
            
            _logger.Info($"Loading OMOD from {path}");
            
            OMODFile = new OMODFile(path, _frameworkSettings);

            if (checkIntegrity)
                OMODFile.CheckIntegrity();

            if (!OMODFile.IsValidOMOD())
                _logger.ErrorThrow(new OMODException("OMOD is not valid, check previous errors!"));

            using var configStream = OMODFile.GetEntryFileStream(OMODEntryFileType.Config);
            OMODConfig = OMODConfig.ParseConfig(configStream);

            OMODFile.CompressionType = OMODConfig.CompressionType;
            
            _logger.Info($"Successfully loaded OMOD {OMODConfig.Name}");
        }

        /// <summary>
        /// Check if the OMOD contains the provided <see cref="OMODEntryFileType"/>.
        /// </summary>
        /// <param name="entryFileType">File to look for</param>
        /// <returns></returns>
        public bool HasFile(OMODEntryFileType entryFileType) => OMODFile.HasEntryFile(entryFileType);

        /// <summary>
        /// Returns the decompressed Stream of a <see cref="OMODEntryFileType"/> from the OMOD.
        /// Use <see cref="HasFile"/> before doing this to ensure you don't get a <see cref="ZipFileEntryNotFoundException"/>
        /// </summary>
        /// <param name="entryFileType">File to extract</param>
        /// <returns></returns>
        /// <exception cref="ZipFileEntryNotFoundException">Thrown when the OMOD does not contain the provided <see cref="OMODEntryFileType"/></exception>
        public Stream GetEntryFileStream(OMODEntryFileType entryFileType)
        {
            return OMODFile.GetEntryFileStream(entryFileType);
        }

        private string GetStringFromEntry(OMODEntryFileType entryFileType)
        {
            if (!HasFile(entryFileType))
            {
                _logger.Error($"OMOD does not have a {entryFileType.ToFileString()}!");
                return string.Empty;
            }
            using var stream = GetEntryFileStream(entryFileType);
            using var br = new BinaryReader(stream);
            return br.ReadString();
        }
        
        /// <summary>
        /// Returns the Readme.
        /// </summary>
        /// <returns></returns>
        public string GetReadme()
        {
            return GetStringFromEntry(OMODEntryFileType.Readme);
        }

        /// <summary>
        /// Returns the Script
        /// </summary>
        /// <param name="removeType">Whether to remove the script type identifier from the string.</param>
        /// <returns></returns>
        public string GetScript(bool removeType = true)
        {
            var script = GetStringFromEntry(OMODEntryFileType.Script);
            if (script.Equals(string.Empty)) return script;
            if (!removeType) return script;

            ReadOnlySpan<char> span = script.AsSpan();
            if ((byte) span[0] < 4)
                span = span.Slice(1);
            return span.ToString();
        }

        /// <summary>
        /// Returns the Image of the OMOD or null if it does not have one.
        /// </summary>
        /// <returns></returns>
        public Bitmap? GetImage()
        {
            if (!HasFile(OMODEntryFileType.Image))
            {
                _logger.Error("OMOD does not have an Image!");
                return null;
            }
            
            //stream has be kept open for the lifetime of the image
            //see https://docs.microsoft.com/en-us/dotnet/api/system.drawing.image.fromstream?view=dotnet-plat-ext-3.1
            var stream = GetEntryFileStream(OMODEntryFileType.Image);
            var image = Image.FromStream(stream);
            return (Bitmap) image;
        }

        public IEnumerable<OMODCompressedFile> GetDataFilesInfo()
        {
            return OMODFile.DataFiles;
        }

        public IEnumerable<OMODCompressedFile> GetPluginFilesInfo()
        {
            return OMODFile.PluginFiles;
        }

        /// <inheritdoc cref="OMODFramework.OMODFile.ExtractFiles"/>
        public void ExtractFiles(bool data, string output)
        {
            OMODFile.ExtractFiles(data, output);
        }

        /// <inheritdoc cref="OMODFramework.OMODFile.ExtractFilesParallel"/>
        public void ExtractFilesParallel(bool data, string output, byte numStreams, int degreeOfParallelism = 0, CancellationToken? token = null)
        {
            OMODFile.ExtractFilesParallel(data, output, numStreams, degreeOfParallelism, token);
        }
        
        /// <inheritdoc cref="OMODFramework.OMODFile.ExtractFilesAsync"/>
        public async Task ExtractFilesAsync(bool data, string output, byte numThreads, CancellationToken? token = null)
        {
            await OMODFile.ExtractFilesAsync(data, output, numThreads, token);
        }
        
        /// <inheritdoc /> 
        public void Dispose()
        {
            OMODFile.Dispose();
        }
    }
}
