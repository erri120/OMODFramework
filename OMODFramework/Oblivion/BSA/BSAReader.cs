using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using SharpCompress.Compressors;
using SharpCompress.Compressors.Deflate;

namespace OMODFramework.Oblivion.BSA
{
    /// <summary>
    /// Represents a reader for BSA files.
    /// </summary>
    [PublicAPI]
    public sealed class BSAReader : IDisposable
    {
        private readonly BinaryReader _br;
        
        /// <summary>
        /// Game version for the BSA.
        /// </summary>
        public readonly VersionType HeaderType;
        
        /// <summary>
        /// Archive flags of the BSA.
        /// </summary>
        public readonly ArchiveFlags ArchiveFlags;
        
        /// <summary>
        /// File flags of the BSA.
        /// </summary>
        public readonly FileFlags FileFlags;

        private readonly uint _folderRecordOffset;
        private readonly uint _folderCount;
        private readonly uint _fileCount;
        private readonly uint _totalFolderNameLength;
        private readonly uint _totalFileNameLength;

        /// <summary>
        /// Whether the Archive has folder names.
        /// </summary>
        public bool HasFolderNames => ArchiveFlags.HasFlag(ArchiveFlags.HasFolderNames);
        
        /// <summary>
        /// Whether the archive has file names.
        /// </summary>
        public bool HasFileNames => ArchiveFlags.HasFlag(ArchiveFlags.HasFileNames);
        
        /// <summary>
        /// Whether the files are compressed.
        /// </summary>
        public bool CompressedByDefault => ArchiveFlags.HasFlag(ArchiveFlags.Compressed);
        
        /// <summary>
        /// Whether the archive has file name blobs.
        /// </summary>
        public bool Bit9Set => ArchiveFlags.HasFlag(ArchiveFlags.HasFileNameBlobs);

        /// <summary>
        /// Whether the archive has name blobs.
        /// </summary>
        public bool HasNameBlobs
        {
            get
            {
                if (HeaderType == VersionType.FO3 || HeaderType == VersionType.SSE) return Bit9Set;
                return false;
            }
        }

        /// <summary>
        /// List of all folders in the BSA.
        /// </summary>
        public List<BSAFolderInfo> Folders { get; internal set; }
        
        /// <summary>
        /// List of all files in the BSA. 
        /// </summary>
        public List<BSAFileInfo> Files { get; internal set; }
        
        /// <summary>
        /// Creates a new BSAReader instance from a path.
        /// </summary>
        /// <param name="path">Path to the .bsa file.</param>
        /// <exception cref="ArgumentException">The file does not exist.</exception>
        /// <exception cref="InvalidDataException">File is not a BSA archive.</exception>
        /// <exception cref="NotSupportedException">BSA archive is not for TES4.</exception>
        public BSAReader(string path)
        {
            if (!File.Exists(path))
                throw new ArgumentException("File does not exist!", nameof(path));
            
            var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            _br = new BinaryReader(fs, Encoding.UTF8);

            var magic = Encoding.ASCII.GetString(_br.ReadBytes(4));
            if (magic != "BSA\0")
                throw new InvalidDataException($"Archive at {path} is not a BSA!");

            HeaderType = (VersionType) _br.ReadUInt32();
            if (HeaderType != VersionType.TES4)
                throw new NotSupportedException($"OMODFramework only supports TES4 BSAs not {HeaderType}");
            
            _folderRecordOffset = _br.ReadUInt32();
            ArchiveFlags = (ArchiveFlags) _br.ReadUInt32();
            _folderCount = _br.ReadUInt32();
            _fileCount = _br.ReadUInt32();
            _totalFolderNameLength = _br.ReadUInt32();
            _totalFileNameLength = _br.ReadUInt32();
            FileFlags = (FileFlags) _br.ReadUInt32();

            Folders = GetFolders(_br).ToList();
            Files = new List<BSAFileInfo>();
            
            foreach (var folder in Folders)
            {
                if (HasFolderNames)
                {
                    folder.Name = new string(_br.ReadChars(_br.ReadByte())[..^1]);
                }

                for (var i = 0; i < folder.FileCount; i++)
                {
                    Files.Add(new BSAFileInfo(_br, HasNameBlobs, folder));
                }
            }

            if (HasFileNames)
            {
                foreach (var fileInfo in Files)
                {
                    var sb = new StringBuilder();
                    char c;
                    while ((c = _br.ReadChar()) != '\0')
                        sb.Append(c);
                    fileInfo.Name = sb.ToString();
                }
            }
        }

        private IEnumerable<BSAFolderInfo> GetFolders(BinaryReader br)
        {
            br.BaseStream.Position = _folderRecordOffset;
            for (var i = 0; i < _folderCount; i++)
            {
                yield return new BSAFolderInfo(br);
            }
        }

        /// <summary>
        /// Copies to contents of a file in the BSA to a stream.
        /// </summary>
        /// <param name="fileInfo">File to copy.</param>
        /// <param name="outputStream">Output Stream to copy to, has to be seekable and writable.</param>
        /// <exception cref="ArgumentException">Thrown when the Stream is not seekable or writable.</exception>
        public void CopyFileTo(BSAFileInfo fileInfo, Stream outputStream)
        {
            if (!outputStream.CanWrite)
                throw new ArgumentException("Stream is not writable!", nameof(outputStream));
            if (!outputStream.CanSeek)
                throw new ArgumentException("Stream is not seekable!", nameof(outputStream));
            
            _br.BaseStream.Position = fileInfo.Offset;
            if (CompressedByDefault)
            {
                using var deflateStream = new DeflateStream(_br.BaseStream, CompressionMode.Decompress);
                deflateStream.CopyToLimit(outputStream, fileInfo.Size);
            }
            else
            {
                _br.BaseStream.CopyToLimit(outputStream, fileInfo.Size);
            }

            outputStream.Position = 0;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _br.Dispose();
        }
    }

    /// <summary>
    /// Represents a folder with files from an BSA archive.
    /// </summary>
    [PublicAPI]
    public class BSAFolderInfo
    {
        /// <summary>
        /// Name of the folder.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Hash of the folder.
        /// </summary>
        public readonly ulong Hash;
        
        /// <summary>
        /// Amount of files in the folder.
        /// </summary>
        public readonly int FileCount;

        internal BSAFolderInfo(BinaryReader br)
        {
            Name = string.Empty;

            Hash = br.ReadUInt64();
            FileCount = br.ReadInt32();
            br.ReadUInt32();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"\"{Name}\": {FileCount} files";
        }
    }

    /// <summary>
    /// Represents a file in an BSA archive.
    /// </summary>
    [PublicAPI]
    public class BSAFileInfo
    {
        /// <summary>
        /// Name of the file.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Hash of the file.
        /// </summary>
        public readonly ulong Hash;
        
        /// <summary>
        /// Size of the file.
        /// </summary>
        public readonly uint Size;
        
        /// <summary>
        /// Offset of the data in the BSA.
        /// </summary>
        public readonly uint Offset;

        /// <summary>
        /// The <see cref="BSAFolderInfo"/> that the file goes into.
        /// </summary>
        public readonly BSAFolderInfo FolderInfo;

        internal BSAFileInfo(BinaryReader br, bool hasNameBlobs, BSAFolderInfo folderInfo)
        {
            Name = string.Empty;
            FolderInfo = folderInfo;

            Hash = br.ReadUInt64();
            Size = br.ReadUInt32();
            Offset = br.ReadUInt32();

            if (hasNameBlobs)
                Size ^= 0x1 << 30;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"\"{Name}\": {Size} {Hash:x8}";
        }
    }
}
