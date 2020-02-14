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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace OMODFramework
{ 
    public class OMOD
    {
        protected class PrivateData
        {
            internal ZipFile ModFile;
            internal Image Image;
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
        public readonly CompressionType Compression;

        public string Version => "" + MajorVersion + (MinorVersion != -1 ? "." + MinorVersion + (BuildVersion != -1 ? "." + BuildVersion : "") : "");

        public string FullFilePath => Path.Combine(FilePath, FileName);

        /*public HashSet<string> Plugins;
        public HashSet<DataFileInfo> DataFiles;
        public HashSet<string> BSAs;
        public HashSet<INIEditInfo> INIEdits;
        public HashSet<SDPEditInfo> SDPEdits;*/

        public ConflictLevel Conflict = ConflictLevel.NoConflict;

        public readonly List<ConflictData> ConflictsWith = new List<ConflictData>();
        public readonly List<ConflictData> DependsOn = new List<ConflictData>();

        private ZipFile ModFile
        {
            get
            {
                if (_pD.ModFile != null) return _pD.ModFile;
                _pD.ModFile = new ZipFile(FullFilePath);
                return _pD.ModFile;
            }
        }

        public Image Image
        {
            get
            {
                if (_pD.Image != null)
                    return _pD.Image;

                using (var stream = ExtractWholeFile("image"))
                {
                    if (stream == null) return null;

                    _pD.Image = Image.FromStream(stream);

                }

                return _pD.Image;
            }
        }

        public OMOD(string path)
        {
            Utils.Info($"Loading OMOD from {path}...");

            if(!File.Exists(path))
                throw new OMODFrameworkException($"The provided file at {path} does not exists!");

            FilePath = Path.GetDirectoryName(path);
            FileName = Path.GetFileName(path);
            LowerFileName = FileName.ToLower();

            Utils.Debug("Parsing config file from OMOD");
            using (var configStream = ExtractWholeFile("config"))
            {
                if(configStream == null)
                    throw new OMODFrameworkException($"Could not find the configuration data for {FileName} !");
                using (var br = new BinaryReader(configStream))
                {
                    var fileVersion = br.ReadByte();
                    if(fileVersion > Framework.Settings.CurrentOMODVersion && !Framework.Settings.IgnoreVersionCheck)
                        throw new OMODFrameworkException($"{FileName} was created with a newer version of OBMM and could not be loaded!");

                    ModName = br.ReadString();
                    MajorVersion = br.ReadInt32();
                    MinorVersion = br.ReadInt32();
                    Author = br.ReadString();
                    Email = br.ReadString();
                    Website = br.ReadString();
                    Description = br.ReadString();

                    if (fileVersion >= 2)
                        CreationTime = DateTime.FromBinary(br.ReadInt64());
                    else
                    {
                        var sCreationTime = br.ReadString();
                        if (!DateTime.TryParseExact(sCreationTime, "dd/MM/yyyy HH:mm", null,
                            System.Globalization.DateTimeStyles.NoCurrentDateDefault, out CreationTime))
                            CreationTime = new DateTime(2006, 1, 1);
                    }

                    if (Description == "") Description = "No description";
                    Compression = (CompressionType)br.ReadByte();

                    if (fileVersion >= 1)
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
                Utils.Debug("Finished parsing of the config file");
            }
        }

        internal void Close()
        {
            Utils.Debug("Closing internal mod file and image");
            _pD.ModFile?.Close();
            _pD.ModFile = null;
            _pD.Image = null;
        }

        public static void CreateOMOD(OMODCreationOptions ops, string omodFileName)
        {
            Utils.Info($"Creating OMOD to {omodFileName}");
            if(File.Exists(omodFileName))
                throw new OMODFrameworkException($"The provided omodFileName {omodFileName} already exists!");
            using (var zipStream = new ZipOutputStream(File.Open(omodFileName, FileMode.CreateNew)))
            using (var omodStream = new BinaryWriter(zipStream))
            {
                ZipEntry ze;
                zipStream.SetLevel(ZipHandler.GetCompressionLevel(ops.OMODCompressionLevel));

                if (!string.IsNullOrWhiteSpace(ops.Readme))
                {
                    Utils.Debug("Writing readme to OMOD");
                    ze = new ZipEntry("readme");
                    zipStream.PutNextEntry(ze);
                    omodStream.Write(ops.Readme);
                    omodStream.Flush();
                }

                if (!string.IsNullOrWhiteSpace(ops.Script))
                {
                    Utils.Debug("Writing script to OMOD");
                    ze = new ZipEntry("script");
                    zipStream.PutNextEntry(ze);
                    omodStream.Write(ops.Script);
                    omodStream.Flush();
                }

                if (!string.IsNullOrWhiteSpace(ops.Image))
                {
                    Utils.Debug("Writing image to OMOD");
                    ze = new ZipEntry("image");
                    zipStream.PutNextEntry(ze);

                    try
                    {
                        using (var fs = File.OpenRead(ops.Image))
                        {
                            CompressionHandler.WriteStreamToZip(omodStream, fs);
                            omodStream.Flush();
                        }
                    }
                    catch (Exception e)
                    {
                        throw new OMODFrameworkException($"There was an exception while trying to read the image at {ops.Image}!\n{e}");
                    }
                }

                Utils.Debug("Writing config to OMOD");
                ze = new ZipEntry("config");
                zipStream.PutNextEntry(ze);

                omodStream.Write(Framework.Settings.CurrentOMODVersion);
                omodStream.Write(ops.Name);
                omodStream.Write(ops.MajorVersion);
                omodStream.Write(ops.MinorVersion);
                omodStream.Write(ops.Author);
                omodStream.Write(ops.Email);
                omodStream.Write(ops.Website);
                omodStream.Write(ops.Description);
                omodStream.Write(DateTime.Now.ToBinary());
                omodStream.Write((byte)ops.CompressionType);
                omodStream.Write(ops.BuildVersion);
                
                omodStream.Flush();


                FileStream dataCompressed;
                Stream dataInfo;

                if (ops.ESPs.Count > 0)
                {
                    Utils.Debug("Writing plugins.crc to OMOD");
                    //TODO: find out why OBMM calls GC.Collect here
                    ze = new ZipEntry("plugins.crc");
                    zipStream.PutNextEntry(ze);

                    CompressionHandler.CompressFiles(ops.ESPs, ops.ESPPaths, out dataCompressed, out dataInfo,
                        ops.CompressionType, ops.DataFileCompressionLevel);
                    CompressionHandler.WriteStreamToZip(omodStream, dataInfo);

                    omodStream.Flush();
                    zipStream.SetLevel(0);

                    Utils.Debug("Writing plugins to OMOD");
                    ze = new ZipEntry("plugins");
                    zipStream.PutNextEntry(ze);

                    CompressionHandler.WriteStreamToZip(omodStream, dataCompressed);
                    omodStream.Flush();

                    zipStream.SetLevel(ZipHandler.GetCompressionLevel(ops.OMODCompressionLevel));

                    dataCompressed.Close();
                    dataInfo.Close();
                }

                if (ops.DataFiles.Count > 0)
                {
                    Utils.Debug("Writing data.crc to OMOD");
                    //TODO: find out why OBMM calls GC.Collect here
                    ze = new ZipEntry("data.crc");
                    zipStream.PutNextEntry(ze);

                    CompressionHandler.CompressFiles(ops.DataFiles, ops.DataFilePaths, out dataCompressed, out dataInfo,
                        ops.CompressionType, ops.DataFileCompressionLevel);
                    CompressionHandler.WriteStreamToZip(omodStream, dataInfo);

                    omodStream.Flush();
                    zipStream.SetLevel(0);

                    Utils.Debug("Writing data to OMOD");
                    ze = new ZipEntry("data");
                    zipStream.PutNextEntry(ze);

                    CompressionHandler.WriteStreamToZip(omodStream, dataCompressed);
                    omodStream.Flush();

                    zipStream.SetLevel(ZipHandler.GetCompressionLevel(ops.OMODCompressionLevel));

                    dataCompressed.Close();
                    dataInfo.Close();
                }

                zipStream.Finish();
            }

            Utils.Info("Finished OMOD creation");
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

        public bool HasReadme => ModFile.GetEntry("readme") != null;

        public bool HasScript => ModFile.GetEntry("script") != null;

        public bool HasImage => ModFile.GetEntry("image") != null;

        private string ParseCompressedStream(string dataInfo, string dataCompressed)
        {
            var infoStream = ExtractWholeFile(dataInfo);
            if (infoStream == null) return null;

            var compressedStream = ExtractWholeFile(dataCompressed);

            var path = CompressionHandler.DecompressFiles(infoStream, compressedStream, Compression);

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
            Utils.Debug($"Extracting {ze.Name} from OMOD");

            var file = ModFile.GetInputStream(ze);
            Stream tempStream;

            if (path != null || ze.Size > Framework.Settings.MaxMemoryStreamSize)
                tempStream = Utils.CreateTempFile(out path);
            else
                tempStream = new MemoryStream((int)ze.Size);

            byte[] buffer = new byte[4096];
            int i;

            while ((i = file.Read(buffer, 0, 4096)) > 0)
                tempStream.Write(buffer, 0, i);

            tempStream.Position = 0;
            return tempStream;
        }
    }
}
