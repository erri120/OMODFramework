using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OMODFramework.Scripting;

namespace OMODFramework.Test
{
    //[TestClass]
    public class CSharpTest : ATest
    {
        public override HashSet<NexusFile> Files { get; set; } = new HashSet<NexusFile>
        {
            new NexusFile // https://www.nexusmods.com/oblivion/mods/10763
            { 
                DownloadFileName = "DarNified UI 1.3.2.zip",
                FileName  = "DarNified UI 1.3.2.omod",
                ModID = 10763,
                FileID = 34631
            }
        };

        //[TestMethod]
        public void TestCSharpScript()
        {
            Files.Do(f =>
            {
                var omod = new OMOD(f.FileName);

                Assert.IsNotNull(omod);

                var scriptFunctions = new ScriptFunctions();

                ScriptRunner.RunScript(omod, scriptFunctions);
            });
        }
    }
}
