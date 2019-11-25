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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pathoschild.FluentNexus;
using File = Alphaleonis.Win32.Filesystem.File;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace OMODFramework.Test
{
    [TestClass]
    public class OMODTests
    {
        private string _apiKey;
        private NexusClient _client;

        // testing with DarNified UI from
        // https://www.nexusmods.com/oblivion/mods/10763
        private const string DownloadFileName = "DarNified UI 1.3.2.zip";
        private const string FileName = "DarNified UI 1.3.2.omod";
        private const int ModID = 10763;
        private const int FileID = 34631;

        [TestInitialize]
        public void Setup()
        {
            Framework.TempDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestTempDir");

            if (File.Exists(DownloadFileName) && File.Exists(FileName))
                return;

            if(!File.Exists("nexus_api_key.txt"))
                throw new Exception("Nexus API Key file does not exist!");

            _apiKey = File.ReadAllText("nexus_api_key.txt");

            _client = new NexusClient(_apiKey, "OMODFramework Unit Tests", "0.0.1");

            var limits = _client.GetRateLimits().Result;

            if(limits.IsBlocked() && !File.Exists(DownloadFileName))
                throw new Exception("Rate limit blocks all Nexus Connections!");

            var downloadLinks = _client.ModFiles.GetDownloadLinks("oblivion", ModID, FileID).Result;

            using (var client = new WebClient())
            {
                client.DownloadFile(downloadLinks[0].Uri, DownloadFileName);
            }

            if(File.Exists(FileName))
                return;

            using (var zipStream = new ZipFile(File.OpenRead(DownloadFileName)))
            using (var fs = new FileStream(FileName, FileMode.CreateNew))
            {
                foreach (ZipEntry ze in zipStream)
                {
                    if(ze.IsFile && ze.Name.ToLower().Contains("omod"))
                        zipStream.GetInputStream(ze).CopyTo(fs);
                }
            }
        }

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

        [TestCleanup]
        public void Cleanup()
        {
            Framework.CleanTempDir(true);
        }
    }
}
