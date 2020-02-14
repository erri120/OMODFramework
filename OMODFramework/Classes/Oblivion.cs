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
using System.Linq;
using System.Text;

namespace OMODFramework.Classes
{
    internal static class OblivionINI
    {
        internal static string GetINIValue(string section, string name)
        {
            var result = "";
            GetINISection(section, out var list);
            if(list == null)
                throw new OMODFrameworkException($"Oblivion.ini section {section} does not exist!");

            list.Where(s => s.Trim().ToLower().StartsWith($"{name.ToLower()}=")).Do(s =>
            {
                var res = s.Substring(s.IndexOf('=') + 1).Trim();
                var i = res.IndexOf(';');
                if (i != -1)
                    res = res.Substring(0, i - 1);
                result = res;
            });

            return result;
        }

        internal static void GetINISection(string section, out List<string> list)
        {
            if(string.IsNullOrWhiteSpace(Framework.Settings.ScriptExecutionSettings.OblivionINIPath))
                throw new OMODFrameworkException("OblivionINI.GetINISection requires a path to oblivion.ini");

            var contents = new List<string>();
            var inSection = false;
            using (var sr = new StreamReader(File.OpenRead(Framework.Settings.ScriptExecutionSettings.OblivionINIPath), Encoding.UTF8))
            {
                try
                {
                    while (sr.Peek() != -1)
                    {
                        var s = sr.ReadLine();
                        if (s == null)
                            break;
                        if (inSection)
                        {
                            if (s.Trim().StartsWith("[") && s.Trim().EndsWith("]")) break;
                            contents.Add(s);
                        }
                        else
                        {
                            if (s.Trim().ToLower() == section)
                                inSection = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new OMODFrameworkException($"Could not read from oblivion.ini at {Framework.Settings.ScriptExecutionSettings.OblivionINIPath}\n{e}");
                }
            }

            if (!inSection) list = null;
            list = contents;
        }
    }

    internal static class OblivionRenderInfo
    {
        internal static string GetInfo(string s)
        {
            if(string.IsNullOrWhiteSpace(Framework.Settings.ScriptExecutionSettings.OblivionRendererInfoPath))
                throw new OMODFrameworkException("OblivionRenderInfo.GetInfo requires a path to the RenderInfo.txt file");

            var result = $"Value {s} not found";

            try
            {
                var lines = File.ReadAllLines(Framework.Settings.ScriptExecutionSettings.OblivionRendererInfoPath);
                lines.Where(t => t.Trim().ToLower().StartsWith(s)).Do(t =>
                {
                    var split = t.Split(':');
                    if (split.Length != 2) result = "-1";
                    result = split[1].Trim();
                });
            }
            catch (Exception e)
            {
                throw new OMODFrameworkException($"Could not read from RenderInfo.txt file at {Framework.Settings.ScriptExecutionSettings.OblivionRendererInfoPath}\n{e}");
            }

            return result;
        }
    }
}
