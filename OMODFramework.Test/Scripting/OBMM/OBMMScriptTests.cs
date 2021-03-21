using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OMODFramework.Scripting;
using OMODFramework.Scripting.Data;
using Xunit;

namespace OMODFramework.Test.Scripting.OBMM
{
    public class OBMMScriptTests
    {
        [Theory]
        //https://www.nexusmods.com/oblivion/mods/40462
        [InlineData("NoMaaM Breathing Idles V1 OMOD-40462-1-0.omod", 1053485)]
        //https://www.nexusmods.com/oblivion/mods/35551
        [InlineData("NoMaaM BBB Animation Replacer V3_1 OMOD-35551-3-1.omod", 7388700)]
        //https://www.nexusmods.com/oblivion/mods/34442
        [InlineData("HGEC Body with BBB v1dot12-34442.omod", 34641874)]
        //https://www.nexusmods.com/oblivion/mods/24078
        [InlineData("EVE_HGEC_BodyStock and Clothing OMOD-24078.omod", 55442432)]
        //https://www.nexusmods.com/oblivion/mods/40532
        [InlineData("Robert Male Body Replacer v52 OMOD-40532-1.omod", 155453816)]
        public void TestScriptExecution(string fileName, long expectedFileLength)
        {
            /*
             * Get the mods listed above and put them into the OMODFramework.Test/files/obmm-scripting folder. They
             * will be copied to the output folder post build. I don't want to download those mods in the CI so this
             * test can only be run locally.
             */
            
            //TODO: make this run on the CI without having to download the mods (use data+plugin files index)
            
            var file = Path.Combine("files", "obmm-scripting", fileName);
            if (!File.Exists(file))
                return;

            var fi = new FileInfo(file);
            var actualFileLength = fi.Length;
            Assert.Equal(expectedFileLength, actualFileLength);

            var resultsFile = Path.Combine("files", "obmm-scripting", fileName + "-Results.txt");
            Assert.True(File.Exists(resultsFile));
            
            using var omod = new OMOD(file);
            
            var externalScriptFunctions = new ExternalScriptFunctionsForTesting(resultsFile, fileName);
            var settings = new OMODScriptSettings(externalScriptFunctions)
            {
                DryRun = true,
                UseBitmapOverloads = false
            };
            
            var srd = OMODScriptRunner.RunScript(omod, settings);

            VerifyFiles(externalScriptFunctions.DataFiles, srd.DataFiles);
            VerifyFiles(externalScriptFunctions.PluginFiles, srd.PluginFiles);
        }

        private static void VerifyFiles(IReadOnlyCollection<string> expected, IReadOnlyCollection<ScriptReturnFile> actual)
        {
            var notInExpected = actual.ToHashSet();
            var inActualCount = notInExpected.RemoveWhere(x => expected.Contains(x.Output, StringComparer.OrdinalIgnoreCase));

            var notInActual = expected.ToHashSet();
            var inExpectedCount = notInActual.RemoveWhere(x => actual.Any(y => y.Output.Equals(x, StringComparison.OrdinalIgnoreCase)));
            
            Assert.Empty(notInExpected);
            Assert.Empty(notInActual);
            Assert.Equal(actual.Count, inActualCount);
            Assert.Equal(expected.Count, inExpectedCount);
            Assert.Equal(expected.Count, actual.Count);
        }

        private class ExternalScriptFunctionsForTesting : IExternalScriptFunctions
        {
            public readonly HashSet<string> DataFiles = new HashSet<string>();
            public readonly HashSet<string> PluginFiles = new HashSet<string>();
            //key: select title, value: item to select
            private readonly Dictionary<string, string> Selects = new Dictionary<string, string>();
            
            public ExternalScriptFunctionsForTesting(string resultsFile, string omodFileName)
            {
                var lines = File.ReadAllLines(resultsFile)
                    .Where(x => !string.IsNullOrWhiteSpace(x));

                var inData = false;
                var inPlugins = false;
                
                foreach (var current in lines)
                {
                    if (current[0] == '#')
                    {
                        var expectedFileName = current[1..];
                        Assert.Equal(expectedFileName, omodFileName);
                        continue;
                    }

                    if (current.StartsWith("Select:"))
                    {
                        //Select:Choose Breathing and BBB level:[$]Strong / BBB moderate
                        var line = current[7..];
                        var split = line.Split("[$]");
                        Selects.AddOrReplace(split[0], split[^1]);
                        continue;
                    }

                    if (current.StartsWith("$Data"))
                    {
                        inData = true;
                        inPlugins = false;
                        continue;
                    }

                    if (current.StartsWith("$Plugins"))
                    {
                        inPlugins = true;
                        inData = false;
                        continue;
                    }

                    if (inData)
                    {
                        DataFiles.Add(current.Replace("\\", "\\\\"));
                        continue;
                    }

                    if (inPlugins)
                    {
                        PluginFiles.Add(current.Replace("\\", "\\\\"));
                        continue;
                    }
                }
            }
            
            public void Message(string message) { }

            public void Message(string message, string title) { }

            public string InputString(string? title, string? initialText)
            {
                throw new NotImplementedException();
            }

            public DialogResult DialogYesNo(string message)
            {
                throw new NotImplementedException();
            }

            public DialogResult DialogYesNo(string message, string title)
            {
                throw new NotImplementedException();
            }

            public void DisplayImage(string imagePath, string? title) { }
            public void DisplayImage(Bitmap image, string? title) { }

            public void DisplayText(string text, string? title) { }

            public IEnumerable<int> Select(IEnumerable<string> items, string title, bool isMany,
                IEnumerable<Bitmap> previews, IEnumerable<string> descriptions)
            {
                throw new NotImplementedException();
            }

            
            public IEnumerable<int> Select(IEnumerable<string> items, string title, bool isMany, IEnumerable<string> previews, IEnumerable<string> descriptions)
            {
                var (_, value) = Selects.First(x => x.Key.Equals(title));
                var list = items.Select(x => x.StartsWith("|") ? x[1..] : x).ToList();

                var results = new List<int>();
                
                if (value.Contains('|'))
                {
                    var split = value.Split('|');
                    foreach (var s in split)
                    {
                        var i = list.IndexOf(s);
                        Assert.NotEqual(-1, i);
                        results.Add(i);
                    }
                }
                else
                {
                    var i = list.IndexOf(value);
                    Assert.NotEqual(-1, i);
                    results.Add(i);
                }

                return results;
            }

            public bool HasScriptExtender()
            {
                throw new NotImplementedException();
            }

            public bool HasGraphicsExtender()
            {
                throw new NotImplementedException();
            }

            public Version GetScriptExtenderVersion()
            {
                throw new NotImplementedException();
            }

            public Version GetGraphicsExtenderVersion()
            {
                throw new NotImplementedException();
            }

            public Version GetOblivionVersion()
            {
                return new Version(1, 2, 416, 0);
            }

            public Version GetOBSEPluginVersion(string file)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<Plugin> GetPlugins()
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

            public bool DataFileExists(string path)
            {
                return false;
            }

            public string ReadINI(string section, string valueName)
            {
                throw new NotImplementedException();
            }

            public string ReadRendererInfo(string valueName)
            {
                throw new NotImplementedException();
            }

            public void SetNewLoadOrder(string[] plugins)
            {
                throw new NotImplementedException();
            }
        }
    }
}
