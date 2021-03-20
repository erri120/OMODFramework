﻿using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace OMODFramework.Scripting.Data
{
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

        public List<ConflictData> Conflicts { get; internal set; } = new List<ConflictData>();

        public HashSet<DataFile> DataFiles { get; internal set; } = new HashSet<DataFile>(new ScriptReturnFileEqualityComparer());

        public HashSet<PluginFile> PluginFiles { get; internal set; } = new HashSet<PluginFile>(new ScriptReturnFileEqualityComparer());

        public HashSet<string> RegisteredBSAs { get; internal set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        public HashSet<string> UnregisteredBSAs { get; internal set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public HashSet<INIEditInfo> INIEdits { get; internal set; } = new HashSet<INIEditInfo>();

        public HashSet<SDPEditInfo> SDPEdits { get; internal set; } = new HashSet<SDPEditInfo>();

        public HashSet<PluginEditInfo> PluginEdits { get; internal set; } = new HashSet<PluginEditInfo>();
        
        internal ScriptReturnData(string dataFolder, string pluginsFolder)
        {
            DataFolder = dataFolder;
            PluginsFolder = pluginsFolder;
        }
    }
}
