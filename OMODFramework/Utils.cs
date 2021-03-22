using System.Text;

namespace OMODFramework
{
    internal static class Utils
    {
        internal static string MakePath(this string s)
        {
            //TODO: find better solution
            var sb = new StringBuilder(s);
            sb.Replace("\\\\", "\\");
            sb.Replace("/", "\\");
            return sb.ToString();
        }
    }
}
