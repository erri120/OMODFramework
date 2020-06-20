using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using JetBrains.Annotations;
using OblivionModManager.Scripting;

namespace OMODFramework.Scripting
{
    /// <summary>
    /// Thrown during script execution
    /// </summary>
    public class ScriptException : Exception
    {
        public ScriptException(string s) : base(s){ }
    }

    /// <summary>
    /// Thrown when FatalError was triggered
    /// </summary>
    public class ScriptingFatalErrorException : ScriptException
    {
        public ScriptingFatalErrorException() : base("Fatal Error was triggered in the script!") { }
    }

    /// <summary>
    /// Thrown when script execution was canceled
    /// </summary>
    public class ScriptingCanceledException : ScriptException
    {
        public ScriptingCanceledException() : base("Script execution was canceled!") { }
        public ScriptingCanceledException(string s) : base($"Script execution was canceled!\n{s}") { }
    }

    /// <summary>
    /// Results for Dialogs
    /// </summary>
    [PublicAPI]
    public enum DialogResult
    {
        /// <summary>
        /// Yes
        /// </summary>
        Yes,
        /// <summary>
        /// No
        /// </summary>
        No,
        /// <summary>
        /// Canceled, this will throw <see cref="ScriptingCanceledException"/>
        /// </summary>
        Canceled
    }

    /// <summary>
    /// ESP/ESM/Plugin struct
    /// </summary>
    [PublicAPI]
    public struct ESP
    {
        /// <summary>
        /// Name of the plugin without extension
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Whether the plugin is active or not
        /// </summary>
        public bool Active { get; set; }
    }

    /// <summary>
    /// Settings for Script Execution
    /// </summary>
    [PublicAPI]
    public class ScriptSettings
    {
        /// <summary>
        /// Framework Settings
        /// </summary>
        public readonly FrameworkSettings FrameworkSettings;
        /// <summary>
        /// Script Functions
        /// </summary>
        public readonly IScriptFunctions ScriptFunctions;

        public ScriptSettings(IScriptFunctions scriptFunctions, FrameworkSettings? frameworkSettings)
        {
            ScriptFunctions = scriptFunctions;
            FrameworkSettings = frameworkSettings ?? FrameworkSettings.DefaultFrameworkSettings;
        }
    }

    /// <summary>
    /// Scripts Functions that you have to implement
    /// </summary>
    [PublicAPI]
    public interface IScriptFunctions
    {
        //void Warn(string msg);

        /// <summary>
        /// Message for the user
        /// </summary>
        /// <param name="msg">Message to be displayed</param>
        void Message(string msg);

        /// <summary>
        /// Message for the user
        /// </summary>
        /// <param name="msg">Message to be displayed</param>
        /// <param name="title">Title of the window</param>
        void Message(string msg, string title);

        /// <summary>
        /// Let the user select one or multiple items from an enumerable of items.
        /// </summary>
        /// <param name="items">Enumerable of all items to select from</param>
        /// <param name="title">Title of the window</param>
        /// <param name="isMultiSelect">Whether the user can select one or multiple items</param>
        /// <param name="previews">Enumerable containing <see cref="Bitmap"/> previews. Can be empty! Preview for items[i] is previews[i].</param>
        /// <param name="descriptions">Enumerable containing descriptions of the items. Can be empty! Description for items[i] is descriptions[i]</param>
        /// <returns>Enumerable with the indices of the selected items.</returns>
        IEnumerable<int> Select(IEnumerable<string> items, string title, bool isMultiSelect, IEnumerable<Bitmap> previews,
            IEnumerable<string> descriptions);

        /// <summary>
        /// Let the user input a string
        /// </summary>
        /// <param name="title">Title of the window (can be null)</param>
        /// <param name="initialText">Initial text (can be null)</param>
        /// <returns></returns>
        string InputString(string? title, string? initialText);

        /// <summary>
        /// Yes, No dialog prompt
        /// </summary>
        /// <param name="title">Title of the window</param>
        /// <returns></returns>
        DialogResult DialogYesNo(string title);

        /// <summary>
        /// Yes, No dialog prompt
        /// </summary>
        /// <param name="title">Title of the window</param>
        /// <param name="message">Message ot be displayewd</param>
        /// <returns></returns>
        DialogResult DialogYesNo(string title, string message);

        /// <summary>
        /// Display an image
        /// </summary>
        /// <param name="image"><see cref="Bitmap"/> of the image. Make sure you dispose of it!</param>
        /// <param name="title">Title of the window</param>
        void DisplayImage(Bitmap image, string? title);

        /// <summary>
        /// Display text to the user
        /// </summary>
        /// <param name="text">Text to be displayed</param>
        /// <param name="title">Title of the window (can be null)</param>
        void DisplayText(string text, string? title);

        void Patch(string from, string to);

        string ReadOblivionINI(string section, string name);

        string ReadRenderInfo(string name);

        bool DataFileExists(string file);

        /// <summary>
        /// Check if the Oblivion Script Extender is installed or not
        /// </summary>
        /// <returns></returns>
        bool HasScriptExtender();

        /// <summary>
        /// Check if the Oblivion Graphics Extender is installed or not
        /// </summary>
        /// <returns></returns>
        bool HasGraphicsExtender();

        /// <summary>
        /// Get the <see cref="Version"/> of the Oblivion Script Extender
        /// </summary>
        /// <returns></returns>
        Version ScriptExtenderVersion();

        /// <summary>
        /// Get the <see cref="Version"/> of the Oblivion Graphics Extender
        /// </summary>
        /// <returns></returns>
        Version GraphicsExtenderVersion();

        /// <summary>
        /// Get the <see cref="Version"/> of the Oblivion executable
        /// </summary>
        /// <returns></returns>
        Version OblivionVersion();

        /// <summary>
        /// Get the <see cref="Version"/> of an Oblivion Script Extender Plugin inside the
        /// <c>data\obse\plugin</c> folder.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        Version OBSEPluginVersion(string file);

        /// <summary>
        /// Get an enumerable of all Plugins, active and non-active
        /// </summary>
        /// <returns></returns>
        IEnumerable<ESP> GetESPs();

        /// <summary>
        /// Get an enumerable of the names of all active OMODs
        /// </summary>
        /// <returns></returns>
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

    /// <summary>
    /// Conflict Types for <see cref="ConflictData"/>
    /// </summary>
    [PublicAPI]
    public enum ConflictType
    {
        /// <summary>
        /// Conflict with another file
        /// </summary>
        Conflicts, 
        /// <summary>
        /// Depends on another file
        /// </summary>
        Depends
    }

    /// <summary>
    /// Conflict Data class
    /// </summary>
    [PublicAPI]
    public class ConflictData
    {
        /// <summary>
        /// Type of Conflict
        /// </summary>
        public ConflictType Type { get; set; }
        /// <summary>
        /// Level of the Conflict
        /// </summary>
        public ConflictLevel Level { get; set; }
        /// <summary>
        /// File that teh current OMOD conflicts with/depends on
        /// </summary>
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

    /// <summary>
    /// INI Edit Info
    /// </summary>
    [PublicAPI]
    public struct INIEditInfo
    {
        /// <summary>
        /// Section in the INI
        /// </summary>
        public readonly string Section;
        /// <summary>
        /// Name of the variable
        /// </summary>
        public readonly string Name;
        /// <summary>
        /// New Value
        /// </summary>
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

    /// <summary>
    /// Shader package edits
    /// </summary>
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

    /// <summary>
    /// Script Return Data. This class holds information about everything that
    /// gets returned and modified during script exeuction.
    /// </summary>
    [PublicAPI]
    public class ScriptReturnData
    {
        /// <summary>
        /// Data Files to be installed
        /// </summary>
        public HashSet<DataFile> DataFiles { get; set; } = new HashSet<DataFile>();
        /// <summary>
        /// Plugins to be installed
        /// </summary>
        public HashSet<PluginFile> PluginFiles { get; set; } = new HashSet<PluginFile>();

        /// <summary>
        /// Conflicts with other files
        /// </summary>
        public List<ConflictData> Conflicts { get; } = new List<ConflictData>();

        /// <summary>
        /// BSAs to be registered
        /// </summary>
        public HashSet<string> RegisteredBSAs { get; } = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// INI Edits to be made
        /// </summary>
        public List<INIEditInfo> INIEdits { get; } = new List<INIEditInfo>();

        /// <summary>
        /// Shader Package Edits to be made
        /// </summary>
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
