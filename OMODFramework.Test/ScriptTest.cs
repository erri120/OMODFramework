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

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OMODFramework.Test
{
    [TestClass]
    public class ScriptTest : ATest
    {
        public override HashSet<NexusFile> Files { get; set; } = new HashSet<NexusFile>
        {
            new NexusFile // https://www.nexusmods.com/oblivion/mods/40462
            {
                DownloadFileName = "NoMaaM Breathing Idles V1 OMOD-40462-1-0.omod",
                FileName = "NoMaaM Breathing Idles V1 OMOD-40462-1-0.omod",
                ModID = 40462,
                FileID = 85415
            },
            new NexusFile // https://www.nexusmods.com/oblivion/mods/34442
            {
                DownloadFileName = "HGEC Body with BBB v1dot12-34442.omod",
                FileName = "HGEC Body with BBB v1dot12-34442.omod",
                ModID = 34442,
                FileID = 80882
            },
            new NexusFile // https://www.nexusmods.com/oblivion/mods/24078
            {
                DownloadFileName = "EVE_HGEC_BodyStock and Clothing OMOD-24078.omod",
                FileName = "EVE_HGEC_BodyStock and Clothing OMOD-24078.omod",
                ModID = 24078,
                FileID = 41472
            },
            new NexusFile // https://www.nexusmods.com/oblivion/mods/15619
            {
                DownloadFileName = "Oblivion XP v415 - OMOD-15619.omod",
                FileName = "Oblivion XP v415 - OMOD-15619.omod",
                ModID = 15619,
                FileID = 46662
            },
            new NexusFile // https://www.nexusmods.com/oblivion/mods/35551
            {
                DownloadFileName = "NoMaaM BBB Animation Replacer V3_1 OMOD-35551-3-1.omod",
                FileName = "NoMaaM BBB Animation Replacer V3_1 OMOD-35551-3-1.omod",
                ModID = 35551,
                FileID = 87078
            },
            /* huge mod (150MB)
             new NexusFile // https://www.nexusmods.com/oblivion/mods/40532
            {
                DownloadFileName = "Robert Male Body Replacer v52 OMOD-40532-1.omod",
                FileName = "Robert Male Body Replacer v52 OMOD-40532-1.omod",
                ModID = 40532,
                FileID = 90010
            }*/
        };

        [TestMethod]
        public void TestOBMMScript()
        {
            Files.Do(f =>
            {
                var omod = new OMOD(f.FileName);

                Assert.IsNotNull(omod);

                var data = omod.GetDataFiles();
                var plugins = omod.GetPlugins();

                var scriptFunctions = new ScriptFunctions();

                var srd = omod.RunScript(scriptFunctions, data, plugins);

                Assert.IsNotNull(srd);
                Assert.IsTrue(!srd.CancelInstall);

                srd.Pretty(omod, data, plugins);

                Assert.IsNotNull(srd.InstallFiles);
            });
        }
    }
}
