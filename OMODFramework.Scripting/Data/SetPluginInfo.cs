using System;
using JetBrains.Annotations;

namespace OMODFramework.Scripting.Data
{
    /// <summary>
    /// Represents a binary edit to a plugin.
    /// </summary>
    [PublicAPI]
    public class SetPluginInfo : IEquatable<SetPluginInfo>
    {
        /// <summary>
        /// The plugin to edit.
        /// </summary>
        public readonly ScriptReturnFile PluginFile;

        /// <summary>
        /// The offset at which to change the data.
        /// </summary>
        public readonly long Offset;

        /// <summary>
        /// Type of <see cref="Value"/>.
        /// </summary>
        public Type ValueType { get; internal set; }
        
        /// <summary>
        /// Value to edit. Possible value types:
        /// <list type="bullet">
        /// <item><see cref="byte"/></item>
        /// <item><see cref="short"/></item>
        /// <item><see cref="int"/></item>
        /// <item><see cref="long"/></item>
        /// <item><see cref="float"/></item>
        /// </list>
        /// </summary>
        public object Value { get; internal set; }
        
        internal SetPluginInfo(long offset, ScriptReturnFile pluginFile, Type valueType, object value)
        {
            Offset = offset;
            PluginFile = pluginFile;
            ValueType = valueType;
            Value = value;
        }

        /// <inheritdoc />
        public bool Equals(SetPluginInfo? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return PluginFile.Equals(other.PluginFile) && Offset == other.Offset;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((SetPluginInfo) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(PluginFile, Offset);
        }
    }
}
