using System;
using System.IO;
using NLog;
using OMODFramework.Scripting.Data;

namespace OMODFramework.Scripting.ScriptHandlers
{
    internal abstract class AScriptHandler
    {
        private protected OMODScriptSettings ScriptSettings;
        private protected IExternalScriptFunctions ExternalScriptFunctions => ScriptSettings.ExternalScriptFunctions;
        
        private protected OMOD OMOD;
        private protected string Script;
        
        private protected readonly string ExtractionFolder;
        private protected readonly string DataFolder;
        private protected readonly string PluginsFolder;

        private protected ScriptReturnData ScriptReturnData;
        private protected ScriptFunctions ScriptFunctions;
        
        internal AScriptHandler(OMOD omod, string script, OMODScriptSettings settings)
        {
            ScriptSettings = settings;

            OMOD = omod;
            Script = script;

            var guid = Guid.NewGuid();
            ExtractionFolder = settings.CreateModSpecificExtractionFolder
                ? Path.Combine(settings.ExtractionFolder, guid.ToString("D"))
                : settings.ExtractionFolder;
            
            DataFolder = Path.Combine(ExtractionFolder, "data");
            PluginsFolder = Path.Combine(ExtractionFolder, "plugins");

            if (!settings.DryRun)
            {
                //TODO: maybe use async or parallel overloads
                omod.ExtractFiles(true, DataFolder);
                if (omod.HasEntryFile(OMODEntryFileType.Plugins))
                    omod.ExtractFiles(false, PluginsFolder);
            }

            ScriptReturnData = new ScriptReturnData(DataFolder, PluginsFolder);
            ScriptFunctions = new ScriptFunctions(ScriptSettings, omod, ScriptReturnData);
        }

        private protected abstract void PrivateRunScript();

        internal ScriptReturnData RunScript()
        {
            PrivateRunScript();
            return ScriptReturnData;
        }
    }
}
