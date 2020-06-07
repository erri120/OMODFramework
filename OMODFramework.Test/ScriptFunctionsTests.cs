using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using OMODFramework.Scripting;
using Xunit;

namespace OMODFramework.Test
{
    public class ScriptFunctionsTests
    {
        private OMOD OMOD { get; }
        private ScriptFunctions ScriptFunctions { get; set; }
        private ScriptReturnData ScriptReturnData { get; set; }

        private class Settings : IScriptSettings
        {
            public FrameworkSettings FrameworkSettings => FrameworkSettings.DefaultFrameworkSettings;
            public IScriptFunctions ScriptFunctions => new Functions();
        }

        private class Functions : IScriptFunctions
        {
            public bool DataFileExists(string file)
            {
                throw new NotImplementedException();
            }

            public DialogResult DialogYesNo(string title)
            {
                throw new NotImplementedException();
            }

            public DialogResult DialogYesNo(string title, string message)
            {
                throw new NotImplementedException();
            }

            public void DisplayImage(Bitmap image, string? title)
            {
                throw new NotImplementedException();
            }

            public void DisplayText(string text, string? title)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<string> GetActiveOMODNames()
            {
                throw new NotImplementedException();
            }

            public byte[] GetDataFileFromBSA(string file)
            {
                throw new NotImplementedException();
            }

            public byte[] GetDataFileFromBSA(string bsa, string file)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<ScriptESP> GetESPs()
            {
                throw new NotImplementedException();
            }

            public Version GraphicsExtenderVersion()
            {
                throw new NotImplementedException();
            }

            public bool HasGraphicsExtender()
            {
                throw new NotImplementedException();
            }

            public bool HasScriptExtender()
            {
                throw new NotImplementedException();
            }

            public string InputString(string? title, string? initialText)
            {
                throw new NotImplementedException();
            }

            public void Message(string msg)
            {
                throw new NotImplementedException();
            }

            public void Message(string msg, string title)
            {
                throw new NotImplementedException();
            }

            public Version OblivionVersion()
            {
                throw new NotImplementedException();
            }

            public Version OBSEPluginVersion(string file)
            {
                throw new NotImplementedException();
            }

            public void Patch(string from, string to)
            {
                throw new NotImplementedException();
            }

            public byte[] ReadExistingDataFile(string file)
            {
                throw new NotImplementedException();
            }

            public string ReadOblivionINI(string section, string name)
            {
                throw new NotImplementedException();
            }

            public string ReadRenderInfo(string name)
            {
                throw new NotImplementedException();
            }

            public Version ScriptExtenderVersion()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<int> Select(IEnumerable<string> items, string title, bool isMultiSelect, IEnumerable<Bitmap> previews, IEnumerable<string> descriptions)
            {
                throw new NotImplementedException();
            }
        }

        public ScriptFunctionsTests()
        {
            var options = CreationTest.CreateOptions();
            OMOD.CreateOMOD(options, new FileInfo("script-test.omod"));

            OMOD = new OMOD(new FileInfo("script-test.omod"));
            OMOD.OMODFile.Decompress(OMODEntryFileType.Data);
            if(OMOD.HasFile(OMODEntryFileType.PluginsCRC))
                OMOD.OMODFile.Decompress(OMODEntryFileType.Plugins);

            ScriptReturnData = new ScriptReturnData();
            ScriptFunctions = new ScriptFunctions(new Settings(), OMOD, ScriptReturnData);
        }

        private void Cleanup()
        {
            ScriptReturnData = new ScriptReturnData();
            ScriptFunctions = new ScriptFunctions(new Settings(), OMOD, ScriptReturnData);
        }
        
        [Fact]
        public void TestGetFiles()
        {
            Cleanup();

            var dataFiles = ScriptFunctions.GetDataFiles("files", "*", false);
            Assert.NotEmpty(dataFiles);

            var dataFolders = ScriptFunctions.GetDataFolders("", "*", true);
            Assert.Single(dataFolders);
        }

        [Fact]
        public void TestInstall()
        {
            Cleanup();

            Assert.Empty(ScriptReturnData.DataFiles);
            ScriptFunctions.InstallAllDataFiles();
            Assert.NotEmpty(ScriptReturnData.DataFiles);
            ScriptFunctions.DontInstallAnyDataFiles();
            Assert.Empty(ScriptReturnData.DataFiles);

            ScriptFunctions.InstallDataFolder("files", false);
            Assert.NotEmpty(ScriptReturnData.DataFiles);
        }

        [Fact]
        public void TestCopy()
        {
            Cleanup();

            ScriptFunctions.CopyDataFile("files//0.txt", "text//0.txt");
            Assert.True(ScriptReturnData.DataFiles[0].Output.EqualsPath("text\\0.txt"));

            ScriptFunctions.DontInstallAnyDataFiles();

            ScriptFunctions.CopyDataFolder("files", "text", false);
            Assert.NotEmpty(ScriptReturnData.DataFiles);
            Assert.True(ScriptReturnData.DataFiles.TrueForAll(x => x.Output.StartsWith("text")));
        }
    }
}
