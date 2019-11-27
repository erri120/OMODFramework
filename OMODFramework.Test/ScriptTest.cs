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

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OMODFramework.Test
{
    [TestClass]
    public class ScriptTest : DownloadTest
    {
        // testing with NoMaaM Breathing Idles from
        // https://www.nexusmods.com/oblivion/mods/40462
        public override string DownloadFileName { get; set; } = "NoMaaM Breathing Idles V1 OMOD-40462-1-0.omod";
        public override string FileName { get; set; } = "NoMaaM Breathing Idles V1 OMOD-40462-1-0.omod";
        public override int ModID { get; set; } = 40462;
        public override int FileID { get; set; } = 85415;
        public override bool DeleteOnFinish { get; set; } = false; //TODO:

        [TestMethod]
        public void TestOBMMScript()
        {
            var omod = new OMOD(FileName);

            Assert.IsNotNull(omod);

            var scriptFunctions = new ScriptFunctions();

            omod.RunScript(scriptFunctions);
        }
    }
}
