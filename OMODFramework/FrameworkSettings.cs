using System;
using System.IO;
using JetBrains.Annotations;
using OMODFramework.Exceptions;
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

        /// <summary>
        /// Settings only used during Script Execution
        /// </summary>
        public ScriptExecutionSettings? ScriptExecutionSettings { get; set; }
    }

    /// <summary>
    /// Scripts only used during Script Execution
    /// </summary>
    [PublicAPI]
    public class ScriptExecutionSettings
    {
        /// <summary>
        /// Verify your ScriptExecutionSettings. Do note that this will throw
        /// Exceptions on purpose if it finds bad settings to make sure to
        /// use try/catch or other Exception logging methods.
        /// </summary>
        /// <exception cref="BadSettingsException"></exception>
        /// <returns></returns>
        public bool VerifySettings() => VerifySettings(this);

        /// <summary>
        /// Verify your ScriptExecutionSettings. Do note that this will throw
        /// Exceptions on purpose if it finds bad settings to make sure to
        /// use try/catch or other Exception logging methods.
        /// </summary>
        /// <param name="settings">Settings to verify</param>
        /// <exception cref="BadSettingsException"></exception>
        /// <returns></returns>
        public static bool VerifySettings(ScriptExecutionSettings? settings)
        {
            if(settings == null)
                throw new BadSettingsException("ScriptExecutionSettings can not be null!", nameof(settings));

            if (!settings.ReadINIWithInterface)
            {
                if(settings.OblivionINIPath == null)
                    throw new BadSettingsException("OblivionINIPath must not be null if ReadINIWithInterface is set to false!", nameof(settings.OblivionINIPath));
                if(!File.Exists(settings.OblivionINIPath))
                    throw new BadSettingsException($"OblivionINIPath ({settings.OblivionINIPath}) must exist if ReadINIWithInterface is set to true!", nameof(settings.OblivionINIPath));
            }

            if (!settings.ReadRendererInfoWithInterface)
            {
                if (settings.OblivionRendererInfoPath == null)
                    throw new BadSettingsException("OblivionRendererInfoPath must not be null if ReadRendererInfoWithInterface is set to false!", nameof(settings.OblivionRendererInfoPath));
                if (!File.Exists(settings.OblivionRendererInfoPath))
                    throw new BadSettingsException($"OblivionRendererInfoPath ({settings.OblivionRendererInfoPath}) must exist if ReadRendererInfoWithInterface is set to true!", nameof(settings.OblivionRendererInfoPath));
            }

            return true;
        }

        /// <summary>
        ///     Absolute path to the oblivion.ini file
        /// </summary>
        public string? OblivionINIPath { get; set; }

        /// <summary>
        ///     Absolute path to the RendererInfo.txt file
        /// </summary>
        public string? OblivionRendererInfoPath { get; set; }

        /// <summary>
        ///     <para>If this is set to <c>true</c>:</para>
        ///     <c>IScriptFunctions.ReadOblivionINI</c> will be called so you can
        ///     handle the reading of the oblivion.ini file the way you want.
        ///     <para>If this is set to <c>false</c>:</para>
        ///     Internal functions will be called to read the ini file, this requires
        ///     <see cref="OblivionINIPath"/> to be set to the <c>oblivion.ini</c> file
        ///     <para>Default is <c>true</c></para>
        /// </summary>
        public bool ReadINIWithInterface { get; set; } = true;

        /// <summary>
        ///     <para>If this is set to <c>true</c>:</para>
        ///     <c>IScriptFunctions.ReadRendererInfo</c> will be called so you can
        ///     handle the reading of the RendererInfo.txt file the way you want.
        ///     <para>If this is set to <c>false</c>:</para>
        ///     Internal functions will be called to read the file, this requires
        ///     <see cref="OblivionRendererInfoPath"/> to be set to the <c>RendererInfo.txt</c> file
        ///     <para>Default is <c>true</c></para>
        /// </summary>
        public bool ReadRendererInfoWithInterface { get; set; } = true;
    }
}
