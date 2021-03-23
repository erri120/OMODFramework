using JetBrains.Annotations;

namespace OMODFramework.Compression
{
    /// <summary>
    /// Type of compression used in OMODs.
    /// </summary>
    [PublicAPI]
    public enum CompressionType : byte
    {
        /// <summary>
        /// LZMA.
        /// </summary>
        SevenZip,
        
        /// <summary>
        /// ZIP.
        /// </summary>
        Zip
    }
}
