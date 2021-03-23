using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace OMODFramework.Oblivion.BSA
{
    [PublicAPI]
    internal static class Utility
    {
        private static readonly Lazy<Encoding> Windows1252 = new Lazy<Encoding>(() =>
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(1252);
        });

        private static Encoding GetEncoding(VersionType version)
        {
            return version switch
            {
                VersionType.SSE => Windows1252.Value,
#pragma warning disable 618
#pragma warning disable SYSLIB0001
                _ => Encoding.UTF7
#pragma warning restore 618
#pragma warning restore SYSLIB0001
            };
        }

        public static string ReadStringLenTerm(this ReadOnlyMemorySlice<byte> bytes, VersionType version)
        {
            return bytes.Length <= 1 
                ? string.Empty 
                : GetEncoding(version).GetString(bytes.Slice(1, bytes[0]));
        }

        public static string ReadStringTerm(this ReadOnlyMemorySlice<byte> bytes, VersionType version)
        {
            return bytes.Length <= 1 
                ? string.Empty 
                : GetEncoding(version).GetString(bytes[0..^1]);
        }

        public static void CopyToLimit(this Stream from, Stream to, long limit)
        {
            var buff = new byte[1024];
            while (limit > 0)
            {
                var toRead = (int)Math.Min(buff.Length, limit);
                var read = from.Read(buff, 0, toRead);
                if (read == 0)
                    throw new Exception("End of stream before end of limit");
                to.Write(buff, 0, read);
                limit -= read;
            }

            to.Flush();
        }

        public static async Task CopyToLimitAsync(this Stream from, Stream to, long limit)
        {
            var buff = new byte[1024];
            while (limit > 0)
            {
                var toRead = Math.Min(buff.Length, limit);
                var read = await from.ReadAsync(buff.AsMemory(0, (int)toRead)).ConfigureAwait(false);
                if (read == 0)
                    throw new Exception("End of stream before end of limit");
                await to.WriteAsync(buff.AsMemory(0, read));
                limit -= read;
            }

            await to.FlushAsync();
        }
    }
}
