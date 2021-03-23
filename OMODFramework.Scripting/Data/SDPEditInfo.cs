using System;
using JetBrains.Annotations;

namespace OMODFramework.Scripting.Data
{
    /// <summary>
    /// Represents an edit to an Oblivion Shader Package.
    /// </summary>
    [PublicAPI]
    public class SDPEditInfo : IEquatable<SDPEditInfo>
    {
        /// <summary>
        /// ID of the Package to change.
        /// </summary>
        public readonly byte Package;

        /// <summary>
        /// Name of the shader to edit.
        /// </summary>
        public readonly string Shader;

        /// <summary>
        /// File containing the new binary data that should replace the existing shader.
        /// </summary>
        public ScriptReturnFile File { get; set; }

        internal SDPEditInfo(byte package, string shader, ScriptReturnFile file)
        {
            Package = package;
            Shader = shader;
            File = file;
        }

        /// <inheritdoc />
        public bool Equals(SDPEditInfo? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Package == other.Package && string.Equals(Shader, other.Shader, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SDPEditInfo) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Package);
            hashCode.Add(Shader, StringComparer.OrdinalIgnoreCase);
            return hashCode.ToHashCode();
        }
    }
}
