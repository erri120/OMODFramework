using System;
using JetBrains.Annotations;

namespace OMODFramework.Scripting.Data
{
    [PublicAPI]
    public class SetPluginInfo : IEquatable<SetPluginInfo>
    {
        public readonly ScriptReturnFile PluginFile;

        public readonly long Offset;

        public Type ValueType { get; internal set; }
        
        public object Value { get; internal set; }
        
        internal SetPluginInfo(long offset, ScriptReturnFile pluginFile, Type valueType, object value)
        {
            Offset = offset;
            PluginFile = pluginFile;
            ValueType = valueType;
            Value = value;
        }

        public bool Equals(SetPluginInfo? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return PluginFile.Equals(other.PluginFile) && Offset == other.Offset;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((SetPluginInfo) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PluginFile, Offset);
        }
    }
}
