using System;
using JetBrains.Annotations;
using OMODFramework.Scripting.Data;
using OMODFramework.Scripting.ScriptHandlers;
using OMODFramework.Scripting.ScriptHandlers.CSharp;
using OMODFramework.Scripting.ScriptHandlers.OBMMScript;

namespace OMODFramework.Scripting
{
    /// <summary>
    /// Provides static function for running the script inside an OMOD.
    /// </summary>
    [PublicAPI]
    public static class OMODScriptRunner
    {
        /// <summary>
        /// Run the script inside an OMOD.
        /// </summary>
        /// <param name="omod">The OMOD with the script to run.</param>
        /// <param name="settings">The settings to use during Script Execution.</param>
        /// <param name="extractionFolder">The folder to extract the data and plugin files to. Defaults to a path
        /// inside the users temp folder if set to null.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">The omod does not have a script.</exception>
        /// <exception cref="NotImplementedException">The script is of type <see cref="OMODScriptType.Python"/> or
        /// <see cref="OMODScriptType.VisualBasic"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The script has an unknown script type.</exception>
        public static ScriptReturnData RunScript(OMOD omod, OMODScriptSettings settings, string? extractionFolder = null)
        {
            if (!omod.HasEntryFile(OMODEntryFileType.Script))
                throw new ArgumentException("OMOD does not have a script!", nameof(omod));

            var script = omod.GetScript(out var scriptType);

            AScriptHandler handler = scriptType switch
            {
                OMODScriptType.OBMMScript => new OBMMScriptHandler(omod, script, settings, extractionFolder),
                OMODScriptType.CSharp => new CSharpScriptHandler(omod, script, settings, extractionFolder),
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
