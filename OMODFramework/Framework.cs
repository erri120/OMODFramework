/*
    Copyright (C) 2019  erri120

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using OMODFramework.Classes;

namespace OMODFramework
{
    public class Framework
    {
        public static FrameworkSettings Settings = new FrameworkSettings();

        /// <summary>
        ///     This function will load all BSAs inside the provided HashSet. Do note that
        ///     this function is required when <see cref="ScriptExecutionSettings.HandleBSAsWithInterface"/>
        ///     is set to <c>false</c>
        /// </summary>
        /// <param name="fileList"></param>
        public static void LoadBSAs(HashSet<string> fileList)
        {
            BSAArchive.Load(fileList);
        }

        /// <summary>
        ///     This function should be called if you in your cleanup section after script execution if you have
        ///     used <see cref="LoadBSAs"/> before. Not calling this function can result in unpredictable consequences
        /// </summary>
        public static void ClearBSAs(){BSAArchive.Clear();}

        /// <summary>
        ///     Convenience function that will clean the entire temp folder at
        ///     <see cref="FrameworkSettings.TempPath"/> for you
        /// </summary>
        /// <param name="deleteRoot">Whether to delete the folder itself</param>
        public static void CleanTempDir(bool deleteRoot = false)
        {
            if (!Directory.Exists(Settings.TempPath))
                return;

            try
            {
                var dInfo = new DirectoryInfo(Settings.TempPath);
                dInfo.GetFiles().Do(f =>
                {
                    if (!f.Exists || f.IsReadOnly)
                        return;

                    f.Delete();
                });
                dInfo.GetDirectories().Do(d =>
                {
                    if (d.Exists && !d.Attributes.HasFlag(FileAttributes.ReadOnly)) d.Delete(true);
                });

                if (deleteRoot)
                    Directory.Delete(Settings.TempPath);
            }
            catch
            {
                // ignored, file is used by another process or something/someone fucked up
            }
        }
    }

    public class OMODFrameworkException : ApplicationException
    {
        public OMODFrameworkException(string s) : base(s) { }
    }

    public class FrameworkSettings
    {
        /// <summary>
        ///     String representation of the simulated OBMM version
        /// </summary>
        public string Version = "1.1.12";

        /// <summary>
        ///     Major Version of OBMM
        /// </summary>
        public byte MajorVersion = 1;

        /// <summary>
        ///     Minor Version of OBMM
        /// </summary>
        public byte MinorVersion = 1;

        /// <summary>
        ///     Build Version of OBMM
        /// </summary>
        public byte BuildNumber = 12;

        /// <summary>
        ///     Current OMOD Version
        /// </summary>
        public byte CurrentOMODVersion = 4;

        /// <summary>
        ///     Ignore the version check when loading the OMOD
        /// </summary>
        public bool IgnoreVersionCheck = false;

        /// <summary>
        ///     DO NOT TOUCH THIS UNLESS YOU KNOW WHAT YOU'RE DOING
        /// </summary>
        public int MaxMemoryStreamSize = 67108864;

        /// <summary>
        ///     Temp folder where all extracted files, compiled scripts and other temp files go.
        ///     Default is <c>%temp%\OMODFramework\</c>
        /// </summary>
        public string TempPath = Path.Combine(Path.GetTempPath(), "OMODFramework");

        /// <summary>
        ///     Path to the OMODFramework.dll in case <c>Assembly.GetExecutingAssembly().Location</c> returns
        ///     something that doesn't make any sense
        /// </summary>
        public string DllPath = Assembly.GetExecutingAssembly().Location;

        /// <summary>
        ///     Custom code progress class for displaying the progress of compression/decompression
        /// </summary>
        public ICodeProgress CodeProgress;

        /// <summary>
        ///     Settings used for Script execution
        /// </summary>
        public ScriptExecutionSettings ScriptExecutionSettings;

        /// <summary>
        ///     Settings used for logging
        /// </summary>
        public LoggingSettings LoggingSettings = new LoggingSettings();
    }

    public class LoggingSettings
    {
        /// <summary>
        ///     Whether you want logging or not
        /// </summary>
        private bool _useLogger;

        public bool UseLogger
        {
            get => _useLogger;
            set
            {
                _useLogger = value;
                Utils.Logger.Init();
            }
        }

        /// <summary>
        ///     If <see cref="UseLogger"/> is set to <c>true</c> than you can set this to <c>false</c>
        ///     and create a custom <see cref="ILogger"/> for <see cref="Logger"/> or set this to <c>true</c>
        ///     and use the internal logger that logs to the file <c>OMODFramework.log</c>
        /// </summary>
        public bool LogToFile = true;

        /// <summary>
        ///     Set the lowest logging level, any logging that is below that level will not be logged
        /// </summary>
        public LoggingLevel LowestLoggingLevel = LoggingLevel.INFO;

        private ILogger _logger;

        /// <summary>
        ///     Set your custom <see cref="ILogger"/>, only needed when <see cref="LogToFile"/> is set to <c>false</c>
        /// </summary>
        public ILogger Logger
        {
            get => _logger;
            set
            {
                _logger = value;
                Utils.Logger = _logger;
                _logger.Init();
            }
        }
    }

    public class ScriptExecutionSettings
    {
        /// <summary>
        ///     HIGHLY RECOMMENDED TO LEAVE THIS ON TRUE, setting this to false will
        ///     disable all warnings during script executing
        /// </summary>
        public bool EnableWarnings = true;

        /// <summary>
        ///     Absolute path to the Oblivion game folder
        /// </summary>
        public string OblivionGamePath = "";

        internal string OblivionDataPath => Path.Combine(OblivionGamePath, "data");

        /// <summary>
        ///     Absolute path to the oblivion.ini file
        /// </summary>
        public string OblivionINIPath = "";

        /// <summary>
        ///     Absolute path to the RendererInfo.txt file
        /// </summary>
        public string OblivionRendererInfoPath = "";

        /// <summary>
        ///     <para>If this is set to <c>true</c>:</para>
        ///     <c>IScriptFunctions.ReadOblivionINI</c> will be called so you can
        ///     handle the reading of the oblivion.ini file the way you want.
        ///     <para>If this is set to <c>false</c>:</para>
        ///     Internal functions will be called to read the ini file, this requires
        ///     <see cref="OblivionINIPath"/> to be set to the <c>oblivion.ini</c> file
        ///     <para>Default is <c>true</c></para>
        /// </summary>
        public bool ReadINIWithInterface = true;

        /// <summary>
        ///     <para>If this is set to <c>true</c>:</para>
        ///     <c>IScriptFunctions.ReadRendererInfo</c> will be called so you can
        ///     handle the reading of the RendererInfo.txt file the way you want.
        ///     <para>If this is set to <c>false</c>:</para>
        ///     Internal functions will be called to read the file, this requires
        ///     <see cref="OblivionRendererInfoPath"/> to be set to the <c>RendererInfo.txt</c> file
        ///     <para>Default is <c>true</c></para>
        /// </summary>
        public bool ReadRendererInfoWithInterface = true;

        /// <summary>
        ///     <para>If this is set to <c>true</c>:</para>
        ///     <c>IScriptFunctions.GetDataFileFromBSA</c> will be called so you can
        ///     handle BSAs the way you want.
        ///     <para>If this is set to <c>false</c>:</para>
        ///     The original OBMM functions for all BSA related functions will be called.
        ///     This requires <see cref="Framework.LoadBSAs"/> to be used before script execution.
        ///     You also need/should call <see cref="Framework.ClearBSAs"/> after script execution.
        ///     <para>Default is <c>true</c></para>
        /// </summary>
        public bool HandleBSAsWithInterface = true;

        /// <summary>
        ///     <para>If this is set to <c>true</c>:</para>
        ///     <c>IScriptFunctions.Patch</c> will be called so you can handle the patching of files
        ///     the way you want.
        ///     <para>If this is set to <c>false</c>:</para>
        ///     Depending on <see cref="UseSafePatching"/> will the original game files be overwritten
        ///     or a patch folder will be created inside the Oblivion data folder.
        ///     <para>Default is <c>true</c></para>
        /// </summary>
        public bool PatchWithInterface = true;

        /// <summary>
        ///     If <see cref="PatchWithInterface"/> is set to false than you need to decide
        ///     whether you want the script to overwrite files inside the game data folder or
        ///     if this Framework should place the file inside a patching folder
        ///     <para>Default is <c>true</c></para>
        /// </summary>
        public bool UseSafePatching = true;
    }
}
