using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using OblivionModManager.Scripting;

namespace OMODFramework.Scripting
{
    #region Exceptions

    /// <summary>
    /// Thrown during script execution
    /// </summary>
    public class ScriptException : Exception
    {
        internal ScriptException(string s) : base(s) { }
    }

    /// <summary>
    /// Thrown when FatalError was triggered
    /// </summary>
    public class ScriptingFatalErrorException : ScriptException
    {
        internal ScriptingFatalErrorException() : base("Fatal Error was triggered in the script!") { }
    }

    /// <summary>
    /// Thrown when script execution was canceled
    /// </summary>
    public class ScriptingCanceledException : ScriptException
    {
        internal ScriptingCanceledException() : base("Script execution was canceled!") { }
        internal ScriptingCanceledException(string s) : base($"Script execution was canceled!\n{s}") { }
    }

    #endregion

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

        /// <summary>
        /// Initializes a new <see cref="ScriptSettings"/> object
        /// </summary>
        /// <param name="scriptFunctions">The functions to use</param>
        /// <param name="frameworkSettings">The Framework settings to use. Default settings will be used if this is null.</param>
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
        /// <param name="message">Message ot be displayed</param>
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

    /// <summary>
    /// Script Return File
    /// </summary>
    [PublicAPI]
    public class ScriptReturnFile
    {
        /// <summary>
        /// Original Entry
        /// </summary>
        public readonly OMODCompressedEntry OriginalFile;
        /// <summary>
        /// Output path
        /// </summary>
        public string Output { get; }

        internal ScriptReturnFile(OMODCompressedEntry entry)
        {
            OriginalFile = entry;
            Output = OriginalFile.Name;
        }

        internal ScriptReturnFile(OMODCompressedEntry entry, string output)
        {
            OriginalFile = entry;
            Output = output;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{OriginalFile.Name} to {Output}";
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (!(obj is ScriptReturnFile file))
                return false;

            return file.OriginalFile.Equals(OriginalFile) && file.Output.Equals(Output, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(OriginalFile, Output);
        }
    }

    /// <summary>
    /// A Data File
    /// </summary>
    [PublicAPI]
    public sealed class DataFile : ScriptReturnFile
    {
        internal DataFile(OMODCompressedEntry entry) : base(entry) { }
        internal DataFile(OMODCompressedEntry entry, string output) : base(entry, output) { }
    }

    /// <summary>
    /// A Plugin File
    /// </summary>
    [PublicAPI]
    public sealed class PluginFile : ScriptReturnFile
    {
        /// <summary>
        /// Whether the file is unchecked or not
        /// </summary>
        public bool IsUnchecked { get; set; }

        /// <summary>
        /// (Can be null) Warning
        /// </summary>
        public DeactiveStatus? Warning { get; set; }
        /// <summary>
        /// Whether to load the plugin early
        /// </summary>
        public bool LoadEarly { get; set; }

        /// <summary>
        /// Plugins that should be loaded after the current plugin
        /// </summary>
        public List<PluginFile> LoadBefore { get; set; } = new List<PluginFile>();
        /// <summary>
        /// Plugins that should be loaded before the current plugin
        /// </summary>
        public List<PluginFile> LoadAfter { get; set; } = new List<PluginFile>();

        internal PluginFile(OMODCompressedEntry entry) : base(entry) { }
        internal PluginFile(OMODCompressedEntry entry, string output) : base(entry, output) { }
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
        /// File that the current OMOD conflicts with/depends on
        /// </summary>
        public string File { get; set; } = string.Empty;
        
        /// <summary>
        /// Conflict is only viable if the <see cref="File"/> has this minimum version
        /// </summary>
        public Version? MinVersion { get; set; }
        /// <summary>
        /// Conflict is only viable if the <see cref="File"/> has this maximum version
        /// </summary>
        public Version? MaxVersion { get; set; }

        /// <summary>
        /// (Can be null) Comment
        /// </summary>
        public string? Comment { get; set; }
        /// <summary>
        /// Whether <see cref="File"/> is a regex
        /// </summary>
        public bool Partial { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{(Type == ConflictType.Conflicts ? "Conflicts with" : "Depends on")} {File}";
        }
    }

    /// <summary>
    /// Abstract class for Edits
    /// </summary>
    [PublicAPI]
    public abstract class AEdit
    {
        internal readonly OMOD OMOD;

        internal AEdit(OMOD omod)
        {
            OMOD = omod;
        }
    }

    /// <summary>
    /// Abstract class for File Edits
    /// </summary>
    [PublicAPI]
    public abstract class AFileEdit : AEdit
    {
        /// <summary>
        /// The File to edit
        /// </summary>
        public readonly ScriptReturnFile File;

        internal AFileEdit(ScriptReturnFile file, OMOD omod) : base(omod)
        {
            File = file;
        }

        /// <summary>
        /// Extracts <see cref="File"/> and reads the the data into the provided buffer. You should
        /// use <see cref="OMODCompressedEntry.Length"/> to get the length of the file. This function
        /// is supposed to be used if you want to execute the edit yourself.
        /// </summary>
        /// <param name="buffer">The buffer</param>
        /// <exception cref="ArgumentException">When the length of the provided buffer does not equal the length the file</exception>
        public void GetBytesFromFile(ref byte[] buffer)
        {
            using var stream = OMOD.OMODFile.ExtractDecompressedFile(File.OriginalFile, File is DataFile);
            if(buffer.Length != stream.Length)
                throw new ArgumentException($"Buffer size does not equal size of stream! Expected size: {stream.Length} actual size: {buffer.Length}");
            stream.Read(buffer, 0, (int)stream.Length);
        }
    }

    /// <summary>
    /// INI Edit Info
    /// </summary>
    [PublicAPI]
    public class INIEditInfo : AEdit
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

        internal INIEditInfo(string section, string name, string newValue, OMOD omod) : base(omod)
        {
            Section = section;
            Name = name;
            NewValue = newValue;
        }

        public void ExecuteEdit(OMOD omod)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"[{Section}]{Name}:{NewValue}";
        }
    }

    /// <summary>
    /// Shader package edits
    /// </summary>
    [PublicAPI]
    public class SDPEditInfo : AFileEdit
    {
        /// <summary>
        /// Shader package number, between 1 and 19
        /// </summary>
        public readonly byte Package;
        /// <summary>
        /// Name of the shader
        /// </summary>
        public readonly string Shader;

        internal SDPEditInfo(byte package, string shader, ScriptReturnFile file, OMOD omod) : base(file, omod)
        {
            Package = package;
            Shader = shader;
        }

        /// <summary>
        /// <para>Use this function if you don't have code for dealing with shader edits.</para>
        /// <para>
        /// This function reads the provided shader file and replaces the shader inside of it. Use the
        /// <paramref name="safeReplace"/> parameter if you don't want the original file to be changed.
        /// If <paramref name="safeReplace"/> is set to true, you also have to provide an <paramref name="outputFile"/>
        /// where the final shader file will go to.
        /// </para>
        /// </summary>
        /// <param name="shaderFile">The Shader file to replace. Do note that this has to match shaderpackage{ID}.sdp where ID is <see cref="Package"/> with a PadLeft of 3. Meaning that Package 1 becomes 001 and package 18 becomes 018.</param>
        /// <param name="outputFile">The output file, only needed if <paramref name="safeReplace"/> is set to true</param>
        /// <param name="safeReplace">Whether to use export the final shader file to <paramref name="outputFile"/></param>
        public void ExecuteEdit(FileInfo shaderFile, FileInfo? outputFile, bool safeReplace = true)
        {
            if(!shaderFile.Exists)
                throw new ArgumentException($"Provided shader file does not exist: {shaderFile}!", nameof(shaderFile));
            if(shaderFile.Extension != ".sdp")
                throw new ArgumentException($"Extension of provided shader file is not .sdp but {shaderFile.Extension}!", nameof(shaderFile));

            var fileName = $"shaderpackage{Package.ToString().PadLeft(3, '0')}.sdp";
            if(!shaderFile.Name.Equals(fileName, StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException($"Provided shader file does not equal name {fileName} but is {shaderFile.Name}!", nameof(shaderFile));

            if(safeReplace && outputFile == null)
                throw new ArgumentException($"{nameof(safeReplace)} is true but {nameof(outputFile)} is null! {nameof(outputFile)} has to be set or {nameof(safeReplace)} has to be set to false!", nameof(outputFile));

            byte[] buffer = new byte[File.OriginalFile.Length];
            GetBytesFromFile(ref buffer);
            OblivionSDP.EditShader(shaderFile, Shader, buffer, outputFile);
        }
    }

    /// <summary>
    /// Class containing information about Patches to be made
    /// </summary>
    [PublicAPI]
    public class PatchInfo : AFileEdit
    {
        /// <summary>
        /// The file to patch
        /// </summary>
        public readonly string FileToPatch;
        /// <summary>
        /// Whether the file at <see cref="FileToPatch"/> should be created if it does not exist
        /// </summary>
        public readonly bool Create;

        internal readonly bool IsDataFile;

        internal PatchInfo(ScriptReturnFile file, string fileToPatch, bool create, bool data, OMOD omod) : base(file, omod)
        {
            FileToPatch = fileToPatch;
            Create = create;
            IsDataFile = data;
        }

        public void ExecuteEdit(OMOD omod, string output)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Patch {FileToPatch} with {File}";
        }
    }

    /// <summary>
    /// Type of the data in <see cref="SetPluginInfo"/>
    /// </summary>
    [PublicAPI]
    public enum SetPluginInfoType
    {
        /// <summary>
        /// Byte (1 byte... duh)
        /// </summary>
        Byte,
        /// <summary>
        /// Short (2 bytes)
        /// </summary>
        Short,
        /// <summary>
        /// Int (4 bytes)
        /// </summary>
        Int,
        /// <summary>
        /// Long (8 bytes)
        /// </summary>
        Long,
        /// <summary>
        /// Float (4 bytes)
        /// </summary>
        Float
    }

    /// <summary>
    /// Class for plugin changes
    /// </summary>
    [PublicAPI]
    public class SetPluginInfo : AFileEdit
    {
        /// <summary>
        /// Type of the data in <see cref="Value"/>
        /// </summary>
        public readonly SetPluginInfoType Type;
        /// <summary>
        /// The data to replace in the plugin
        /// </summary>
        public readonly object Value;
        /// <summary>
        /// Offset of the data to replace
        /// </summary>
        public readonly long Offset;

        internal SetPluginInfo(SetPluginInfoType type, object value, long offset, ScriptReturnFile file, OMOD omod) : base(file, omod)
        {
            Type = type;
            Value = value;
            Offset = offset;
        }

        /// <summary>
        /// Returns the amount of bytes to be written depending on the <see cref="Type"/>
        /// </summary>
        /// <returns></returns>
        public int GetValueLength()
        {
            return Type switch
            {
                SetPluginInfoType.Byte => 1,
                SetPluginInfoType.Float => 4,
                SetPluginInfoType.Short => 2,
                SetPluginInfoType.Int => 4,
                SetPluginInfoType.Long => 8,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public void ExecuteEdit(OMOD omod, string output)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"SetPlugin{Type} at {Offset} in {File}";
        }
    }

    /// <summary>
    /// Class holding info about ESP edits to certain records
    /// </summary>
    [PublicAPI]
    public class ESPEditInfo
    {
        /// <summary>
        /// Whether the edit is for GMST or Global
        /// </summary>
        public readonly bool IsGMST;
        /// <summary>
        /// Plugin file to edit
        /// </summary>
        public readonly string File;
        /// <summary>
        /// The EDID of the record
        /// </summary>
        public readonly string EDID;
        /// <summary>
        /// The Value to change
        /// </summary>
        public readonly string Value;

        internal ESPEditInfo(string value, string file, string edid, bool isGMST)
        {
            Value = value;
            File = file;
            EDID = edid;
            IsGMST = isGMST;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"ESPEdit to {File} at {EDID}: {Value}";
        }
    }

    /// <summary>
    /// Information about XML edits
    /// </summary>
    [PublicAPI]
    public class EditXMLInfo : AFileEdit
    {
        /// <summary>
        /// Whether to find and replace
        /// </summary>
        public readonly bool IsReplace;
        /// <summary>
        /// Whether to replace a Line
        /// </summary>
        public readonly bool IsEditLine;

        /// <summary>
        /// Only set if <see cref="IsEditLine"/> is set to <c>true</c>. Line number
        /// to replace.
        /// </summary>
        public readonly int Line;
        /// <summary>
        /// Only set if <see cref="IsEditLine"/> is set to <c>true</c>. Line replacement.
        /// </summary>
        public readonly string Value;

        /// <summary>
        /// Only set if <see cref="IsReplace"/> is set to <c>true</c>. String to find.
        /// </summary>
        public readonly string Find;
        /// <summary>
        /// Only set if <see cref="IsReplace"/> is set to <c>true</c>. String to replace <see cref="Find"/>with.
        /// </summary>
        public readonly string Replace;

        internal EditXMLInfo(ScriptReturnFile file, int line, string value, OMOD omod) : base(file, omod)
        {
            IsReplace = false;
            IsEditLine = true;

            Line = line;
            Value = value;

            Find = string.Empty;
            Replace = string.Empty;
        }

        internal EditXMLInfo(ScriptReturnFile file, string find, string replace, OMOD omod) : base(file, omod)
        {
            IsReplace = true;
            IsEditLine = false;

            Line = -1;
            Value = string.Empty;

            Find = find;
            Replace = replace;
        }

        public void ExecuteEdit(OMOD omod, string output)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return IsEditLine 
                ? $"Replace Line {Line} with {Value}" 
                : $"Find {Find} and replace with {Replace}";
        }
    }

    /// <summary>
    /// Script Return Data. This class holds information about everything that
    /// gets returned and modified during script execution.
    /// </summary>
    [PublicAPI]
    public class ScriptReturnData
    {
        private readonly OMOD _omod;

        internal ScriptReturnData(OMOD omod)
        {
            _omod = omod;
        }

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

        /// <summary>
        /// List of all Patches to be made
        /// </summary>
        public List<PatchInfo> Patches { get; } = new List<PatchInfo>();

        /// <summary>
        /// List of all <see cref="SetPluginInfo"/>s
        /// </summary>
        public List<SetPluginInfo> SetPluginList { get; } = new List<SetPluginInfo>();

        /// <summary>
        /// List of all ESP Edits
        /// </summary>
        public List<ESPEditInfo> ESPEdits { get; } = new List<ESPEditInfo>();

        /// <summary>
        /// List of all XML Edits
        /// </summary>
        public List<EditXMLInfo> XMLEdits { get; } = new List<EditXMLInfo>();

        internal ScriptReturnFile GetScriptReturnFileFromPath(string path, bool data)
        {
            var entry = data
                ? _omod.OMODFile.DataFiles.First(x => x.Name.EqualsPath(path))
                : _omod.OMODFile.Plugins.First(x => x.Name.EqualsPath(path));

            ScriptReturnFile file = data
                ? DataFiles.First(x => Equals(x.OriginalFile, entry))
                : (ScriptReturnFile)PluginFiles.First(x => Equals(x.OriginalFile, entry));

            return file;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Data Files: {DataFiles.Count}, Plugins: {PluginFiles.Count}";
        }
    }
}
