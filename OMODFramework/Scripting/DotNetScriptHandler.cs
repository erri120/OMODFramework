using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Alphaleonis.Win32.Filesystem;
using Microsoft.CSharp;
using Microsoft.VisualBasic;
using OblivionModManager.Scripting;
using OMODFramework;

namespace OMODFramework.Scripting
{
    internal static class DotNetScriptHandler
    {
        private static readonly CSharpCodeProvider CSharpCompiler = new CSharpCodeProvider();
        private static readonly VBCodeProvider VBCompiler = new VBCodeProvider();
        private static readonly CompilerParameters Params;
        private static readonly Evidence Evidence;

        private static readonly string ScriptOutputPath = Path.Combine(Framework.Settings.TempPath, "dotnetscript.dll");

        static DotNetScriptHandler()
        {
            Params = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = false,
                IncludeDebugInformation = false,
                OutputAssembly = ScriptOutputPath,
                ReferencedAssemblies =
                {
                    Framework.Settings.DllPath,
                    "System.dll",
                    "System.Drawing.dll",
                    "System.Windows.Forms.dll",
                    "System.Xml.dll"
                }
            };

            Evidence = new Evidence();
            Evidence.AddHostEvidence(new Zone(SecurityZone.Internet));
        }

        private static byte[] Compile(string code, ScriptType type)
        {
            Utils.Debug("Starting compilation...");
            CompilerResults results = null;
            switch (type)
            {
                case ScriptType.OBMMScript:
                    break;
                case ScriptType.Python:
                    break;
                case ScriptType.CSharp:
                    results = CSharpCompiler.CompileAssemblyFromSource(Params, code);
                    break;
                case ScriptType.VB:
                    results = VBCompiler.CompileAssemblyFromSource(Params, code);
                    break;
                case ScriptType.Count:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            if(results == null)
                throw new OMODFrameworkException("The script could not be compiled!");

            var stdout = ""; //TODO: maybe do something with this?
            var warn = 0;
            var err = 0;
            var e = "";
            var w = "";
            results.Output.Do(s => { stdout += s + Environment.NewLine; });

            if (results.Errors.HasErrors || results.Errors.HasWarnings)
            {
                results.Errors.Do(o =>
                {
                    var ce = (CompilerError)o;
                    if (ce.IsWarning)
                    {
                        w += $"Warning on Line {ce.Line}: {ce.ErrorText}\n";
                        Utils.Warn($"Warning on Line {ce.Line}: {ce.ErrorText}");
                        warn++;
                    }
                    else
                    {
                        e += $"Error on Line {ce.Line}: {ce.ErrorText}\n";
                        Utils.Warn($"Error on Line {ce.Line}: {ce.ErrorText}");
                        err++;
                    }
                });
                
            }
            Utils.Info($"Compilation finished with {warn} Warnings and {err} Errors");
            if(results.Errors.HasErrors)
                throw new OMODFrameworkException($"Problems during script compilation: \n{e}\n{w}");

            byte[] data = File.ReadAllBytes(results.PathToAssembly);
            return data;
        }

        private static void Execute(string script, ref DotNetScriptFunctions functions, ScriptType type)
        {
            byte[] data = Compile(script, type);

            var asm = AppDomain.CurrentDomain.Load(data, null);
            if (!(asm.CreateInstance("Script") is IScript s))
            {
                throw new OMODFrameworkException("C# or VB Script did not contain a 'Script' class in the root namespace, or IScript was not implemented");
            }
            s.Execute(functions);
            Utils.Info("Finished script execution");
        }

        internal static void ExecuteCS(string script, ref DotNetScriptFunctions functions)
        {
            Execute(script, ref functions, ScriptType.CSharp);
        }

        internal static void ExecuteVB(string script, ref DotNetScriptFunctions functions)
        {
            Execute(script, ref functions, ScriptType.VB);
        }
    }
}
