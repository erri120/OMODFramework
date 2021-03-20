using System;
using JetBrains.Annotations;
using OMODFramework.Scripting.Data;
using OMODFramework.Scripting.ScriptHandlers;
using OMODFramework.Scripting.ScriptHandlers.OBMMScript;

namespace OMODFramework.Scripting
{
    [PublicAPI]
    public static class OMODScriptRunner
    {
        public static ScriptReturnData RunScript(OMOD omod, OMODScriptSettings settings)
        {
            if (!omod.HasEntryFile(OMODEntryFileType.Script))
                throw new ArgumentException("OMOD does not have a script!", nameof(omod));

            var script = omod.GetScript(out var scriptType);

            AScriptHandler handler = scriptType switch
            {
                OMODScriptType.OBMMScript => new OBMMScriptHandler(omod, script, settings),
                OMODScriptType.CSharp => throw new NotImplementedException(),
#pragma warning disable 618
                OMODScriptType.Python => throw new NotImplementedException(),
                OMODScriptType.VisualBasic => throw new NotImplementedException(),
#pragma warning restore 618
                _ => throw new ArgumentOutOfRangeException(nameof(scriptType), scriptType.ToString(),
                    "Unknown script type")
            };

            return handler.RunScript();
        }
    }
}
