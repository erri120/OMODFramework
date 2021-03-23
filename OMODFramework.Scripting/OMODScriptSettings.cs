using System;
using JetBrains.Annotations;

namespace OMODFramework.Scripting
{
    [PublicAPI]
    public class OMODScriptSettings
    {
        public bool DryRun { get; set; }

        public bool UseBitmapOverloads { get; set; } = true;

        public bool UseInternalBSAFunctions { get; set; } = true;
        
        public Version CurrentOBMMVersion { get; set; } = new Version(1, 1, 12, 0);
        
        public readonly IExternalScriptFunctions ExternalScriptFunctions;

        public OMODScriptSettings(IExternalScriptFunctions externalScriptFunctions)
        {
            ExternalScriptFunctions = externalScriptFunctions;
        }
    }
}
