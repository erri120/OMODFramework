using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;

namespace OMODFramework.Scripting
{
    public class ScriptException : Exception
    {
        protected ScriptException(string s) : base(s){ }
    }

    public class ScriptingNullListException : ScriptException
    {
        public ScriptingNullListException(bool isData = true) : base($"The {(isData ? "DataList" : "PluginsList")} of the OMOD is null!") { }
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
    public enum ScriptType : byte
    {
        OBMMScript,
        Python,
        CSharp,
        VB
    }

    [PublicAPI]
    public enum DialogResult
    {
        Yes,
        No,
        Canceled
    }

    [PublicAPI]
    public struct ScriptESP
    {
        public string Name { get; set; }
        public bool Active { get; set; }
    }

    [PublicAPI]
    public interface IScriptFunctions
    {
        void Warn(string msg);

        void Message(string msg);

        void Message(string msg, string title);

        IEnumerable<int> Select(IEnumerable<string> items, string title, bool isMultiSelect, IEnumerable<string> previews,
            IEnumerable<string> descriptions);

        string InputString(string? title, string? initialText);

        DialogResult DialogYesNo(string title);

        DialogResult DialogYesNo(string title, string message);

        void DisplayImage(FileInfo file, string title);

        void DisplayText(string text, string title);

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

        IEnumerable<ScriptESP> GetESPs();

        IEnumerable<string> GetActiveOMODNames();

        byte[] ReadExistingDataFile(string file);

        byte[] GetDataFileFromBSA(string file);

        byte[] GetDataFileFromBSA(string bsa, string file);
    }

    [PublicAPI]
    public interface IScriptSettings
    {
        FrameworkSettings FrameworkSettings { get; }
        IScriptFunctions ScriptFunctions { get; }
    }

    [PublicAPI]
    public class ScriptReturnFile
    {
        public readonly OMODCompressedEntry OriginalFile;
        public string Output { get; set; }

        public ScriptReturnFile(OMODCompressedEntry entry)
        {
            OriginalFile = entry;
            Output = OriginalFile.Name;
        }

        public override string ToString()
        {
            return $"{OriginalFile.Name} to {Output}";
        }
    }

    [PublicAPI]
    public sealed class DataFile : ScriptReturnFile
    {
        public DataFile(OMODCompressedEntry entry) : base(entry)
        {
        }
    }

    [PublicAPI]
    public sealed class PluginFile : ScriptReturnFile
    {
        public bool IsUnchecked { get; set; }

        public PluginFile(OMODCompressedEntry entry) : base(entry)
        {
        }
    }

    [PublicAPI]
    public class ScriptReturnData
    {
        public List<DataFile> DataFiles { get; set; } = new List<DataFile>();
        public List<PluginFile> PluginFiles { get; set; } = new List<PluginFile>();

        internal List<string> UnCheckedPlugins { get; set; } = new List<string>();

        public override string ToString()
        {
            return $"Data Files: {DataFiles.Count}, Plugins: {PluginFiles.Count}";
        }
    }
}
