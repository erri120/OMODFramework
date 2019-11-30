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
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace OMODFramework
{
    public class Framework
    {
        public static string Version = "1.1.12";
        public static byte MajorVersion = 1;
        public static byte MinorVersion = 1;
        public static byte BuildNumber = 12;
        public static byte CurrentOmodVersion = 4;

        /// <summary>
        ///     Whether the internal omod version check should be ignored
        /// </summary>
        public static bool IgnoreVersion { get; set; } = false;

        /// <summary>
        ///     Whether to enable warnings during script executing. HIGHLY recommended to have this set to true
        /// </summary>
        public static bool EnableWarnings { get; set; } = true;

        /// <summary>
        ///     DO NOT TOUCH UNLESS TOLD TO
        /// </summary>
        public static int MaxMemoryStreamSize => 67108864;

        /// <summary>
        ///     Temp folder used for extraction. Default is %temp%\\OMODFramework\\
        /// </summary>
        public static string TempDir { get; set; } = Path.Combine(Path.GetTempPath(), "OMODFramework");

        public static string DLLPath { get; set; } =
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "OMODFramework.dll");

        /// <summary>
        ///     Absolute path to the Oblivion Game folder where <c>oblivion.exe</c> is located
        /// </summary>
        public static string OblivionGameFolder { get; set; } = "";

        internal static string OblivionDataFolder => Path.Combine(OblivionGameFolder, "data");

        /// <summary>
        ///     Absolute path to the oblivion.ini file
        /// </summary>
        public static string OblivionINIFile { get; set; } = "";

        /// <summary>
        ///     Absolute path to the RendererInfo.txt file
        /// </summary>
        public static string OblivionRenderInfoFile { get; set; } = "";

        /// <summary>
        ///     Methods for patching files.
        ///     <para><c>OverwriteGameFolder</c> - will overwrite the file from the oblivion data folder if found</para>
        ///     <para>
        ///         <c>CreatePatchGameFolder</c> - will create a patch folder in the oblivion data folder containing the patched
        ///         files
        ///     </para>
        ///     <para><c>CreatePatchInMod</c> - will populate the <c>ScriptReturnData.PatchFiles</c> HashSet and not write to disk</para>
        ///     <para><c>PathWithInterface</c> - will call <c>IScriptFunctions.Patch</c> and let you decide how to patch something</para>
        /// </summary>
        public enum PatchMethod { OverwriteGameFolder, CreatePatchGameFolder, CreatePatchInMod, PatchWithInterface }

        /// <summary>
        ///     Method used for patching files, see <see cref="PatchMethod" /> for all available options
        /// </summary>
        public static PatchMethod CurrentPatchMethod { get; set; } = PatchMethod.CreatePatchInMod;

        /// <summary>
        ///     Methods for reading the oblivion.ini file
        ///     <para><c>ReadOriginalINI</c> - reads the oblivion.ini specified at <see cref="OblivionINIFile" /></para>
        ///     <para><c>ReadWithInterface</c> - will call <c>IScriptFunctions.ReadOblivionINI</c></para>
        /// </summary>
        public enum ReadINIMethod { ReadOriginalINI, ReadWithInterface }

        /// <summary>
        ///     Method used for reading the oblivion.ini file, see <see cref="ReadINIMethod" /> for all available options
        /// </summary>
        public static ReadINIMethod CurrentReadINIMethod { get; set; } = ReadINIMethod.ReadOriginalINI;

        /// <summary>
        ///     Methods for reading the RendererInfo.txt file
        ///     <para>
        ///         <c>ReadOriginalRenderer</c> - reads the RendererInfo.txt specified at <see cref="OblivionRenderInfoFile" />
        ///     </para>
        ///     <para><c>ReadWithInterface</c> - will call <c>IScriptFunctions.ReadRendererInfo</c></para>
        /// </summary>
        public enum ReadRendererMethod { ReadOriginalRenderer, ReadWithInterface }

        /// <summary>
        ///     Method used for reading the RendererInfo.txt file, see <see cref="ReadRendererMethod" /> for all available options
        /// </summary>
        public static ReadRendererMethod CurrentReadRendererMethod { get; set; } =
            ReadRendererMethod.ReadOriginalRenderer;

        /// <summary>
        ///     Methods for handling BSA related functions like getting a file or extracting
        ///     <para>
        ///         <c>OriginalOBMM</c> - will use the original methods used by OBMM, requires <see cref="Framework.LoadBSAs"/>
        ///     </para>
        ///     <para>
        ///         <c>WithInterface</c> - will redirect all requests to the interface functions
        ///     </para>
        /// </summary>
        public enum BSAHandling { OriginalOBMM, WithInterface }

        /// <summary>
        ///     Method used for handling BSA related functions, see <see cref="BSAHandling"/> for all available options
        /// </summary>
        public static BSAHandling CurrentBSAHandling { get; set; } = BSAHandling.WithInterface;

        /// <summary>
        ///     This function will load all BSAs inside the provided HashSet. Do note that
        ///     this function is required when you 
        /// </summary>
        /// <param name="fileList"></param>
        public static void LoadBSAs(HashSet<string> fileList)
        {
            BSAArchive.Load(fileList);
        }

        /// <summary>
        ///     This function should be called after you are finished with everything and have set <see cref="CurrentBSAHandling"/>
        ///     to <c>OriginalOBMM</c>
        /// </summary>
        public static void ClearBSAs(){BSAArchive.Clear();}

        /// <summary>
        ///     Convenience function that will clean the entire temp folder for you
        /// </summary>
        /// <param name="deleteRoot">Whether to delete the folder itself</param>
        public static void CleanTempDir(bool deleteRoot = false)
        {
            if (!Directory.Exists(TempDir))
                return;

            var dInfo = new DirectoryInfo(TempDir);
            dInfo.GetFiles().Do(f =>
            {
                if (!f.Exists || f.IsReadOnly)
                    return;

                try
                {
                    f.Delete();
                }
                catch
                {
                    // ignored
                }
            });
            dInfo.GetDirectories().Do(d =>
            {
                if (d.Exists && !d.Attributes.HasFlag(FileAttributes.ReadOnly)) d.Delete(true);
            });

            if (deleteRoot)
                Directory.Delete(TempDir);
        }
    }

    public class OMODFrameworkException : ApplicationException
    {
        public OMODFrameworkException(string s) : base(s) { }
    }
}
