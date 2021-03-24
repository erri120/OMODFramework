using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace OMODFramework.Oblivion.BSA
{
    [PublicAPI]
    internal sealed class BSACreator
    {
        private readonly Dictionary<string, FolderRecord> Folders = new Dictionary<string, FolderRecord>(StringComparer.OrdinalIgnoreCase);
        private List<FolderRecord> FolderList = new List<FolderRecord>();
        private readonly List<FileRecord> Files = new List<FileRecord>();

        private uint TotalFolderNameLength => GetTotalFolderNameLength();
        private uint TotalFileNameLength => GetTotalFileNameLength();

        public void WriteToFile(string outputFile)
        {
            using var fs = File.Open(outputFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            using var bw = new BinaryWriter(fs, Encoding.UTF8);

            const VersionType headerType = VersionType.TES4;
            //TODO: compression
            var archiveFlags = ArchiveFlags.HasFileNames |
                               ArchiveFlags.HasFolderNames |
                               ArchiveFlags.Unk10 | ArchiveFlags.Unk11;
            var fileFlags = GetFileFlags();
            
            bw.Write(Encoding.ASCII.GetBytes("BSA\0"));
            bw.Write((uint) headerType);
            bw.Write(0x24);
            bw.Write((uint) archiveFlags);
            bw.Write((uint) Folders.Count);
            bw.Write((uint) Files.Count);
            bw.Write(TotalFolderNameLength);
            bw.Write(TotalFileNameLength);
            bw.Write((uint) fileFlags);

            FolderList = Folders.Select(x => x.Value).ToList();
            for (var i = 0; i < FolderList.Count; i++)
            {
                var folder = FolderList[i];
                WriteFolderRecord(bw, folder, i);
            }

            foreach (var folderRecord in FolderList)
            {
                bw.Write(folderRecord.NameBytes);
                
                foreach (var file in folderRecord.Files)
                {
                    WriteFileRecord(bw, file);
                }
            }

            foreach (var file in Files)
            {
                bw.Write(file.NameBytes);
            }

            foreach (var file in Files)
            {
                WriteFileData(bw, file);
            }
        }

        private void WriteFolderRecord(BinaryWriter bw, FolderRecord folderRecord, int index)
        {
            var offset = (uint) bw.BaseStream.Position;
            offset += (uint) FolderList.Skip(index).Select(x => (long) FolderRecord.SelfSize).Sum();
            offset += TotalFileNameLength;
            offset += (uint) FolderList.Take(index).Select(x => (long) x.FileRecordSize).Sum();
            
            bw.Write(folderRecord.Hash);
            bw.Write(folderRecord.Files.Count);
            bw.Write(offset);
        }

        private void WriteFileRecord(BinaryWriter bw, FileRecord fileRecord)
        {
            bw.Write(fileRecord.Hash);
            //TODO: adjust size for compression
            bw.Write(fileRecord.Size);
            fileRecord.OffsetOffset = bw.BaseStream.Position;
            bw.Write(0xDEADBEEF);
        }

        private void WriteFileData(BinaryWriter bw, FileRecord fileRecord)
        {
            var offset = (uint) bw.BaseStream.Position;
            bw.BaseStream.Position = fileRecord.OffsetOffset;
            bw.Write(offset);
            bw.BaseStream.Position = offset;
            
            //TODO: compression
            using var stream = fileRecord.DataStream;
            stream.CopyToLimit(bw.BaseStream, stream.Length);
        }
        
        private FileFlags GetFileFlags()
        {
            var extensions = Files
                .Select(x => x.OutputPath)
                .Select(Path.GetExtension)
                .Distinct();

            var result = (FileFlags) 0;
            foreach (var extension in extensions)
            {
                switch (extension)
                {
                    case ".nif":
                        result |= FileFlags.Meshes;
                        break;
                    case ".dds":
                        result |= FileFlags.Textures;
                        break;
                    case ".xml":
                        result |= FileFlags.Menus;
                        break;
                    case ".wav":
                        result |= FileFlags.Sounds;
                        break;
                    case ".mp3":
                        result |= FileFlags.Voices;
                        break;
                    case ".txt":
                    case ".html":
                    case ".htm":
                    case ".bat":
                    case ".scc":
                        result |= FileFlags.Menus;
                        break;
                    case ".spt":
                        result |= FileFlags.Trees;
                        break;
                    case ".tex":
                    case ".fon":
                        result |= FileFlags.Fonts;
                        break;
                    default:
                        result |= FileFlags.Miscellaneous;
                        break;
                }
            }

            return result;
        }
        
        private uint GetTotalFolderNameLength()
        {
            return (uint) Folders
                .Select(x => x.Value)
                .Select(x => x.Name.Length + 1)
                .Sum();
        }

        private uint GetTotalFileNameLength()
        {
            return (uint) Files
                .Select(x => x.OutputPath.Length + 1)
                .Sum();
        }
        
        public void AddFile(string inputPath, string outputPath)
        {
            if (!File.Exists(inputPath))
                throw new ArgumentException("File does not exist!", nameof(inputPath));
            
            var input = inputPath.MakePath();
            var output = outputPath.MakePath();

            var outputFolder = Path.GetDirectoryName(output);
            if (outputFolder == null)
                throw new ArgumentException("Files in BSAs must have a parent-folder!", nameof(outputPath));

            if (!Folders.TryGetValue(outputFolder, out var folderRecord))
            {
                folderRecord = new FolderRecord(outputFolder);
                Folders.Add(outputFolder, folderRecord);
            }

            var outputFileName = Path.GetFileName(output);
            var fileRecord = new FileRecord(input, outputFileName, folderRecord);
            folderRecord.AddFile(fileRecord);
            Files.Add(fileRecord);
        }

        internal static ulong GetBSAHash(string name, string extension)
        {
            name = name.ToLowerInvariant();
            extension = extension.ToLowerInvariant();

            if (string.IsNullOrEmpty(name))
                return 0;

            var hashBytes = new[]
            {
                (byte) (name.Length == 0 ? '\0' : name[^1]),
                (byte) (name.Length < 3 ? '\0' : name[^2]),
                (byte) name.Length,
                (byte) name[0]
            };
            var hash1 = BitConverter.ToUInt32(hashBytes, 0);
            switch (extension)
            {
                case ".kf":
                    hash1 |= 0x80;
                    break;
                case ".nif":
                    hash1 |= 0x8000;
                    break;
                case ".dds":
                    hash1 |= 0x8080;
                    break;
                case ".wav":
                    hash1 |= 0x80000000;
                    break;
            }

            uint hash2 = 0;
            for (var i = 1; i < name.Length - 2; i++) hash2 = hash2 * 0x1003f + (byte) name[i];

            var hash3 = extension.Aggregate<char, uint>(0, (current, t) => current * 0x1003f + (byte) t);
            return ((ulong) (hash2 + hash3) << 32) + hash1;
        }
        
        [PublicAPI]
        private class FolderRecord
        {
            public readonly string Name;
            public readonly byte[] NameBytes;
            
            public readonly List<FileRecord> Files = new List<FileRecord>();

            public ulong Hash => GetBSAHash(Name, "");

            public static uint SelfSize => sizeof(ulong) + sizeof(uint) + sizeof(uint);

            public ulong FileRecordSize
            {
                get
                {
                    var size = 0UL;
                    size += (ulong) NameBytes.Length;
                    size += (ulong) Files.Select(_ => sizeof(ulong) + sizeof(uint) + sizeof(uint)).Sum();
                    return size;
                }
            }

            public FolderRecord(string name)
            {
                Name = name;

                var bytes = new byte[Name.Length + 2];
                var nameBytes = Encoding.UTF7.GetBytes(name);
                nameBytes.CopyTo(bytes, 1);
                bytes[0] = (byte) (nameBytes.Length + 1);
                NameBytes = bytes;
            }

            public void AddFile(FileRecord fileRecord)
            {
                Files.Add(fileRecord);
            }
        }

        [PublicAPI]
        private class FileRecord
        {
            public readonly string InputPath;
            public readonly string OutputPath;
            public readonly FolderRecord Folder;
            public readonly byte[] NameBytes;

            public uint Size
            {
                get
                {
                    var fi = new FileInfo(InputPath);
                    return (uint) fi.Length;
                }
            }

            public ulong Hash => GetBSAHash(Path.GetFileNameWithoutExtension(InputPath), Path.GetExtension(InputPath));

            internal long OffsetOffset = -1;

            internal Stream DataStream => File.Open(InputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            
            public FileRecord(string input, string output, FolderRecord folder)
            {
                InputPath = input;
                OutputPath = output;
                Folder = folder;

                var bytes = new byte[output.Length + 1];
                var nameBytes = Encoding.UTF7.GetBytes(output);
                nameBytes.CopyTo(bytes, 0);
                NameBytes = bytes;
            }
        }
    }
}
