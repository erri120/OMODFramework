// /*
//     Copyright (C) 2020  erri120
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <https://www.gnu.org/licenses/>.
// */

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace OMODFramework.Test
{
    public class OMODExtractionTests : IClassFixture<NexusTestFixture>
    {
        private readonly NexusTestFixture _fixture;
        
        public OMODExtractionTests(NexusTestFixture fixture)
        {
            _fixture = fixture;
        }

        internal static void CheckExtractedFiles(IReadOnlyCollection<OMODCompressedFile> dataFiles, string dataOutputFolder)
        {
            var expectedSize = dataFiles
                .Select(x => x.Length)
                .Aggregate((x, y) => x + y);

            List<string> extractedFiles = Directory.EnumerateFiles(dataOutputFolder, "*", SearchOption.AllDirectories).ToList();
            Assert.Equal(dataFiles.Count, extractedFiles.Count);
            
            var actualSize = extractedFiles
                .Select(x =>
                {
                    var fi = new FileInfo(x);
                    return fi.Length;
                })
                .Aggregate((x, y) => x + y);
            
            Assert.Equal(expectedSize, actualSize);

            List<(string path, uint crc)> crcExtractedFiles = extractedFiles.AsParallel().Select(x => (path: x, crc: CRCUtils.FromFile(x))).ToList();
            List<OMODCompressedFile> badCRC = dataFiles.Where(x =>
            {
                var extracted = crcExtractedFiles.First(y => y.path.Equals(x.GetFullPath(dataOutputFolder)));
                var result = extracted.crc.Equals(x.CRC);
                //if (!result)
                //    Debugger.Break();
                return !result;
            }).ToList();

            Assert.Empty(badCRC);
        }

        [Fact]
        public void TestExtraction()
        {
            const string testOMOD = "DarkUId DarN 16 OMOD Version - 11280.omod";
            const string dataOutputFolder = "omod-extraction-test-output";
            
            if (Directory.Exists(dataOutputFolder))
                Directory.Delete(dataOutputFolder, true);
            
            var res = TestUtils.Download(_fixture.Client, 11280, 37571, testOMOD.InDownloadsFolder()).Result;
            
            Assert.True(res);
            
            using var omod = new OMOD(testOMOD.InDownloadsFolder());
            var dic = new Dictionary<OMODEntryFileType, long>
            {
                {OMODEntryFileType.Config, 384},
                {OMODEntryFileType.Readme, 23297},
                {OMODEntryFileType.Script, 36141},
                {OMODEntryFileType.Image, 188845},
                {OMODEntryFileType.DataCRC, 33071},
                {OMODEntryFileType.Data, 18427763}
            };
            
            dic.Do(pair =>
            {
                var (key, value) = pair;
                using var stream = omod.GetEntryFileStream(key);
                Assert.Equal(value, stream.Length);
            });

            var image = omod.GetImage();
            Assert.NotNull(image);
            Assert.Equal(1440, image!.Width);
            Assert.Equal(900, image!.Height);

            List<OMODCompressedFile> dataFiles = omod.GetDataFilesInfo().ToList();
            Assert.NotEmpty(dataFiles);
            
            if (Directory.Exists(dataOutputFolder))
                Directory.Delete(dataOutputFolder, true);

            Directory.CreateDirectory(dataOutputFolder);
            omod.ExtractFiles(true, dataOutputFolder);
            
            CheckExtractedFiles(dataFiles, dataOutputFolder);
        }

        [Fact]
        public void TestParallelExtraction()
        {
            const string testOMOD = "DarkUId DarN 16 OMOD Version - 11280.omod";
            const string dataOutputFolder = "omod-extraction-test-parallel-output";
            
            if (Directory.Exists(dataOutputFolder))
                Directory.Delete(dataOutputFolder, true);
            
            var res = TestUtils.Download(_fixture.Client, 11280, 37571, testOMOD.InDownloadsFolder()).Result;
            
            Assert.True(res);
            
            using var omod = new OMOD(testOMOD.InDownloadsFolder());
            
            List<OMODCompressedFile> dataFiles = omod.GetDataFilesInfo().ToList();
            Assert.NotEmpty(dataFiles);
            
            if (Directory.Exists(dataOutputFolder))
                Directory.Delete(dataOutputFolder, true);

            Directory.CreateDirectory(dataOutputFolder);
            
            omod.ExtractFilesParallel(true, dataOutputFolder, 4);
            
            CheckExtractedFiles(dataFiles, dataOutputFolder);
        }
    }
}
