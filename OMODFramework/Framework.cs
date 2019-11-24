using Path = Alphaleonis.Win32.Filesystem.Path;

namespace OMODFramework
{
    public class Framework
    {
        internal string Version = "1.1.12";
        internal byte MajorVersion = 1;
        internal byte MinorVersion = 1;
        internal byte BuildNumber = 12;
        internal byte CurrentOmodVersion = 4;

        internal bool IgnoreVersion = false;

        internal static int MaxMemoryStreamSize => 67108864;

        internal static string TempDir { get; set; } = Path.Combine(Path.GetTempPath(), "OMODFramework");
    }
}
