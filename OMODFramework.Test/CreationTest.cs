using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace OMODFramework.Test
{
    public class CreationTest
    {
        public static CreationOptions CreateOptions()
        {
            var random = new Random();

            var dataFiles = new List<CreationOptions.CreationOptionFile>();

            Directory.CreateDirectory("creation");

            for (var i = 0; i < 4; i++)
            {
                var path = Path.Combine("creation", $"{i}.txt");

                if (File.Exists(path))
                    File.Delete(path);

                var min = random.Next(1, 10);
                var max = random.Next(11, 30);
                var content = $"{min}{max}";

                File.WriteAllText(path, content);
                var file = new FileInfo(path);

                var creationFile = new CreationOptions.CreationOptionFile(file, $"files\\{i}.txt");
                dataFiles.Add(creationFile);
            }

            //var script = File.ReadAllText("TestScript.txt");

            var options = new CreationOptions
            {
                Name = "Test OMOD",
                Author = "erri120",
                Description = "An amazing test mod!",
                Email = "iLoveOmods@lel.com",
                Website = "https://www.github.com/erri120/OMODFramework",

                Readme = @"
This amazing test mod will make you want to download and use the OMODFramework!
Requires 3 brain cells, 10 buckets of milk and 4 eggs.
Conflicts with the CTD on Death mod by erri120.",
                //Script = script,
                //ScriptType = ScriptType.OBMMScript,

                CompressionType = CompressionType.SevenZip,
                DataCompressionLevel = CompressionLevel.Medium,
                OMODCompressionLevel = CompressionLevel.Medium,

                DataFiles = dataFiles
            };

            return options;
        }

        [Fact]
        public void TestOMODCreation()
        {
            var options = CreateOptions();

            var omodPath = Path.Combine("creation", "output.omod");
            OMOD.CreateOMOD(options, new FileInfo(omodPath));

            using var omod = new OMOD(new FileInfo(omodPath));

            Assert.Equal(options.Name, omod.Config.Name);
            Assert.Equal(options.Author, omod.Config.Author);
            Assert.Equal(options.Description, omod.Config.Description);
            Assert.Equal(options.Email, omod.Config.Email);
            Assert.Equal(options.Website, omod.Config.Website);

            var omodReadme = omod.GetReadme();
            Assert.Equal(options.Readme, omodReadme);

            var omodFiles = omod.GetDataFileList().ToList();
            Assert.Equal(options.DataFiles!.Count, omodFiles.Count);

            var extractedDir = new DirectoryInfo(Path.Combine("creation"));
            omod.ExtractDataFiles(extractedDir);

            options.DataFiles!.Select(x => x.From).Do(x =>
            {
                var crc = OMODFramework.Utils.CRC32(x);
                var first = omodFiles.FirstOrDefault(y => y.Length == x.Length && y.CRC == crc);
                Assert.NotNull(first);

                var firstPath = first.GetFullPath(extractedDir);

                var expectedString = File.ReadAllText(x.FullName);
                var actualString = File.ReadAllText(firstPath);

                Assert.Equal(expectedString, actualString);
            });
        }
    }
}
