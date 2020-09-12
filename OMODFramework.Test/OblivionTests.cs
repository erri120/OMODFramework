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

using System;
using System.IO;
using System.Text;
using Xunit;

namespace OMODFramework.Test
{
    public class OblivionTests
    {
        [Fact]
        public void TestOblivionINI()
        {
            const string file = "oblivion-test-ini.ini";
            const string contents = "[General]\nname=Peter Griffin\nage=18 ;very important!";

            File.WriteAllText(file, contents, Encoding.UTF8);

            var actualName = OblivionINI.GetINIValue(file, "general", "Name");
            var actualAge = OblivionINI.GetINIValue(file, "gENeRaL", "aGe");
            
            Assert.Equal("Peter Griffin", actualName);
            Assert.Equal("18", actualAge);
        }

        [Fact]
        public void TestOblivionRendererInfo()
        {
            const string file = "oblivion-test-rendererInfo.txt";
            const string contents = @"
	SLI mode           		: no
	Water shader       		: yes
	Water reflections  		: yes
	Water displacement 		: yes
	Water high res     		: yes
	Multisample Type   		: 0
	Shader Package     		: 13";
            
            File.WriteAllText(file, contents, Encoding.UTF8);

            var actualWaterDisplacement = OblivionRendererInfo.GetInfo(file, "Water displacement");
            var actualShaderPackage = OblivionRendererInfo.GetInfo(file, "shader package");
            
            Assert.Equal("yes", actualWaterDisplacement);
            Assert.Equal("13", actualShaderPackage);
        }

        [Fact]
        public void TestOblivionSDP()
        {
            const string file = "oblivion-test-shaderpackage.sdp";
            const string outputFile = "oblivion-test-shaderpackage-output.sdp";

            if (File.Exists(file))
                File.Delete(file);
            
            using var fs = File.Open(file, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
            using (var bw = new BinaryWriter(fs, Encoding.UTF8, false))
            {
                const byte count = 2;
                bw.Write(0x64);
                bw.Write((uint) count);
                bw.Write(0x1234);

                for (byte i = 0; i < count; i++)
                {
                    Span<char> name = new char[0x100];
                    name.Fill((char) 0);
                    name[0] = 'B';
                    name[1] = 'o';
                    name[2] = 'b';
                    name[3] = Convert.ToChar($"{i}");
                    
                    bw.Write(name);
                    bw.Write(0x20);
                    Span<byte> data = new byte[0x20];
                    data.Fill(0);
                    data[0] = i;
                    bw.Write(data);
                }
            }

            var newData = new byte[10];
            OblivionSDP.EditShader(file, "bob1", newData, outputFile);

            using var outputFs = File.Open(outputFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var br = new BinaryReader(outputFs, Encoding.UTF8, false);
            
            Assert.Equal(0x64, br.ReadInt32());
            Assert.Equal(2, br.ReadInt32());
            br.ReadInt32();

            br.ReadChars(0x100);
            br.ReadInt32();
            br.ReadBytes(0x20);
            
            br.ReadChars(0x100);
            //new length
            Assert.Equal(10, br.ReadInt32());
        }
    }
}
