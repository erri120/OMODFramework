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

/*
 * This file contains parts of the Oblivion Mod Manager licensed under GPLv2
 * and has been modified for use in this OMODFramework
 * Original source: https://www.nexusmods.com/oblivion/mods/2097
 * GPLv2: https://opensource.org/licenses/gpl-2.0.php
 */

using System.Collections.Generic;

namespace OMODFramework.Classes
{
    public enum ConflictLevel { Active, NoConflict, MinorConflict, MajorConflict, Unusable }

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

    public struct OMODCreationOptions
    {
        public string Name;
        public string Author;
        public string Email;
        public string Website;
        public string Description;
        public string Image;
        public int MajorVersion;
        public int MinorVersion;
        public int BuildVersion;
        public CompressionType CompressionType;
        public CompressionLevel DataFileCompressionLevel;
        public CompressionLevel OMODCompressionLevel;
        public List<string> ESPs;
        public List<string> ESPPaths;
        public List<string> DataFiles;
        public List<string> DataFilePaths;
        public string Readme;
        public string Script;
    }
}
