using System.IO;
using System.Text;
using OMODFramework.Compression;
using Xunit;

namespace OMODFramework.Test
{
    public class CompressionTests
    {
        private static string GetDummyString()
        {
            const string text = "Hello World!";
            var sb = new StringBuilder();
            for (var i = 0; i < 1000; i++)
            {
                sb.AppendLine(text);
            }

            return sb.ToString();
        }
        
        [Fact]
        public void TestSevenZipCompression()
        {
            var inputText = GetDummyString();

            var bytes = Encoding.UTF8.GetBytes(inputText);
            using var inputStream = new MemoryStream(bytes, false);
            
            using var compressedStream = CompressionHandler.SevenZipCompress(inputStream, SevenZipCompressionLevel.Medium);
            using var decompressedStream = CompressionHandler.SevenZipDecompress(compressedStream, inputStream.Length);

            var outputBytes = new byte[decompressedStream.Length];
            decompressedStream.Read(outputBytes, 0, outputBytes.Length);

            var outputString = Encoding.UTF8.GetString(outputBytes, 0, outputBytes.Length);
            
            Assert.Equal(inputText, outputString);
        }

        [Fact]
        public void TestZipCompression()
        {
            var inputText = GetDummyString();

            var bytes = Encoding.UTF8.GetBytes(inputText);
            using var inputStream = new MemoryStream(bytes, true);
            
            using var compressedStream = CompressionHandler.ZipCompress(inputStream, System.IO.Compression.CompressionLevel.Optimal);
            using var decompressedStream = CompressionHandler.ZipDecompress(compressedStream, inputStream.Length);

            var outputBytes = new byte[decompressedStream.Length];
            decompressedStream.Read(outputBytes, 0, outputBytes.Length);

            var outputString = Encoding.UTF8.GetString(outputBytes, 0, outputBytes.Length);
            
            Assert.Equal(inputText, outputString);
        }
    }
}
