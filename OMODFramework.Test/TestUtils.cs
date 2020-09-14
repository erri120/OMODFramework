using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Wabbajack.Downloader.Common;
using Wabbajack.Downloader.NexusMods;

namespace OMODFramework.Test
{
    internal static class TestUtils
    {
        internal static string InDownloadsFolder(this string s)
        {
            return Path.Combine("downloads", s);
        }

        internal static async Task<bool> Download(NexusAPIClient client, int modID, int fileID, string path)
        {
            if (File.Exists(path))
                return true;
            var link = await client.GetNexusDownloadLink("oblivion", modID, fileID);
            return await HTTPDownloader.Download(link, path, client.HttpClient);
        }

        internal static void Do<T>(this IEnumerable<T> col, [InstantHandle] Action<T> a)
        {
            foreach (var item in col) a(item);
        }
    }
}
