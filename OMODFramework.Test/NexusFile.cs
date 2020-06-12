using System;
using Wabbajack.Downloader.NexusMods;

namespace OMODFramework.Test
{
    public class NexusTestFixture : IDisposable
    {
        public readonly NexusAPIClient Client;

        public NexusTestFixture()
        {
            var apiKey = Environment.GetEnvironmentVariable("NEXUSAPIKEY");
            Client = new NexusAPIClient("OMODFramework.Test", "1.0.0", apiKey);
        }

        public void Dispose()
        {
        }
    }

    public class NexusFile
    {
        public readonly int ModID;
        public readonly int FileID;

        private string _path = string.Empty;

        public string Path
        {
            get => _path.InDownloadsFolder();
            private set => _path = value;
        }

        public NexusFile(int modID, int fileID, string path)
        {
            ModID = modID;
            FileID = fileID;
            Path = path;
        }

        public bool Download(NexusAPIClient client)
        {
            var res = Utils.Download(client, ModID, FileID, Path).Result;
            return res;
        }
    }
}
