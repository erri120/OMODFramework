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
                        //TODO: if(FlowControl.Count!=0&&FlowControl.Peek().type==0) FlowControl.Peek().active=false;
                        //TODO: else Warn("Unexpected Else");
                        break;
                    case "EndIf":
                        //TODO: if(FlowControl.Count!=0&&FlowControl.Peek().type==0) FlowControl.Pop();
                        //TODO: else Warn("Unexpected EndIf");
                        break;
                    case "Select":
                        //TODO: FlowControl.Push(new FlowControlStruct(i, FunctionSelect(line, false, false, false)));
                        break;
                    case "SelectMany":
                        //TODO: FlowControl.Push(new FlowControlStruct(i, FunctionSelect(line, true, false, false)));
                        break;
                    case "SelectWithPreview":
                        //TODO: FlowControl.Push(new FlowControlStruct(i, FunctionSelect(line, false, true, false)));
                        break;
                    case "SelectManyWithPreview":
                        //TODO: FlowControl.Push(new FlowControlStruct(i, FunctionSelect(line, true, true, false)));
                        break;
                    case "SelectWithDescriptions":
                        //TODO: FlowControl.Push(new FlowControlStruct(i, FunctionSelect(line, false, false, true)));
                        break;
                    case "SelectManyWithDescriptions":
                        //TODO: FlowControl.Push(new FlowControlStruct(i, FunctionSelect(line, true, false, true)));
                        break;
                    case "SelectWithDescriptionsAndPreviews":
                        //TODO: FlowControl.Push(new FlowControlStruct(i, FunctionSelect(line, false, true, true)));
                        break;
                    case "SelectManyWithDescriptionsAndPreviews":
                        //TODO: FlowControl.Push(new FlowControlStruct(i, FunctionSelect(line, true, true, true)));
                        break;
                    case "SelectVar":
                        //TODO: FlowControl.Push(new FlowControlStruct(i, FunctionSelectVar(line, true)));
                        break;
                    case "SelectString":
                        //TODO: FlowControl.Push(new FlowControlStruct(i, FunctionSelectVar(line, false)));
                        break;
                    case "Break": {
                        /*TODO: 
                            bool found=false;
                            FlowControlStruct[] fcs=FlowControl.ToArray();
                            for(int k=0;k<fcs.Length;k++) {
                                if(fcs[k].type==1) {
                                    for(int j=0;j<=k;j++) fcs[j].active=false;
                                    found=true;
                                    break;
                                }
                            }
                            if(!found) Warn("Unexpected Break");*/
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
                            /*TODO: FlowControlStruct fc=FunctionFor(line, i);
                            FlowControl.Push(fc);
                            if(fc.line!=-1&&fc.values.Length>0) {
                                variables[fc.var]=fc.values[0];
                                fc.active=true;
                            }*/
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
                        //TODO: FunctionLoadEarly(line);
                        break;
                    case "LoadBefore":
                        //TODO: FunctionLoadOrder(line, false);
                        break;
                    case "LoadAfter":
                        //TODO: FunctionLoadOrder(line, true);
                        break;
                    case "ConflictsWith":
                        //TODO: FunctionConflicts(line, true, false);
                        break;
                    case "DependsOn":
                        //TODO: FunctionConflicts(line, false, false);
                        break;
                    case "ConflictsWithRegex":
                        //TODO: FunctionConflicts(line, true, true);
                        break;
                    case "DependsOnRegex":
                        //TODO: FunctionConflicts(line, false, true);
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
                        //TODO: FunctionModifyInstall(line, true, true);
                        break;
                    case "DontInstallPlugin":
                        //TODO: FunctionModifyInstall(line, true, false);
                        break;
                    case "InstallDataFile":
                        //TODO: FunctionModifyInstall(line, false, true);
                        break;
                    case "DontInstallDataFile":
                        //TODO: FunctionModifyInstall(line, false, false);
                        break;
                    case "DontInstallDataFolder":
                        //TODO: FunctionModifyInstallFolder(line, false);
                        break;
                    case "InstallDataFolder":
                        //TODO: FunctionModifyInstallFolder(line, true);
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
                        //TODO: FunctionUncheckESP(line);
                        break;
                    case "SetDeactivationWarning":
                        //TODO: FunctionSetDeactivationWarning(line);
                        break;
                    case "CopyDataFile":
                        //TODO: FunctionCopyDataFile(line, false);
                        break;
                    case "CopyPlugin":
                        //TODO: FunctionCopyDataFile(line, true);
                        break;
                    case "CopyDataFolder":
                        //TODO: FunctionCopyDataFolder(line);
                        break;
                    case "PatchPlugin":
                        //TODO: FunctionPatch(line, true);
                        break;
                    case "PatchDataFile":
                        //TODO: FunctionPatch(line, false);
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
                        //TODO: FunctionSetVar(line);
                        break;
                    case "GetFolderName":
                    case "GetDirectoryName":
                        //TODO: FunctionGetDirectoryName(line);
                        break;
                    case "GetFileName":
                        //TODO: FunctionGetFileName(line);
                        break;
                    case "GetFileNameWithoutExtension":
                        //TODO: FunctionGetFileNameWithoutExtension(line);
                        break;
                    case "CombinePaths":
                        //TODO: FunctionCombinePaths(line);
                        break;
                    case "Substring":
                        //TODO: FunctionSubstring(line);
                        break;
                    case "RemoveString":
                        //TODO: FunctionRemoveString(line);
                        break;
                    case "StringLength":
                        //TODO: FunctionStringLength(line);
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
                        //TODO: FunctionExecLines(line, ExtraLines);
                        break;
                    case "iSet":
                        //TODO: FunctionSet(line, true);
                        break;
                    case "fSet":
                        //TODO: FunctionSet(line, false);
                        break;
                    case "EditXMLLine":
                        //TODO: FunctionEditXMLLine(line);
                        break;
                    case "EditXMLReplace":
                        //TODO: FunctionEditXMLReplace(line);
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
    }
}
