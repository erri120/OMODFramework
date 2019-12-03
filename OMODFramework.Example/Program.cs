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
            Framework.Settings.TempPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestTempDir");

            Framework.Settings.ScriptExecutionSettings.OblivionINIPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "Oblivion.ini");

            Framework.Settings.CodeProgress = new Progress();

            var settings = new FrameworkSettings()
            {
                
            };

            if(Directory.Exists(Framework.Settings.TempPath))
                Framework.CleanTempDir();

            var omod = new OMOD("DarNified UI 1.3.2.omod");

            var data = omod.GetDataFiles().Result; // extracts all data files
            var plugins = omod.GetPlugins().Result; // extracts all plugins

            var scriptFunctions = new ScriptFunctions();
            var srd = omod.RunScript(scriptFunctions, data, plugins);

            if(srd.CancelInstall)
                Console.WriteLine("Canceled");
        }
    }
}
