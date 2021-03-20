using System;
using JetBrains.Annotations;
using OMODFramework.Compression;

namespace OMODFramework.Scripting.Data
{
    [PublicAPI]
    public class SDPEditInfo : IEquatable<SDPEditInfo>
    {
        public readonly byte Package;

        public readonly string Shader;

        public OMODCompressedFile File { get; set; }

        internal SDPEditInfo(byte package, string shader, OMODCompressedFile file)
        {
            Package = package;
            Shader = shader;
            File = file;
        }

        public bool Equals(SDPEditInfo? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Package == other.Package && string.Equals(Shader, other.Shader, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SDPEditInfo) obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Package);
            hashCode.Add(Shader, StringComparer.OrdinalIgnoreCase);
            return hashCode.ToHashCode();
        }
    }
}
