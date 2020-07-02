﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.dotMemoryUnit;
using Xunit;
using Xunit.Abstractions;

namespace OMODFramework.Test
{
    public class ExtractionTests : IClassFixture<NexusTestFixture>
    {
        private readonly NexusTestFixture _fixture;

        public ExtractionTests(ITestOutputHelper outputHelper, NexusTestFixture fixture)
        {
            DotMemoryUnitTestOutput.SetOutputMethod(outputHelper.WriteLine);
            _fixture = fixture;
        }

        [Fact]
        [DotMemoryUnit(FailIfRunWithoutSupport = false)]
        public void TestExtraction()
        {
            var res = Utils.Download(_fixture.Client, 11280, 37571,
                "DarkUId DarN 16 OMOD Version - 11280.omod".InDownloadsFolder()).Result;
            Assert.True(res);

            var isolator = new Action(() =>
            {
                using var omod =
                    new OMOD(new FileInfo("DarkUId DarN 16 OMOD Version - 11280.omod".InDownloadsFolder()));
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
                    using var stream = omod.ExtractFile(key);
                    Assert.Equal(value, stream.Length);
                });

                var image = omod.GetImage();
                Assert.Equal(1440, image.Width);
                Assert.Equal(900, image.Height);

                omod.OMODFile.Decompress(OMODEntryFileType.Data);

                var dataFiles = omod.GetDataFiles().ToHashSet();
                Assert.NotEmpty(dataFiles);

                var dir = new DirectoryInfo("output");
                dir.Create();
                omod.OMODFile.ExtractAllDecompressedFiles(dir, true);

                var expectedSize = dataFiles.Select(x => x.Length).Aggregate((x, y) => x + y);
                var actualSize = dir.EnumerateFiles("*", SearchOption.AllDirectories).Select(x => x.Length)
                    .Aggregate((x, y) => x + y);

                Assert.Equal(expectedSize, actualSize);
            });

            isolator();

            dotMemory.Check(memory => Assert.Equal(0, memory.GetObjects(where => where.Type.Is<OMOD>()).ObjectsCount));
        }

        [Fact]
        public async Task TestMultiThreadedExtraction()
        {
            var nexusFile = new NexusFile(35551, 87078, "NoMaaM BBB Animation Replacer V3_1 OMOD-35551-3-1.omod");
            var res = nexusFile.Download(_fixture.Client);
            Assert.True(res);

            using var omod = new OMOD(new FileInfo(nexusFile.Path));

            var output = new DirectoryInfo("multi-threaded-output");
            await omod.ExtractDataFilesAsync(output, 4);

            var dataFiles = omod.GetDataFiles();

            var expectedSize = dataFiles.Select(x => x.Length).Aggregate((x, y) => x + y);
            var actualSize = output.EnumerateFiles("*", SearchOption.AllDirectories).Select(x => x.Length)
                .Aggregate((x, y) => x + y);

            Assert.Equal(expectedSize, actualSize);
        }
    }
}