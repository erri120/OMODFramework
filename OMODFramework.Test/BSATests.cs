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
    }
}
