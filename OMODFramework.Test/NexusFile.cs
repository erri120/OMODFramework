using Wabbajack.Downloader.NexusMods;

namespace OMODFramework.Test
{
    public class NexusFile
    {
        private readonly int _modID;
        private readonly int _fileID;
        public string Game { get; set; } = "oblivion";

        private string _path = string.Empty;

        public string Path
        {
            get => _path.InDownloadsFolder();
            set => _path = value;
        }

        public NexusFile(int modID, int fileID, string path)
        {
            _modID = modID;
            _fileID = fileID;
            Path = path;
        }

        public bool Download(NexusAPIClient client)
        {
            var res = Utils.Download(client, _modID, _fileID, Path).Result;
            return res;
        }
    }
}
