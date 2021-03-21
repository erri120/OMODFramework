using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace OMODFramework.Oblivion.BSA
{
    [PublicAPI]
    public class BSAReader : IArchiveReader
    {
        public const int HeaderLength = 0x24;

        private readonly uint _folderRecordOffset;
        private readonly Lazy<FolderRecord[]> _folders;
        private readonly Lazy<Dictionary<string, FolderRecord>> _foldersByName;
        private readonly string _magic;
        private readonly Func<Stream> _streamGetter;

        public uint FolderCount { get; }
        public uint FileCount { get; }
        public uint TotalFileNameLength { get; }
        public uint TotalFolderNameLength { get; }

        public VersionType HeaderType { get; private set; }

        public ArchiveFlags ArchiveFlags { get; private set; }

        public FileFlags FileFlags { get; private set; }

        public IEnumerable<IArchiveFile> Files => _folders.Value.SelectMany(f => f.Files);

        public IEnumerable<IArchiveFolder> Folders => _folders.Value;

        public bool HasFolderNames => ArchiveFlags.HasFlag(ArchiveFlags.HasFolderNames);

        public bool HasFileNames => ArchiveFlags.HasFlag(ArchiveFlags.HasFileNames);

        public bool CompressedByDefault => ArchiveFlags.HasFlag(ArchiveFlags.Compressed);

        public bool Bit9Set => ArchiveFlags.HasFlag(ArchiveFlags.HasFileNameBlobs);

        public bool HasNameBlobs
        {
            get
            {
                if (HeaderType == VersionType.FO3 || HeaderType == VersionType.SSE) return Bit9Set;
                return false;
            }
        }

        public BSAReader(string filename)
            : this(() => File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
        }

        public BSAReader(Func<Stream> streamGetter)
        {
            _streamGetter = streamGetter;
            using var rdr = GetStream();

            var fourcc = Encoding.ASCII.GetString(rdr.ReadBytes(4));

            if (fourcc != "BSA\0")
                throw new InvalidDataException("Archive is not a BSA");

            _magic = fourcc;
            HeaderType = (VersionType)rdr.ReadUInt32();
            _folderRecordOffset = rdr.ReadUInt32();
            ArchiveFlags = (ArchiveFlags)rdr.ReadUInt32();
            FolderCount = rdr.ReadUInt32();
            FileCount = rdr.ReadUInt32();
            TotalFolderNameLength = rdr.ReadUInt32();
            TotalFileNameLength = rdr.ReadUInt32();
            FileFlags = (FileFlags)rdr.ReadUInt32();

            _folders = new Lazy<FolderRecord[]>(
                isThreadSafe: true,
                valueFactory: LoadFolderRecords);
            _foldersByName = new Lazy<Dictionary<string, FolderRecord>>(
                isThreadSafe: true,
                valueFactory: GetFolderDictionary);
        }

        internal BinaryReader GetStream()
        {
            return new BinaryReader(_streamGetter());
        }

        private FolderRecord[] LoadFolderRecords()
        {
            using var rdr = GetStream();
            rdr.BaseStream.Position = _folderRecordOffset;
            var folderHeaderLength = FolderRecord.HeaderLength(HeaderType);
            ReadOnlyMemorySlice<byte> folderHeaderData = rdr.ReadBytes(checked((int)(folderHeaderLength * FolderCount)));

            var ret = new FolderRecord[FolderCount];
            for (var idx = 0; idx < FolderCount; idx += 1)
                ret[idx] = new FolderRecord(this, folderHeaderData.Slice(idx * folderHeaderLength, folderHeaderLength), idx);

            // Slice off appropriate file header data per folder
            var fileCountTally = 0;
            foreach (var folder in ret)
            {
                folder.ProcessFileRecordHeadersBlock(rdr, fileCountTally);
                fileCountTally = checked(fileCountTally + folder.FileCount);
            }

            if (HasFileNames)
            {
                var filenameBlock = new FileNameBlock(this, rdr.BaseStream.Position);
                foreach (var folder in ret)
                {
                    folder.FileNameBlock = filenameBlock;
                }
            }

            return ret;
        }

        private Dictionary<string, FolderRecord> GetFolderDictionary()
        {
            if (!HasFolderNames)
            {
                throw new ArgumentException("Cannot get folders by name if the BSA does not have folder names.");
            }

            return _folders.Value.ToDictionary(folder => folder.Path!);
        }

        public bool TryGetFolder(string path, [MaybeNullWhen(false)] out IArchiveFolder folder)
        {
            if (!HasFolderNames
                || !_foldersByName.Value.TryGetValue(path, out var folderRec))
            {
                folder = default;
                return false;
            }
            folder = folderRec;
            return true;
        }
    }
}
