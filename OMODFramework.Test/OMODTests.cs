using System;
using System.IO;
using System.Net;
using System.Reflection;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OMODFramework.Classes;
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
            var f = new Framework();

            var omod = new OMOD(FileName, ref f);

            Assert.IsNotNull(omod);
        }

        [TestMethod]
        public void TestExtraction()
        {
            var f = new Framework();

            var omod = new OMOD(FileName, ref f);

            Assert.IsNotNull(omod);

            var data = omod.GetDataFiles();
            Assert.IsNotNull(data);

            var plugins = omod.GetPlugins();
            Assert.IsTrue(omod.AllPlugins.Count == 0 && plugins == null ||
                          omod.AllPlugins.Count >= 1 && plugins != null);
        }
    }
}
