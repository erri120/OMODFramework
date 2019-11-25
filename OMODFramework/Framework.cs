using System.IO;
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

        public static void CleanTempDir(bool deleteRoot = false)
        {
            if(!Directory.Exists(TempDir))
                return;

            var dInfo = new DirectoryInfo(TempDir);
            dInfo.GetFiles().Do(f => {if(f.Exists && !f.IsReadOnly) f.Delete();});
            dInfo.GetDirectories().Do(d => {if(d.Exists && !d.Attributes.HasFlag(FileAttributes.ReadOnly)) d.Delete(true);});

            if(deleteRoot)
                Directory.Delete(TempDir);
        }
    }
}
