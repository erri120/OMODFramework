using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OMODFramework.Scripting;
using Xunit;

namespace OMODFramework.Test
{
    public class ScriptTests
    {
        private class Functions : IScriptFunctions
        {
            public void Warn(string msg)
            {
                throw new NotImplementedException();
            }

            public void Message(string msg) { }

            public void Message(string msg, string title)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<int> Select(IEnumerable<string> items, string title, bool isMultiSelect, IEnumerable<string> previews, IEnumerable<string> descriptions)
            {
                return new List<int>{0};
            }

            public string InputString(string? title, string? initialText)
            {
                throw new NotImplementedException();
            }

            public DialogResult DialogYesNo(string title)
            {
                return DialogResult.Yes;
            }

            public DialogResult DialogYesNo(string title, string message)
            {
                return DialogResult.Yes;
            }

            public void DisplayImage(FileInfo file, string title) { }

            public void DisplayText(string text, string title) { }

            public void Patch(string from, string to)
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

            public bool DataFileExists(string file)
            {
                return true;
            }

            public bool HasScriptExtender()
            {
                throw new NotImplementedException();
            }

            public bool HasGraphicsExtender()
            {
                throw new NotImplementedException();
            }

            public Version ScriptExtenderVersion()
            {
                throw new NotImplementedException();
            }

            public Version GraphicsExtenderVersion()
            {
                throw new NotImplementedException();
            }

            public Version OblivionVersion()
            {
                return new Version(1, 2, 214);
            }

            public Version OBSEPluginVersion(string file)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<ScriptESP> GetESPs()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<string> GetActiveOMODNames()
            {
                throw new NotImplementedException();
            }

            public byte[] ReadExistingDataFile(string file)
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
        }

        private class Settings : IScriptSettings
        {
            public FrameworkSettings FrameworkSettings => FrameworkSettings.DefaultFrameworkSettings;
            public IScriptFunctions ScriptFunctions => new Functions();
        }

        [Fact]
        public void ScriptTest()
        {
            var dic = new Dictionary<FileInfo, (int, int)>
            {
                {new FileInfo("M:\\Projects\\omod\\NoMaaM BBB Animation Replacer V3_1 OMOD-35551-3-1.omod"), (315, 0)},
                {new FileInfo("M:\\Projects\\omod\\NoMaaM Breathing Idles V1 OMOD-40462-1-0.omod"), (26, 0)},
                {new FileInfo("M:\\Projects\\omod\\HGEC Body with BBB v1dot12-34442.omod"), (134, 0)},
                {new FileInfo("M:\\Projects\\omod\\EVE_HGEC_BodyStock and Clothing OMOD-24078.omod"), (251, 3)},
                {new FileInfo("M:\\Projects\\omod\\Robert Male Body Replacer v52 OMOD-40532-1.omod"), (294, 1)}
            };

            var srdDic = dic.Select(pair =>
            {
                var (key, (item1, item2)) = pair;
                using var omod = new OMOD(key);
                omod.GetDataFileList();
                if (omod.HasFile(OMODFile.PluginsCRC))
                    omod.GetPlugins();
                return (ScriptRunner.ExecuteScript(omod, new Settings()), item1, item2);
            }).ToList();

            Assert.NotEmpty(srdDic);
            srdDic.Do(tuple =>
            {
                var (scriptReturnData, dataFilesLength, pluginFilesLength) = tuple;
                Assert.NotEmpty(scriptReturnData.DataFiles);
                Assert.Equal(dataFilesLength, scriptReturnData.DataFiles.Count);
                Assert.Equal(pluginFilesLength, scriptReturnData.PluginFiles.Count);
            });
        }
    }
}
