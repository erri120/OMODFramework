﻿/*
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

/*
 * This file contains parts of the Oblivion Mod Manager licensed under GPLv2
 * and has been modified for use in this OMODFramework
 * Original source: https://www.nexusmods.com/oblivion/mods/2097
 * GPLv2: https://opensource.org/licenses/gpl-2.0.php
 */

using System.Collections.Generic;

namespace OMODFramework
{
    /// <summary>
    /// Describes the Conflict Level between two files/plugins/OMODs
    /// </summary>
    public enum ConflictLevel { Active, NoConflict, MinorConflict, MajorConflict, Unusable }

    /// <summary>
    /// Describes the status for OMOD/Plugin deactivation
    /// </summary>
    public enum DeactivationStatus { Allow, WarnAgainst, Disallow }

    /// <summary>
    /// Describes the programming language used in the OMOD script
    /// </summary>
    public enum ScriptType { OBMMScript, Python, Csharp, VB, Count }

    /// <summary>
    /// Struct containing conflict information for a file
    /// </summary>
    public struct ConflictData
    {
        public ConflictLevel Level;
        public string File;
        public int MinMajorVersion;
        public int MinMinorVersion;
        public int MaxMajorVersion;
        public int MaxMinorVersion;
        public string Comment;
        public bool Partial;
    }

    public class DataFileInfo
    {
        public readonly string FileName;
        public readonly string LowerFileName;
        public uint CRC;

        public DataFileInfo(string s, uint crc)
        {
            FileName = s;
            LowerFileName = FileName.ToLower();
            CRC = crc;
        }

        public DataFileInfo(DataFileInfo original)
        {
            FileName = original.FileName;
            LowerFileName = original.LowerFileName;
            CRC = original.CRC;
        }
    }

    /// <summary>
    /// Contains all information needed for the creation of an OMOD file
    /// </summary>
    public struct OMODCreationOptions
    {
        /// <summary>
        /// Required
        /// </summary>
        public string Name;
        /// <summary>
        /// Required
        /// </summary>
        public string Author;
        /// <summary>
        /// Required
        /// </summary>
        public string Description;
        /// <summary>
        /// Optional
        /// </summary>
        public string Email;
        /// <summary>
        /// Optional
        /// </summary>
        public string Website;
        

        /// <summary>
        /// Optional, this needs to be the path to the image.
        /// The image will be read during creation so make sure it's readable!
        /// </summary>
        public string Image;
        /// <summary>
        /// Optional, this is the entire readme as text. Best as UTF-8
        /// </summary>
        public string Readme;
        /// <summary>
        /// Optional, this is the entire script as text. Best as UTF-8
        /// </summary>
        public string Script;

        /// <summary>
        /// Required
        /// </summary>
        public int MajorVersion;
        /// <summary>
        /// Required
        /// </summary>
        public int MinorVersion;
        /// <summary>
        /// Optional
        /// </summary>
        public int BuildVersion;

        /// <summary>
        /// Required
        /// </summary>
        public CompressionType CompressionType;
        /// <summary>
        /// Required, this is the level of compression for all data files inside the .omod file
        /// </summary>
        public CompressionLevel DataFileCompressionLevel;
        /// <summary>
        /// Required, this is the overall level of compression for the .omod file
        /// </summary>
        public CompressionLevel OMODCompressionLevel;

        /// <summary>
        /// Optional if you have no ESPs
        /// </summary>
        public List<string> ESPs;
        /// <summary>
        /// Optional if you have no ESPs
        /// </summary>
        public List<string> ESPPaths;
        /// <summary>
        /// Required, this List contains all absolute paths of the files you want to include
        /// </summary>
        public List<string> DataFiles;
        /// <summary>
        /// Required, this List contains all relative paths of the files from DataFiles:
        /// This is a List and not a HashSet since the framework will loop through the DataFiles list
        /// using the indices and needs a corresponding path at the same index in this List.
        /// EG:
        /// <example>
        /// <code>
        /// DataFiles = {
        ///     "C:\\Modding\\MyOMOD\\readme.txt",
        ///     "C:\\Modding\\MyOMOD\\textures.bsa",
        ///     "C:\\Modding\\MyOMOD\\ui_stuff\\repair_menu_final.xml"
        /// };
        ///
        /// DataFilePaths = {
        ///     "docs\\readme.txt",
        ///     "textures.bsa",
        ///     "menus\\repair_menu.xml"
        /// };
        /// </code>
        /// </example>
        /// </summary>
        public List<string> DataFilePaths;
    }

    /// <summary>
    /// If you want plugin A to load after plugin B you would
    /// set <c>PluginLoadInfo.Plugin</c> to A, <c>PluginLoadInfo.Target</c> to B
    /// and <c>PluginLoadInfo.LoadAfter</c> to true
    /// </summary>
    public struct PluginLoadInfo
    {
        public string Plugin;
        public string Target;
        public bool LoadAfter;

        public PluginLoadInfo(string plugin, string target, bool loadAfter)
        {
            Plugin = plugin;
            Target = target;
            LoadAfter = loadAfter;
        }
    }

    public struct ScriptESPEdit
    {
        public readonly bool IsGMST;
        public readonly string Plugin;
        public readonly string EDID;
        public readonly string Value;

        public ScriptESPEdit(bool gmst, string plugin, string edid, string value)
        {
            IsGMST = gmst;
            Plugin = plugin;
            EDID = edid;
            Value = value;
        }
    }

    /// <summary>
    /// Contains information about the deactivation status of a plugin. Eg you might have
    /// a plugin that should not be deactivated
    /// </summary>
    public struct ScriptESPWarnAgainst
    {
        public string Plugin;
        public DeactivationStatus Status;

        public ScriptESPWarnAgainst(string plugin, DeactivationStatus status)
        {
            Plugin = plugin;
            Status = status;
        }
    }

    /// <summary>
    /// Information about what file goes where
    /// </summary>
    public struct ScriptCopyDataFile
    {
        public readonly string CopyFrom;
        public readonly string CopyTo;

        public ScriptCopyDataFile(string from, string to)
        {
            CopyFrom = from;
            CopyTo = to;
        }
    }

    /// <summary>
    /// Contains all information about the script execution
    /// </summary>
    public class ScriptReturnData
    {
        public bool CancelInstall = false;

        // Plugins
        public bool InstallAllPlugins = true;
        public readonly HashSet<string> IgnorePlugins = new HashSet<string>();
        public readonly HashSet<string> InstallPlugins = new HashSet<string>();
        public readonly HashSet<ScriptCopyDataFile> CopyPlugins = new HashSet<ScriptCopyDataFile>();

        // Data files
        public bool InstallAllData = true;
        public readonly HashSet<string> IgnoreData = new HashSet<string>();
        public readonly HashSet<string> InstallData = new HashSet<string>();
        public readonly HashSet<ScriptCopyDataFile> CopyDataFiles = new HashSet<ScriptCopyDataFile>();

        // Load order stuff
        public readonly HashSet<string> UncheckedPlugins = new HashSet<string>();
        public readonly HashSet<ScriptESPWarnAgainst> ESPDeactivation = new HashSet<ScriptESPWarnAgainst>();
        public readonly HashSet<string> EarlyPlugins = new HashSet<string>();
        public readonly HashSet<PluginLoadInfo> LoadOrderSet = new HashSet<PluginLoadInfo>();
        public readonly HashSet<ConflictData> ConflictsWith = new HashSet<ConflictData>();
        public readonly HashSet<ConflictData> DependsOn = new HashSet<ConflictData>();

        // Edits
        public readonly HashSet<string> RegisterBSASet = new HashSet<string>();
        public readonly HashSet<INIEditInfo> INIEdits = new HashSet<INIEditInfo>();
        public readonly HashSet<SDPEditInfo> SDPEdits = new HashSet<SDPEditInfo>();
        public readonly HashSet<ScriptESPEdit> ESPEdits = new HashSet<ScriptESPEdit>();
        public readonly HashSet<ScriptCopyDataFile> PatchFiles = new HashSet<ScriptCopyDataFile>();
    }

    public class INIEditInfo
    {
        public readonly string Section;
        public readonly string Name;
        public readonly string NewValue;
        public string OldValue;
        public OMOD Plugin;

        public INIEditInfo(string section, string name, string newValue)
        {
            Section = section;
            Name = name;
            NewValue = newValue;
        }

        public static bool operator==(INIEditInfo a, INIEditInfo b) { return a?.Section==b?.Section && a?.Name==b?.Name; }
        public static bool operator!=(INIEditInfo a, INIEditInfo b) { return a?.Section!=b?.Section || a?.Name!=b?.Name; }
        public override bool Equals(object obj) { return this==obj as INIEditInfo; }
        public override int GetHashCode() { return Section.GetHashCode() + Name.GetHashCode(); }
    }

    public class SDPEditInfo
    {
        public readonly byte Package;
        public readonly string Shader;
        public string BinaryObject;

        public SDPEditInfo(byte package, string shader, string binaryObject)
        {
            Package = package;
            Shader = shader.ToLower();
            BinaryObject = binaryObject.ToLower();
        }
    }
}
