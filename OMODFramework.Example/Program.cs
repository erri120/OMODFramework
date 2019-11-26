using System;
using System.IO;
using System.Reflection;

namespace OMODFramework.Example
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // set the temp folder if you don't want it at %temp%/OMODFramework/
            Framework.TempDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            var omod = new OMOD("DarNified UI 1.3.2.omod");

            var data = omod.GetDataFiles(); // extracts all data files
            var plugins = omod.GetPlugins(); // extracts all plugins

            Console.WriteLine($"Data files: {data}");
            Console.WriteLine($"Plugins: {plugins}");
        }
    }
}
