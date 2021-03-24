using System.IO;
using System.Linq;
using System.Text;
using OMODFramework.Oblivion.BSA;
using Xunit;

namespace OMODFramework.Test
{
    public class BSATests
    {
        [Fact]
        public void TestBSAParsing()
        {
            const string file = "test-bsa.bsa";
            var path = Path.Combine("files", file);

            using var reader = new BSAReader(path);
            Assert.Equal(115, reader.Files.Count);
            Assert.Equal(10, reader.Folders.Count);

            const string expectedContents = "@#$%";

            var fileInfo = reader.Files.First(x => x.Name.Equals("s.txt"));
            
            using var ms = new MemoryStream();
            reader.CopyFileTo(fileInfo, ms);
            var buffer = new byte[ms.Length];
            ms.Read(buffer, 0, buffer.Length);

            var actualContents = Encoding.UTF8.GetString(buffer);
            
            Assert.Equal(expectedContents, actualContents);
        }

        [Fact]
        public void TestBSACreation()
        {
            const string outputPath = "output.bsa";
            const string expectedContent = "Hello World!";
            const string dummyFile = "bsa-dummy-file.txt";
            File.WriteAllText(dummyFile, expectedContent, Encoding.ASCII);
            var fi = new FileInfo(dummyFile);
            var expectedLength = fi.Length;
            
            var creator = new BSACreator();
            creator.AddFile(dummyFile, "text\\test.txt");
            creator.WriteToFile(outputPath);
            
            Assert.True(File.Exists(outputPath));

            using var reader = new BSAReader(outputPath);
            Assert.Single(reader.Folders);
            Assert.Single(reader.Files);

            var folder = reader.Folders.First();
            var file = reader.Files.First();
            
            Assert.Equal("text", folder.Name);
            Assert.Equal("test.txt", file.Name);
            Assert.Equal((uint) expectedLength, file.Size);
            
            using var ms = new MemoryStream();
            reader.CopyFileTo(file, ms);
            var buffer = new byte[ms.Length];
            ms.Read(buffer, 0, buffer.Length);

            var actualContents = Encoding.ASCII.GetString(buffer);
            Assert.Equal(expectedContent, actualContents);
        }
    }
}
