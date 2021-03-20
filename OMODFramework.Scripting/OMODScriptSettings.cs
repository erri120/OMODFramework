using System;
using System.IO;
using JetBrains.Annotations;

namespace OMODFramework.Scripting
{
    [PublicAPI]
    public class OMODScriptSettings
    {
        public string ExtractionFolder { get; set; } = Path.Combine(Path.GetTempPath(), "OMODFramework");

        public bool CreateModSpecificExtractionFolder { get; set; } = true;

        public bool DryRun { get; set; } = false;

        public bool UseBitmapOverloads { get; set; } = true;
        
        public Version CurrentOBMMVersion { get; set; } = new Version(1, 1, 12, 0);
        
        public readonly IExternalScriptFunctions ExternalScriptFunctions;

        public OMODScriptSettings(IExternalScriptFunctions externalScriptFunctions)
        {
            ExternalScriptFunctions = externalScriptFunctions;
        }
    }
}
