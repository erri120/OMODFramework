using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OMODFramework.Scripting;
using Xunit;

namespace OMODFramework.Test.Scripting
{
    public class ExternalScriptFunctionsForTesting : IExternalScriptFunctions
    {
        public readonly HashSet<string> DataFiles = new HashSet<string>();

        public readonly HashSet<string> PluginFiles = new HashSet<string>();

        //key: select title, value: item to select
        private readonly Dictionary<string, string> Selects = new Dictionary<string, string>();

        public ExternalScriptFunctionsForTesting() { }
        
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
                    DataFiles.Add(current.MakePath());
                    continue;
                }

                if (inPlugins)
                {
                    PluginFiles.Add(current.MakePath());
                    continue;
                }
            }
        }

        public void Message(string message)
        {
        }

        public void Message(string message, string title)
        {
        }

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

        public void DisplayImage(string imagePath, string? title)
        {
        }

        public void DisplayImage(Bitmap image, string? title)
        {
        }

        public void DisplayText(string text, string? title)
        {
        }

        public IEnumerable<int> Select(IEnumerable<string> items, string title, bool isMany,
            IEnumerable<Bitmap> previews, IEnumerable<string> descriptions)
        {
            throw new NotImplementedException();
        }


        public IEnumerable<int> Select(IEnumerable<string> items, string title, bool isMany,
            IEnumerable<string> previews, IEnumerable<string> descriptions)
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
            return new List<Plugin>();
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
