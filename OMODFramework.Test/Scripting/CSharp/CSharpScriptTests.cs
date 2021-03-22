using System;
using System.IO;
using System.Text;
using Force.Crc32;
using OMODFramework.Scripting;
using OMODFramework.Scripting.ScriptHandlers.CSharp.InlinedScripts;
using Xunit;

namespace OMODFramework.Test.Scripting.CSharp
{
    public class CSharpScriptTests
    {
        [Theory]
        //https://www.nexusmods.com/oblivion/mods/10763
        [InlineData("DarNified UI 1.3.2.omod", 11293969, DarNifiedUI.CRC)]
        //https://www.nexusmods.com/oblivion/mods/11280
        [InlineData("DarkUId DarN 16 OMOD Version-11280.omod", 18644802, DarkUIdDarN.CRC)]
        //https://www.nexusmods.com/oblivion/mods/46657
        [InlineData("Horse Armor Revamped 1.8.omod-46657-1-8.omod", 9386737, 0x9646E015)]
        public void TestCSharpScript(string fileName, long expectedFileLength, uint scriptCRC)
        {
            var file = Path.Combine("files", "csharp-scripting", fileName);
            if (!File.Exists(file))
                return;

            var fi = new FileInfo(file);
            var actualFileLength = fi.Length;
            Assert.Equal(expectedFileLength, actualFileLength);

            using var omod = new OMOD(file);

            var script = omod.GetScript();
            var crc = Crc32Algorithm.Compute(Encoding.UTF8.GetBytes(script));
            Assert.Equal(scriptCRC, crc);
            
            //TODO: set this variable back to false once done testing locally
            const bool runScript = false;
            if (!runScript) return;
            if (TestUtils.IsCI)
                throw new Exception($"Someone forgot to change the runScript variable back to false before commiting!");
            var scriptFunctions = new ExternalScriptFunctionsForTesting();
            var srd = OMODScriptRunner.RunScript(omod, new OMODScriptSettings(scriptFunctions));
            
            Directory.Delete(srd.DataFolder);
            Directory.Delete(srd.PluginsFolder);
        }
    }
}
