using System;
using JetBrains.Annotations;
using OMODFramework.Logging;

namespace OMODFramework
{
    /// <summary>
    /// Settings for the entire Framework
    /// </summary>
    [PublicAPI]
    public class FrameworkSettings
    {
        /// <summary>
        /// Default Framework Settings used when they are not provided
        /// </summary>
        public static FrameworkSettings DefaultFrameworkSettings => new FrameworkSettings();

        /// <summary>
        /// Current OMOD version, Default is 4
        /// </summary>
        public byte CurrentOMODVersion { get; set; } = 4;

        /// <summary>
        /// Current version of the Oblivion Mod Manager, Default is 1.1.12.0
        /// </summary>
        public Version CurrentOBMMVersion { get; set; } = new Version(1, 1, 12, 0);

        /// <summary>
        /// Progress reporter for compression and decompression of SevenZip archives.
        /// </summary>
        public ICodeProgress? CodeProgress { get; set; }

        /// <summary>
        /// Logger to use. Default value is an internal FileLogger that will create a OMODFramework.log file.
        /// Can be set to null if you don't want any logging at all.
        /// </summary>
        public ILogger? Logger
        {
            get => _logger;
            set
            {
                _logger = value;
                if(Utils.Logger != _logger)
                    Utils.Logger?.Dispose();
                Utils.Logger = value;
            }
        }
        private ILogger? _logger = Utils.Logger;
    }
}
