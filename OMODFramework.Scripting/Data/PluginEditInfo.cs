using System;
using JetBrains.Annotations;

namespace OMODFramework.Scripting.Data
{
    [PublicAPI]
    public class PluginEditInfo : IEquatable<PluginEditInfo>
    {
        public readonly bool IsGMST;

        public readonly ScriptReturnFile File;

        public readonly string EditorId;

        public string NewValue { get; set; }

        internal PluginEditInfo(string value, ScriptReturnFile file, string editorId, bool isGMST)
        {
            NewValue = value;
            File = file;
            EditorId = editorId;
            IsGMST = isGMST;
        }

        public bool Equals(PluginEditInfo? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return IsGMST == other.IsGMST && File.Equals(other.File) 
                                          && string.Equals(EditorId, other.EditorId, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((PluginEditInfo) obj);
        }

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
