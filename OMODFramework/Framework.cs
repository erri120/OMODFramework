using Path = Alphaleonis.Win32.Filesystem.Path;

namespace OMODFramework
{
    public class Framework
    {
        internal static string TempDir { get; set; } = Path.Combine(Path.GetTempPath(), "OMODFramework");
    }
}
