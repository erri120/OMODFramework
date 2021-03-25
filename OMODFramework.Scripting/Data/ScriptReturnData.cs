using System;
using System.Collections.Generic;
using System.Linq;
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

        private void CopyAllScriptReturnFiles(string outputPath, IEnumerable<ScriptReturnFile> files, string extractionFolder)
        {
            foreach (var file in files)
            {
                file.CopyToOutput(extractionFolder, outputPath);
            }
        }

        private void CopyAllScriptReturnFilesParallel(string outputPath, IEnumerable<ScriptReturnFile> files,
            string extractionFolder)
        {
            files
                .AsParallel()
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                .ForAll(x => x.CopyToOutput(extractionFolder, outputPath));
        }
        
        /// <summary>
        /// Copies all data files in <see cref="DataFiles"/> to the output folder.
        /// </summary>
        /// <param name="outputPath">Output folder</param>
        public void CopyAllDataFiles(string outputPath) => CopyAllScriptReturnFiles(outputPath, DataFiles, DataFolder);
        
        /// <summary>
        /// Copies all plugin files in <see cref="PluginFiles"/> to the output folder. 
        /// </summary>
        /// <param name="outputPath">Output folder</param>
        public void CopyAllPluginFiles(string outputPath) => CopyAllScriptReturnFiles(outputPath, PluginFiles, PluginsFolder);
        
        /// <summary>
        /// Copies all data files in parallel to the output folder.
        /// </summary>
        /// <param name="outputPath">Output folder</param>
        public void CopyAllDataFilesParallel(string outputPath) => CopyAllScriptReturnFilesParallel(outputPath, DataFiles, DataFolder);

        /// <summary>
        /// Copies all files to the output folder.
        /// </summary>
        /// <param name="outputPath">Output folder</param>
        public void CopyAllFiles(string outputPath)
        {
            CopyAllDataFiles(outputPath);
            CopyAllPluginFiles(outputPath);
        }

        /// <summary>
        /// Copies all files in parallel to the output folder.
        /// </summary>
        /// <param name="outputPath">Output folder</param>
        public void CopyAllFilesParallel(string outputPath)
        {
            CopyAllDataFilesParallel(outputPath);
            CopyAllPluginFiles(outputPath);
        }
    }
}
