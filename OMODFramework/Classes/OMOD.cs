/*
 * This file contains parts of the Oblivion Mod Manager licensed under GPLv2
 * and has been modified for use in this OMODFramework
 * Original source: https://www.nexusmods.com/oblivion/mods/2097
 * GPLv2: https://opensource.org/licenses/gpl-2.0.php
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace OMODFramework.Classes
{ 
    public class OMOD
    {
        protected class PrivateData
        {
            internal ZipFile modFile = null;
            internal Image image;
        }

        private PrivateData _pD = new PrivateData();
        internal void RecreatePrivateData() { if(_pD == null) _pD = new PrivateData(); }

        public readonly string FilePath;
        public readonly string FileName;
        public readonly string LowerFileName;
        public readonly string ModName;
        public readonly int MajorVersion;
        public readonly int MinorVersion;
        public readonly int BuildVersion;
        public readonly string Description;
        public readonly string Email;
        public readonly string Website;
        public readonly string Author;
        public readonly DateTime CreationTime;
        public readonly HashSet<string> AllPlugins;
        public readonly HashSet<DataFileInfo> AllDataFiles;
        public readonly uint CRC;
        public readonly CompressionType CompType;
        private readonly byte FileVersion;
        public bool Hidden = false;

        // TODO: minor and build version can be -1
        public string Version => $"{MajorVersion}.{MinorVersion}.{BuildVersion}";

        public string FullFilePath => Path.Combine(FilePath, FileName);

        public HashSet<string> Plugins;
        public HashSet<DataFileInfo> DataFiles;
        public HashSet<string> BSAs;
        //TODO: public List<INIEditInfo> INIEdits;
        //TODO: public List<SDPEditInfo> SDPEdits;

        //TODO: public ConflictLevel Conflict=ConflictLevel.NoConflict;

        //TODO: public readonly List<ConflictData> ConflictsWith=new List<ConflictData>();
        //TODO: public readonly List<ConflictData> DependsOn=new List<ConflictData>();

        private ZipFile ModFile
        {
            get
            {
                if (_pD.modFile != null) return _pD.modFile;
                _pD.modFile = new ZipFile(FullFilePath);
                return _pD.modFile;
            }
        }

        //TODO: public Image image

        public OMOD(string path, ref Framework f)
        {
            if(!File.Exists(path))
                throw new OMODFrameworkException($"The provided file at {path} does not exists!");

            FilePath = Path.GetDirectoryName(path);
            FileName = Path.GetFileName(path);
            LowerFileName = FileName.ToLower();

            using (var configStream = ExtractWholeFile("config"))
            {
                if(configStream == null)
                    throw new OMODFrameworkException($"Could not find the configuration data for {FileName} !");
                using (var br = new BinaryReader(configStream))
                {
                    FileVersion = br.ReadByte();
                    if(FileVersion > f.CurrentOmodVersion && !f.IgnoreVersion)
                        throw new OMODFrameworkException($"{FileName} was created with a newer version of OBMM and could not be loaded!");

                    ModName = br.ReadString();
                    MajorVersion = br.ReadInt32();
                    MinorVersion = br.ReadInt32();
                    Author = br.ReadString();
                    Email = br.ReadString();
                    Website = br.ReadString();
                    Description = br.ReadString();

                    if (FileVersion >= 2)
                        CreationTime = DateTime.FromBinary(br.ReadInt64());
                    else
                    {
                        var sCreationTime = br.ReadString();
                        if (!DateTime.TryParseExact(sCreationTime, "dd/MM/yyyy HH:mm", null,
                            System.Globalization.DateTimeStyles.NoCurrentDateDefault, out CreationTime))
                            CreationTime = new DateTime(2006, 1, 1);
                    }

                    if (Description == "") Description = "No description";
                    CompType = (CompressionType)br.ReadByte();

                    if (FileVersion >= 1)
                        BuildVersion = br.ReadInt32();
                    else
                        BuildVersion = -1;

                    AllPlugins = GetPluginSet();
                    AllDataFiles = GetDataSet();

                    AllPlugins.Do(p =>
                    {
                        if(!Utils.IsSafeFileName(p))
                            throw new OMODFrameworkException($"File {FileName} has been modified and will not be loaded!");
                    });

                    AllDataFiles.Do(d =>
                    {
                        if(!Utils.IsSafeFileName(d.FileName))
                            throw new OMODFrameworkException($"File {FileName} has been modified and will not be loaded!");
                    });

                    CRC = CompressionHandler.CRC(FullFilePath);
                }

                Close();
            }
        }

        public void Close()
        {
            _pD.modFile?.Close();
            _pD.modFile = null;
            _pD.image = null;
        }

        private HashSet<string> GetPluginSet()
        {
            var tempStream = ExtractWholeFile("plugins.crc");
            if(tempStream == null) return new HashSet<string>(0);

            using (var br = new BinaryReader(tempStream))
            {
                var ar = new HashSet<string>();
                while (br.PeekChar() != -1)
                {
                    ar.Add(br.ReadString());
                    br.ReadInt32();
                    br.ReadInt64();
                }

                return ar;
            }
        }

        private HashSet<DataFileInfo> GetDataSet()
        {
            var tempStream = ExtractWholeFile("data.crc");
            if(tempStream == null) return new HashSet<DataFileInfo>(0);

            using (var br = new BinaryReader(tempStream))
            {
                var ar = new HashSet<DataFileInfo>();
                while (br.PeekChar() != -1)
                {
                    var s = br.ReadString();
                    ar.Add(new DataFileInfo(s, br.ReadUInt32()));
                    br.ReadInt64();
                }

                return ar;
            }
        }

        public string GetPlugins()
        {
            return ParseCompressedStream("plugins.crc", "plugins");
        }

        public string GetDataFiles()
        {
            return ParseCompressedStream("data.crc", "data");
        }

        public string GetReadme()
        {
            var s = ExtractWholeFile("readme");
            if (s == null) return null;

            using (var br = new BinaryReader(s))
            {
                var script = br.ReadString();
                return script;
            }
        }

        public string GetScript()
        {
            var s = ExtractWholeFile("script");
            if (s == null) return null;

            using (var br = new BinaryReader(s))
            {
                var script = br.ReadString();
                return script;
            }
        }

        public string GetImage()
        {
            var bitmapPath = "";
            var s = ExtractWholeFile("image", ref bitmapPath);
            if (s == null)
                bitmapPath = null;
            else
                s.Close();
            return bitmapPath;
        }

        private string ParseCompressedStream(string dataInfo, string dataCompressed)
        {
            var infoStream = ExtractWholeFile(dataInfo);
            if (infoStream == null) return null;

            var compressedStream = ExtractWholeFile(dataCompressed);
            var path = CompressionHandler.DecompressFiles(infoStream, compressedStream, CompType);

            infoStream.Close();
            compressedStream.Close();

            return path;
        }

        private Stream ExtractWholeFile(string s)
        {
            string s2 = null;
            return ExtractWholeFile(s, ref s2);
        }

        private Stream ExtractWholeFile(string s, ref string path)
        {
            var ze = ModFile.GetEntry(s);
            return ze == null ? null : ExtractWholeFile(ze, ref path);
        }

        private Stream ExtractWholeFile(ZipEntry ze, ref string path)
        {
            var file = ModFile.GetInputStream(ze);
            Stream tempStream;

            if (path != null || ze.Size > Framework.MaxMemoryStreamSize)
                tempStream = Utils.CreateTempFile(out path);
            else
                tempStream = new MemoryStream((int)ze.Size);

            byte[] buffer = new byte[4096];
            int i;

            while((i = file.Read(buffer, 0, 4096)) > 0)
                tempStream.Write(buffer, 0, i);

            tempStream.Position = 0;
            return tempStream;
        }
    }
}
