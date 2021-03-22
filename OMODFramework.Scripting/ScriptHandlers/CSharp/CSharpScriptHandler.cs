using System;
using System.Text;
using Force.Crc32;
using OblivionModManager.Scripting;
using OMODFramework.Scripting.ScriptHandlers.CSharp.InlinedScripts;

namespace OMODFramework.Scripting.ScriptHandlers.CSharp
{
    internal class CSharpScriptHandler : AScriptHandler
    {
        private readonly uint ScriptCRC;
        
        public CSharpScriptHandler(OMOD omod, string script, OMODScriptSettings settings, string? extractionFolder) 
            : base(omod, script, settings, extractionFolder)
        {
            var scriptBytes = Encoding.UTF8.GetBytes(script);
            ScriptCRC = Crc32Algorithm.Compute(scriptBytes, 0, scriptBytes.Length);
        }

        private protected override void PrivateRunScript()
        {
            IScript script = ScriptCRC switch
            {
                DarkUIdDarN.CRC => new DarkUIdDarN(),
                DarNifiedUI.CRC => new DarNifiedUI(),
                _ => throw new NotImplementedException($"{ScriptCRC:X8}")
            };

            script.Execute(ScriptFunctions);
        }
    }
}
