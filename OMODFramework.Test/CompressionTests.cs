using System.IO;
using System.Text;
using Xunit;

namespace OMODFramework.Test
{
    public class CompressionTests
    {
        [Fact]
        public void TestSevenZipCompression()
        {
            const string expectedString = "Hello World";

            var bytes = Encoding.UTF8.GetBytes(expectedString);
            using var ms = new MemoryStream(bytes.Length);
            ms.Write(bytes, 0, bytes.Length);
            ms.Position = 0;

            using var compressedStream = CompressionHandler.SevenZipCompress(ms, CompressionLevel.Medium);
            using var decompressedStream = CompressionHandler.SevenZipDecompress(compressedStream, bytes.Length);

            var buffer = new byte[bytes.Length];
            decompressedStream.Read(buffer, 0, bytes.Length);

            var actualString = Encoding.UTF8.GetString(buffer);

            Assert.Equal(expectedString, actualString);
        }

        [Fact]
        public void TestZipCompression()
        {
            const string expectedString = "Hello World";

            var bytes = Encoding.UTF8.GetBytes(expectedString);
            using var ms = new MemoryStream(bytes.Length);
            ms.Write(bytes, 0, bytes.Length);
            ms.Position = 0;

            using var compressedStream = CompressionHandler.ZipCompress(ms, CompressionLevel.Medium);
            using var decompressedStream = CompressionHandler.ZipDecompress(compressedStream, bytes.Length);

            var buffer = new byte[bytes.Length];
            decompressedStream.Read(buffer, 0, bytes.Length);

            var actualString = Encoding.UTF8.GetString(buffer);

            Assert.Equal(expectedString, actualString);
        }
    }
}
