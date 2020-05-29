using System.Collections.Generic;
using System.IO;
using System.Linq;
using OMODFramework.Scripting;
using Xunit;

namespace OMODFramework.Test
{
    public class ScriptTests
    {
        private class Settings : IScriptSettings
        {

        }

        [Fact]
        public void ScriptTest()
        {
            var list = new List<FileInfo>
            {
                new FileInfo("M:\\Projects\\omod\\NoMaaM BBB Animation Replacer V3_1 OMOD-35551-3-1.omod"),
                new FileInfo("M:\\Projects\\omod\\NoMaaM Breathing Idles V1 OMOD-40462-1-0.omod"),
                new FileInfo("M:\\Projects\\omod\\HGEC Body with BBB v1dot12-34442.omod"),
                new FileInfo("M:\\Projects\\omod\\EVE_HGEC_BodyStock and Clothing OMOD-24078.omod"),
                new FileInfo("M:\\Projects\\omod\\Robert Male Body Replacer v52 OMOD-40532-1.omod"),
            };

            var srdList = list.Select(x =>
            {
                using var omod = new OMOD(x);
                return ScriptRunner.ExecuteScript(omod, new Settings());
            }).ToList();

            Assert.NotEmpty(srdList);
        }
    }
}
