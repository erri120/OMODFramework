using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using OblivionModManager.Scripting;
using OMODFramework.Compression;

namespace OMODFramework.Scripting.Data
{
    /// <summary>
    /// Represents a file from the OMOD that should be installed after script execution.
    /// </summary>
    [PublicAPI]
    public class ScriptReturnFile
    {
        /// <summary>
        /// File from the OMOD to install.
        /// </summary>
        public OMODCompressedFile Input { get; internal set; }
        
        /// <summary>
        /// Relative output path.
        /// </summary>
        public string Output { get; internal set; }

        internal ScriptReturnFile(OMODCompressedFile compressedFile)
        {
            Input = compressedFile;
            Output = compressedFile.Name;
        }

        internal ScriptReturnFile(OMODCompressedFile compressedFile, string output)
        {
            Input = compressedFile;
            Output = output.MakePath();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Input.Name} -> {Output}";
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (!(obj is ScriptReturnFile scriptReturnFile)) return false;
            return ScriptReturnFileEqualityComparer.Instance.Equals(this, scriptReturnFile);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ScriptReturnFileEqualityComparer.Instance.GetHashCode(this);
        }
    }

    /// <summary>
    /// <see cref="EqualityComparer{T}"/> for <see cref="ScriptReturnFile"/>.
    /// </summary>
    [PublicAPI]
    public class ScriptReturnFileEqualityComparer : EqualityComparer<ScriptReturnFile>
    {
        /// <summary>
        /// Returns the instance of <see cref="ScriptReturnFileEqualityComparer"/>.
        /// </summary>
        public static readonly ScriptReturnFileEqualityComparer Instance = new ScriptReturnFileEqualityComparer();

        /// <inheritdoc />
        public override bool Equals(ScriptReturnFile? x, ScriptReturnFile? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return x.Output.Equals(y.Output, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public override int GetHashCode(ScriptReturnFile obj)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Output);
        }
    }

    /// <summary>
    /// Represents a data file that should be installed after script execution.
    /// </summary>
    [PublicAPI]
    public sealed class DataFile : ScriptReturnFile
    {
        internal DataFile(OMODCompressedFile compressedFile) : base(compressedFile) { }

        internal DataFile(OMODCompressedFile compressedFile, string output) : base(compressedFile, output) { }
    }

    /// <summary>
    /// Represents a plugin file that should be installed after script execution.
    /// </summary>
    [PublicAPI]
    public sealed class PluginFile : ScriptReturnFile
    {
        /// <summary>
        /// Whether the plugin is disabled in the load order or not.
        /// </summary>
        public bool IsUnchecked { get; internal set; }
        
        /// <summary>
        /// Warning to be shown when disabling the plugin in the load order.
        /// </summary>
        public DeactiveStatus? Warning { get; internal set; }
        
        /// <summary>
        /// Whether to load the plugin early or not.
        /// </summary>
        public bool LoadEarly { get; internal set; }
        
        /// <summary>
        /// Other plugins that should be loaded after the current one.
        /// </summary>
        public List<PluginFile> LoadBefore { get; internal set; } = new List<PluginFile>();
        
        /// <summary>
        /// Other plugins that should be loaded before the current one.
        /// </summary>
        public List<PluginFile> LoadAfter { get; internal set; } = new List<PluginFile>();
        
        internal PluginFile(OMODCompressedFile compressedFile) : base(compressedFile) { }

        internal PluginFile(OMODCompressedFile compressedFile, string output) : base(compressedFile, output) { }
    }
}
