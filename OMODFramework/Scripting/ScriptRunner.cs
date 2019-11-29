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

namespace OMODFramework.Scripting
{
    internal class ScriptRunner
    {
        internal static ScriptReturnData ExecuteScript(string script, string dataPath, string pluginsPath, IScriptFunctions scriptFunctions)
        {
            if (string.IsNullOrWhiteSpace(script)) 
                return new ScriptReturnData();

            ScriptType type;
            if ((byte)script[0] >= (byte)ScriptType.Count)
                type = ScriptType.OBMMScript;
            else
            {
                type = (ScriptType)script[0];
                script = script.Substring(1);
            }

            var handler = new SharedFunctionsHandler(type, ref scriptFunctions);
            var srd = new ScriptReturnData();
            var dotNetScriptFunctions = new Lazy<DotNetScriptFunctions>(() =>
                new DotNetScriptFunctions(srd, dataPath, pluginsPath, ref handler)).Value;

            switch (type) {
                case ScriptType.OBMMScript:
                    return OBMMScriptHandler.Execute(script, dataPath, pluginsPath, ref handler);
                case ScriptType.Python: //TODO
                    break;
                case ScriptType.Csharp:
                    DotNetScriptHandler.ExecuteCS(script, ref dotNetScriptFunctions);
                    break;
                case ScriptType.VB:
                    DotNetScriptHandler.ExecuteVB(script, ref dotNetScriptFunctions);
                    break;
                case ScriptType.Count: //Impossible
                    break;
                default: //Impossible
                    return srd;
            }

            return srd;
        }
    }
}
