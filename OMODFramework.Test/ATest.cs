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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pathoschild.FluentNexus;

namespace OMODFramework.Test
{
    public abstract class ATest
    {
        private string _apiKey;
        private NexusClient _client;

        public abstract HashSet<NexusFile> Files { get; set; }

        [TestInitialize]
        public void Setup()
        {
            Framework.Settings.TempPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestTempDir");

            if (!Directory.Exists(Framework.Settings.TempPath))
                Directory.CreateDirectory(Framework.Settings.TempPath);
            else
                Framework.CleanTempDir();

            if (Files.All(f => File.Exists(f.DownloadFileName) && File.Exists(f.FileName)))
                return;

            if(!File.Exists("nexus_api_key.txt"))
                throw new Exception("Nexus API Key file does not exist!");

            _apiKey = File.ReadAllText("nexus_api_key.txt");

            _client = new NexusClient(_apiKey, "OMODFramework Unit Tests", "0.0.1");

            var limits = _client.GetRateLimits().Result;

            if(limits.IsBlocked())
                throw new Exception("Rate limit blocks all Nexus Connections!");

            Files.Do(f =>
            {
                var downloadLinks = _client.ModFiles.GetDownloadLinks("oblivion", f.ModID, f.FileID).Result;

                using (var client = new WebClient())
                {
                    client.DownloadFile(downloadLinks[0].Uri, f.DownloadFileName);
                }

                if(File.Exists(f.FileName))
                    return;

                using (var zipStream = new ZipFile(File.OpenRead(f.DownloadFileName)))
                using (var fs = new FileStream(f.FileName, FileMode.CreateNew))
                {
                    foreach (ZipEntry ze in zipStream)
                    {
                        if(ze.IsFile && ze.Name.ToLower().Contains("omod"))
                            zipStream.GetInputStream(ze).CopyTo(fs);
                    }
                }
            });
        }
    }
}
