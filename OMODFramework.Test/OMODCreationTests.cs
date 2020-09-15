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
using System.IO;
using System.Linq;
using Xunit;

namespace OMODFramework.Test
{
    public class OMODCreationTests
    {
        [Fact]
        public void TestOMODCreation()
        {
            const string omodOutput = "omod-creation-test-output.omod";
            const string dataDir = "omod-creation-test";
            const string extractionDir = "omod-creation-extraction";
            const int count = 4;

            var options = DummyOMOD.CreateDummyCreationOptions(dataDir, count, 1 << 20);

            Assert.True(options.VerifyOptions(false));
            OMOD.CreateOMOD(options, omodOutput);
            Assert.True(File.Exists(omodOutput));
            
            using var omod = new OMOD(omodOutput);
            
            Assert.Equal(options.Name, omod.OMODConfig.Name);
            Assert.Equal(options.Author, omod.OMODConfig.Author);
            Assert.Equal(options.Description, omod.OMODConfig.Description);
            Assert.Equal(options.Email, omod.OMODConfig.Email);
            Assert.Equal(options.Website, omod.OMODConfig.Website);
            
            Assert.Equal(options.Readme, omod.GetReadme());

            List<OMODCompressedFile> dataFiles = omod.GetDataFilesInfo().ToList();
            
            Assert.NotEmpty(dataFiles);
            Assert.Equal(options.DataFiles.Count, dataFiles.Count);
            
            Assert.True(dataFiles.All(x =>
            {
                var file = options.DataFiles.First(y => y.To.Equals(x.Name));
                return CRCUtils.FromFile(file.From).Equals(x.CRC);
            }));
            
            if (Directory.Exists(extractionDir))
                Directory.Delete(extractionDir, true);

            Directory.CreateDirectory(extractionDir);
            
            omod.ExtractFilesParallel(true, extractionDir, 2);
            
            OMODExtractionTests.CheckExtractedFiles(dataFiles, extractionDir);
        }
    }
}
