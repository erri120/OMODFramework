using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OMODFramework.Scripting;
using OMODFramework.Scripting.Data;
using Xunit;

namespace OMODFramework.Test.Scripting.OBMM
{
    public class OBMMScriptTests
    {
        [Theory]
        //https://www.nexusmods.com/oblivion/mods/40462
        [InlineData("NoMaaM Breathing Idles V1 OMOD-40462-1-0.omod", 1053485)]
        //https://www.nexusmods.com/oblivion/mods/35551
        [InlineData("NoMaaM BBB Animation Replacer V3_1 OMOD-35551-3-1.omod", 7388700)]
        //https://www.nexusmods.com/oblivion/mods/34442
        [InlineData("HGEC Body with BBB v1dot12-34442.omod", 34641874)]
        //https://www.nexusmods.com/oblivion/mods/24078
        [InlineData("EVE_HGEC_BodyStock and Clothing OMOD-24078.omod", 55442432)]
        //https://www.nexusmods.com/oblivion/mods/40532
        [InlineData("Robert Male Body Replacer v52 OMOD-40532-1.omod", 155453816)]
        public void TestScriptExecution(string fileName, long expectedFileLength)
        {
            /*
             * Get the mods listed above and put them into the OMODFramework.Test/files/obmm-scripting folder. They
             * will be copied to the output folder post build. I don't want to download those mods in the CI so this
             * test can only be run locally.
             */
            
            //TODO: make this run on the CI without having to download the mods (use data+plugin files index)
            
            var file = Path.Combine("files", "obmm-scripting", fileName);
            if (!File.Exists(file))
                return;

            var fi = new FileInfo(file);
            var actualFileLength = fi.Length;
            Assert.Equal(expectedFileLength, actualFileLength);

            var resultsFile = Path.Combine("files", "obmm-scripting", fileName + "-Results.txt");
            Assert.True(File.Exists(resultsFile));
            
            using var omod = new OMOD(file);
            
            var externalScriptFunctions = new ExternalScriptFunctionsForTesting(resultsFile, fileName);
            var settings = new OMODScriptSettings(externalScriptFunctions)
            {
                DryRun = true,
                UseBitmapOverloads = false
            };
            
            var srd = OMODScriptRunner.RunScript(omod, settings);

            VerifyFiles(externalScriptFunctions.DataFiles, srd.DataFiles);
            VerifyFiles(externalScriptFunctions.PluginFiles, srd.PluginFiles);
        }

        private static void VerifyFiles(IReadOnlyCollection<string> expected, IReadOnlyCollection<ScriptReturnFile> actual)
        {
            var notInExpected = actual.ToHashSet();
            var inActualCount = notInExpected.RemoveWhere(x => expected.Contains(x.Output, StringComparer.OrdinalIgnoreCase));

            var notInActual = expected.ToHashSet();
            var inExpectedCount = notInActual.RemoveWhere(x => actual.Any(y => y.Output.Equals(x, StringComparison.OrdinalIgnoreCase)));
            
            Assert.Empty(notInExpected);
            Assert.Empty(notInActual);
            Assert.Equal(actual.Count, inActualCount);
            Assert.Equal(expected.Count, inExpectedCount);
            Assert.Equal(expected.Count, actual.Count);
        }
    }
}
