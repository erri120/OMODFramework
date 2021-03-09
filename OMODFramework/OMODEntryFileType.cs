using System;
using JetBrains.Annotations;

namespace OMODFramework
{
    [PublicAPI]
    public enum OMODEntryFileType
    {
        Config,
        Readme,
        Script,
        Image,
        Data,
        DataCRC,
        Plugins,
        PluginsCRC
    }

    public static partial class OMODUtils
    {
        public static string ToFileString(this OMODEntryFileType entryFileType)
        {
            return entryFileType switch
            {
                OMODEntryFileType.Config => "config",
                OMODEntryFileType.Readme => "readme",
                OMODEntryFileType.Script => "script",
                OMODEntryFileType.Image => "image",
                OMODEntryFileType.Data => "data",
                OMODEntryFileType.DataCRC => "data.crc",
                OMODEntryFileType.Plugins => "plugins",
                OMODEntryFileType.PluginsCRC => "plugins.crc",
                _ => throw new ArgumentOutOfRangeException(nameof(entryFileType), entryFileType, null)
            };
        }
    }
}
