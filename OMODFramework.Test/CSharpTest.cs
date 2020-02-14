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

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OMODFramework.Scripting;

namespace OMODFramework.Test
{
    //[TestClass]
    public class CSharpTest : ATest
    {
        public override HashSet<NexusFile> Files { get; set; } = new HashSet<NexusFile>
        {
            new NexusFile // https://www.nexusmods.com/oblivion/mods/10763
            { 
                DownloadFileName = "DarNified UI 1.3.2.zip",
                FileName  = "DarNified UI 1.3.2.omod",
                ModID = 10763,
                FileID = 34631
            }
        };

        //[TestMethod]
        public void TestCSharpScript()
        {
            Files.Do(f =>
            {
                var omod = new OMOD(f.FileName);

                Assert.IsNotNull(omod);

                var scriptFunctions = new ScriptFunctions();

                ScriptRunner.RunScript(omod, scriptFunctions);
            });
        }
    }
}
