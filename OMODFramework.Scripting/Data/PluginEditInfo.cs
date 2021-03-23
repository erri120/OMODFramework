using System;
using JetBrains.Annotations;

namespace OMODFramework.Scripting.Data
{
    /// <summary>
    /// Represents an edit of a plugin file.
    /// </summary>
    [PublicAPI]
    public class PluginEditInfo : IEquatable<PluginEditInfo>
    {
        /// <summary>
        /// Whether the field is of type GMST or Global.
        /// </summary>
        public readonly bool IsGMST;

        /// <summary>
        /// Plugin file to edit.
        /// </summary>
        public readonly ScriptReturnFile File;

        /// <summary>
        /// EDID of the record to change.
        /// </summary>
        public readonly string EditorId;

        /// <summary>
        /// New value of the record.
        /// </summary>
        public string NewValue { get; set; }

        internal PluginEditInfo(string value, ScriptReturnFile file, string editorId, bool isGMST)
        {
            NewValue = value;
            File = file;
            EditorId = editorId;
            IsGMST = isGMST;
        }

        /// <inheritdoc />
        public bool Equals(PluginEditInfo? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return IsGMST == other.IsGMST && File.Equals(other.File) 
                                          && string.Equals(EditorId, other.EditorId, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((PluginEditInfo) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(IsGMST);
            hashCode.Add(File);
            hashCode.Add(EditorId, StringComparer.OrdinalIgnoreCase);
            return hashCode.ToHashCode();
        }
    }
}
