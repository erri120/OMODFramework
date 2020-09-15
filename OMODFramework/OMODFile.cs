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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using JetBrains.Annotations;
using NLog;
using OMODFramework.Logging;

namespace OMODFramework
{
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
    
    [PublicAPI]
    public class OMODCompressedFile
    {
        public readonly string Name;
        public readonly uint CRC;
        public readonly long Length;
        
        internal long Offset { get; set; }

        public OMODCompressedFile(string name, uint crc, long length)
        {
            Name = name;
            CRC = crc;
            Length = length;
        }

        internal string GetFullPath(string directory)
        {
            return Path.Combine(directory, Name);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is OMODCompressedFile file))
                return false;

            return CRC.Equals(file.CRC) && Name.Equals(file.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Name, StringComparer.OrdinalIgnoreCase);
            hashCode.Add(CRC);
            return hashCode.ToHashCode();
        }

        public override string ToString()
        {
            return $"{Name} {Length} bytes ({CRC:x8})";
        }
    }

    internal class OMODCompressedFileEqualityComparer : IEqualityComparer<OMODCompressedFile>
    {
        public bool Equals(OMODCompressedFile x, OMODCompressedFile y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(OMODCompressedFile obj)
        {
            return obj.GetHashCode();
        }
    }

    internal class OMODCompressedFileComparer : IComparer<OMODCompressedFile>
    {
        public int Compare(OMODCompressedFile x, OMODCompressedFile y)
        {
            return (int) (x.Offset - y.Offset);
        }
    }
    
    internal class GenericSplitMemoryStream<T> : MemoryStream
    {
        public readonly Stack<T> Stack;
        
        public GenericSplitMemoryStream(MemoryStream parent) : 
            base(parent.GetBuffer(), 0, (int) parent.Length, false)
        {
            Stack = new Stack<T>();
        }
    }
    
    internal class OMODFile : IDisposable
    {
        private readonly Logger _logger;
        
        private readonly ZipFile _zipFile;
        private readonly FrameworkSettings _frameworkSettings;

        public CompressionType CompressionType { get; internal set; }

        private HashSet<OMODCompressedFile>? _dataFiles;
        public HashSet<OMODCompressedFile> DataFiles
        {
            get
            {
                _dataFiles ??= GetCRCSet(true);
                return _dataFiles ?? new HashSet<OMODCompressedFile>();
            }
        }

        private HashSet<OMODCompressedFile>? _pluginFiles;
        public HashSet<OMODCompressedFile> PluginFiles
        {
            get
            {
                _pluginFiles ??= GetCRCSet(false);
                return _pluginFiles ?? new HashSet<OMODCompressedFile>();
            }
        }

        public OMODFile(string path, FrameworkSettings settings)
        {
            _logger = OMODFrameworkLogging.GetLogger("OMODFile");
            
            _zipFile = new ZipFile(path);
            _frameworkSettings = settings;
        }

        public bool IsValidOMOD()
        {
            var valid = true;
            
            var check = new Action<OMODEntryFileType>(type =>
            {
                if (HasEntryFile(type)) return;
                _logger.Error($"OMOD does not have a {type.ToFileString()} file!");
                valid = false;
            });

            check(OMODEntryFileType.Config);
            check(OMODEntryFileType.DataCRC);
            check(OMODEntryFileType.Data);

            return valid;
        }
        
        public bool HasEntryFile(OMODEntryFileType entryFileType)
        {
            return _zipFile.HasFile(entryFileType.ToFileString());
        }

        public Stream GetEntryFileStream(OMODEntryFileType entryFileType)
        {
            return _zipFile.ExtractFile(entryFileType.ToFileString());
        }
        
        public void CheckIntegrity()
        {
            try
            {
                _zipFile.CheckIntegrity();
            }
            catch (Exception e)
            {
                _logger.ErrorThrow(e);
            }
        }
        
        private HashSet<OMODCompressedFile>? GetCRCSet(bool data)
        {
            var entry = data ? OMODEntryFileType.DataCRC : OMODEntryFileType.PluginsCRC;           
            var result = new HashSet<OMODCompressedFile>(new OMODCompressedFileEqualityComparer());

            if (!HasEntryFile(entry))
            {
                _logger.Error($"Entry {entry} does not exist in the OMOD!");
                return null;
            }

            using var stream = GetEntryFileStream(entry);
            using var br = new BinaryReader(stream);

            var offset = 0L;
            while (br.PeekChar() != -1)
            {
                var name = br.ReadString();
                var crc = br.ReadUInt32();
                var length = br.ReadInt64();
                var compressedFile = new OMODCompressedFile(name, crc, length)
                {
                    Offset = offset
                };
                
                if(!result.Add(compressedFile))
                    _logger.Error($"Unable to add compressed file {compressedFile} to the HashSet!");
                offset += length;
            }
            
            return result;
        }

        private MemoryStream DecompressFiles(OMODEntryFileType entryFileType)
        {
            var isData = entryFileType == OMODEntryFileType.Data;
            return isData
                ? (MemoryStream) CompressionHandler.DecompressStream(DataFiles, GetEntryFileStream(entryFileType), CompressionType, _frameworkSettings.CodeProgress)
                : (MemoryStream) CompressionHandler.DecompressStream(PluginFiles, GetEntryFileStream(entryFileType), CompressionType, _frameworkSettings.CodeProgress);
        }

        private static void ExtractDecompressedFile(Stream decompressedStream, OMODCompressedFile file, string outputFolder)
        {
            var output = file.GetFullPath(outputFolder);
            var directory = Path.GetDirectoryName(output);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory!);

            if (File.Exists(output))
            {
                var fi = new FileInfo(output);
                if (fi.Length == file.Length)
                    return;
            }

            using var fs = File.Open(output, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            var buffer = new byte[file.Length];
            decompressedStream.Position = file.Offset;
            decompressedStream.Read(buffer, 0, buffer.Length);
            fs.Write(buffer, 0, buffer.Length);
        }

        private bool CanExtract(bool data)
        {
            var filesEntry = data ? OMODEntryFileType.Data : OMODEntryFileType.Plugins;
            var crcEntry = data ? OMODEntryFileType.DataCRC : OMODEntryFileType.PluginsCRC;
            
            if (!HasEntryFile(filesEntry))
            {
                _logger.Error($"OMOD does not contain {filesEntry}!");
                return false;
            }

            if (!HasEntryFile(crcEntry))
            {
                _logger.Error($"OMOD does not contain {crcEntry}!");
                return false;
            }

            return true;
        }

        private static void ExtractFilesFromStream(Stream stream, IEnumerable<OMODCompressedFile> files, string output)
        {
            foreach (var file in files)
            {
                ExtractDecompressedFile(stream, file, output);
            }
        }
        
        /// <summary>
        /// Extracts all Data/Plugin files using a single Thread.
        /// </summary>
        /// <param name="data">Whether to extract data or plugin files.</param>
        /// <param name="output">Output directory.</param>
        public void ExtractFiles(bool data, string output)
        {
            if (!CanExtract(data)) return;

            HashSet<OMODCompressedFile> files = data ? DataFiles : PluginFiles;
            using var decompressedStream = DecompressFiles(data ? OMODEntryFileType.Data : OMODEntryFileType.Plugins);
            ExtractFilesFromStream(decompressedStream, files, output);
        }

        private static IEnumerable<GenericSplitMemoryStream<OMODCompressedFile>> CreateGenericSplitStreams(
            MemoryStream decompressedStream, IEnumerable<OMODCompressedFile> files, byte numStreams)
        {
            /*
             * Stream splitting:
             *
             * We have one massive MemoryStream containing the entire decompressed data and want to create
             * x amount of "Sub-Streams/Split-Streams" so we can extract more than one file at a time.
             *
             * We first calculate how much data each Stream should handle (lengthEach) but since we are dealing
             * with files and don't want to cut a file in half, this value is not set in stone. We get the last
             * entry in our list where the offset is smaller than the length of our Stream.
             *
             * The GenericSplitMemoryStream has a Stack where we can push all items on there that
             * are within its range, meaning every file with an offset: usedLength <= offset <= LastEntry.Offset
             */
            
            List<OMODCompressedFile> filesList = files.ToList();
            var totalLength = decompressedStream.Length;
            var lengthEach = totalLength / numStreams;
            var usedLength = 0L;
            
            var streams = new List<GenericSplitMemoryStream<OMODCompressedFile>>(numStreams);

            for (var i = 0; i < numStreams; i++)
            {
                if (i == numStreams - 1)
                {
                    var stream = new GenericSplitMemoryStream<OMODCompressedFile>(decompressedStream);
                    var innerLength = usedLength;
                    filesList.Where(x => x.Offset >= innerLength)
                        .Do(x => stream.Stack.Push(x));
                    streams.Add(stream);
                }
                else
                {
                    var length = lengthEach + usedLength;
                    var lastEntry = filesList.Last(x => x.Offset < length);
                    length = lastEntry.Offset - usedLength;
                    var stream = new GenericSplitMemoryStream<OMODCompressedFile>(decompressedStream);
                    var innerLength = usedLength;
                    filesList
                        .Where(x => x.Offset >= innerLength && x.Offset <= lastEntry.Offset)
                        .Do(x => stream.Stack.Push(x));
                    streams.Add(stream);
                    usedLength += length;
                }
            }
            
            return streams;
        }
        
        /// <summary>
        /// Extracts the Data/Plugin Files in parallel using Split Memory Streams.
        /// </summary>
        /// <param name="data">Whether to extract Data or Plugin files.</param>
        /// <param name="output">Output directory.</param>
        /// <param name="numStreams">Number of Streams to use.</param>
        /// <param name="degreeOfParallelism">Specifies the maximum number of processors that PLINQ should use to parallelize the query. See https://docs.microsoft.com/en-us/dotnet/api/system.linq.parallelenumerable.withdegreeofparallelism?view=netcore-3.1 for more info.</param>
        /// <param name="token">Cancellation Token to cancel the execution if requested.</param>
        /// <exception cref="ArgumentException"></exception>
        public void ExtractFilesParallel(bool data, string output, byte numStreams, int degreeOfParallelism = 0, CancellationToken? token = null)
        {
            if (numStreams == 0)
                throw new ArgumentException("Can't extract Files with 0 streams!", nameof(numStreams));
            
            if (numStreams == 1)
            {
                ExtractFiles(data, output);
                return;
            }
            
            if (!CanExtract(data)) return;

            List<OMODCompressedFile> files = data ? DataFiles.ToList() : PluginFiles.ToList();
            files.Sort(new OMODCompressedFileComparer());
            
            using var decompressedStream = DecompressFiles(data ? OMODEntryFileType.Data : OMODEntryFileType.Plugins);
            List<GenericSplitMemoryStream<OMODCompressedFile>> streams = CreateGenericSplitStreams(decompressedStream, files, numStreams).ToList();

            ParallelQuery<GenericSplitMemoryStream<OMODCompressedFile>> query = streams.AsParallel()
                .WithCancellation(token ?? CancellationToken.None)
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism);
            
            if (degreeOfParallelism != 0)
            {
                query = query.WithDegreeOfParallelism(degreeOfParallelism);
            }

            query.ForAll(x =>
            {
                while (x.Stack.TryPop(out var current))
                {
                    ExtractDecompressedFile(x, current, output);
                }
            });

            streams.Do(x => x.Dispose());
        }

        /// <summary>
        /// Extracts the Data/Plugin files asynchronously using Split Memory Streams.
        /// </summary>
        /// <param name="data">Whether to extract Data or Plugin files</param>
        /// <param name="output">Output directory</param>
        /// <param name="numThreads">Number of threads to use</param>
        /// <param name="token">Cancellation token</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task ExtractFilesAsync(bool data, string output, byte numThreads, CancellationToken? token = null)
        {
            if (numThreads == 0)
                throw new ArgumentException("Can't extract files with 0 threads!", nameof(numThreads));
            
            if (numThreads == 1)
            {
                ExtractFiles(data, output);
                return;
            }
            
            if (!CanExtract(data)) return;
            
            List<OMODCompressedFile> files = data ? DataFiles.ToList() : PluginFiles.ToList();
            files.Sort(new OMODCompressedFileComparer());

            await using var decompressedStream = DecompressFiles(data ? OMODEntryFileType.Data : OMODEntryFileType.Plugins);
            List<GenericSplitMemoryStream<OMODCompressedFile>> streams = CreateGenericSplitStreams(decompressedStream, files, numThreads).ToList();

            await Task.WhenAll(streams.Select(async stream =>
            {
                await Task.Run(() =>
                {
                    while (stream.Stack.TryPop(out var current))
                    {
                        ExtractDecompressedFile(stream, current, output);
                    }
                }, token ?? CancellationToken.None);
            }));
            
            streams.Do(x => x.Dispose());
        }
        
        public void Dispose()
        {
            _zipFile.Close();
        }
    }
}
