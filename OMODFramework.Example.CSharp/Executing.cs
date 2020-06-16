using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using CommandLine;
using JetBrains.Annotations;
using OMODFramework.Scripting;

namespace OMODFramework.Example.CSharp
{
    [Verb("script", HelpText = "Run the script in the OMOD")]
    [PublicAPI]
    public class ExecuteScriptOptions
    {
        [Option('i', "input", HelpText = "Input file", Required = true)]
        public string Input { get; set; } = string.Empty;

        [Option('o', "output", HelpText = "Output directory", Required = true)]
        public string Output { get; set; } = string.Empty;

        public static int Execute(ExecuteScriptOptions options)
        {
            var file = new FileInfo(options.Input);
            if (!file.Exists)
                throw new ArgumentException($"File {file} does not exist!");
            if (file.Extension != ".omod")
                throw new ArgumentException($"File {file} is not an OMOD!");

            var output = new DirectoryInfo(options.Output);
            if (!output.Exists)
                output.Create();

            //make sure you are using the "using" statement so that the OMOD is disposed correctly
            //more info: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/using-statement
            using var omod = new OMOD(file);

            Utils.Log($"Starting script execution of OMOD {omod.Config.Name} by {omod.Config.Author}");

            //script execution is more complex than simple extraction as the script might have to 
            //interact with outside parts such as the oblivion ini or already existing file in the
            //data folder. It might want to check if OBSE is installed or what's the version of Oblivion
            //for this reason you need to implement the IScriptSettings interface and provide an instance
            //of the class.

            //ScriptSettings is defined below
            var srd = ScriptRunner.ExecuteScript(omod, new ScriptSettings());

            //after you got the script return data you can call the ExtractAllFiles of ScriptRunner
            //this will extract only the files needed to be installed instead of extracting every file
            //to a temp folder and then copying the needed files to the output
            ScriptRunner.ExtractAllFiles(omod, srd, output);

            return 0;
        }
    }

    public class ScriptSettings : IScriptSettings
    {
        public FrameworkSettings FrameworkSettings => new FrameworkSettings();
        public IScriptFunctions ScriptFunctions => new ScriptFunctions();
    }

    public class ScriptFunctions : IScriptFunctions
    {
        public void Message(string msg)
        {
            Console.WriteLine(msg);
        }

        public void Message(string msg, string title)
        {
            Console.WriteLine($"{title}: {msg}");
        }

        public IEnumerable<int> Select(IEnumerable<string> items, string title, bool isMultiSelect, IEnumerable<Bitmap> previews, IEnumerable<string> descriptions)
        {
            Console.WriteLine(title);
            if(isMultiSelect)
                Console.WriteLine("(multi select is enabled)");
            var list = items.ToList();
            var results = new List<int>();
            var hasDescriptions = descriptions.Any();

            for (var i = 0; i < list.Count; i++)
            {
                Console.WriteLine($"[{i}]: {list[i]} {(hasDescriptions ? descriptions.ElementAt(i) : "")}");
            }

            var input = Console.Read();


            return results;
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public Version OBSEPluginVersion(string file)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ESP> GetESPs()
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
