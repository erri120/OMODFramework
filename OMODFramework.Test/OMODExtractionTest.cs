using System.Collections.Generic;
using System.Reflection;
using Alphaleonis.Win32.Filesystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OMODFramework.Test
{
    [TestClass]
    public class OMODExtractionTest : ATest
    {
        public override HashSet<NexusFile> Files { get; set; } = new HashSet<NexusFile>
        {
            new NexusFile // https://www.nexusmods.com/oblivion/mods/15619
            {
                DownloadFileName = "Oblivion XP v415 - OMOD-15619.omod",
                FileName = "Oblivion XP v415 - OMOD-15619.omod",
                ModID = 15619,
                FileID = 46662
            },
        };

        [TestMethod]
        public void TestExtraction()
        {
            OMODExtraction.Framework.TempDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestTempDir");

            if (!Directory.Exists(OMODExtraction.Framework.TempDir))
                Directory.CreateDirectory(OMODExtraction.Framework.TempDir);
            else
                OMODExtraction.Framework.CleanTempDir();

            Files.Do(f =>
            {
                var omod = new OMODExtraction.OMOD(f.FileName);

                Assert.IsNotNull(omod);

                var data = omod.ExtractDataFiles();
                var plugins = omod.ExtractPlugins();

                Assert.IsTrue(string.IsNullOrWhiteSpace(data));
                Assert.IsTrue(string.IsNullOrWhiteSpace(plugins));
            });
        }
    }
}
