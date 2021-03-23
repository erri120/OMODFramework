using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using K4os.Compression.LZ4.Streams;
using SharpCompress.Compressors;
using SharpCompress.Compressors.Deflate;

namespace OMODFramework.Oblivion.BSA
{
    [PublicAPI]
    internal class FileRecord : IArchiveFile
    {
        public const int HeaderLength = 0x10;

        private readonly ReadOnlyMemorySlice<byte> _headerData;
        private readonly int _index;
        private readonly int _overallIndex;
        private readonly FileNameBlock? _nameBlock;
        private readonly Lazy<string?> _name;
        private Lazy<(uint Size, uint OnDisk, uint Original)> _size;

        public ulong Hash => BinaryPrimitives.ReadUInt64LittleEndian(_headerData);
        protected uint RawSize => BinaryPrimitives.ReadUInt32LittleEndian(_headerData[0x8..]);
        public uint Offset => BinaryPrimitives.ReadUInt32LittleEndian(_headerData[0xC..]);
        public string? Name => _name.Value;
        public uint Size => _size.Value.Size;

        public bool FlipCompression => (RawSize & (0x1 << 30)) > 0;

        internal FolderRecord Folder { get; }
        internal BSAReader BSA => Folder.BSA;

        internal FileRecord(
            FolderRecord folderRecord,
            ReadOnlyMemorySlice<byte> data,
            int index,
            int overallIndex,
            FileNameBlock? nameBlock)
        {
            _index = index;
            _overallIndex = overallIndex;
            _headerData = data;
            _nameBlock = nameBlock;
            Folder = folderRecord;
            _name = new Lazy<string?>(GetName, System.Threading.LazyThreadSafetyMode.PublicationOnly);

            // Will be replaced if CopyDataTo is called before value is created
            _size = new Lazy<(uint Size, uint OnDisk, uint Original)>(
                mode: System.Threading.LazyThreadSafetyMode.ExecutionAndPublication,
                valueFactory: () =>
                {
                    using var rdr = BSA.GetStream();
                    rdr.BaseStream.Position = Offset;
                    return ReadSize(rdr);
                });
        }

        public string Path
        {
            get
            {
                if (Name == null) return string.Empty;
                return string.IsNullOrEmpty(Folder.Path) ? Name : System.IO.Path.Combine(Folder.Path, Name);
            }
        }

        public bool Compressed
        {
            get
            {
                if (FlipCompression) return !BSA.CompressedByDefault;
                return BSA.CompressedByDefault;
            }
        }

        public void CopyDataTo(Stream output)
        {
            using var rdr = BSA.GetStream();
            rdr.BaseStream.Position = Offset;

            var size = ReadSize(rdr);
            if (!_size.IsValueCreated)
            {
                _size = new Lazy<(uint Size, uint OnDisk, uint Original)>(size);
            }

            if (BSA.HeaderType == VersionType.SSE)
            {
                if (Compressed && size.Size != size.OnDisk)
                {
                    using var r = LZ4Stream.Decode(rdr.BaseStream);
                    r.CopyToLimit(output, size.Original);
                }
                else
                {
                    rdr.BaseStream.CopyToLimit(output, size.OnDisk);
                }
            }
            else
            {
                if (Compressed)
                {
                    //using var z = new InflaterInputStream(rdr.BaseStream);
                    using var z = new DeflateStream(rdr.BaseStream, CompressionMode.Decompress);
                    z.CopyToLimit(output, size.Original);
                }
                else
                {
                    rdr.BaseStream.CopyToLimit(output, size.OnDisk);
                }
            }
        }

        public async ValueTask CopyDataToAsync(Stream output)
        {
            using var rdr = BSA.GetStream();
            rdr.BaseStream.Position = Offset;

            var size = ReadSize(rdr);
            if (!_size.IsValueCreated)
            {
                _size = new Lazy<(uint Size, uint OnDisk, uint Original)>(size);
            }

            if (BSA.HeaderType == VersionType.SSE)
            {
                if (Compressed && size.Size != size.OnDisk)
                {
                    await using var r = LZ4Stream.Decode(rdr.BaseStream);
                    await r.CopyToLimitAsync(output, size.Original).ConfigureAwait(false);
                }
                else
                {
                    await rdr.BaseStream.CopyToLimitAsync(output, size.OnDisk).ConfigureAwait(false);
                }
            }
            else
            {
                if (Compressed)
                {
                    //await using var z = new InflaterInputStream(rdr.BaseStream);
                    await using var z = new DeflateStream(rdr.BaseStream, CompressionMode.Decompress);
                    await z.CopyToLimitAsync(output, size.Original).ConfigureAwait(false);
                }
                else
                {
                    await rdr.BaseStream.CopyToLimitAsync(output, size.OnDisk).ConfigureAwait(false);
                }
            }
        }

        private string? GetName()
        {
            if (_nameBlock == null) return null;
            var names = _nameBlock.Names.Value;
            return names[_overallIndex].ReadStringTerm(BSA.HeaderType);
        }

        private (uint Size, uint OnDisk, uint Original) ReadSize(BinaryReader rdr)
        {
            var size = RawSize;
            if (FlipCompression)
                size = size ^ (0x1 << 30);

            if (Compressed)
                size -= 4;

            byte nameBlobOffset;
            if (BSA.HasNameBlobs)
            {
                nameBlobOffset = rdr.ReadByte();
                // Just skip, not using
                rdr.BaseStream.Position += nameBlobOffset;
                // Minus one more for the size of the name blob offset size
                nameBlobOffset++;
            }
            else
            {
                nameBlobOffset = 0;
            }

            var originalSize = Compressed ? rdr.ReadUInt32() : 0;

            var onDiskSize = size - nameBlobOffset;
            return Compressed 
                ? (Size: originalSize, OnDisk: onDiskSize, Original: originalSize) 
                : (Size: onDiskSize, OnDisk: onDiskSize, Original: originalSize);
        }
    }
}
