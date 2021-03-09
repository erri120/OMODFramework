using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using OMODFramework.Compression;
using Xunit;

namespace OMODFramework.Test
{
    public class OMODCreationTests
    {
        [Fact]
        public void TestOMODCreation()
        {
            var imageFile = Path.Combine("files", "image.png");
            Assert.True(File.Exists(imageFile));
            using var bitmap = (Bitmap) Image.FromFile(imageFile);

            var dataFile = Path.Combine("files", "data.txt");
            var pluginFile = Path.Combine("files", "plugin.esp");
            Assert.True(File.Exists(dataFile));
            Assert.True(File.Exists(pluginFile));
            
            var options = new OMODCreationOptions(new Version(1, 2, 3))
            {
                Name = "erri120's Mod",
                Author = "erri120",
                Email = "erri120@protonmail.com",
                Description = "The best mod in existence",
                Website = "https://github.com/erri120/OMODFramework",
                CompressionType = CompressionType.SevenZip,
                OMODCompressionLevel = CompressionLevel.Optimal,
                Readme = "This mod is very nice.",
                Script = "Some script",
                Image = bitmap,
                
                DataFiles = new List<OMODCreationFile>
                {
                    new OMODCreationFile(dataFile, "data.txt")
                },
                
                PluginFiles = new List<OMODCreationFile>
                {
                    new OMODCreationFile(pluginFile, "plugin.esp")
                }
            };

            using var ms = OMODCreation.CreateOMOD(options);
            using var omod = new OMOD(ms);
            
            Assert.Equal(options.Name, omod.Config.Name);
            Assert.Equal(options.Author, omod.Config.Author);
            Assert.Equal(options.Email, omod.Config.Email);
            Assert.Equal(options.Description, omod.Config.Description);
            Assert.Equal(options.Website, omod.Config.Website);
            Assert.Equal(options.CompressionType, omod.Config.CompressionType);
            Assert.Equal(options.Version, omod.Config.Version);

            var readme = omod.GetReadme();
            Assert.Equal(options.Readme, readme);

            var script = omod.GetScript();
            Assert.Equal(options.Script, script);

            var image = omod.GetImage();
            Assert.Equal(options.Image.Size, image.Size);
            options.Image.Dispose();

            var dataFiles = omod.GetDataFiles();
            var pluginFiles = omod.GetPluginFiles();
            
            Assert.Single(dataFiles);
            Assert.Single(pluginFiles);
        }
    }
}
