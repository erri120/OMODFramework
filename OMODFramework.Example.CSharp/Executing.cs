using CommandLine;
using JetBrains.Annotations;

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
            return 0;
        }
    }
}
