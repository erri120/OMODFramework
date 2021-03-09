using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using OMODFramework.Oblivion;
using Xunit;

namespace OMODFramework.Test
{
    public class OblivionTests
    {
        [Fact]
        public void TestOblivionINI_Stream()
        {
            const string contents = "[General]\nname=Peter Griffin\nage=18 ;very important!";
            var bytes = Encoding.UTF8.GetBytes(contents);

            using var ms = new MemoryStream(bytes.Length);
            ms.Write(bytes, 0, bytes.Length);
            ms.Position = 0;

            var name = OblivionINI.GetINIValue(ms, "general", "Name", "TestCacheName");
            var age = OblivionINI.GetINIValue(ms, "gENeRaL", "aGe", "TestCacheName");
            var nothing = OblivionINI.GetINIValue(ms, "General", "Address", "TestCacheName");
            
            Assert.Equal("Peter Griffin", name);
            Assert.Equal("18", age);
            Assert.Null(nothing);
        }
        
        [Fact]
        public void TestOblivionINI_File()
        {
            const string file = "oblivion-test-ini.ini";
            const string contents = "[General]\nname=Peter Griffin\nage=18 ;very important!";

            File.WriteAllText(file, contents, Encoding.UTF8);
            
            var name = OblivionINI.GetINIValue(file, "general", "Name");
            var age = OblivionINI.GetINIValue(file, "gENeRaL", "aGe");
            var nothing = OblivionINI.GetINIValue(file, "General", "Address");
            
            Assert.Equal("Peter Griffin", name);
            Assert.Equal("18", age);
            Assert.Null(nothing);
        }

        [Fact]
        [SuppressMessage("ReSharper", "IdentifierTypo")]
        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        public void TestOblivionRendererInfo_Stream()
        {
	        const string contents = @"
	SLI mode           		: no
	Water shader       		: yes
	Water reflections  		: maybe
	Water displacement 		: possible
	Water high res     		: certainly
	Multisample Type   		: 0
	Shader Package     		: 13";
            
            var bytes = Encoding.UTF8.GetBytes(contents);

            using var ms = new MemoryStream(bytes.Length);
            ms.Write(bytes, 0, bytes.Length);
            ms.Position = 0;

            var sliMode = OblivionRendererInfo.GetRendererInfo(ms, "SLI mode");
            var waterShader = OblivionRendererInfo.GetRendererInfo(ms, "Water shader");
            var waterReflections = OblivionRendererInfo.GetRendererInfo(ms, "Water reflections");
            var waterDisplacement = OblivionRendererInfo.GetRendererInfo(ms, "Water displacement");
            var waterHighRes = OblivionRendererInfo.GetRendererInfo(ms, "Water high res");
            var multisampleType = OblivionRendererInfo.GetRendererInfo(ms, "Multisample Type");
            var shaderPackage = OblivionRendererInfo.GetRendererInfo(ms, "Shader Package");
            var nothing = OblivionRendererInfo.GetRendererInfo(ms, "This does not exist");
            
            Assert.Equal("no", sliMode);
            Assert.Equal("yes", waterShader);
            Assert.Equal("maybe", waterReflections);
            Assert.Equal("possible", waterDisplacement);
            Assert.Equal("certainly", waterHighRes);
            Assert.Equal("0", multisampleType);
            Assert.Equal("13", shaderPackage);
            Assert.Null(nothing);
        }
        
        [Fact]
        [SuppressMessage("ReSharper", "IdentifierTypo")]
        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        public void TestOblivionRendererInfo_File()
        {
            const string file = "oblivion-test-rendererInfo.txt";
            const string contents = @"
	SLI mode           		: no
	Water shader       		: yes
	Water reflections  		: maybe
	Water displacement 		: possible
	Water high res     		: certainly
	Multisample Type   		: 0
	Shader Package     		: 13";
            
            File.WriteAllText(file, contents, Encoding.UTF8);

            var sliMode = OblivionRendererInfo.GetRendererInfo(file, "SLI mode");
            var waterShader = OblivionRendererInfo.GetRendererInfo(file, "Water shader");
            var waterReflections = OblivionRendererInfo.GetRendererInfo(file, "Water reflections");
            var waterDisplacement = OblivionRendererInfo.GetRendererInfo(file, "Water displacement");
            var waterHighRes = OblivionRendererInfo.GetRendererInfo(file, "Water high res");
            var multisampleType = OblivionRendererInfo.GetRendererInfo(file, "Multisample Type");
            var shaderPackage = OblivionRendererInfo.GetRendererInfo(file, "Shader Package");
            var nothing = OblivionRendererInfo.GetRendererInfo(file, "This does not exist");
            
            Assert.Equal("no", sliMode);
            Assert.Equal("yes", waterShader);
            Assert.Equal("maybe", waterReflections);
            Assert.Equal("possible", waterDisplacement);
            Assert.Equal("certainly", waterHighRes);
            Assert.Equal("0", multisampleType);
            Assert.Equal("13", shaderPackage);
            Assert.Null(nothing);
        }

        [Fact]
        public void TestOblivionSDP_Stream()
        {
	        using var ms = new MemoryStream(1024);
	        using (var bw = new BinaryWriter(ms, Encoding.UTF8, true))
	        {
		        const uint count = 2;
		        bw.Write(0x64);
		        bw.Write(count);
		        bw.Write(0x248);

		        for (var i = 0; i < count; i++)
		        {
			        Span<char> name = new char[0x100];
			        name.Fill((char) 0);
			        var sName = $"MyShader{i}".AsSpan();
			        sName.CopyTo(name);
			        
			        bw.Write(name);
			        bw.Write(0x20);
			        Span<byte> data = new byte[0x20];
			        data.Fill(0);
			        data[0] = (byte) i;
			        bw.Write(data);
		        }
		        
		        ms.Position = 0;
	        }

	        var newData = new byte[0x10];
	        OblivionSDP.EditShaderPackage(ms, "MyShader1", newData, out var outputStream);

	        using var br = new BinaryReader(outputStream, Encoding.UTF8, false);
	        
	        Assert.Equal((uint) 0x64, br.ReadUInt32());
	        Assert.Equal((uint) 2, br.ReadUInt32());
	        //new changed size
	        Assert.Equal((uint) 0x238, br.ReadUInt32());

	        //skip first shader and the name of the second one
	        br.BaseStream.Seek(0x100 + 4 + 0x20 + 0x100, SeekOrigin.Current);
	        
	        //new length of the changed shader
	        Assert.Equal((uint) newData.Length, br.ReadUInt32());
        }

        [Fact]
        public void TestOblivionSDP_File()
        {
	        const string file = "oblivion-test-sdp.sdp";
	        using (var fs = File.Open(file, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
	        using (var bw = new BinaryWriter(fs, Encoding.UTF8, false))
	        {
		        const uint count = 2;
		        bw.Write(0x64);
		        bw.Write(count);
		        bw.Write(0x248);

		        for (var i = 0; i < count; i++)
		        {
			        Span<char> name = new char[0x100];
			        name.Fill((char) 0);
			        var sName = $"MyShader{i}".AsSpan();
			        sName.CopyTo(name);
			        
			        bw.Write(name);
			        bw.Write(0x20);
			        Span<byte> data = new byte[0x20];
			        data.Fill(0);
			        data[0] = (byte) i;
			        bw.Write(data);
		        }
	        }

	        var newData = new byte[0x10];
	        OblivionSDP.EditShaderPackage(file, "MyShader1", newData);

	        using var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
	        using var br = new BinaryReader(stream, Encoding.UTF8, false);
	        
	        Assert.Equal((uint) 0x64, br.ReadUInt32());
	        Assert.Equal((uint) 2, br.ReadUInt32());
	        //new changed size
	        Assert.Equal((uint) 0x238, br.ReadUInt32());

	        //skip first shader and the name of the second one
	        br.BaseStream.Seek(0x100 + 4 + 0x20 + 0x100, SeekOrigin.Current);
	        
	        //new length of the changed shader
	        Assert.Equal((uint) newData.Length, br.ReadUInt32());
        }
    }
}
