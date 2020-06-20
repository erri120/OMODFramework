using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace OMODFramework.Scripting
{
    [PublicAPI]
    public static class ScriptRunner
    {
        public static ScriptReturnData ExecuteScript(OMOD omod, ScriptSettings settings)
        {
            if(!omod.HasFile(OMODEntryFileType.Script))
                throw new ArgumentException("The given omod does not contain a script!", nameof(omod));

            var script = omod.GetScript();
            ScriptType scriptType;
            if ((byte) script[0] >= 4)
                scriptType = ScriptType.OBMMScript;
            else
            {
                scriptType = (ScriptType)script[0];
                script = script.Substring(1);
            }

            omod.OMODFile.Decompress(OMODEntryFileType.Data);
            if(omod.HasFile(OMODEntryFileType.PluginsCRC))
                omod.OMODFile.Decompress(OMODEntryFileType.Plugins);

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

        public static void ExtractAllFiles(OMOD omod, ScriptReturnData data, DirectoryInfo output)
        {
            data.DataFiles.Do(d => ExtractFile(omod, output, d));
            data.PluginFiles.Where(x => !x.IsUnchecked).Do(p => ExtractFile(omod, output, p));
        }

        private static void ExtractFile(OMOD omod, DirectoryInfo output, ScriptReturnFile returnFile)
        {
            var file = new FileInfo(Path.Combine(output.FullName, returnFile.Output));
            if (file.Directory == null)
                throw new NullReferenceException("Directory is null!");
            if (!file.Directory.Exists)
                file.Directory.Create();

            if (file.Exists)
            {
                if (file.Length == returnFile.OriginalFile.Length)
                    return;
                file.Delete();
            }

            using var fs = omod.OMODFile.ExtractDecompressedFile(returnFile.OriginalFile, file);
            if (fs.Length != returnFile.OriginalFile.Length)
                throw new Exception($"Decompressed length does not equal length of the original file: {returnFile.OriginalFile.Name} at {file.FullName}, expected: {returnFile.OriginalFile.Length} actual: {fs.Length}");
        }
    }

    public abstract class AScriptHandler
    {
        internal abstract ScriptReturnData Execute(OMOD omod, string script, ScriptSettings settings);
    }
}
