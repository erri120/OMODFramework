using System;
using JetBrains.Annotations;

namespace OMODFramework
{
    /// <summary>
    /// Possible files found in an OMOD archive.
    /// </summary>
    [PublicAPI]
    public enum OMODEntryFileType
    {
        /// <summary>
        /// Config, always present.
        /// </summary>
        Config,
        
        /// <summary>
        /// Readme
        /// </summary>
        Readme,
        
        /// <summary>
        /// Script 
        /// </summary>
        Script,
        
        /// <summary>
        /// Image 
        /// </summary>
        Image,
        
        /// <summary>
        /// Compressed data files.
        /// </summary>
        Data,
        
        /// <summary>
        /// File information of the compressed data files.
        /// </summary>
        DataCRC,
        
        /// <summary>
        /// Compressed plugin files.
        /// </summary>
        Plugins,
        
        /// <summary>
        /// File information of the compressed plugin files.
        /// </summary>
        PluginsCRC
    }

    
    /// <summary>
    /// Provides static utility functions when dealing with OMODs.
    /// </summary>
    public static class OMODUtils
    {
        /// <summary>
        /// Converts a <see cref="OMODEntryFileType"/> to a string.
        /// </summary>
        /// <param name="entryFileType">The entry file type to convert.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
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
