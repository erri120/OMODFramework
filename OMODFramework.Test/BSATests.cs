using System.IO;
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

            var reader = new BSAReader(path);
            Assert.Equal((uint) 115, reader.FileCount);
            Assert.Equal((uint) 10, reader.FolderCount);
            Assert.Equal((uint) 2084, reader.TotalFileNameLength);
            Assert.Equal((uint) 114, reader.TotalFolderNameLength);
        }
    }
}
