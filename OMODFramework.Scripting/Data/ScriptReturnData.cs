using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace OMODFramework.Scripting.Data
{
    /// <summary>
    /// Represents all data returned by a script.
    /// </summary>
    [PublicAPI]
    public class ScriptReturnData
    {
        /// <summary>
        /// Path to the extracted data files.
        /// </summary>
        public readonly string DataFolder;

        /// <summary>
        /// Path to the extracted plugins.
        /// </summary>
        public readonly string PluginsFolder;

        /// <summary>
        /// All conflicts.
        /// </summary>
        public List<ConflictData> Conflicts { get; internal set; } = new List<ConflictData>();

        /// <summary>
        /// All data files to install.
        /// </summary>
        public HashSet<DataFile> DataFiles { get; internal set; } = new HashSet<DataFile>(new ScriptReturnFileEqualityComparer());

        /// <summary>
        /// All plugin files to install.
        /// </summary>
        public HashSet<PluginFile> PluginFiles { get; internal set; } = new HashSet<PluginFile>(new ScriptReturnFileEqualityComparer());

        /// <summary>
        /// All BSAs that should be registered.
        /// </summary>
        public HashSet<string> RegisteredBSAs { get; internal set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        /// <summary>
        /// All BSAs that should be unregistered.
        /// </summary>
        public HashSet<string> UnregisteredBSAs { get; internal set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// All INI edits to be performed.
        /// </summary>
        public HashSet<INIEditInfo> INIEdits { get; internal set; } = new HashSet<INIEditInfo>();

        /// <summary>
        /// All Shader Package edits to be performed.
        /// </summary>
        public HashSet<SDPEditInfo> SDPEdits { get; internal set; } = new HashSet<SDPEditInfo>();

        /// <summary>
        /// All plugin edits to be performed.
        /// </summary>
        public HashSet<PluginEditInfo> PluginEdits { get; internal set; } = new HashSet<PluginEditInfo>();

        /// <summary>
        /// All binary plugin patches to be performed.
        /// </summary>
        public HashSet<SetPluginInfo> SetPluginInfos { get; internal set; } = new HashSet<SetPluginInfo>();

        /// <summary>
        /// All XML edits to be performed.
        /// </summary>
        public HashSet<EditXMLInfo> EditXMLInfos { get; internal set; } = new HashSet<EditXMLInfo>();

        /// <summary>
        /// All file patches to be performed.
        /// </summary>
        public HashSet<FilePatch> FilePatches { get; internal set; } = new HashSet<FilePatch>();

        internal ScriptReturnData(string dataFolder, string pluginsFolder)
        {
            DataFolder = dataFolder;
            PluginsFolder = pluginsFolder;
        }
    }
}
