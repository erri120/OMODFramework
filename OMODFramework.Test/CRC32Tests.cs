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

using System.IO;
using System.Text;
using Xunit;

namespace OMODFramework.Test
{
    public class CRC32Tests
    {
        [Fact]
        public void TestCRC32String()
        {
            const string input = "Hello";

            byte[] bytes = Encoding.UTF8.GetBytes(input, 0, input.Length);
            
            var crc = new CRC32();
            crc.Update(bytes);

            const uint expectedOutput = 0xF7D18982;
            var actualOutput = crc.Value;
            
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Fact]
        public void TestCRC32File()
        {
            const string file = "crc32-test-file.txt";
            const string input = "Hello";
            
            File.WriteAllText(file, input, Encoding.UTF8);
            
            const uint expectedOutput = 0xC7CF2CC1;
            var actualOutput = CRCUtils.FromFile(file);
            
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Fact]
        public void TestCRC32Stream()
        {
            const string input = "Hello";
            byte[] bytes = Encoding.UTF8.GetBytes(input, 0, input.Length);

            using var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, Encoding.UTF8, true))
            {
                bw.Write(bytes);
            }

            ms.Position = 0;

            const uint expectedOutput = 0xF7D18982;
            var actualOutput = CRCUtils.FromStream(ms);
            
            Assert.Equal(expectedOutput, actualOutput);
        }
    }
}
