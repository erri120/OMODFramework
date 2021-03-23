using System;
using System.IO;
using JetBrains.Annotations;

namespace OMODFramework.Compression
{
    /// <summary>
    /// Represents a compressed file in an OMOD.
    /// </summary>
    [PublicAPI]
    public sealed class OMODCompressedFile : IEquatable<OMODCompressedFile>
    {
        /// <summary>
        /// Relative path of the file.
        /// </summary>
        public readonly string Name;
        
        /// <summary>
        /// CRC32 hash of the file.
        /// </summary>
        public readonly uint CRC;
        
        /// <summary>
        /// File length.
        /// </summary>
        public readonly long Length;
        
        /// <summary>
        /// Offset of the file in the decompressed data stream.
        /// </summary>
        public readonly long Offset;

        /// <summary>
        /// Initializes a new instance of the <see cref="OMODCompressedFile"/> class.
        /// </summary>
        /// <param name="name"><see cref="Name"/></param>
        /// <param name="crc"><see cref="CRC"/></param>
        /// <param name="length"><see cref="Length"/></param>
        /// <param name="offset"><see cref="Offset"/></param>
        public OMODCompressedFile(string name, uint crc, long length, long offset)
        {
            Name = name.MakePath();
            CRC = crc;
            Length = length;

            Offset = offset;
        }

        internal string GetFileInFolder(string folder) => Path.Combine(folder, Name);
        
        /// <inheritdoc />
        public bool Equals(OMODCompressedFile? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) && CRC == other.CRC;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((OMODCompressedFile) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Name, StringComparer.OrdinalIgnoreCase);
            hashCode.Add(CRC);
            return hashCode.ToHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Name} ({CRC:X})";
        }
    }
}
