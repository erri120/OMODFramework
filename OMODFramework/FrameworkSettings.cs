using JetBrains.Annotations;

namespace OMODFramework
{
    [PublicAPI]
    public class FrameworkSettings
    {
        public static FrameworkSettings DefaultFrameworkSettings => new FrameworkSettings();

        public byte CurrentOMODVersion { get; set; } = 4;
    }
}
