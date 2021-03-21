using System;
using JetBrains.Annotations;
using OMODFramework.Compression;

namespace OMODFramework.Scripting.Data
{
    [PublicAPI]
    public class FilePatch : IEquatable<FilePatch>
    {
        public readonly string To;
        
        public readonly bool IsPlugin;
        
        public OMODCompressedFile From { get; internal set; }

        public bool Create { get; internal set; }
        
        internal FilePatch(OMODCompressedFile from, string to, bool create, bool plugin)
        {
            From = from;
            To = to;
            Create = create;
            IsPlugin = plugin;
        }

        public bool Equals(FilePatch? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(To, other.To, StringComparison.OrdinalIgnoreCase) && IsPlugin == other.IsPlugin;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((FilePatch) obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(To, StringComparer.OrdinalIgnoreCase);
            hashCode.Add(IsPlugin);
            return hashCode.ToHashCode();
        }
    }
}
