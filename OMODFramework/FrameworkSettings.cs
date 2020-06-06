using System;
using JetBrains.Annotations;

namespace OMODFramework
{
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
        public byte CurrentOMODVersion { get; } = 4;

        /// <summary>
        /// Current version of the Oblivion Mod Manager, Default is 1.1.12.0
        /// </summary>
        public Version CurrentOBMMVersion { get; } = new Version(1, 1, 12, 0);
    }
}
