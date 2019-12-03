using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using File = Alphaleonis.Win32.Filesystem.File;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace OMODFramework.Test
{
    [TestClass]
    public class EndToEndTest
    {
        public string OMODFile;
        public List<string> ModFiles;
        public List<string> FolderStructure;
        public string Readme;
        public string Script;

        [TestInitialize]
        public void Setup()
        {
            Framework.Settings.TempPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestTempDir");

            if (!Directory.Exists(Framework.Settings.TempPath))
                Directory.CreateDirectory(Framework.Settings.TempPath);
            else
                Framework.CleanTempDir();

            OMODFile = Path.Combine(Framework.Settings.TempPath, "ILoveTesting.omod");
            ModFiles = new List<string>();
            FolderStructure = new List<string>();

            if (File.Exists("readme.txt"))
                Readme = File.ReadAllText("readme.txt");

            if (File.Exists("script.txt"))
                Script = File.ReadAllText("script.txt");

            Directory.GetFiles("files", "*", SearchOption.AllDirectories).Do(f =>
            {
                ModFiles.Add(f);
                FolderStructure.Add(f.Substring("files\\".Length));
            });
        }

        [TestMethod]
        public void EndToEnd()
        {
            // Creation

            var ops = new OMODCreationOptions
            {
                Name = "I love, love... moist towelettes",
                Author = "erri120",
                Email = "erri120@ILoveUnitTesting.co.uk.totally.not.a.virus.com",
                Website = "https://github.com/erri120",
                Description = "The best OMOD you can find on the internet!",
                Image = "",
                MajorVersion = 6,
                MinorVersion = 6,
                BuildVersion = 6,
                CompressionType = CompressionType.SevenZip,
                DataFileCompressionLevel = CompressionLevel.High,
                OMODCompressionLevel = CompressionLevel.Medium,
                ESPs = new List<string>(0),
                ESPPaths = new List<string>(0),
                DataFiles = ModFiles,
                DataFilePaths = FolderStructure,
                Readme = Readme,
                Script = Script
            };

            OMOD.CreateOMOD(ops, OMODFile);

            Assert.IsTrue(File.Exists(OMODFile));

            // Test parsing

            var omod = new OMOD(OMODFile);

            Assert.IsNotNull(omod);
            Assert.IsTrue(CompareCreationToFile(ref ops, ref omod));

            // Test extraction

            var data = omod.GetDataFiles();

            var fList1 = new List<FileInfo>();
            var fList2 = new List<FileInfo>();

            Directory.GetFiles("files", "*", SearchOption.AllDirectories).Do(f =>
            {
                fList1.Add(new FileInfo(f));
            });

            Directory.GetFiles(data, "*", SearchOption.AllDirectories).Do(f =>
            {
                fList2.Add(new FileInfo(f));
            });

            Assert.IsTrue(fList1.Count == fList2.Count);

            for (var i = 0; i < fList1.Count; i++)
            {
                var f1 = fList1[i];
                var f2 = fList2[i];

                Assert.IsTrue(Equals(f1, f2, Path.GetFullPath("files"), data));
            }

            // Test scripting

            var scriptFunctions = new ScriptFunctions();

            var srd = omod.RunScript(scriptFunctions, data);

            Assert.IsNotNull(srd);
            Assert.IsTrue(!srd.CancelInstall);
            Assert.IsTrue(srd.CopyDataFiles.Count == 1 && srd.CopyDataFiles.TryGetValue(new ScriptCopyDataFile("A\\A.txt", "meshes\\a.txt"), out _));
            Assert.IsTrue(srd.InstallData.Contains("something.txt"));
        }

        private static bool CompareCreationToFile(ref OMODCreationOptions ops, ref OMOD omod)
        {
            if (ops.Name != omod.ModName) return false;
            if (ops.Author != omod.Author) return false;
            if (ops.Email != omod.Email) return false;
            if (ops.Website != omod.Website) return false;
            if (ops.Description != omod.Description) return false;
            if (ops.MajorVersion != omod.MajorVersion) return false;
            if (ops.MinorVersion != omod.MinorVersion) return false;
            if (ops.BuildVersion != omod.BuildVersion) return false;
            if (ops.CompressionType != omod.Compression) return false;
            return true;
        }

        private static bool Equals(FileInfo f1, FileInfo f2, string path1, string path2)
        {
            return f1.Name == f2.Name && f1.Length == f2.Length &&
                   f1.DirectoryName?.Replace(path1, path2) == f2.DirectoryName?.Replace(path1, path2);
        }
    }
}
