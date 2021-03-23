using System;
using JetBrains.Annotations;

namespace OMODFramework.Scripting
{
    /// <summary>
    /// Represents settings to use during script execution.
    /// </summary>
    [PublicAPI]
    public class OMODScriptSettings
    {
        /// <summary>
        /// Whether to extract the data and plugin files before script execution. Useful for script debugging without
        /// having to re-extract everything.
        /// </summary>
        public bool DryRun { get; set; }

        /// <summary>
        /// Whether to use the Bitmap Overload functions. Alternatively the other functions provide a path to the image
        /// instead of the image as a Bitmap so you can load it however you see fit.
        /// </summary>
        public bool UseBitmapOverloads { get; set; } = true;

        /// <summary>
        /// Whether to use the internal BSA functions.
        /// </summary>
        public bool UseInternalBSAFunctions { get; set; } = true;
        
        /// <summary>
        /// The current OBMM version.
        /// </summary>
        public Version CurrentOBMMVersion { get; set; } = new Version(1, 1, 12, 0);
        
        internal readonly IExternalScriptFunctions ExternalScriptFunctions;

        /// <summary>
        /// Initializes a new instance of the <see cref="OMODScriptSettings"/> class.
        /// </summary>
        /// <param name="externalScriptFunctions">External Script Functions to use during script execution.</param>
        public OMODScriptSettings(IExternalScriptFunctions externalScriptFunctions)
        {
            ExternalScriptFunctions = externalScriptFunctions;
        }
    }
}
