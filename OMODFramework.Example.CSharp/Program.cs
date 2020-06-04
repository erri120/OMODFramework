using CommandLine;

namespace OMODFramework.Example.CSharp
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<ExtractOptions, ExecuteScriptOptions>(args)
                .MapResult(
                    (ExtractOptions opts) => ExtractOptions.Extract(opts),
                    (ExecuteScriptOptions opts) => ExecuteScriptOptions.Execute(opts),
                    errs => 1);
        }
    }
}
