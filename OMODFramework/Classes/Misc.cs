/*
 * This file contains parts of the Oblivion Mod Manager licensed under GPLv2
 * and has been modified for use in this OMODFramework
 * Original source: https://www.nexusmods.com/oblivion/mods/2097
 * GPLv2: https://opensource.org/licenses/gpl-2.0.php
 */

namespace OMODFramework.Classes
{
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
}
