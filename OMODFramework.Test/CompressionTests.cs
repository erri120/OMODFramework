#define DELETEFILES

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Alphaleonis.Win32.Security;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OMODFramework.Classes;
using File = Alphaleonis.Win32.Filesystem.File;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace OMODFramework.Test
{
    [TestClass]
    public class CompressionTests
    {
        private const string HelloFile = "hello.txt";

        [TestInitialize]
        public void Setup()
        {
            Framework.TempDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestTempDir");

            File.WriteAllText(HelloFile, "Hello World!");
        }

        [TestMethod]
        public void TestWriteStreamToZip()
        {
            using (var zipStream = new ZipOutputStream(File.Open("hello.zip", FileMode.CreateNew)))
            using (var bw = new BinaryWriter(zipStream))
            {
                var ze = new ZipEntry(HelloFile);
                zipStream.PutNextEntry(ze);
                var fs = File.OpenRead(HelloFile);
                WriteStreamToZip(bw, fs);
                bw.Flush();
                fs.Close();
            }

            using (var zf = new ZipFile(File.OpenRead("hello.zip")))
            using (var fs = new FileStream("hello_out.txt", FileMode.Create))
            {
                zf.GetInputStream(zf.GetEntry("hello.txt")).CopyTo(fs);
            }

            Assert.AreEqual(File.GetSize("hello.txt"), File.GetSize("hello_out.txt"));
            Assert.AreEqual(File.GetHash("hello.txt", HashType.CRC32), File.GetHash("hello_out.txt", HashType.CRC32));
        }

        [TestMethod]
        public void TestCompression()
        {
            var files = new List<string> { HelloFile };
            var filesPath = new List<string> { "" };

            CompressionHandler.CompressFiles(files, filesPath, out var dataCompressed, out var dataInfo,
                CompressionType.Zip, CompressionLevel.Medium);

            Assert.IsNotNull(dataCompressed);
            Assert.IsNotNull(dataInfo);

            using (var zipStream = new ZipOutputStream(File.Open("hello.zip", FileMode.CreateNew)))
            using (var bw = new BinaryWriter(zipStream))
            {
                zipStream.SetLevel(0);
                var ze = new ZipEntry("hello");
                zipStream.PutNextEntry(ze);
                WriteStreamToZip(bw, dataCompressed);
                bw.Flush();
                
                zipStream.SetLevel(ZipHandler.GetCompressionLevel(CompressionLevel.Medium));
                ze = new ZipEntry("hello.crc");
                zipStream.PutNextEntry(ze);
                WriteStreamToZip(bw, dataInfo);
                bw.Flush();
            }

            Assert.IsTrue(File.Exists("hello.zip"));

            dataCompressed.Close();
            dataInfo.Close();
        }

        private static void WriteStreamToZip(BinaryWriter bw, Stream input)
        {
            input.Position = 0;
            byte[] buffer = new byte[4096];
            int upTo = 0;

            while (input.Length - upTo > 4096)
            {
                input.Read(buffer, 0, 4096);
                bw.Write(buffer, 0, 4096);
                upTo += 4096;
            }

            if (input.Length - upTo <= 0)
                return;

            input.Read(buffer, 0, (int)(input.Length - upTo));
            bw.Write(buffer, 0, (int)(input.Length - upTo));
        }

        [TestCleanup]
        public void Cleanup()
        {
#if DELETEFILES
            if(File.Exists("hello.txt"))
                File.Delete("hello.txt");
            if(File.Exists("hello.zip"))
                File.Delete("hello.zip");
            if(File.Exists("hello_out.txt"))
                File.Delete("hello_out.txt");
#endif
        }
    }
}
