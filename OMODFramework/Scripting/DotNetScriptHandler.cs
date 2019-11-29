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

        private static readonly string ScriptOutputPath = Path.Combine(Framework.TempDir, "dotnetscript.dll");

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
                    //TODO: OMODFramework.dll
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
            CompilerResults results = null;
            switch (type)
            {
                case ScriptType.OBMMScript:
                    break;
                case ScriptType.Python:
                    break;
                case ScriptType.Csharp:
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

            var stdout = ""; //TODO: create interface for script outputs so it lands at the end user
            foreach (var s in results.Output)
            {
                stdout += s + Environment.NewLine;
            }

            if (results.Errors.HasErrors || results.Errors.HasWarnings)
            {
                var e = "";
                var w = "";
                foreach (CompilerError ce in results.Errors)
                {
                    if (ce.IsWarning)
                    {
                        w += $"Warning on Line {ce.Line}: {ce.ErrorText}\n";
                    }
                    else
                    {
                        e += $"Error on Line {ce.Line}: {ce.ErrorText}\n";
                    }
                }

                if(results.Errors.HasErrors)
                    throw new OMODFrameworkException($"Problems during script compilation: \n{e}\n{w}");
            }

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
        }

        internal static void ExecuteCS(string script, ref DotNetScriptFunctions functions)
        {
            Execute(script, ref functions, ScriptType.Csharp);
        }

        internal static void ExecuteVB(string script, ref DotNetScriptFunctions functions)
        {
            Execute(script, ref functions, ScriptType.VB);
        }
    }
}
