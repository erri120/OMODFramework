using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OMODFramework.Scripting;
using Xunit;

namespace OMODFramework.Test
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ScriptResults : NexusTestFixture
    {
        public List<ScriptResult> Results { get; }

        public ScriptResults()
        {
            Results = new List<ScriptResult>();
        }

        public void ReadFromFile(FileInfo file)
        {
            var lines = File.ReadAllLines(file.FullName);
            ScriptResult currentResult = null!;
            var inData = false;
            var inPlugin = false;

            foreach (var s in lines)
            {
                var current = s;
                if (current.StartsWith("#"))
                {
                    if (currentResult != null)
                    {
                        Results.Add(currentResult);
                    }

                    currentResult = new ScriptResult();

                    current = current.Substring(1);
                    var split = current.Split("-");
                    currentResult.ModID = int.Parse(split[0]);
                    currentResult.FileID = int.Parse(split[^1]);
                    continue;
                }

                if (current.StartsWith("Select:"))
                {
                    //Select:Choose BMF (Breast Movement Factor)[$]0.8 (Moderate)
                    current = current.Substring(7);
                    //Choose BMF (Breast Movement Factor)[$]0.8 (Moderate)
                    var split = current.Split("[$]");
                    currentResult!.Selects ??= new Dictionary<string, string>();
                    currentResult.Selects.Add(split[0], split[^1]);
                    continue;
                }

                if (current.StartsWith("$"))
                {
                    current = current.Substring(1);
                    if (current == "Data")
                    {
                        currentResult!.DataFiles = new HashSet<string>();
                        inData = true;
                        if (inPlugin)
                            inPlugin = false;
                    } else if (current == "Plugin")
                    {
                        currentResult!.PluginFiles = new HashSet<string>();
                        inPlugin = true;
                        if (inData)
                            inData = false;
                    }

                    continue;
                }

                if (inData)
                {
                    currentResult.DataFiles!.Add(current.ToLower());
                } else if (inPlugin)
                {
                    currentResult.PluginFiles!.Add(current.ToLower());
                }
            }

            Results.Add(currentResult);
        }

        public class ScriptResult
        {
            public int ModID { get; set; }
            public int FileID { get; set; }
            public HashSet<string>? DataFiles { get; set; }
            public HashSet<string>? PluginFiles { get; set; }
            public Dictionary<string, string>? Selects { get; set; }
        }
    }

    public class OBMMScriptTestSettings : IScriptSettings
    {
        private readonly ScriptResults.ScriptResult _result;

        public OBMMScriptTestSettings(ScriptResults.ScriptResult result)
        {
            _result = result;
        }

        public FrameworkSettings FrameworkSettings => FrameworkSettings.DefaultFrameworkSettings;
        public IScriptFunctions ScriptFunctions => new OBMMScriptTestFunctions(_result);

        private class OBMMScriptTestFunctions : IScriptFunctions
        {
            private readonly ScriptResults.ScriptResult _result;

            public OBMMScriptTestFunctions(ScriptResults.ScriptResult result)
            {
                _result = result;
            }

            public void Message(string msg)
            {
            }

            public void Message(string msg, string title)
            {
            }

            public IEnumerable<int> Select(IEnumerable<string> items, string title, bool isMultiSelect, IEnumerable<Bitmap> previews, IEnumerable<string> descriptions)
            {
                if (_result.Selects == null)
                    throw new Exception();

                var select = _result.Selects.First(x => x.Key.Equals(title, StringComparison.InvariantCultureIgnoreCase)).Value;
                var result = new List<int>();

                var list = items.Select(x => x.StartsWith("|") ? x.Substring(1) : x).ToList();
                var index = list.IndexOf(select);

                if (index == -1)
                    throw new Exception();

                result.Add(index);

                return result;
            }

            public string InputString(string? title, string? initialText)
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

            public void Patch(string @from, string to)
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
                return false;
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
                return new Version(1,2,214,0);
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
    }

    public class OBMMScriptTest : IClassFixture<ScriptResults>
    {
        private readonly ScriptResults _fixture;

        public OBMMScriptTest(ScriptResults fixture)
        {
            _fixture = fixture;
            _fixture.ReadFromFile(new FileInfo("ScriptResults.txt"));
        }

        [Fact]
        public void CompareScriptResultWithOriginalOBMM()
        {
            var fileList = new List<NexusFile>
            {
                new NexusFile(35551,87078, "NoMaaM BBB Animation Replacer V3_1 OMOD-35551-3-1.omod"),
                new NexusFile(40462,85415, "NoMaaM Breathing Idles V1 OMOD-40462-1-0.omod"),
                new NexusFile(34442,80882, "HGEC Body with BBB v1dot12-34442.omod"),
                new NexusFile(24078,41472, "EVE_HGEC_BodyStock and Clothing OMOD-24078.omod"),
                //new NexusFile(40532,90010, "Robert Male Body Replacer v52 OMOD-40532-1.omod")
            };

            fileList.Do(file =>
            {
                var res = file.Download(_fixture.Client);
                Assert.True(res);

                var result = _fixture.Results.FirstOrDefault(x => x.ModID == file.ModID && x.FileID == file.FileID);
                Assert.NotNull(result);
                var settings = new OBMMScriptTestSettings(result);

                using var omod = new OMOD(new FileInfo(file.Path));
                //omod.ExtractDataFiles(new DirectoryInfo("omodscripttest-output"));
                var srd = ScriptRunner.ExecuteScript(omod, settings);
                Assert.NotNull(srd);

                if (srd.DataFiles.Count > 0)
                {
                    Assert.NotNull(result.DataFiles);
                    Assert.NotEmpty(result.DataFiles);
                }

                if (srd.PluginFiles.Count > 0)
                {
                    Assert.NotNull(result.PluginFiles);
                    Assert.NotEmpty(result.PluginFiles);
                }

                var dataFiles = srd.DataFiles.Select(x => x.Output).ToHashSet();

                VerifyResult(result.DataFiles!, dataFiles);

                if (result.PluginFiles != null)
                {
                    VerifyResult(result.PluginFiles, srd.PluginFiles.Select(x => x.Output).ToHashSet());
                }
            });
        }

        private static void ToFile(IEnumerable<string> list, FileSystemInfo file)
        {
            File.WriteAllText(file.FullName, list.OrderBy(x => x).ToAggregatedString("\n"));
        }

        private static void VerifyResult(HashSet<string> expectedFiles, HashSet<string> actualFiles)
        {
            actualFiles = actualFiles.Select(x => x.ToLower()).ToHashSet();
            //ToFile(expectedFiles, new FileInfo("out.txt"));
            //ToFile(actualFiles, new FileInfo("out2.txt"));
            Assert.Equal(expectedFiles.Count, actualFiles.Count);

            var notInExpected = actualFiles.Where(x => expectedFiles.All(y => !y.Equals(x)));
            Assert.Empty(notInExpected);
            var notInActual = expectedFiles.Where(x => actualFiles.All(y => !y.Equals(x)));
            Assert.Empty(notInActual);
        }
    }
}
