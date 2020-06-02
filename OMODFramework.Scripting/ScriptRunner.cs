using System;
using JetBrains.Annotations;

namespace OMODFramework.Scripting
{
    [PublicAPI]
    public static class ScriptRunner
    {
        public static ScriptReturnData ExecuteScript(OMOD omod, IScriptSettings settings)
        {
            if(!omod.HasFile(OMODFile.Script))
                throw new ArgumentException("The given omod does not contain a script!", nameof(omod));

            var script = omod.ExtractScript();
            ScriptType scriptType;
            if ((byte) script[0] >= 4)
                scriptType = ScriptType.OBMMScript;
            else
            {
                scriptType = (ScriptType)script[0];
                script = script.Substring(1);
            }

            if (omod.DataList == null)
                omod.GetDataFileList();

            if (omod.PluginsList == null && omod.HasFile(OMODFile.PluginsCRC))
                omod.GetPlugins();

            var handler = scriptType switch
            {
                ScriptType.OBMMScript => new OBMMScriptHandler(),
                ScriptType.Python => throw new NotImplementedException(),
                ScriptType.CSharp => throw new NotImplementedException(),
                ScriptType.VB => throw new NotImplementedException(),
                _ => throw new ArgumentOutOfRangeException(nameof(scriptType), scriptType.ToString(), "Unknown script type")
            };

            return handler.Execute(omod, script, settings);
        }
    }

    public abstract class AScriptHandler
    {
        internal abstract ScriptReturnData Execute(OMOD omod, string script, IScriptSettings settings);
    }
}
