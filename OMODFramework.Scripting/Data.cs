using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using JetBrains.Annotations;
using OblivionModManager.Scripting;

namespace OMODFramework.Scripting
{
    public class ScriptException : Exception
    {
        public ScriptException(string s) : base(s){ }
    }

    public class ScriptingNullListException : ScriptException
    {
        public ScriptingNullListException(bool isData = true) : base($"The {(isData ? "DataFiles" : "Plugins")} of the OMOD is null!") { }
    }

    public class ScriptingFatalErrorException : ScriptException
    {
        public ScriptingFatalErrorException() : base("Fatal Error was triggered in the script!") { }
    }

    public class ScriptingCanceledException : ScriptException
    {
        public ScriptingCanceledException() : base("Script execution was canceled!") { }
        public ScriptingCanceledException(string s) : base($"Script execution was canceled!\n{s}") { }
    }

    [PublicAPI]
    public enum DialogResult
    {
        Yes,
        No,
        Canceled
    }

    [PublicAPI]
    public struct ESP
    {
        public string Name { get; set; }
        public bool Active { get; set; }
    }

    [PublicAPI]
    public interface IScriptSettings
    {
        FrameworkSettings FrameworkSettings { get; }
        IScriptFunctions ScriptFunctions { get; }
    }

    [PublicAPI]
    public interface IScriptFunctions
    {
        //void Warn(string msg);

        void Message(string msg);

        void Message(string msg, string title);

        IEnumerable<int> Select(IEnumerable<string> items, string title, bool isMultiSelect, IEnumerable<Bitmap> previews,
            IEnumerable<string> descriptions);

        string InputString(string? title, string? initialText);

        DialogResult DialogYesNo(string title);

        DialogResult DialogYesNo(string title, string message);

        void DisplayImage(Bitmap image, string? title);

        void DisplayText(string text, string? title);

        void Patch(string from, string to);

        string ReadOblivionINI(string section, string name);

        string ReadRenderInfo(string name);

        bool DataFileExists(string file);

        bool HasScriptExtender();

        bool HasGraphicsExtender();

        Version ScriptExtenderVersion();

        Version GraphicsExtenderVersion();

        Version OblivionVersion();

        Version OBSEPluginVersion(string file);

        IEnumerable<ESP> GetESPs();

        IEnumerable<string> GetActiveOMODNames();

        byte[] ReadExistingDataFile(string file);

        byte[] GetDataFileFromBSA(string file);

        byte[] GetDataFileFromBSA(string bsa, string file);
    }

    [PublicAPI]
    public class ScriptReturnFile
    {
        public readonly OMODCompressedEntry OriginalFile;
        public string Output { get; }

        public ScriptReturnFile(OMODCompressedEntry entry)
        {
            OriginalFile = entry;
            Output = OriginalFile.Name;
        }

        public ScriptReturnFile(OMODCompressedEntry entry, string output)
        {
            OriginalFile = entry;
            Output = output;
        }

        public override string ToString()
        {
            return $"{OriginalFile.Name} to {Output}";
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is ScriptReturnFile file))
                return false;

            return file.OriginalFile.Equals(OriginalFile) && file.Output.Equals(Output, StringComparison.InvariantCultureIgnoreCase);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(OriginalFile, Output);
        }
    }

    [PublicAPI]
    public sealed class DataFile : ScriptReturnFile
    {
        public DataFile(OMODCompressedEntry entry) : base(entry) { }

        public DataFile(OMODCompressedEntry entry, string output) : base(entry, output) { }
    }

    [PublicAPI]
    public sealed class PluginFile : ScriptReturnFile
    {
        public bool IsUnchecked { get; set; }

        public DeactiveStatus? Warning { get; set; }
        public bool LoadEarly { get; set; }

        public List<PluginFile> LoadBefore { get; set; } = new List<PluginFile>();
        public List<PluginFile> LoadAfter { get; set; } = new List<PluginFile>();

        public PluginFile(OMODCompressedEntry entry) : base(entry) { }
        public PluginFile(OMODCompressedEntry entry, string output) : base(entry, output) { }
    }

    [PublicAPI]
    public enum ConflictType { Conflicts, Depends }

    [PublicAPI]
    public class ConflictData
    {
        public ConflictType Type { get; set; }
        public ConflictLevel Level { get; set; }
        public string File { get; set; } = string.Empty;
        
        public Version? MinVersion { get; set; }
        public Version? MaxVersion { get; set; }

        public string? Comment { get; set; }
        public bool Partial { get; set; }

        public override string ToString()
        {
            return $"{(Type == ConflictType.Conflicts ? "Conflicts with" : "Depends on")} {File}";
        }
    }

    [PublicAPI]
    public struct INIEditInfo
    {
        public readonly string Section;
        public readonly string Name;
        public readonly string NewValue;

        public INIEditInfo(string section, string name, string newValue)
        {
            Section = section;
            Name = name;
            NewValue = newValue;
        }

        public override string ToString()
        {
            return $"[{Section}]{Name}:{NewValue}";
        }
    }

    [PublicAPI]
    public struct SDPEditInfo
    {
        public readonly byte Package;
        public readonly string Shader;
        public readonly string BinaryObject;

        public SDPEditInfo(byte package, string shader, string binaryObject)
        {
            Package = package;
            Shader = shader;
            BinaryObject = binaryObject;
        }
    }

    [PublicAPI]
    public class ScriptReturnData
    {
        public HashSet<DataFile> DataFiles { get; set; } = new HashSet<DataFile>();
        public HashSet<PluginFile> PluginFiles { get; set; } = new HashSet<PluginFile>();

        public List<ConflictData> Conflicts { get; } = new List<ConflictData>();

        public HashSet<string> RegisteredBSAs { get; } = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        public List<INIEditInfo> INIEdits { get; } = new List<INIEditInfo>();
        public List<SDPEditInfo> SDPEditInfos { get; } = new List<SDPEditInfo>();

        public bool HasDataFiles => DataFiles.Any();
        public bool HasPlugins => PluginFiles.Any();
        public bool HasConflicts => Conflicts.Any();
        public bool HasRegisteredBSAs => RegisteredBSAs.Any();
        public bool HasINIEdits => INIEdits.Any();
        public bool HasSDPEdits => SDPEditInfos.Any();

        public override string ToString()
        {
            return $"Data Files: {DataFiles.Count}, Plugins: {PluginFiles.Count}";
        }
    }
}
