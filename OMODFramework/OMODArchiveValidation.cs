using System.IO.Compression;
using System.Linq;

namespace OMODFramework
{
    internal static class OMODArchiveValidation
    {
        internal static void ValidateArchive(ZipArchive archive)
        {
            var entries = archive.Entries.Select(x => x.Name).ToList();

            if (!entries.Contains("config"))
                throw new OMODValidationException("OMOD does not have a config file!");

            if (!entries.Contains("data") && !entries.Contains("plugins"))
                throw new OMODValidationException("OMOD does not have data and plugin files!");

            if (entries.Contains("data") && !entries.Contains("data.crc"))
                throw new OMODValidationException("OMOD does not have a data.crc file!");

            if (entries.Contains("plugins") && !entries.Contains("plugins.crc"))
                throw new OMODValidationException("OMOD does not have a plugins.crc file!");
        }
    }
}
