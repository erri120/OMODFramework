using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using OblivionModManager.Scripting;
using OMODFramework.Compression;

namespace OMODFramework.Scripting.Data
{
    [PublicAPI]
    public class ScriptReturnFile
    {
        public OMODCompressedFile Input { get; internal set; }
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

        public override string ToString()
        {
            return $"{Input.Name} -> {Output}";
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is ScriptReturnFile scriptReturnFile)) return false;
            return ScriptReturnFileEqualityComparer.Instance.Equals(this, scriptReturnFile);
        }

        public override int GetHashCode()
        {
            return ScriptReturnFileEqualityComparer.Instance.GetHashCode(this);
        }
    }

    [PublicAPI]
    public class ScriptReturnFileEqualityComparer : EqualityComparer<ScriptReturnFile>
    {
        public static readonly ScriptReturnFileEqualityComparer Instance = new ScriptReturnFileEqualityComparer();
        
        public override bool Equals(ScriptReturnFile? x, ScriptReturnFile? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return x.Output.Equals(y.Output, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode(ScriptReturnFile obj)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Output);
        }
    }

    [PublicAPI]
    public sealed class DataFile : ScriptReturnFile
    {
        internal DataFile(OMODCompressedFile compressedFile) : base(compressedFile) { }

        internal DataFile(OMODCompressedFile compressedFile, string output) : base(compressedFile, output) { }
    }

    [PublicAPI]
    public sealed class PluginFile : ScriptReturnFile
    {
        public bool IsUnchecked { get; internal set; }
        
        public DeactiveStatus? Warning { get; internal set; }
        
        public bool LoadEarly { get; internal set; }
        
        public List<PluginFile> LoadBefore { get; internal set; } = new List<PluginFile>();
        
        public List<PluginFile> LoadAfter { get; internal set; } = new List<PluginFile>();
        
        internal PluginFile(OMODCompressedFile compressedFile) : base(compressedFile) { }

        internal PluginFile(OMODCompressedFile compressedFile, string output) : base(compressedFile, output) { }
    }
}
