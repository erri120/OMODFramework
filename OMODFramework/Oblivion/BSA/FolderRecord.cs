using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;

namespace OMODFramework.Oblivion.BSA
{
    [PublicAPI]
    public class FolderRecord : IArchiveFolder
    {
        internal readonly BSAReader BSA;
        private readonly ReadOnlyMemorySlice<byte> _data;
        private Lazy<FileRecord[]> _files = null!;
        private int _prevFileCount;
        internal FileNameBlock? FileNameBlock;
        public int Index { get; }
        public string? Path { get; private set; }

        public IReadOnlyCollection<IArchiveFile> Files => _files.Value;

        internal FolderRecord(BSAReader bsa, ReadOnlyMemorySlice<byte> data, int index)
        {
            BSA = bsa;
            _data = data;
            Index = index;
        }

        private bool IsLongForm => BSA.HeaderType == VersionType.SSE;

        public ulong Hash => BinaryPrimitives.ReadUInt64LittleEndian(_data);

        public int FileCount => checked((int)BinaryPrimitives.ReadUInt32LittleEndian(_data[0x8..]));

        public uint Unknown => IsLongForm ?
            BinaryPrimitives.ReadUInt32LittleEndian(_data[0xC..]) :
            0;

        public ulong Offset => IsLongForm ?
            BinaryPrimitives.ReadUInt64LittleEndian(_data[0x10..]) :
            BinaryPrimitives.ReadUInt32LittleEndian(_data[0xC..]);

        public static int HeaderLength(VersionType version)
        {
            return version switch
            {
                VersionType.SSE => 0x18,
                _ => 0x10,
            };
        }

        internal void ProcessFileRecordHeadersBlock(BinaryReader rdr, int fileCountTally)
        {
            _prevFileCount = fileCountTally;
            var totalFileLen = checked(FileCount * FileRecord.HeaderLength);

            ReadOnlyMemorySlice<byte> data;
            if (BSA.HasFolderNames)
            {
                var len = rdr.ReadByte();
                data = rdr.ReadBytes(len + totalFileLen);
                Path = data[..len].ReadStringTerm(BSA.HeaderType);
                data = data[len..];
            }
            else
            {
                data = rdr.ReadBytes(totalFileLen);
            }

            _files = new Lazy<FileRecord[]>(
                isThreadSafe: true,
                valueFactory: () => ParseFileRecords(data));
        }

        private FileRecord[] ParseFileRecords(ReadOnlyMemorySlice<byte> data)
        {
            var fileCount = FileCount;
            var ret = new FileRecord[fileCount];
            for (var idx = 0; idx < fileCount; idx += 1)
            {
                var fileData = data.Slice(idx * FileRecord.HeaderLength, FileRecord.HeaderLength);
                ret[idx] = new FileRecord(this, fileData, idx, idx + _prevFileCount, FileNameBlock);
            }
            return ret;
        }
    }
}
