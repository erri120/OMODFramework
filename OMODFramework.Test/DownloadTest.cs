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
using System.IO;
using System.Net;
using System.Reflection;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pathoschild.FluentNexus;
using File = Alphaleonis.Win32.Filesystem.File;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace OMODFramework.Test
{
    public abstract class DownloadTest
    {
        private string _apiKey;
        private NexusClient _client;

        public abstract string DownloadFileName { get; set; }
        public abstract string FileName { get; set; }
        public abstract int ModID { get; set; }
        public abstract int FileID { get; set; }

        public abstract bool DeleteOnFinish { get; set; }

        [TestInitialize]
        public void Setup()
        {
            Framework.TempDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestTempDir");

            if (File.Exists(DownloadFileName) && File.Exists(FileName))
                return;

            if(!File.Exists("nexus_api_key.txt"))
                throw new Exception("Nexus API Key file does not exist!");

            _apiKey = File.ReadAllText("nexus_api_key.txt");

            _client = new NexusClient(_apiKey, "OMODFramework Unit Tests", "0.0.1");

            var limits = _client.GetRateLimits().Result;

            if(limits.IsBlocked() && !File.Exists(DownloadFileName))
                throw new Exception("Rate limit blocks all Nexus Connections!");

            var downloadLinks = _client.ModFiles.GetDownloadLinks("oblivion", ModID, FileID).Result;

            using (var client = new WebClient())
            {
                client.DownloadFile(downloadLinks[0].Uri, DownloadFileName);
            }

            if(File.Exists(FileName))
                return;

            using (var zipStream = new ZipFile(File.OpenRead(DownloadFileName)))
            using (var fs = new FileStream(FileName, FileMode.CreateNew))
            {
                foreach (ZipEntry ze in zipStream)
                {
                    if(ze.IsFile && ze.Name.ToLower().Contains("omod"))
                        zipStream.GetInputStream(ze).CopyTo(fs);
                }
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            if(DeleteOnFinish)
                Framework.CleanTempDir(true);
        }
    }
}
