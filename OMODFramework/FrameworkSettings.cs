using System;
using JetBrains.Annotations;

namespace OMODFramework
{
    [PublicAPI]
    public class FrameworkSettings
    {
        public static FrameworkSettings DefaultFrameworkSettings => new FrameworkSettings();

        public byte CurrentOMODVersion { get; } = 4;

        public Version CurrentOBMMVersion { get; } = new Version(1, 1, 12, 0);
    }
}
