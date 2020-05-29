using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace OMODFramework.Scripting
{
    [PublicAPI]
    public static class ScriptRunner
    {
        internal static Dictionary<ScriptType, Lazy<AScriptHandler>> ScriptHandlers = new Dictionary<ScriptType, Lazy<AScriptHandler>>
        {
            {ScriptType.OBMMScript, new Lazy<AScriptHandler>(() => new OBMMScriptHandler())}
        };

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

            var handler = ScriptHandlers.First(x => x.Key == scriptType).Value.Value;
            return handler.Execute(omod, script, settings);
        }
    }

    public abstract class AScriptHandler
    {
        internal abstract ScriptReturnData Execute(OMOD omod, string script, IScriptSettings settings);
    }
}
