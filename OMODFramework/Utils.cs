using System;
using System.IO;
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

        internal static void CopyToLimit(this Stream from, Stream to, long limit)
        {
            var buffer = new byte[1024];
            while (limit > 0)
            {
                var toRead = Math.Min(buffer.Length, limit);
                var read = from.Read(buffer, 0, (int) toRead);
                if (read == 0)
                    throw new Exception("EOS reached before limit!");
                to.Write(buffer, 0, read);
                limit -= read;
            }
            
            to.Flush();
        }
    }
}
