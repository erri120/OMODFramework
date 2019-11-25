/*
    Copyright (C) 2019  erri120

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

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
                CompressionHandler.WriteStreamToZip(bw, fs);
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
                CompressionHandler.WriteStreamToZip(bw, dataCompressed);
                bw.Flush();
                
                zipStream.SetLevel(ZipHandler.GetCompressionLevel(CompressionLevel.Medium));
                ze = new ZipEntry("hello.crc");
                zipStream.PutNextEntry(ze);
                CompressionHandler.WriteStreamToZip(bw, dataInfo);
                bw.Flush();
            }

            Assert.IsTrue(File.Exists("hello.zip"));

            dataCompressed.Close();
            dataInfo.Close();
        }

        [TestCleanup]
        public void Cleanup()
        {
            Framework.CleanTempDir(true);

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
