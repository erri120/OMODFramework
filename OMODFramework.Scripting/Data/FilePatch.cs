using System;
using JetBrains.Annotations;
using OMODFramework.Compression;

namespace OMODFramework.Scripting.Data
{
    /// <summary>
    /// Represents a file patch.
    /// </summary>
    [PublicAPI]
    public class FilePatch : IEquatable<FilePatch>
    {
        /// <summary>
        /// The file to patch.
        /// </summary>
        public readonly string To;
        
        /// <summary>
        /// Plugin or Data file.
        /// </summary>
        public readonly bool IsPlugin;
        
        /// <summary>
        /// What <see cref="To"/> should be replaced with.
        /// </summary>
        public OMODCompressedFile From { get; internal set; }

        /// <summary>
        /// Whether to create or replace the file at <see cref="To"/>.
        /// </summary>
        public bool Create { get; internal set; }
        
        internal FilePatch(OMODCompressedFile from, string to, bool create, bool plugin)
        {
            From = from;
            To = to.MakePath();
            Create = create;
            IsPlugin = plugin;
        }

        /// <inheritdoc />
        public bool Equals(FilePatch? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(To, other.To, StringComparison.OrdinalIgnoreCase) && IsPlugin == other.IsPlugin;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((FilePatch) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(To, StringComparer.OrdinalIgnoreCase);
            hashCode.Add(IsPlugin);
            return hashCode.ToHashCode();
        }
    }
}
