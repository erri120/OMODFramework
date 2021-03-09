using System.IO;
using System.Linq;
using Xunit;

namespace OMODFramework.Test
{
    public class OMODTests
    {
        [Fact]
        public async void TestOMOD()
        {
            var file = Path.Combine("files", "test.omod");
            using var omod = new OMOD(file);
        }

        [Fact]
        public async void TestOMODExtraction()
        {
            var file = Path.Combine("files", "test.omod");
            using var omod = new OMOD(file);

            const string outputDir = @"output-extraction";
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, true);
            Directory.CreateDirectory(outputDir);
            
            var files = omod.GetFilesFromCRC(true);
            Assert.NotEmpty(files);
            
            omod.ExtractFiles(true, outputDir, true);

            foreach (var outputPath in files.Select(compressedFile => Path.Combine(outputDir, compressedFile.Name)))
            {
                Assert.True(File.Exists(outputPath), $"File does not exist: {outputPath}");
            }
        }
        
        [Fact]
        public async void TestOMODExtractionParallel()
        {
            var file = Path.Combine("files", "test.omod");
            using var omod = new OMOD(file);

            const string outputDir = @"output-extraction-parallel";
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, true);
            Directory.CreateDirectory(outputDir);
            
            var files = omod.GetFilesFromCRC(true);
            Assert.NotEmpty(files);
            
            omod.ExtractFilesParallel(outputDir, 4, 2, true);

            foreach (var outputPath in files.Select(compressedFile => Path.Combine(outputDir, compressedFile.Name)))
            {
                Assert.True(File.Exists(outputPath), $"File does not exist: {outputPath}");
            }
        }
    }
}
