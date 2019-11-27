/*
    Copyright (C) 2019  erri120

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using File = Alphaleonis.Win32.Filesystem.File;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace OMODFramework.Test
{
    [TestClass]
    public class OMODTests : DownloadTest
    {
        // testing with DarNified UI from
        // https://www.nexusmods.com/oblivion/mods/10763
        public override string DownloadFileName { get; set; } = "DarNified UI 1.3.2.zip";
        public override string FileName { get; set; } = "DarNified UI 1.3.2.omod";
        public override int ModID { get; set; } = 10763;
        public override int FileID { get; set; } = 34631;

        [TestMethod]
        public void TestOMOD()
        {
            var omod = new OMOD(FileName);

            Assert.IsNotNull(omod);
        }

        [TestMethod]
        public void TestExtraction()
        {
            var omod = new OMOD(FileName);

            Assert.IsNotNull(omod);

            var data = omod.GetDataFiles();
            Assert.IsNotNull(data);

            var plugins = omod.GetPlugins();
            Assert.IsTrue(omod.AllPlugins.Count == 0 && plugins == null ||
                          omod.AllPlugins.Count >= 1 && plugins != null);
        }

        [TestMethod]
        public void TestCreation()
        {
            if(File.Exists("test.omod"))
                File.Delete("test.omod");
            Directory.CreateDirectory(Path.Combine(Framework.TempDir, "text_files"));

            var file1 = Path.Combine(Framework.TempDir, "file.txt");
            var file2 = Path.Combine(Framework.TempDir, "file2.txt");
            var file3 = Path.Combine(Framework.TempDir, "text_files", "file3.txt");

            var text1 = "This is some text";
            var text2 = "This is more text";
            var text3 = "MORE TEXT !!!!!!!!";

            File.WriteAllText(file1, text1);
            File.WriteAllText(file2, text2);
            File.WriteAllText(file3, text3);

            var ops = new OMODCreationOptions
            {
                Name = "Test OMOD",
                Author = "erri120",
                Email = "erri120@ILoveUnitTesting.co.uk.totally.not.a.virus.com",
                Website = "https://github.com/erri120",
                Description = "The best OMOD you can find on the internet!",
                Image = "",
                MajorVersion = 1,
                MinorVersion = 0,
                BuildVersion = 0,
                CompressionType = CompressionType.SevenZip,
                DataFileCompressionLevel = CompressionLevel.Medium,
                OMODCompressionLevel = CompressionLevel.Medium,
                ESPs = new List<string>(0),
                ESPPaths = new List<string>(0),
                DataFiles = new List<string>
                {
                    file1,
                    file2,
                    file3
                },
                DataFilePaths = new List<string>
                {
                    "file.txt",
                    "file2.txt",
                    "text_files\\file3.txt"
                },
                Readme = "",
                Script = ""
            };

            OMOD.CreateOMOD(ops, "test.omod");

            Assert.IsTrue(File.Exists("test.omod"));

            var omod = new OMOD("test.omod");

            Assert.IsNotNull(omod);

            Assert.IsTrue(omod.ModName == ops.Name);
            Assert.IsTrue(omod.Author == ops.Author);
            Assert.IsTrue(omod.AllPlugins.Count == ops.ESPs.Count);
            Assert.IsTrue(omod.AllDataFiles.Count == ops.DataFiles.Count);

            var data = omod.GetDataFiles();
            
            Directory.EnumerateFiles(data, "*", SearchOption.AllDirectories).Do(file =>
            {
                var contents = File.ReadAllText(file);
                Assert.IsTrue(contents == text1 || contents == text2 || contents == text3);
            });
        }
    }
}
