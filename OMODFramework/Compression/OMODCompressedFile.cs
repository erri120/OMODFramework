using System;
using System.IO;
using JetBrains.Annotations;

namespace OMODFramework.Compression
{
    [PublicAPI]
    public sealed class OMODCompressedFile : IEquatable<OMODCompressedFile>
    {
        public readonly string Name;
        public readonly uint CRC;
        public readonly long Length;
        
        public readonly long Offset;

        public OMODCompressedFile(string name, uint crc, long length, long offset)
        {
            Name = name.MakePath();
            CRC = crc;
            Length = length;

            Offset = offset;
        }

        internal string GetFileInFolder(string folder) => Path.Combine(folder, Name);
        
        public bool Equals(OMODCompressedFile? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) && CRC == other.CRC;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((OMODCompressedFile) obj);
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
            return $"{Name} ({CRC:X})";
        }
    }
}
