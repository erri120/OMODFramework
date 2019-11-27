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
using System.Globalization;
using System.IO;
using System.Linq;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using File = Alphaleonis.Win32.Filesystem.File;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace OMODFramework.Scripting
{
    internal static class OBMMScriptHandler
    {
        private class FlowControlStruct
        {
            public readonly int line;
            public readonly byte type;
            public readonly string[] values;
            public readonly string var;
            public bool active;
            public bool hitCase = false;
            public int forCount = 0;

            //Inactive
            public FlowControlStruct(byte type)
            {
                line = -1;
                this.type = type;
                values = null;
                var = null;
                active = false;
            }

            //If
            public FlowControlStruct(int line, bool active)
            {
                this.line = line;
                type = 0;
                values = null;
                var = null;
                this.active = active;
            }

            //Select
            public FlowControlStruct(int line, string[] values)
            {
                this.line = line;
                type = 1;
                this.values = values;
                var = null;
                active = false;
            }

            //For
            public FlowControlStruct(string[] values, string var, int line)
            {
                this.line = line;
                type = 2;
                this.values = values;
                this.var = var;
                active = false;
            }
        }

        private static ScriptReturnData srd;
        private static Dictionary<string, string> variables;

        private static string DataFiles;
        private static string Plugins;
        private static string cLine = "0";

        private static IScriptFunctions _scriptFunctions;

        internal static ScriptReturnData Execute(string inputScript, string dataPath, string pluginsPath, IScriptFunctions scriptFunctions)
        {
            srd = new ScriptReturnData();
            if (string.IsNullOrWhiteSpace(inputScript))
                return srd;

            _scriptFunctions = scriptFunctions ?? throw new OMODFrameworkException("The provided script functions can not be null!");

            DataFiles = dataPath;
            Plugins = pluginsPath;
            variables = new Dictionary<string, string>();

            var flowControl = new Stack<FlowControlStruct>();
            var extraLines = new Queue<string>();

            variables["NewLine"] = Environment.NewLine;
            variables["Tab"] = "\t";

            var script = inputScript.Replace("\r", "").Split('\n');
            string[] line;
            string s, skipTo = null;
            bool allowRunOnLines = false;
            bool Break = false;

            for (var i = 0; i < script.Length || extraLines.Count > 0; i++)
            {
                if (extraLines.Count > 0)
                {
                    i--;
                    s = extraLines.Dequeue().Replace('\t', ' ').Trim();
                }
                else
                {
                    s = script[i].Replace('\t', ' ').Trim();
                }

                cLine = i.ToString();
                if (allowRunOnLines)
                {
                    while (s.EndsWith("\\"))
                    {
                        s = s.Remove(s.Length - 1);
                        if (extraLines.Count > 0)
                        {
                            s += extraLines.Dequeue().Replace('\t', ' ').Trim();
                        }
                        else
                        {
                            if (++i == script.Length)
                                Warn($"Run-on line passed end of script");
                            else
                                s += script[i].Replace('\t', ' ').Trim();
                        }
                    }
                }

                if (skipTo != null)
                {
                    if (s == skipTo) skipTo = null;
                    else continue;
                }

                line = SplitLine(s);
                if (line.Length == 0) continue;

                if (flowControl.Count != 0 && !flowControl.Peek().active)
                {
                    switch (line[0])
                    {
                    case "":
                        Warn($"Empty function");
                        break;
                    case "If":
                    case "IfNot":
                        flowControl.Push(new FlowControlStruct(0));
                        break;
                    case "Else":
                        if (flowControl.Count != 0 && flowControl.Peek().type == 0)
                            flowControl.Peek().active = flowControl.Peek().line != -1;
                        else Warn($"Unexpected Else");
                        break;
                    case "EndIf":
                        if (flowControl.Count != 0 && flowControl.Peek().type == 0) flowControl.Pop();
                        else Warn($"Unexpected EndIf");
                        break;
                    case "Select":
                    case "SelectMany":
                    case "SelectWithPreview":
                    case "SelectManyWithPreview":
                    case "SelectWithDescriptions":
                    case "SelectManyWithDescriptions":
                    case "SelectWithDescriptionsAndPreviews":
                    case "SelectManyWithDescriptionsAndPreviews":
                    case "SelectVar":
                    case "SelectString":
                        flowControl.Push(new FlowControlStruct(1));
                        break;
                    case "Case":
                        if (flowControl.Count != 0 && flowControl.Peek().type == 1)
                        {
                            if (flowControl.Peek().line != -1 && Array.IndexOf(flowControl.Peek().values, s) != -1)
                            {
                                flowControl.Peek().active = true;
                                flowControl.Peek().hitCase = true;
                            }
                        }
                        else Warn($"Unexpected Break");

                        break;
                    case "Default":
                        if (flowControl.Count != 0 && flowControl.Peek().type == 1)
                        {
                            if (flowControl.Peek().line != -1 && !flowControl.Peek().hitCase)
                                flowControl.Peek().active = true;
                        }
                        else Warn($"Unexpected Default");

                        break;
                    case "EndSelect":
                        if (flowControl.Count != 0 && flowControl.Peek().type == 1) flowControl.Pop();
                        else Warn($"Unexpected EndSelect");
                        break;
                    case "For":
                        flowControl.Push(new FlowControlStruct(2));
                        break;
                    case "EndFor":
                        if (flowControl.Count != 0 && flowControl.Peek().type == 2) flowControl.Pop();
                        else Warn($"Unexpected EndFor");
                        break;
                    case "Break":
                    case "Continue":
                    case "Exit":
                        break;
                    default:
                        Warn($"Unrecognized function: {line[0]}!");
                        break;
                    }
                }
                else
                {
                    switch (line[0])
                    {
                    case "Goto":
                        if (line.Length < 2)
                            Warn("Not enough arguments to function 'Goto'!");
                        else
                        {
                            if (line.Length > 2) Warn("Unexpected extra arguments to function 'Goto'");
                            skipTo = $"Label {line[1]}";
                            flowControl.Clear();
                        }
                        break;
                    case "Label":
                        break;
                    case "If":
                        //TODO: FlowControl.Push(new FlowControlStruct(i, FunctionIf(line)));
                        break;
                    case "IfNot":
                        //TODO: FlowControl.Push(new FlowControlStruct(i, !FunctionIf(line)));
                        break;
                    case "Else":
                        if (flowControl.Count != 0 && flowControl.Peek().type == 0) flowControl.Peek().active = false;
                        else Warn("Unexpected Else");
                        break;
                    case "EndIf":
                        if (flowControl.Count != 0 && flowControl.Peek().type == 0) flowControl.Pop();
                        else Warn("Unexpected EndIf");
                        break;
                    case "Select":
                        flowControl.Push(new FlowControlStruct(i, FunctionSelect(line, false, false, false)));
                        break;
                    case "SelectMany":
                        flowControl.Push(new FlowControlStruct(i, FunctionSelect(line, true, false, false)));
                        break;
                    case "SelectWithPreview":
                        flowControl.Push(new FlowControlStruct(i, FunctionSelect(line, false, true, false)));
                        break;
                    case "SelectManyWithPreview":
                        flowControl.Push(new FlowControlStruct(i, FunctionSelect(line, true, true, false)));
                        break;
                    case "SelectWithDescriptions":
                        flowControl.Push(new FlowControlStruct(i, FunctionSelect(line, false, false, true)));
                        break;
                    case "SelectManyWithDescriptions":
                        flowControl.Push(new FlowControlStruct(i, FunctionSelect(line, true, false, true)));
                        break;
                    case "SelectWithDescriptionsAndPreviews":
                        flowControl.Push(new FlowControlStruct(i, FunctionSelect(line, false, true, true)));
                        break;
                    case "SelectManyWithDescriptionsAndPreviews":
                        flowControl.Push(new FlowControlStruct(i, FunctionSelect(line, true, true, true)));
                        break;
                    case "SelectVar":
                        flowControl.Push(new FlowControlStruct(i, FunctionSelectVar(line, true)));
                        break;
                    case "SelectString":
                        flowControl.Push(new FlowControlStruct(i, FunctionSelectVar(line, false)));
                        break;
                    case "Break": {
                        bool found = false;
                        var fcs = flowControl.ToArray();
                        for (int k = 0; k < fcs.Length; k++)
                        {
                            if (fcs[k].type != 1)
                                continue;

                            for (int j = 0; j <= k; j++) fcs[j].active = false;
                            found = true;
                            break;
                        }

                        if (!found) Warn("Unexpected Break");
                        break;
                        }
                    case "Case":
                        if (flowControl.Count == 0 || flowControl.Peek().type != 1) Warn($"Unexpected Case");
                        break;
                    case "Default":
                        if (flowControl.Count == 0 || flowControl.Peek().type != 1)
                            Warn($"Unexpected Default");
                        break;
                    case "EndSelect":
                        if(flowControl.Count!=0&&flowControl.Peek().type==1) flowControl.Pop();
                        else Warn($"Unexpected EndSelect");
                        break;
                    case "For": {
                        var fc = FunctionFor(line, i);
                        flowControl.Push(fc);
                        if (fc.line != -1 && fc.values.Length > 0)
                        {
                            variables[fc.var] = fc.values[0];
                            fc.active = true;
                        }

                        break;
                        }
                    case "Continue": {
                        var found = false;
                        var fcs = flowControl.ToArray();
                        for (int k = 0; k < fcs.Length; k++)
                        {
                            if (fcs[k].type != 2)
                                continue;

                            fcs[k].forCount++;
                            if (fcs[k].forCount == fcs[k].values.Length)
                            {
                                for (int j = 0; j <= k; j++) fcs[j].active = false;
                            }
                            else
                            {
                                i = fcs[k].line;
                                variables[fcs[k].var] = fcs[k].values[fcs[k].forCount];
                                for (int j = 0; j < k; j++) flowControl.Pop();
                            }

                            found = true;
                            break;
                        }

                        if (!found) Warn($"Unexpected Continue");
                        break;
                        }
                    case "Exit": {
                        bool found = false;
                        var fcs = flowControl.ToArray();
                        for (int k = 0; k < fcs.Length; k++)
                        {
                            if (fcs[k].type != 2)
                                continue;

                            for (int j = 0; j <= k; j++) flowControl.Peek().active = false;
                            found = true;
                            break;
                        }

                        if (!found) Warn($"Unexpected Exit");
                        break;
                        }
                    case "EndFor":
                        if (flowControl.Count != 0 && flowControl.Peek().type == 2)
                        {
                            var fc = flowControl.Peek();
                            fc.forCount++;
                            if (fc.forCount == fc.values.Length) flowControl.Pop();
                            else
                            {
                                i = fc.line;
                                variables[fc.var] = fc.values[fc.forCount];
                            }
                        }
                        else Warn("Unexpected EndFor");
                        break;
                    //Functions
                    case "Message":
                        FunctionMessage(line);
                        break;
                    case "LoadEarly":
                        FunctionLoadEarly(line);
                        break;
                    case "LoadBefore":
                        FunctionLoadOrder(line, false);
                        break;
                    case "LoadAfter":
                        FunctionLoadOrder(line, true);
                        break;
                    case "ConflictsWith":
                        FunctionConflicts(line, true, false);
                        break;
                    case "DependsOn":
                        FunctionConflicts(line, false, false);
                        break;
                    case "ConflictsWithRegex":
                        FunctionConflicts(line, true, true);
                        break;
                    case "DependsOnRegex":
                        FunctionConflicts(line, false, true);
                        break;
                    case "DontInstallAnyPlugins":
                        srd.InstallAllPlugins = false;
                        break;
                    case "DontInstallAnyDataFiles":
                        srd.InstallAllData = false;
                        break;
                    case "InstallAllPlugins":
                        srd.InstallAllPlugins = true;
                        break;
                    case "InstallAllDataFiles":
                        srd.InstallAllData = true;
                        break;
                    case "InstallPlugin":
                        FunctionModifyInstall(line, true, true);
                        break;
                    case "DontInstallPlugin":
                        FunctionModifyInstall(line, true, false);
                        break;
                    case "InstallDataFile":
                        FunctionModifyInstall(line, false, true);
                        break;
                    case "DontInstallDataFile":
                        FunctionModifyInstall(line, false, false);
                        break;
                    case "DontInstallDataFolder":
                        FunctionModifyInstallFolder(line, false);
                        break;
                    case "InstallDataFolder":
                        FunctionModifyInstallFolder(line, true);
                        break;
                    case "RegisterBSA":
                        //TODO: FunctionRegisterBSA(line, true);
                        break;
                    case "UnregisterBSA":
                        //TODO: FunctionRegisterBSA(line, false);
                        break;
                    case "FatalError":
                        srd.CancelInstall = true;
                        break;
                    case "Return":
                        Break = true;
                        break;
                    case "UncheckESP":
                        FunctionUncheckESP(line);
                        break;
                    case "SetDeactivationWarning":
                        FunctionSetDeactivationWarning(line);
                        break;
                    case "CopyDataFile":
                        FunctionCopyDataFile(line, false);
                        break;
                    case "CopyPlugin":
                        FunctionCopyDataFile(line, true);
                        break;
                    case "CopyDataFolder":
                        FunctionCopyDataFolder(line);
                        break;
                    case "PatchPlugin":
                        FunctionPatch(line, true);
                        break;
                    case "PatchDataFile":
                        FunctionPatch(line, false);
                        break;
                    case "EditINI":
                        //TODO: FunctionEditINI(line);
                        break;
                    case "EditSDP":
                    case "EditShader":
                        //TODO: FunctionEditShader(line);
                        break;
                    case "SetGMST":
                        //TODO: FunctionSetEspVar(line, true);
                        break;
                    case "SetGlobal":
                        //TODO: FunctionSetEspVar(line, false);
                        break;
                    case "SetPluginByte":
                        //TODO: FunctionSetEspData(line, typeof(byte));
                        break;
                    case "SetPluginShort":
                        //TODO: FunctionSetEspData(line, typeof(short));
                        break;
                    case "SetPluginInt":
                        //TODO: FunctionSetEspData(line, typeof(int));
                        break;
                    case "SetPluginLong":
                        //TODO: FunctionSetEspData(line, typeof(long));
                        break;
                    case "SetPluginFloat":
                        //TODO: FunctionSetEspData(line, typeof(float));
                        break;
                    case "DisplayImage":
                        //TODO: FunctionDisplayFile(line, true);
                        break;
                    case "DisplayText":
                        //TODO: FunctionDisplayFile(line, false);
                        break;
                    case "SetVar":
                        FunctionSetVar(line);
                        break;
                    case "GetFolderName":
                    case "GetDirectoryName":
                        FunctionGetDirectoryName(line);
                        break;
                    case "GetFileName":
                        FunctionGetFileName(line);
                        break;
                    case "GetFileNameWithoutExtension":
                        FunctionGetFileNameWithoutExtension(line);
                        break;
                    case "CombinePaths":
                        FunctionCombinePaths(line);
                        break;
                    case "Substring":
                        FunctionSubRemoveString(line, false);
                        break;
                    case "RemoveString":
                        FunctionSubRemoveString(line, true);
                        break;
                    case "StringLength":
                        FunctionStringLength(line);
                        break;
                    case "InputString":
                        //TODO: FunctionInputString(line);
                        break;
                    case "ReadINI":
                        //TODO: FunctionReadINI(line);
                        break;
                    case "ReadRendererInfo":
                        //TODO: FunctionReadRenderer(line);
                        break;
                    case "ExecLines":
                        FunctionExecLines(line, ref extraLines);
                        break;
                    case "iSet":
                        FunctionSet(line, true);
                        break;
                    case "fSet":
                        FunctionSet(line, false);
                        break;
                    case "EditXMLLine":
                        FunctionEditXMLLine(line);
                        break;
                    case "EditXMLReplace":
                        FunctionEditXMLReplace(line);
                        break;
                    case "AllowRunOnLines":
                        allowRunOnLines = true;
                        break;
                    default:
                        Warn($"Unrecognized function: {line[0]}!");
                        break;
                    }
                }

                if (Break || srd.CancelInstall) break;
            }

            if (skipTo != null) Warn($"Expected: {skipTo}!");

            var temp = srd;
            srd = null;
            variables = null;

            return temp;
        }

        private static void Warn(string msg)
        {
            if(Framework.EnableWarnings)
                _scriptFunctions.Warn($"'{msg}' at {cLine}");
        }

        private static string[] SplitLine(string s)
        {
            var temp = new List<string>();
            bool wasLastSpace = false;
            bool inQuotes = false;
            bool wasLastEscape = false;
            bool doubleBreak = false;
            bool inVar = false;
            string currentWord = "";
            string currentVar = "";

            if (s == "") return new string[0];
            s += " ";
            foreach (var t in s)
            {
                switch (t)
                {
                    case '%':
                        wasLastSpace = false;
                        if (inVar)
                        {
                            if (variables.ContainsKey(currentWord))
                                currentWord = currentVar + variables[currentWord];
                            else
                                currentWord = currentVar + "%" + currentWord + "%";
                            currentVar = "";
                            inVar = false;
                        }
                        else
                        {
                            if (inQuotes && wasLastEscape)
                            {
                                currentWord += "%";
                            }
                            else
                            {
                                inVar = true;
                                currentVar = currentWord;
                                currentWord = "";
                            }
                        }

                        wasLastEscape = false;
                        break;
                    case ',':
                    case ' ':
                        wasLastEscape = false;
                        if (inVar)
                        {
                            currentWord = currentVar + "%" + currentWord;
                            currentVar = "";
                            inVar = false;
                        }

                        if (inQuotes)
                        {
                            currentWord += t;
                        }
                        else if (!wasLastSpace)
                        {
                            temp.Add(currentWord);

                            currentWord = "";
                            wasLastSpace = true;
                        }

                        break;
                    case ';':
                        wasLastEscape = false;
                        if (!inQuotes)
                        {
                            doubleBreak = true;
                        }
                        else
                            currentWord += t;

                        break;
                    case '"':
                        if (inQuotes && wasLastEscape)
                        {
                            currentWord += t;
                        }
                        else
                        {
                            if (inVar) Warn("String marker found in the middle of a variable name");
                            inQuotes = !inQuotes;
                        }

                        wasLastSpace = false;
                        wasLastEscape = false;
                        break;
                    case '\\':
                        if (inQuotes && wasLastEscape)
                        {
                            currentWord += t;
                            wasLastEscape = false;
                        }
                        else if (inQuotes)
                        {
                            wasLastEscape = true;
                        }
                        else
                        {
                            currentWord += t;
                        }

                        wasLastSpace = false;
                        break;
                    default:
                        wasLastEscape = false;
                        wasLastSpace = false;
                        currentWord += t;
                        break;
                }

                if (doubleBreak) break;
            }

            if (inVar) Warn("Unterminated variable");
            if (inQuotes) Warn("Unterminated quote");
            return temp.ToArray();
        }

        private static FlowControlStruct FunctionFor(IList<string> line, int lineNo)
        {
            var nullLoop = new FlowControlStruct(3);
            if (line.Count < 3)
            {
                Warn("Missing arguments for 'For'");
                return nullLoop;
            }

            if (line[1] == "Each") line[1] = line[2];
            switch (line[1])
            {
            case "Count":
            {
                if (line.Count < 5)
                {
                    Warn("Missing arguments to function 'For Count'");
                    return nullLoop;
                }
                if (line.Count > 6) Warn("Unexpected extra arguments for 'For Count'");
                int step = 1;
                if (!int.TryParse(line[3], out var start) || !int.TryParse(line[4], out var end) ||
                    line.Count >= 6 && !int.TryParse(line[5], out step))
                {
                    Warn("Invalid argument to 'For Count'");
                    return nullLoop;
                }
                var steps = new List<string>();
                for (int i = start; i < +end; i += step)
                {
                    steps.Add(i.ToString());
                }

                return new FlowControlStruct(steps.ToArray(), line[2], lineNo);
            }
            case "DataFolder":
            case "PluginFolder":
            case "DataFile":
            case "Plugin":
            {
                string root;
                if (line[1] == "DataFolder" || line[1] == "DataFile")
                    root = DataFiles;
                else
                    root = Plugins;

                if (line.Count < 5)
                {
                    Warn($"Missing arguments for 'For Each {line[1]}'");
                    return nullLoop;
                }
                if(line.Count > 7) Warn($"Unexpected extra arguments to 'For Each {line[1]}'");
                if (!Utils.IsSafeFolderName(line[4]))
                {
                    Warn($"Invalid argument for 'For Each {line[1]}'\nDirectory '{line[4]}' is not valid");
                    return nullLoop;
                }

                if (!Directory.Exists(Path.Combine(root, line[4])))
                {
                    Warn($"Invalid argument for 'For Each {line[1]}'\nDirectory '{line[4]}' does not exist");
                }

                var option = SearchOption.TopDirectoryOnly;
                if (line.Count > 5)
                {
                    switch (line[5])
                    {
                    case "True":
                        option = SearchOption.AllDirectories;
                        break;
                    case "False":
                        break;
                    default:
                        Warn($"Invalid argument '{line[5]}' for 'For Each {line[1]}'.\nExpected 'True' or 'False'");
                        break;
                    }
                }

                try
                {
                    var paths = Directory.GetDirectories(Path.Combine(root, line[4]),
                        line.Count > 6 ? line[6] : "*", option);
                    for (var i = 0; i < paths.Length; i++)
                    {
                        if (Path.IsPathRooted(paths[i]))
                            paths[i] = paths[i].Substring(root.Length);
                    }
                    return new FlowControlStruct(paths, line[3], lineNo);
                }
                catch
                {
                    Warn($"Invalid argument for 'For Each {line[1]}'");
                    return nullLoop;
                }
            }
            default:
                Warn("Unexpected function for 'For'");
                return nullLoop;
            }
        }

        private static string[] FunctionSelect(IList<string> line, bool isMultiSelect, bool hasPreviews, bool hasDescriptions)
        {
            if (line.Count < 3)
            {
                Warn("Missing arguments for 'Select'");
                return new string[0];
            }

            int argsPerOption = 1 + (hasPreviews ? 1 : 0) + (hasDescriptions ? 1 : 0);

            var title = line[1];
            var items = new List<string>(line.Count - 2);
            line.Where(s => line.IndexOf(s) >= 2).Do(items.Add);
            line = items;

            if (line.Count % argsPerOption != 0)
            {
                Warn("Unexpected extra arguments for 'Select'");
                do
                {
                    line.RemoveAt(line.Count - line.Count % argsPerOption);
                } while (line.Count % argsPerOption != 0);
            }

            items = new List<string>(line.Count/argsPerOption);
            var previews = hasPreviews ? new List<string>(line.Count / argsPerOption) : null;
            var descriptions = hasDescriptions ? new List<string>(line.Count / argsPerOption) : null;

            for (var i = 0; i < line.Count / argsPerOption; i++)
            {
                items[i] = line[i * argsPerOption];
                if (hasPreviews)
                {
                    previews[i] = line[i * argsPerOption + 1];
                    if (hasDescriptions) descriptions[i] = line[i * argsPerOption + 2];
                }
                else
                {
                    if (hasDescriptions) descriptions[i] = line[i * argsPerOption + 1];
                }
            }

            if (previews != null)
            {
                for (var i = 0; i < previews.Count; i++)
                {
                    if (previews[i] == "None")
                    {
                        previews[i] = null;
                    } else if (!Utils.IsSafeFileName(previews[i])) {
                        Warn($"Preview file path '{previews[i]}' is invalid");
                        previews[i] = null;
                    } else if (!File.Exists(Path.Combine(DataFiles, previews[i]))) {
                        Warn($"Preview file path '{previews[i]}' does not exist");
                        previews[i] = null;
                    }
                    else
                    {
                        previews[i] = Path.Combine(DataFiles, previews[i]);
                    }
                }
            }

            var selectedIndex = _scriptFunctions.Select(items, title, isMultiSelect, previews, descriptions);
            if (selectedIndex == null || selectedIndex.Count == 0)
            {
                srd.CancelInstall = true;
                return new string[0];
            }

            var result = new string[selectedIndex.Count];
            for (int i = 0; i < selectedIndex.Count; i++)
            {
                result[i] = $"Case {items[selectedIndex[i]]}";
            }

            return result;
        }

        private static string[] FunctionSelectVar(IReadOnlyList<string> line, bool isVariable)
        {
            string funcName = isVariable ? "SelectVar" : "SelectString";
            if (line.Count < 2)
            {
                Warn($"Missing arguments for '{funcName}'");
                return new string[0];
            }

            if(line.Count > 2) Warn($"Unexpected arguments for '{funcName}'");
            if (!isVariable)
                return new[] {$"Case {line[1]}"};

            if (variables.ContainsKey(line[1]))
                return new[] {$"Case {variables[line[1]]}"};

            Warn($"Invalid argument for '{funcName}'\nVariable '{line[1]}' does not exist");
            return new string[0];

        }

        private static void FunctionMessage(IReadOnlyList<string> line)
        {
            switch(line.Count)
            {
            case 1:
                Warn("Missing arguments to function 'Message'");
                break;
            case 2:
                _scriptFunctions.Message(line[1]);
                break;
            case 3:
                _scriptFunctions.Message(line[1], line[2]);
                break;
            default:
                _scriptFunctions.Message(line[1], line[2]);
                Warn("Unexpected arguments after 'Message'");
                break;
            }
        }

        private static void FunctionSetVar(IReadOnlyList<string> line)
        {
            if (line.Count < 3)
            {
                Warn("Missing arguments for 'SetVar'");
                return;
            }

            if(line.Count > 3) Warn("Unexpected extra arguments for 'SetVar'");
            variables[line[1]] = line[2];
        }

        private static void FunctionCombinePaths(IReadOnlyList<string> line)
        {
            if (line.Count < 4)
            {
                Warn("Missing arguments for 'CombinePaths'");
                return;
            }

            if(line.Count > 4) Warn("Unexpected arguments for 'CombinePaths'");
            try
            {
                variables[line[1]] = Path.Combine(line[2], line[3]);
            }
            catch
            {
                Warn("Invalid arguments for 'CombinePaths'");
            }
        }

        private static void FunctionSubRemoveString(IList<string> line, bool remove)
        {
            string funcName = remove ? "RemoveString" : "Substring";
            if (line.Count < 4)
            {
                Warn($"Missing arguments for '{funcName}'");
                return;
            }

            if (line.Count > 5) Warn($"Unexpected extra arguments for '{funcName}'");
            if (line.Count == 4)
            {
                if (!int.TryParse(line[3], out int start))
                {
                    Warn($"Invalid arguments for '{funcName}'");
                    return;
                }

                variables[line[1]] = remove ? line[2].Remove(start) : line[2].Substring(start);
            }
            else
            {
                if (!int.TryParse(line[3], out int start) || !int.TryParse(line[4], out int end))
                {
                    Warn($"Invalid arguments for '{funcName}'");
                    return;
                }
                variables[line[1]] = remove ? line[2].Remove(start,end) : line[2].Substring(start, end);
            }
        }

        private static void FunctionStringLength(IList<string> line)
        {
            if (line.Count < 3)
            {
                Warn("Missing arguments for 'StringLength'");
                return;
            }

            if(line.Count > 3) Warn("Unexpected extra arguments for 'StringLength'");
            variables[line[1]] = line[2].Length.ToString();
        }

        private static int Set(List<string> func)
        {
            if (func.Count == 0) throw new OMODFrameworkException($"Empty iSet in script at {cLine}");
            if (func.Count == 1) return int.Parse(func[0]);

            var index = func.IndexOf("(");
            while (index != -1)
            {
                int count = 1;
                var newFunc = new List<string>();
                for (int i = index + 1; i < func.Count; i++)
                {
                    if (func[i] == "(") count++;
                    else if (func[i] == ")") count--;

                    if (count != 0)
                        continue;

                    func.RemoveRange(index, (i-index) +1);
                    func.Insert(index, Set(newFunc).ToString());
                    break;
                }

                if(count != 0) throw new OMODFrameworkException($"Mismatched brackets in script at {cLine}");
                index = func.IndexOf("(");
            }

            //not
            index = func.IndexOf("not");
            while (index != -1)
            {
                int i = int.Parse(func[index + 1]);
                i = ~i;
                func[index + 1] = i.ToString();
                func.RemoveAt(index);
                index = func.IndexOf("not");
            }

            //and
            index = func.IndexOf("not");
            while (index != -1)
            {
                int i = int.Parse(func[index - 1]) & int.Parse(func[index + 1]);
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("not");
            }

            //or
            index = func.IndexOf("or");
            while (index != -1)
            {
                int i = int.Parse(func[index - 1]) | int.Parse(func[index + 1]);
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("or");
            }

            //xor
            index = func.IndexOf("xor");
            while (index != -1)
            {
                int i = int.Parse(func[index - 1]) ^ int.Parse(func[index + 1]);
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("xor");
            }

            //mod
            index = func.IndexOf("mod");
            while (index != -1)
            {
                int i = int.Parse(func[index - 1]) % int.Parse(func[index + 1]);
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("mod");
            }

            //mod
            index = func.IndexOf("%");
            while (index != -1)
            {
                int i = int.Parse(func[index - 1]) % int.Parse(func[index + 1]);
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("%");
            }

            //power
            index = func.IndexOf("^");
            while (index != -1)
            {
                int i = (int)Math.Pow(int.Parse(func[index - 1]), int.Parse(func[index + 1]));
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("^");
            }

            //division
            index = func.IndexOf("/");
            while (index != -1)
            {
                int i = int.Parse(func[index - 1]) / int.Parse(func[index + 1]);
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("/");
            }

            //multiplication
            index = func.IndexOf("*");
            while (index != -1)
            {
                int i = int.Parse(func[index - 1]) * int.Parse(func[index + 1]);
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("*");
            }

            //add
            index = func.IndexOf("+");
            while (index != -1)
            {
                int i = int.Parse(func[index - 1]) + int.Parse(func[index + 1]);
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("+");
            }

            //sub
            index = func.IndexOf("-");
            while (index != -1)
            {
                int i = int.Parse(func[index - 1]) - int.Parse(func[index + 1]);
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("-");
            }

            if(func.Count != 1) throw new OMODFrameworkException($"Leftovers in iSet function for script at {cLine}");
            return int.Parse(func[0]);
        }

        private static double FSet(List<string> func)
        {
            if (func.Count == 0) throw new OMODFrameworkException($"Empty fSet in script at {cLine}");
            if (func.Count == 1) return int.Parse(func[0]);
            //check for brackets

            var index = func.IndexOf("(");
            while (index != -1)
            {
                int count = 1;
                var newFunc = new List<string>();
                for (int i = index; i < func.Count; i++)
                {
                    if (func[i] == "(") count++;
                    else if (func[i] == ")") count--;
                    if (count == 0)
                    {
                        func.RemoveRange(index, i - index);
                        func.Insert(index, FSet(newFunc).ToString(CultureInfo.CurrentCulture));
                        break;
                    }

                    newFunc.Add(func[i]);
                }

                if (count != 0) throw new OMODFrameworkException($"Mismatched brackets in script at {cLine}");
                index = func.IndexOf("(");
            }

            //sin
            index = func.IndexOf("sin");
            while (index != -1)
            {
                func[index + 1] = Math.Sin(double.Parse(func[index + 1])).ToString(CultureInfo.CurrentCulture);
                func.RemoveAt(index);
                index = func.IndexOf("sin");
            }

            //cos
            index = func.IndexOf("cos");
            while (index != -1)
            {
                func[index + 1] = Math.Cos(double.Parse(func[index + 1])).ToString(CultureInfo.CurrentCulture);
                func.RemoveAt(index);
                index = func.IndexOf("cos");
            }

            //tan
            index = func.IndexOf("tan");
            while (index != -1)
            {
                func[index + 1] = Math.Tan(double.Parse(func[index + 1])).ToString(CultureInfo.CurrentCulture);
                func.RemoveAt(index);
                index = func.IndexOf("tan");
            }

            //sinh
            index = func.IndexOf("sinh");
            while (index != -1)
            {
                func[index + 1] = Math.Sinh(double.Parse(func[index + 1])).ToString(CultureInfo.CurrentCulture);
                func.RemoveAt(index);
                index = func.IndexOf("sinh");
            }

            //cosh
            index = func.IndexOf("cosh");
            while (index != -1)
            {
                func[index + 1] = Math.Cosh(double.Parse(func[index + 1])).ToString(CultureInfo.CurrentCulture);
                func.RemoveAt(index);
                index = func.IndexOf("cosh");
            }

            //tanh
            index = func.IndexOf("tanh");
            while (index != -1)
            {
                func[index + 1] = Math.Tanh(double.Parse(func[index + 1])).ToString(CultureInfo.CurrentCulture);
                func.RemoveAt(index);
                index = func.IndexOf("tanh");
            }

            //exp
            index = func.IndexOf("exp");
            while (index != -1)
            {
                func[index + 1] = Math.Exp(double.Parse(func[index + 1])).ToString(CultureInfo.CurrentCulture);
                func.RemoveAt(index);
                index = func.IndexOf("exp");
            }

            //log
            index = func.IndexOf("log");
            while (index != -1)
            {
                func[index + 1] = Math.Log10(double.Parse(func[index + 1])).ToString(CultureInfo.CurrentCulture);
                func.RemoveAt(index);
                index = func.IndexOf("log");
            }

            //ln
            index = func.IndexOf("ln");
            while (index != -1)
            {
                func[index + 1] = Math.Log(double.Parse(func[index + 1])).ToString(CultureInfo.CurrentCulture);
                func.RemoveAt(index);
                index = func.IndexOf("ln");
            }

            //mod
            index = func.IndexOf("mod");
            while (index != -1)
            {
                double i = double.Parse(func[index - 1]) % double.Parse(func[index + 1]);
                func[index + 1] = i.ToString(CultureInfo.CurrentCulture);
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("mod");
            }

            //mod2
            index = func.IndexOf("%");
            while (index != -1)
            {
                double i = double.Parse(func[index - 1]) % double.Parse(func[index + 1]);
                func[index + 1] = i.ToString(CultureInfo.CurrentCulture);
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("%");
            }

            //power
            index = func.IndexOf("^");
            while (index != -1)
            {
                double i = Math.Pow(double.Parse(func[index - 1]), double.Parse(func[index + 1]));
                func[index + 1] = i.ToString(CultureInfo.CurrentCulture);
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("^");
            }

            //division
            index = func.IndexOf("/");
            while (index != -1)
            {
                double i = double.Parse(func[index - 1]) / double.Parse(func[index + 1]);
                func[index + 1] = i.ToString(CultureInfo.CurrentCulture);
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("/");
            }

            //multiplication
            index = func.IndexOf("*");
            while (index != -1)
            {
                double i = double.Parse(func[index - 1]) * double.Parse(func[index + 1]);
                func[index + 1] = i.ToString(CultureInfo.CurrentCulture);
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("*");
            }

            //add
            index = func.IndexOf("+");
            while (index != -1)
            {
                double i = double.Parse(func[index - 1]) + double.Parse(func[index + 1]);
                func[index + 1] = i.ToString(CultureInfo.CurrentCulture);
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("+");
            }

            //sub
            index = func.IndexOf("-");
            while (index != -1)
            {
                double i = double.Parse(func[index - 1]) - double.Parse(func[index + 1]);
                func[index + 1] = i.ToString(CultureInfo.CurrentCulture);
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("-");
            }

            if (func.Count != 1) throw new OMODFrameworkException($"Leftovers in iSet function for script at {cLine}");
            return double.Parse(func[0]);
        }

        private static void FunctionSet(IReadOnlyList<string> line, bool integer)
        {
            if (line.Count < 3)
            {
                Warn("Missing arguments for "+(integer ? "iSet":"fSet"));
                return;
            }

            var func = new List<string>();
            for(int i = 2; i < line.Count; i++) func.Add(line[i]);
            try
            {
                string result;
                if (integer)
                {
                    int i = Set(func);
                    result = i.ToString();
                }
                else
                {
                    float f = (float)FSet(func);
                    result = f.ToString(CultureInfo.CurrentCulture);
                }

                variables[line[1]] = result;
            } catch
            {
                Warn("Invalid arguments for "+(integer ? "iSet":"fSet"));
            }
        }

        private static void FunctionExecLines(IList<string> line, ref Queue<string> queue)
        {
            if (line.Count < 2)
            {
                Warn("Missing arguments for 'ExecLines'");
                return;
            }

            if (line.Count > 2) Warn("Unexpected extra arguments for 'ExecLines'");
            string[] lines = line[1].Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);
            lines.Do(queue.Enqueue);
        }

        private static void FunctionLoadEarly(IList<string> line)
        {
            if (line.Count < 2)
            {
                Warn("Missing arguments for 'LoadEarly'");
                return;
            }

            if (line.Count > 2)
            {
                Warn("Unexpected arguments for 'LoadEarly'");
            }

            line[1] = line[1].ToLower();
            if (!srd.EarlyPlugins.Contains(line[1]))
                srd.EarlyPlugins.Add(line[1]);
        }

        private static void FunctionLoadOrder(IReadOnlyList<string> line, bool loadAfter)
        {
            string funcName = loadAfter ? "LoadAfter" : "LoadEarly";
            if (line.Count < 3)
            {
                Warn($"Missing arguments for '{funcName}'");
                return;
            }

            if (line.Count > 3)
            {
                Warn($"Unexpected arguments for '{funcName}'");
            }

            srd.LoadOrderSet.Add(new PluginLoadInfo(line[1], line[2], loadAfter));
        }

        private static void FunctionConflicts(IReadOnlyList<string> line, bool conflicts, bool regex)
        {
            var funcName = conflicts ? "ConflictsWith" : "DependsOn";
            if (regex) funcName += "Regex";

            var cd = new ConflictData {Level = ConflictLevel.MajorConflict};
            switch (line.Count)
            {
            case 1:
                Warn($"Missing arguments for '${funcName}'");
                return;
            case 2:
                cd.File = line[1];
                break;
            case 3:
                cd.Comment = line[2];
                goto case 2;
            case 4:
                switch (line[3])
                {
                case "Unusable":
                    cd.Level = ConflictLevel.Unusable;
                    break;
                case "Major":
                    cd.Level = ConflictLevel.MajorConflict;
                    break;
                case "Minor":
                    cd.Level = ConflictLevel.MinorConflict;
                    break;
                default:
                    Warn($"Unknown conflict level after '{funcName}'");
                    break;
                }

                goto case 3;
            case 5:
                Warn($"Unexpected arguments for '{funcName}'");
                break;
            case 6:
                cd.File = line[1];
                try
                {
                    cd.MinMajorVersion = Convert.ToInt32(line[2]);
                    cd.MinMinorVersion = Convert.ToInt32(line[3]);
                    cd.MaxMajorVersion = Convert.ToInt32(line[4]);
                    cd.MaxMinorVersion = Convert.ToInt32(line[5]);
                }
                catch
                {
                    Warn($"Arguments for '{funcName}' could not been parsed");
                }

                break;
            case 7:
                cd.Comment = line[6];
                goto case 6;
            case 8:
                switch (line[7])
                {
                case "Unusable":
                    cd.Level = ConflictLevel.Unusable;
                    break;
                case "Major":
                    cd.Level = ConflictLevel.MajorConflict;
                    break;
                case "Minor":
                    cd.Level = ConflictLevel.MinorConflict;
                    break;
                default:
                    Warn($"Unknown conflict level after '{funcName}'");
                    break;
                }

                goto case 7;
            default:
                Warn($"Unexpected arguments for '{funcName}'");
                goto case 8;
            }

            cd.Partial = regex;
            if (conflicts)
                srd.ConflictsWith.Add(cd);
            else
                srd.DependsOn.Add(cd);
        }

        private static void FunctionUncheckESP(IList<string> line)
        {
            if (line.Count == 1)
            {
                Warn("Missing arguments for 'UncheckESP'");
                return;
            }

            if(line.Count > 2) Warn("Unexpected arguments for 'UncheckESP'");
            if (!File.Exists(Path.Combine(Plugins, line[1])))
            {
                Warn($"Invalid argument for 'UncheckESP': {line[1]} does not exist");
                return;
            }

            line[1] = line[1].ToLower();
            if (!srd.UncheckedPlugins.Contains(line[1]))
                srd.UncheckedPlugins.Add(line[1]);
        }

        private static void FunctionSetDeactivationWarning(IList<string> line)
        {
            if (line.Count < 3)
            {
                Warn("Missing arguments for 'SetDeactivationWarning'");
                return;
            }

            if(line.Count > 3) Warn("Unexpected arguments for 'SetDeactivationWarning'");
            if (!File.Exists(Path.Combine(Plugins, line[1])))
            {
                Warn($"Invalid argument for 'SetDeactivationWarning'\nFile '{line[1]}' does not exist");
                return;
            }

            line[1] = line[1].ToLower();

            srd.ESPDeactivation.RemoveWhere(a => a.Plugin == line[1]);
            switch (line[2])
            {
            case "Allow":
                srd.ESPDeactivation.Add(new ScriptESPWarnAgainst(line[1], DeactivationStatus.Allow));
                break;
            case "WarnAgainst":
                srd.ESPDeactivation.Add(new ScriptESPWarnAgainst(line[1], DeactivationStatus.WarnAgainst));
                break;
            case "Disallow":
                srd.ESPDeactivation.Add(new ScriptESPWarnAgainst(line[1], DeactivationStatus.Disallow));
                break;
            default:
                Warn("Invalid argument for 'SetDeactivationWarning'");
                return;
            }
        }

        private static void FunctionEditXMLLine(IList<string> line)
        {
            if (line.Count < 4)
            {
                Warn("Missing arguments for 'EditXMLLine'");
                return;
            }

            var file = Path.Combine(DataFiles, line[1]);

            if (line.Count > 4) Warn("Unexpected extra arguments for 'EditXMLLine'");
            line[1] = line[1].ToLower();
            if (!Utils.IsSafeFileName(line[1]) || !File.Exists(file))
            {
                Warn("Invalid filename supplied for 'EditXMLLine'");
                return;
            }

            var ext = Path.GetExtension(line[1]);
            if (ext != ".xml" && ext != ".txt" && ext != ".ini" && ext != ".bat")
            {
                Warn("Invalid filename supplied for 'EditXMLLine'");
                return;
            }

            if (!int.TryParse(line[2], out var index) || index < 1)
            {
                Warn("Invalid line number supplied for 'EditXMLLine'");
                return;
            }

            index -= 1;
            var lines = File.ReadAllLines(file);
            if (lines.Length <= index)
            {
                Warn("Invalid line number supplied for 'EditXMLLine'");
                return;
            }

            lines[index] = line[3];
            File.WriteAllLines(file, lines);
        }

        private static void FunctionEditXMLReplace(IList<string> line)
        {
            if (line.Count < 4)
            {
                Warn("Missing arguments for 'EditXMLReplace'");
                return;
            }

            var file = Path.Combine(DataFiles, line[1]);
            if (line.Count > 4) Warn("Unexpected extra arguments for 'EditXMLReplace'");
            line[1] = line[1].ToLower();
            if (!Utils.IsSafeFileName(line[1]) || !File.Exists(file))
            {
                Warn("Invalid filename supplied for 'EditXMLReplace'");
                return;
            }

            var ext = Path.GetExtension(file);
            if (ext != ".xml" && ext != ".txt" && ext != ".ini" && ext != ".bat")
            {
                Warn("Invalid filename supplied for 'EditXMLLine'");
                return;
            }

            var text = File.ReadAllText(file);
            text = text.Replace(line[2], line[3]);
            File.WriteAllText(file, text);
        }

        private static void FunctionModifyInstall(IReadOnlyCollection<string> line, bool plugins, bool install)
        {
            var funcName = install ? "Install" : "DontInstall";
            funcName += plugins ? "Plugin" : "DataFile";

            if (line.Count == 1)
            {
                Warn($"Missing arguments for '{funcName}'");
                return;
            }

            var l = line.ElementAt(1).ToLower();
            if(line.Count > 2) Warn($"Unexpected arguments for '{funcName}'");
            if (plugins)
            {
                var path = Path.Combine(Plugins, l);
                if (!File.Exists(path))
                {
                    Warn($"Invalid argument for '{funcName}'\nFile '{path}' does not exist");
                    return;
                }

                if (l.IndexOfAny(new [] {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar}) != -1)
                {
                    Warn($"Invalid argument for '{funcName}'\nThis function cannot be used on plugins stored in subdirectories");
                }

                if (install)
                {
                    srd.IgnorePlugins.RemoveWhere(s => s == l);
                    if (!srd.InstallPlugins.Contains(l))
                        srd.InstallPlugins.Add(l);
                }
                else
                {
                    srd.InstallPlugins.RemoveWhere(s => s == l);
                    if (!srd.IgnorePlugins.Contains(l))
                        srd.IgnorePlugins.Add(l);
                }
            }
            else
            {
                var path = Path.Combine(DataFiles, l);
                if(!File.Exists(path)) {
                    Warn($"Invalid argument for '{funcName}'\nFile '{path}' does not exist");
                    return;
                }

                if (install)
                {
                    srd.IgnoreData.RemoveWhere(s => s == l);
                    if (!srd.InstallData.Contains(l))
                        srd.InstallData.Add(l);
                } else
                {
                    srd.InstallData.RemoveWhere(s => s == l);
                    if (!srd.IgnoreData.Contains(l))
                        srd.IgnoreData.Add(l);
                }
            }
        }

        private static void FunctionModifyInstallFolder(IList<string> line, bool install)
        {
            var funcName = (install ? "Install" : "DontInstall") + "DataFolder";

            if (line.Count == 1)
            {
                Warn($"Missing arguments for '{funcName}'");
                return;
            }
            if(line.Count > 3) Warn($"Unexpected arguments for '{funcName}'");

            line[1] = Utils.MakeValidFolderPath(line[1]);
            var path = Path.Combine(DataFiles, line[1]);

            if (!Directory.Exists(path))
            {
                Warn($"Invalid argument for '{funcName}'\nFolder '{path}' does not exist");
                return;
            }

            if (line.Count >= 2)
            {
                switch (line[2])
                {
                case "True":
                    Directory.GetDirectories(path).Do(d =>
                        FunctionModifyInstallFolder(
                            new List<string> {"", d.Substring(DataFiles.Length), "True"}, install));
                    break;
                case "False":
                    break;
                default:
                    Warn($"Invalid argument for '{funcName}'\nExpected True or False");
                    break;
                }
            }
            Directory.GetFiles(path).Do(f =>
            {
                var name = Path.GetFileName(f);
                if (install)
                {
                    srd.IgnoreData.RemoveWhere(s => s == name);
                    if (!srd.InstallData.Contains(name))
                        srd.InstallData.Add(name);
                }
                else
                {
                    srd.InstallData.RemoveWhere(s => s == name);
                    if (!srd.IgnoreData.Contains(name))
                        srd.IgnoreData.Add(name);
                }
            });
        }

        private static void FunctionCopyDataFile(IReadOnlyCollection<string> line, bool plugin)
        {
            var funcName = "Copy";
            funcName += plugin ? "Plugin" : "DataFile";

            if (line.Count < 3)
            {
                Warn($"Missing arguments for '{funcName}'");
                return;
            }

            if(line.Count > 3) Warn($"Unexpected arguments for '{funcName}'");
            var from = line.ElementAt(1);
            var to = line.ElementAt(2);

            if (!Utils.IsSafeFileName(from) || !Utils.IsSafeFileName(to))
            {
                Warn($"Invalid argument for '{funcName}'");
                return;
            }

            if (from == to)
            {
                Warn($"Invalid argument for '{funcName}'\nYou can not copy a file over itself");
                return;
            }

            if(plugin)
            {
                var path = Path.Combine(Plugins, from);
                if (!File.Exists(path))
                {
                    Warn($"Invalid argument for '{funcName}'\nFile '{from}' does not exist");
                    return;
                }

                if (to.IndexOfAny(new [] {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar}) != -1)
                {
                    Warn("Plugins cannot be copied to subdirectories of the data folder");
                    return;
                }

                if (!(to.EndsWith("esp") || to.EndsWith(".esm")))
                {
                    Warn("Copied plugins must have a .esp or .esm extension");
                    return;
                }
            }
            else
            {
                var path = Path.Combine(DataFiles, from);
                if (!File.Exists(path))
                {
                    Warn($"Invalid argument for '{funcName}'\nFile '{from}' does not exist");
                    return;
                }

                if (to.EndsWith("esp") || to.EndsWith(".esm"))
                {
                    Warn("Copied data files cannot have a .esp or .esm extension");
                    return;
                }
            }

            if (plugin)
            {
                srd.CopyPlugins.RemoveWhere(s => s.CopyTo == to.ToLower());
                srd.CopyPlugins.Add(new ScriptCopyDataFile(from.ToLower(), to.ToLower()));
            }
            else
            {
                srd.CopyDataFiles.RemoveWhere(s => s.CopyTo == to.ToLower());
                srd.CopyDataFiles.Add(new ScriptCopyDataFile(from.ToLower(), to.ToLower()));
            }
        }

        private static void FunctionCopyDataFolder(IReadOnlyCollection<string> line)
        {
            if (line.Count < 3)
            {
                Warn("Missing arguments for 'CopyDataFolder'");
                return;
            }

            if(line.Count > 4) Warn("Unexpected arguments for 'CopyDataFolder'");
            var validFrom = Utils.MakeValidFolderPath(line.ElementAt(1).ToLower());
            var validTo = Utils.MakeValidFolderPath(line.ElementAt(2).ToLower());

            if (!Utils.IsSafeFolderName(validFrom) || !Utils.IsSafeFolderName(validTo))
            {
                Warn("Invalid argument for 'CopyDataFolder'");
                return;
            }

            var from = Path.Combine(DataFiles, validFrom);
            var to = Path.Combine(DataFiles, validTo);

            if(!Directory.Exists(from))
            {
                Warn($"Invalid argument for 'CopyDataFolder'\nFolder '{from}' does not exist!");
                return;
            }

            if (from == to)
            {
                Warn("Invalid argument for 'CopyDataFolder'\nYou cannot copy a folder over itself");
                return;
            }

            if (line.Count >= 4)
            {
                switch(line.ElementAt(3)) {
                case "True":
                    Directory.GetDirectories(from).Do(d =>
                    {
                        FunctionCopyDataFolder(new [] {"", d.Substring(DataFiles.Length), line.ElementAt(2) + d.Substring(DataFiles.Length + line.ElementAt(1).Length), "True"});
                    });
                    break;
                case "False":
                    break;
                default:
                    Warn("Invalid argument for 'CopyDataFolder'\nExpected True or False");
                    return;
                }
            }

            Directory.GetFiles(from).Do(f =>
            {
                var fFrom = Path.Combine(line.ElementAt(1), Path.GetFileName(f));
                var fTo = Path.Combine(line.ElementAt(2), Path.GetFileName(f)).ToLower();

                srd.CopyDataFiles.RemoveWhere(s => s.CopyTo == fTo);

                srd.CopyDataFiles.Add(new ScriptCopyDataFile(fFrom, fTo));
            });
        }

        private static void FunctionGetDirectoryName(IReadOnlyCollection<string> line)
        {
            if (line.Count < 3)
            {
                Warn("Missing arguments for 'GetDirectoryName'");
                return;
            }

            if(line.Count > 3) Warn("Unexpected arguments for 'GetDirectoryName'");

            try
            {
                variables[line.ElementAt(1)] = Path.GetDirectoryName(line.ElementAt(2));
            }
            catch
            {
                Warn("Invalid argument for 'GetDirectoryName'");
            }
        }

        private static void FunctionGetFileName(IReadOnlyCollection<string> line) {
            if (line.Count < 3)
            {
                Warn("Missing arguments for 'GetFileName'");
                return;
            }

            if (line.Count > 3) Warn("Unexpected arguments for 'GetFileName'");
            try
            {
                variables[line.ElementAt(1)] = Path.GetFileName(line.ElementAt(2));
            }
            catch
            {
                Warn("Invalid argument for 'GetFileName'");
            }
        }

        private static void FunctionGetFileNameWithoutExtension(IReadOnlyCollection<string> line) {
            if (line.Count < 3)
            {
                Warn("Missing arguments for 'GetFileNameWithoutExtension'");
                return;
            }

            if (line.Count > 3) Warn("Unexpected arguments for 'GetFileNameWithoutExtension'");
            try
            {
                variables[line.ElementAt(1)] = Path.GetFileName(line.ElementAt(2));
            }
            catch
            {
                Warn("Invalid argument for 'GetFileNameWithoutExtension'");
            }
        }

        private static void FunctionPatch(IReadOnlyCollection<string> line, bool plugin)
        {
            var funcName = "Patch";
            funcName += plugin ? "Plugin" : "DataFile";

            if (line.Count < 3)
            {
                Warn($"Missing arguments for '{funcName}'");
                return;
            }

            if(line.Count > 4) Warn($"Unexpected arguments for '{funcName}'");

            var from = line.ElementAt(1);
            var to = line.ElementAt(2);

            if (!Utils.IsSafeFileName(from) || !Utils.IsSafeFileName(to))
            {
                Warn($"Invalid argument for '{funcName}'");
                return;
            }

            var pathFrom = plugin ? Path.Combine(Plugins, from) : Path.Combine(DataFiles, from);
            if(plugin) {
                if (!File.Exists(pathFrom))
                {
                    Warn($"Invalid argument for 'PatchPlugin'\nFile '{from}' does not exist");
                    return;
                }

                if (to.IndexOfAny(new[] {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar}) != -1)
                {
                    Warn("Plugins cannot be copied to subdirectories of the data folder");
                    return;
                }

                if (!(to.EndsWith(".esp") || to.EndsWith(".esm")))
                {
                    Warn("Plugins must have a .esp or .esm extension");
                    return;
                }
            }
            else
            {
                if (!File.Exists(pathFrom))
                {
                    Warn($"Invalid argument to PatchDataFile\nFile '{from}' does not exist");
                    return;
                }

                if (to.EndsWith(".esp") || to.EndsWith(".esm"))
                {
                    Warn("Data files cannot have a .esp or .esm extension");
                    return;
                }
            }

            switch (Framework.CurrentPatchMethod)
            {
            case Framework.PatchMethod.CreatePatchGameFolder:
                if(string.IsNullOrWhiteSpace(Framework.OblivionGameFolder))
                    throw new OMODFrameworkException($"{Framework.OblivionGameFolder} can not be null or whitespace!");

                var patchFolder = Path.Combine(Framework.OblivionDataFolder, "Patch");

                if (!Directory.Exists(patchFolder))
                    Directory.CreateDirectory(patchFolder);

                var patchPath = Path.Combine(patchFolder, to);
                if(File.Exists(patchPath))
                    throw new OMODFrameworkException($"The file {patchPath} already exists");

                var toDataPath = Path.Combine(Framework.OblivionDataFolder, to);
                DateTime toTimeStamp = default;
                if (File.Exists(toDataPath))
                {
                    toTimeStamp = File.GetLastWriteTime(toDataPath);
                }

                try
                {
                    File.Copy(pathFrom, patchPath);
                    if(toTimeStamp != default)
                        File.SetLastWriteTime(patchPath, toTimeStamp);
                }
                catch (Exception e)
                {
                    throw new OMODFrameworkException($"The file {pathFrom} could not be copied to {patchPath}\n{e}");
                }

                break;
            case Framework.PatchMethod.OverwriteGameFolder:
                if(string.IsNullOrWhiteSpace(Framework.OblivionGameFolder))
                    throw new OMODFrameworkException($"{Framework.OblivionGameFolder} can not be null or whitespace!");

                var dataPath = Path.Combine(Framework.OblivionDataFolder, to);
                DateTime timeStamp = default;
                if (File.Exists(dataPath))
                {
                    timeStamp = File.GetLastWriteTime(dataPath);
                    try
                    {
                        File.Delete(dataPath);
                    }
                    catch (Exception e)
                    {
                        throw new OMODFrameworkException($"The file {dataPath} could not be deleted!\n{e}");
                    }
                }
                else if (line.Count < 4 || line.ElementAt(3) != "True") return;

                try
                {
                    File.Move(pathFrom, dataPath);
                    File.SetLastWriteTime(dataPath, timeStamp);
                }
                catch (Exception e)
                {
                    throw new OMODFrameworkException($"The file {pathFrom} could not be moved to {dataPath}\n{e}");
                }

                break;
            case Framework.PatchMethod.CreatePatchInMod:
                srd.PatchFiles.RemoveWhere(s => s.CopyTo == to.ToLower());
                srd.PatchFiles.Add(new ScriptCopyDataFile(from.ToLower(), to.ToLower()));
                break;
            case Framework.PatchMethod.PatchWithInterface:
                _scriptFunctions.Patch(pathFrom, to);
                break;
            default:
                throw new OMODFrameworkException("Unknown PatchMethod for Framework.CurrentPatchMethod!");
            }
        }
    }
}
