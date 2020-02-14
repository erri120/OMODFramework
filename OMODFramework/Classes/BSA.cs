/*
    Copyright (C) 2019-2020  erri120

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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BSAList = System.Collections.Generic.List<OMODFramework.Classes.BSAArchive>;
using HashTable = System.Collections.Generic.Dictionary<ulong, OMODFramework.Classes.BSAArchive.BSAFileInfo>;

namespace OMODFramework.Classes
{
    internal class BSAArchive
    {
        [Flags]
        private enum FileFlags { Meshes = 1, Textures = 2}

        private BinaryReader _br;
        private readonly string _name;
        private readonly bool _defaultCompressed;
        private static bool Loaded;

        private static readonly BSAList LoadedArchives = new BSAList();
        private static readonly HashTable Meshes = new HashTable();
        private static readonly HashTable Textures = new HashTable();
        private static readonly HashTable All = new HashTable();

        private BSAArchive(string path, bool populateAll)
        {
            _name = Path.GetFileNameWithoutExtension(path)?.ToLower();

            _br = new BinaryReader(File.OpenRead(path), Encoding.Default);
            var header = new BSAHeader4(_br);

            if (header.BSAVersion != 0x67 || !populateAll && !header.ContainsMeshes && !header.ContainsTextures)
            {
                _br.Close();
                return;
            }

            _defaultCompressed = (header.ArchiveFlags & 0x100) > 0;

            var folderInfo = new BSAFolderInfo4[header.FolderCount];
            var fileInfo = new BSAFileInfo4[header.FileCount];

            for (int i = 0; i < header.FolderCount; i++) { folderInfo[i] = new BSAFolderInfo4(_br); }

            var count = 0;
            for (uint i = 0; i < header.FolderCount; i++)
            {
                folderInfo[i].Path = new string(_br.ReadChars(_br.ReadByte() - 1));
                _br.BaseStream.Position++;
                folderInfo[i].Offset = count;
                for (int j = 0; j < folderInfo[i].Count; j++)
                {
                    fileInfo[count+j] = new BSAFileInfo4(_br, _defaultCompressed);
                }

                count += folderInfo[i].Count;
            }

            for (uint i = 0; i < header.FileCount; i++)
            {
                fileInfo[i].Path = "";
                char c;
                while ((c = _br.ReadChar()) != '\0')
                    fileInfo[i].Path += c;
            }

            for (var i = 0; i < header.FolderCount; i++)
            {
                for (var j = 0; j < folderInfo[i].Count; j++)
                {
                    var fi4 = fileInfo[folderInfo[i].Offset + j];
                    var ext = Path.GetExtension(fi4.Path);
                    var fi = new BSAFileInfo(this, (int)fi4.Offset, fi4.Size);
                    var fPath = Path.Combine(folderInfo[i].Path, Path.GetFileNameWithoutExtension(fi4.Path));
                    var hash = GenHash(fPath, ext);
                    if (ext == ".nif")
                    {
                        Meshes[hash] = fi;
                    } else if (ext == ".dds")
                    {
                        Textures[hash] = fi;
                    }

                    All[hash] = fi;
                }
            }

            LoadedArchives.Add(this);
        }

        private static ulong GenHash(string file)
        {
            file = file.ToLower().Replace('/', '\\');
            return GenHash(Path.ChangeExtension(file, null), Path.GetExtension(file));
        }

        private static ulong GenHash(string file, string ext)
        {
            file = file.ToLower();
            ext = ext.ToLower();
            ulong hash = 0;
            
            if (file.Length > 0)
            {
                hash = (ulong)(
                    (byte)file[file.Length - 1] * 0x1 +
                    (file.Length > 2 ? (byte)file[file.Length - 2] : 0) * 0x100 +
                    file.Length * 0x10000 +
                    (byte)file[0] * 0x1000000
                );
            }

            if (file.Length > 3)
            {
                hash += (ulong)(GenHash2(file.Substring(1, file.Length - 3)) * 0x100000000);
            }

            if (ext.Length <= 3)
                return hash;

            hash += (ulong)(GenHash2(ext) * 0x100000000);
            byte i = 0;
            switch (ext) {
                case ".nif":
                    i = 1;
                    break;
                case ".dds":
                    i = 3;
                    break;
            }

            if (i == 0)
                return hash;

            byte a = (byte)(((i & 0xfc) << 5) + (byte)((hash & 0xff000000) >> 24));
            byte b = (byte)(((i & 0xfe) << 6) + (byte)(hash & 0xff));
            byte c = (byte)((i << 7) + (byte)((hash & 0xff00) >> 8));
            hash -= hash & 0xFF00FFFF;
            hash += (uint)((a << 24) + b + (c << 8));

            return hash;
        }

        private static uint GenHash2(string s)
        {
            uint hash = 0;
            foreach (var t in s)
            {
                hash *= 0x1003f;
                hash += (byte)t;
            }

            return hash;
        }

        private void Dispose()
        {
            if (_br == null)
                return;

            _br.Close();
            _br = null;
        }
        
        internal static void Clear()
        {
            LoadedArchives.Do(b => b.Dispose());
            Meshes.Clear();
            Textures.Clear();
            All.Clear();
            Loaded = false;
        }

        /*
            private static void Load(bool populateAll) {
                foreach(string s in Directory.GetFiles("data", "*.bsa")) new BSAArchive(s, populateAll);
                Loaded=true;
            }

            internal static bool CheckForTexture(string path) {
                if(!Loaded) Load(false);

                if(File.Exists("data\\"+path)) return true;
                path=path.ToLower().Replace('/', '\\');
                string ext=Path.GetExtension(path);
                ulong hash=GenHash(Path.ChangeExtension(path,null),ext);
                if(Textures.ContainsKey(hash)) return true;
                return false;
            }

            internal static byte[] GetMesh(string path) {
                if(!Loaded) Load(false);

                path=path.ToLower().Replace('/', '\\');
                string ext=Path.GetExtension(path);
                if(ext==".nif") {
                    if(File.Exists("data\\"+path)) return File.ReadAllBytes("data\\"+path);
                    ulong hash=GenHash(Path.ChangeExtension(path, null), ext);
                    if(!Meshes.ContainsKey(hash)) return null;
                    return Meshes[hash].GetRawData();
                }
                return null;
            }
         */

        internal static void Load(HashSet<string> list)
        {
            list.Do(f => new BSAArchive(f, true));
            Loaded = true;
        }

        internal static byte[] GetFileFromBSA(string path)
        {
            if(!Loaded) throw new OMODFrameworkException("BSAs need to be loaded before getting files from them!");

            var hash = GenHash(path);
            return !All.ContainsKey(hash) ? null : All[hash].GetRawData();
        }

        internal static byte[] GetFileFromBSA(string bsa, string path)
        {
            if(!Loaded) throw new OMODFrameworkException("BSAs need to be loaded before getting files from them!");

            var hash = GenHash(path);
            if (!All.ContainsKey(hash)) return null;
            var fi = All[hash];
            return fi.BSA._name != bsa.ToLower() ? null : fi.GetRawData();
        }

        internal struct BSAFileInfo
        {
            internal readonly BSAArchive BSA;
            internal readonly int Offset;
            internal readonly int Size;
            internal readonly bool Compressed;

            internal BSAFileInfo(BSAArchive bsa, int offset, int size)
            {
                BSA = bsa;
                Offset = offset;
                Size = size;

                if ((Size & (1 << 30)) != 0)
                {
                    Size ^= 1 << 30;
                    Compressed = !BSA._defaultCompressed;
                }
                else
                {
                    Compressed = BSA._defaultCompressed;
                }
            }

            internal byte[] GetRawData()
            {
                BSA._br.BaseStream.Seek(Offset, SeekOrigin.Begin);
                if (!Compressed)
                    return BSA._br.ReadBytes(Size);

                byte[] b = new byte[Size - 4];
                byte[] output = new byte[BSA._br.ReadUInt32()];
                BSA._br.Read(b, 0, Size - 4);

                var inflater = new ICSharpCode.SharpZipLib.Zip.Compression.Inflater();
                inflater.SetInput(b, 0, b.Length);
                inflater.Inflate(output);

                return output;

            }
        }

        private struct BSAHeader4
        {
            internal readonly uint BSAVersion;
            internal readonly int DirectorySize;
            internal readonly int ArchiveFlags;
            internal readonly int FolderCount;
            internal readonly int FileCount;
            internal readonly int TotalFolderNameLength;
            internal readonly int TotalFileNameLength;
            internal readonly FileFlags FileFlags;

            internal BSAHeader4(BinaryReader br)
            {
                br.BaseStream.Position += 4;
                BSAVersion = br.ReadUInt32();
                DirectorySize = br.ReadInt32();
                ArchiveFlags = br.ReadInt32();
                FolderCount = br.ReadInt32();
                FileCount = br.ReadInt32();
                TotalFolderNameLength = br.ReadInt32();
                TotalFileNameLength = br.ReadInt32();
                FileFlags = (FileFlags)br.ReadInt32();
            }

            internal bool ContainsMeshes { get { return (FileFlags & FileFlags.Meshes) != 0; } }
            internal bool ContainsTextures { get { return (FileFlags & FileFlags.Textures) != 0; } }
        }

        private struct BSAFileInfo4
        {
            internal string Path;
            internal readonly ulong Hash;
            internal readonly int Size;
            internal readonly uint Offset;

            internal BSAFileInfo4(BinaryReader br, bool defaultCompressed)
            {
                Path = null;

                Hash = br.ReadUInt64();
                Size = br.ReadInt32();
                Offset = br.ReadUInt32();

                if (defaultCompressed) Size ^= 1 << 30;
            }
        }

        private struct BSAFolderInfo4
        {
            internal string Path;
            internal readonly ulong Hash;
            internal readonly int Count;
            internal int Offset;

            internal BSAFolderInfo4(BinaryReader br)
            {
                Path = null;
                Offset = 0;

                Hash = br.ReadUInt64();
                Count = br.ReadInt32();
                //offset=br.ReadInt32();
                br.BaseStream.Position += 4; //Don't need the offset here
            }
        }
    }
}
