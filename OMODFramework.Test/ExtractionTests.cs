using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.dotMemoryUnit;
using Xunit;
using Xunit.Abstractions;

namespace OMODFramework.Test
{
    public class ExtractionTests
    {
        public ExtractionTests(ITestOutputHelper outputHelper)
        {
            DotMemoryUnitTestOutput.SetOutputMethod(outputHelper.WriteLine);
        }

        [Fact]
        [DotMemoryUnit(FailIfRunWithoutSupport = false)]
        public void TestExtraction()
        {
            var isolator = new Action(() =>
            {
                using var omod = new OMOD(new FileInfo("M:\\Projects\\omod\\DarkUId DarN 16 OMOD Version-11280.omod"));
                var dic = new Dictionary<OMODFile, long>
                {
                    {OMODFile.Config, 384},
                    {OMODFile.Readme, 23297},
                    {OMODFile.Script, 36141},
                    {OMODFile.Image, 188845},
                    {OMODFile.DataCRC, 33071},
                    {OMODFile.Data, 18427763}
                };

                dic.Do(pair =>
                {
                    var (key, value) = pair;
                    using var stream = omod.ExtractFile(key);
                    Assert.Equal(value, stream.Length);
                });

                var image = omod.ExtractImage();
                Assert.Equal(1440, image.Width);
                Assert.Equal(900, image.Height);

                var dataFiles = omod.GetDataFileList().ToList();
                Assert.NotEmpty(dataFiles);

                var dir = new DirectoryInfo("output");
                dir.Create();
                omod.ExtractDataFiles(dir);

                var expectedSize = dataFiles.Select(x => x.Length).Aggregate((x, y) => x + y);
                var actualSize = dir.EnumerateFiles("*", SearchOption.AllDirectories).Select(x => x.Length)
                    .Aggregate((x, y) => x + y);

                Assert.Equal(expectedSize, actualSize);
            });

            isolator();

            dotMemory.Check(memory => Assert.Equal(0, memory.GetObjects(where => where.Type.Is<OMOD>()).ObjectsCount));
        }
    }
}
