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
using System.IO;
using System.Linq;

namespace OMODFramework.Scripting
{
    internal static class OBMMScriptHandler
    {

        private class FlowControlStruct
        {
            public readonly int Line;
            public readonly byte Type;
            public readonly string[] Values;
            public readonly string Var;
            public bool Active;
            public bool HitCase;
            public int ForCount;

            //Inactive
            public FlowControlStruct(byte type)
            {
                Line = -1;
                this.Type = type;
                Values = null;
                Var = null;
                Active = false;
            }

            //If
            public FlowControlStruct(int line, bool active)
            {
                this.Line = line;
                Type = 0;
                Values = null;
                Var = null;
                this.Active = active;
            }

            //Select
            public FlowControlStruct(int line, string[] values)
            {
                this.Line = line;
                Type = 1;
                this.Values = values;
                Var = null;
                Active = false;
            }

            //For
            public FlowControlStruct(string[] values, string var, int line)
            {
                this.Line = line;
                Type = 2;
                this.Values = values;
                this.Var = var;
                Active = false;
            }
        }

        internal static ScriptReturnData Srd;
        internal static Dictionary<string, string> Variables;

        private static SharedFunctionsHandler Handler { get; set; }
        internal static readonly Queue<string> ExtraLines = new Queue<string>();

        internal static string DataFiles;
        internal static string Plugins;
        internal static string CLine = "0";

        internal static ScriptReturnData Execute(string inputScript, string dataPath, string pluginsPath, ref SharedFunctionsHandler handler)
        {
            Handler = handler;
            Srd = new ScriptReturnData();
            if (string.IsNullOrWhiteSpace(inputScript))
                return Srd;

            DataFiles = dataPath;
            Plugins = pluginsPath;
            Variables = new Dictionary<string, string>();

            var flowControl = new Stack<FlowControlStruct>();

            Variables["NewLine"] = Environment.NewLine;
            Variables["Tab"] = "\t";

            var script = inputScript.Replace("\r", "").Split('\n');
            string skipTo = null;
            bool allowRunOnLines = false;
            bool Break = false;

            for (var i = 0; i < script.Length || ExtraLines.Count > 0; i++)
            {
                string s;
                if (ExtraLines.Count > 0)
                {
                    i--;
                    s = ExtraLines.Dequeue().Replace('\t', ' ').Trim();
                }
                else
                {
                    s = script[i].Replace('\t', ' ').Trim();
                }

                CLine = i.ToString();
                if (allowRunOnLines)
                {
                    while (s.EndsWith("\\"))
                    {
                        s = s.Remove(s.Length - 1);
                        if (ExtraLines.Count > 0)
                        {
                            s += ExtraLines.Dequeue().Replace('\t', ' ').Trim();
                        }
                        else
                        {
                            if (++i == script.Length)
                                Warn("Run-on line passed end of script");
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

                IReadOnlyCollection<string> line = SplitLine(s);
                if (line.Count == 0) continue;

                if (flowControl.Count != 0 && !flowControl.Peek().Active)
                {
                    switch (line.ElementAt(0))
                    {
                    case "":
                        Warn("Empty function");
                        break;
                    case "If":
                    case "IfNot":
                        flowControl.Push(new FlowControlStruct(0));
                        break;
                    case "Else":
                        if (flowControl.Count != 0 && flowControl.Peek().Type == 0)
                            flowControl.Peek().Active = flowControl.Peek().Line != -1;
                        else Warn("Unexpected Else");
                        break;
                    case "EndIf":
                        if (flowControl.Count != 0 && flowControl.Peek().Type == 0) flowControl.Pop();
                        else Warn("Unexpected EndIf");
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
                        if (flowControl.Count != 0 && flowControl.Peek().Type == 1)
                        {
                            if (flowControl.Peek().Line != -1 && Array.IndexOf(flowControl.Peek().Values, s) != -1)
                            {
                                flowControl.Peek().Active = true;
                                flowControl.Peek().HitCase = true;
                            }
                        }
                        else Warn("Unexpected Break");

                        break;
                    case "Default":
                        if (flowControl.Count != 0 && flowControl.Peek().Type == 1)
                        {
                            if (flowControl.Peek().Line != -1 && !flowControl.Peek().HitCase)
                                flowControl.Peek().Active = true;
                        }
                        else Warn("Unexpected Default");

                        break;
                    case "EndSelect":
                        if (flowControl.Count != 0 && flowControl.Peek().Type == 1) flowControl.Pop();
                        else Warn("Unexpected EndSelect");
                        break;
                    case "For":
                        flowControl.Push(new FlowControlStruct(2));
                        break;
                    case "EndFor":
                        if (flowControl.Count != 0 && flowControl.Peek().Type == 2) flowControl.Pop();
                        else Warn("Unexpected EndFor");
                        break;
                    case "Break":
                    case "Continue":
                    case "Exit":
                        break;
                    }
                }
                else
                {
                    var lineMsg = "";
                    line.Do(cur => lineMsg+="'"+cur+"' ");

                    var registry = new SharedFunctionsRegistry(Handler);
                    var function = registry.GetFunctionByName(line.ElementAt(0));
                    if (function != null)
                    {
                        if (line.Count < function.MinArgs)
                        {
                            Warn($"Missing arguments for '{function.FuncName}'");
                            break;
                        }

                        if(function.MaxArgs != 0 && line.Count > function.MaxArgs)
                            Warn($"Unexpected arguments for '{function.FuncName}'");

                        Utils.Script($"\"{function.FuncName}\" called with line: {lineMsg}");
                        function.Run(ref line);
                    }
                    else
                    {
                        Utils.Script($"\"{line.ElementAt(0)}\" called with line: {lineMsg}");
                    }
                    switch (line.ElementAt(0))
                    {
                    case "Goto":
                        if (line.Count < 2)
                            Warn("Not enough arguments to function 'Goto'!");
                        else
                        {
                            if (line.Count > 2) Warn("Unexpected extra arguments to function 'Goto'");
                            skipTo = $"Label {line.ElementAt(1)}";
                            flowControl.Clear();
                        }
                        break;
                    case "Label":
                        break;
                    case "If":
                        flowControl.Push(new FlowControlStruct(i, FunctionIf(line)));
                        break;
                    case "IfNot":
                        flowControl.Push(new FlowControlStruct(i, !FunctionIf(line)));
                        break;
                    case "Else":
                        if (flowControl.Count != 0 && flowControl.Peek().Type == 0) flowControl.Peek().Active = false;
                        else Warn("Unexpected Else");
                        break;
                    case "EndIf":
                        if (flowControl.Count != 0 && flowControl.Peek().Type == 0) flowControl.Pop();
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
                            if (fcs[k].Type != 1)
                                continue;

                            for (int j = 0; j <= k; j++) fcs[j].Active = false;
                            found = true;
                            break;
                        }

                        if (!found) Warn("Unexpected Break");
                        break;
                        }
                    case "Case":
                        if (flowControl.Count == 0 || flowControl.Peek().Type != 1) Warn("Unexpected Case");
                        break;
                    case "Default":
                        if (flowControl.Count == 0 || flowControl.Peek().Type != 1)
                            Warn("Unexpected Default");
                        break;
                    case "EndSelect":
                        if(flowControl.Count!=0&&flowControl.Peek().Type==1) flowControl.Pop();
                        else Warn("Unexpected EndSelect");
                        break;
                    case "For": {
                        var fc = FunctionFor(line, i);
                        flowControl.Push(fc);
                        if (fc.Line != -1 && fc.Values.Length > 0)
                        {
                            Variables[fc.Var] = fc.Values[0];
                            fc.Active = true;
                        }

                        break;
                        }
                    case "Continue": {
                        var found = false;
                        var fcs = flowControl.ToArray();
                        for (int k = 0; k < fcs.Length; k++)
                        {
                            if (fcs[k].Type != 2)
                                continue;

                            fcs[k].ForCount++;
                            if (fcs[k].ForCount == fcs[k].Values.Length)
                            {
                                for (int j = 0; j <= k; j++) fcs[j].Active = false;
                            }
                            else
                            {
                                i = fcs[k].Line;
                                Variables[fcs[k].Var] = fcs[k].Values[fcs[k].ForCount];
                                for (int j = 0; j < k; j++) flowControl.Pop();
                            }

                            found = true;
                            break;
                        }

                        if (!found) Warn("Unexpected Continue");
                        break;
                        }
                    case "Exit": {
                        bool found = false;
                        var fcs = flowControl.ToArray();
                        for (int k = 0; k < fcs.Length; k++)
                        {
                            if (fcs[k].Type != 2)
                                continue;

                            for (int j = 0; j <= k; j++) flowControl.Peek().Active = false;
                            found = true;
                            break;
                        }

                        if (!found) Warn("Unexpected Exit");
                        break;
                        }
                    case "EndFor":
                        if (flowControl.Count != 0 && flowControl.Peek().Type == 2)
                        {
                            var fc = flowControl.Peek();
                            fc.ForCount++;
                            if (fc.ForCount == fc.Values.Length) flowControl.Pop();
                            else
                            {
                                i = fc.Line;
                                Variables[fc.Var] = fc.Values[fc.ForCount];
                            }
                        }
                        else Warn("Unexpected EndFor");
                        break;
                    //Functions
                    case "DontInstallAnyPlugins":
                        Srd.InstallAllPlugins = false;
                        break;
                    case "DontInstallAnyDataFiles":
                        Srd.InstallAllData = false;
                        break;
                    case "InstallAllPlugins":
                        Srd.InstallAllPlugins = true;
                        break;
                    case "InstallAllDataFiles":
                        Srd.InstallAllData = true;
                        break;
                    case "FatalError":
                        Utils.Error("Script called FatalError!");
                        Srd.CancelInstall = true;
                        break;
                    case "Return":
                        Break = true;
                        break;
                    case "AllowRunOnLines":
                        allowRunOnLines = true;
                        break;
                    default:
                        break;
                    }
                }

                if (Break || Srd.CancelInstall) break;
            }

            if (skipTo != null) Warn($"Expected: {skipTo}!");

            var temp = Srd;
            Srd = null;
            Variables = null;

            return temp;
        }

        private static void Warn(string msg)
        {
            Handler.Warn(msg);
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
                            if (Variables.ContainsKey(currentWord))
                                currentWord = currentVar + Variables[currentWord];
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

        private static bool FunctionIf(IReadOnlyCollection<string> line)
        {
            if (line.Count == 1)
            {
                Warn("Missing arguments for 'If'");
                return false;
            }

            switch (line.ElementAt(1))
            {
            case "DialogYesNo":
                int dialogResult;
                switch (line.Count)
                {
                case 2:
                    Warn("Missing arguments for 'If DialogYesNo'");
                    return false;
                case 3:
                    dialogResult = Handler.ScriptFunctions.DialogYesNo(line.ElementAt(2));
                    if (dialogResult == -1)
                    {
                        Srd.CancelInstall = true;
                        return false;
                    }
                    else
                        return dialogResult == 1;
                case 4:
                    dialogResult = Handler.ScriptFunctions.DialogYesNo(line.ElementAt(2), line.ElementAt(3));
                    if (dialogResult == -1)
                    {
                        Srd.CancelInstall = true;
                        return false;
                    }
                    else
                        return dialogResult == 1;
                default:
                    Warn("Unexpected extra arguments after 'If DialogYesNo'");
                    goto case 4;
                }
            case "DataFileExists":
                if (line.Count != 2)
                    return Handler.ScriptFunctions.DataFileExists(line.ElementAt(2));

                Warn("Missing arguments for 'If DataFileExists'");
                return false;
            case "VersionLessThan":
            case "VersionGreaterThan":
                var funcName = line.ElementAt(1) == "VersionGreaterThan" ? "VersionGreaterThan" : "VersionLessThan";
                if (line.Count == 2)
                {
                    Warn($"Missing arguments for 'If {funcName}'");
                    return false;
                }

                try
                {
                    var v = new Version($"{line.ElementAt(2)}.0");
                    var v2 = new Version($"{Framework.Settings.Version}.0");
                    return line.ElementAt(1) == "VersionGreaterThan" ? v2 > v : v2 < v;
                }
                catch
                {
                    Warn($"Invalid argument for 'If {funcName}'");
                    return false;
                }
            case "ScriptExtenderPresent":
                if (line.Count > 2) Warn("Unexpected extra arguments for 'If ScriptExtenderPresent'");
                return Handler.ScriptFunctions.HasScriptExtender();
            case "ScriptExtenderNewerThan":
                if (line.Count == 2)
                {
                    Warn("Missing arguments for 'If ScriptExtenderNewerThan'");
                    return false;
                }
                if(line.Count > 3) Warn("Unexpected extra arguments for 'If ScriptExtenderNewerThan'");
                if (!Handler.ScriptFunctions.HasScriptExtender()) return false;
                try
                {
                    var v = Handler.ScriptFunctions.ScriptExtenderVersion();
                    var v2 = new Version(line.ElementAt(2));
                    return v >= v2;
                }
                catch
                {
                    Warn("Invalid argument for 'If ScriptExtenderNewerThan'");
                    return false;
                }
            case "GraphicsExtenderPresent":
                if (line.Count > 2) Warn("Unexpected arguments for 'If GraphicsExtenderPresent'");
                return Handler.ScriptFunctions.HasGraphicsExtender();
            case "GraphicsExtenderNewerThan":
                if (line.Count == 2)
                {
                    Warn("Missing arguments for 'If GraphicsExtenderNewerThan'");
                    return false;
                }
                if(line.Count > 3) Warn("Unexpected extra arguments for 'If GraphicsExtenderNewerThan'");
                if (!Handler.ScriptFunctions.HasGraphicsExtender()) return false;
                try
                {
                    var v = Handler.ScriptFunctions.GraphicsExtenderVersion();
                    var v2 = new Version(line.ElementAt(2));
                    return v >= v2;
                }
                catch
                {
                    Warn("Invalid argument for 'If GraphicsExtenderNewerThan'");
                    return false;
                }
            case "OblivionNewerThan":
                if (line.Count == 2)
                {
                    Warn("Missing arguments for 'If OblivionNewerThan'");
                    return false;
                }
                if(line.Count > 3) Warn("Unexpected extra arguments for 'If OblivionNewerThan'");
                try
                {
                    var v = Handler.ScriptFunctions.OblivionVersion();
                    var v2 = new Version(line.ElementAt(2));
                    return v >= v2;
                }
                catch
                {
                    Warn("Invalid argument for 'If OblivionNewerThan'");
                    return false;
                }
            case "Equal":
                if (line.Count >= 4)
                    return line.ElementAt(2) == line.ElementAt(3);

                Warn("Missing arguments for 'If Equal'");
                return false;
            case "GreaterEqual":
            case "GreaterThan":
                if (line.Count < 4)
                {
                    Warn("Missing arguments for 'If Greater'");
                    return false;
                }
                if(line.Count > 4) Warn("Unexpected extra arguments for 'If Greater'");
                if (!int.TryParse(line.ElementAt(2), out var iArg1) || !int.TryParse(line.ElementAt(3), out var iArg2))
                {
                    Warn("Invalid argument supplied to function 'If Greater'");
                    return false;
                }

                if (line.ElementAt(1) == "GreaterEqual") return iArg1 >= iArg2;
                else return iArg1 > iArg2;
            case "fGreaterEqual":
            case "fGreaterThan":
                if (line.Count < 4)
                {
                    Warn("Missing arguments for 'If fGreater'");
                    return false;
                }
                if(line.Count > 4) Warn("Unexpected extra arguments for 'If fGreater'");
                if (!double.TryParse(line.ElementAt(2), out var fArg1) || !double.TryParse(line.ElementAt(3), out var fArg2))
                {
                    Warn("Invalid argument supplied to function 'If fGreater'");
                    return false;
                }

                if (line.ElementAt(1) == "fGreaterEqual") return fArg1 >= fArg2;
                else return fArg1 > fArg2;
            default:
                Warn($"Unknown argument '{line.ElementAt(1)}' for 'If'");
                return false;
            }
        }

        private static FlowControlStruct FunctionFor(IReadOnlyCollection<string> line, int lineNo)
        {
            var nullLoop = new FlowControlStruct(3);
            if (line.Count < 3)
            {
                Warn("Missing arguments for 'For'");
                return nullLoop;
            }

            var elementAt = line.ElementAt(1);
            if (elementAt == "Each") elementAt = line.ElementAt(2);
            switch (elementAt)
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
                if (!int.TryParse(line.ElementAt(3), out var start) || !int.TryParse(line.ElementAt(4), out var end) ||
                    line.Count >= 6 && !int.TryParse(line.ElementAt(5), out step))
                {
                    Warn("Invalid argument to 'For Count'");
                    return nullLoop;
                }
                var steps = new List<string>();
                for (int i = start; i < +end; i += step)
                {
                    steps.Add(i.ToString());
                }

                return new FlowControlStruct(steps.ToArray(), line.ElementAt(2), lineNo);
            }
            case "DataFolder":
            case "PluginFolder":
            case "DataFile":
            case "Plugin":
            {
                string root;
                if (elementAt == "DataFolder" || elementAt == "DataFile")
                    root = DataFiles;
                else
                    root = Plugins;

                if (line.Count < 5)
                {
                    Warn($"Missing arguments for 'For Each {elementAt}'");
                    return nullLoop;
                }
                if(line.Count > 7) Warn($"Unexpected extra arguments to 'For Each {elementAt}'");
                if (!Utils.IsSafeFolderName(line.ElementAt(4)))
                {
                    Warn($"Invalid argument for 'For Each {elementAt}'\nDirectory '{line.ElementAt(4)}' is not valid");
                    return nullLoop;
                }

                if (!Directory.Exists(Path.Combine(root, line.ElementAt(4))))
                {
                    Warn($"Invalid argument for 'For Each {elementAt}'\nDirectory '{line.ElementAt(4)}' does not exist");
                }

                var option = SearchOption.TopDirectoryOnly;
                if (line.Count > 5)
                {
                    switch (line.ElementAt(5))
                    {
                    case "True":
                        option = SearchOption.AllDirectories;
                        break;
                    case "False":
                        break;
                    default:
                        Warn($"Invalid argument '{line.ElementAt(5)}' for 'For Each {elementAt}'.\nExpected 'True' or 'False'");
                        break;
                    }
                }

                try
                {
                    var paths = Directory.GetDirectories(Path.Combine(root, line.ElementAt(4)),
                        line.Count > 6 ? line.ElementAt(6) : "*", option);
                    for (var i = 0; i < paths.Length; i++)
                    {
                        if (Path.IsPathRooted(paths[i]))
                            paths[i] = paths[i].Substring(root.Length);
                    }
                    return new FlowControlStruct(paths, line.ElementAt(3), lineNo);
                }
                catch
                {
                    Warn($"Invalid argument for 'For Each {elementAt}'");
                    return nullLoop;
                }
            }
            default:
                Warn("Unexpected function for 'For'");
                return nullLoop;
            }
        }

        private static string[] FunctionSelect(IReadOnlyCollection<string> line, bool isMultiSelect, bool hasPreviews, bool hasDescriptions)
        {
            if (line.Count < 3)
            {
                Warn("Missing arguments for 'Select'");
                return new string[0];
            }

            int argsPerOption = 1 + (hasPreviews ? 1 : 0) + (hasDescriptions ? 1 : 0);

            var title = line.ElementAt(1);
            var items = new List<string>(line.Count - 2);
            var line1 = line.ToList();
            line1.Where(s => line1.IndexOf(s) >= 2).Do(items.Add);
            line1 = items;

            if (line1.Count % argsPerOption != 0)
            {
                Warn("Unexpected extra arguments for 'Select'");
                do
                {
                    line1.RemoveAt(line1.Count - line1.Count % argsPerOption);
                } while (line1.Count % argsPerOption != 0);
            }

            items = new List<string>(line1.Count/argsPerOption);
            var previews = hasPreviews ? new List<string>(line1.Count / argsPerOption) : null;
            var descriptions = hasDescriptions ? new List<string>(line.Count / argsPerOption) : null;

            for (var i = 0; i < line1.Count / argsPerOption; i++)
            {
                items.Add(line1[i * argsPerOption]);
                if (hasPreviews)
                {
                    previews.Add(line1[i * argsPerOption + 1]);
                    if (hasDescriptions) descriptions.Add(line1[i * argsPerOption + 2]);
                }
                else
                {
                    if (hasDescriptions) descriptions.Add(line1[i * argsPerOption + 1]);
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

            var selectedIndex = Handler.ScriptFunctions.Select(items, title, isMultiSelect, previews, descriptions);
            if (selectedIndex == null || selectedIndex.Count == 0)
            {
                Srd.CancelInstall = true;
                return new string[0];
            }

            var result = new string[selectedIndex.Count];
            for (int i = 0; i < selectedIndex.Count; i++)
            {
                result[i] = $"Case {items[selectedIndex[i]].Replace("|","")}";
            }

            return result;
        }

        private static string[] FunctionSelectVar(IReadOnlyCollection<string> line, bool isVariable)
        {
            string funcName = isVariable ? "SelectVar" : "SelectString";
            if (line.Count < 2)
            {
                Warn($"Missing arguments for '{funcName}'");
                return new string[0];
            }

            if(line.Count > 2) Warn($"Unexpected arguments for '{funcName}'");
            if (!isVariable)
                return new[] {$"Case {line.ElementAt(1)}"};

            if (Variables.ContainsKey(line.ElementAt(1)))
                return new[] {$"Case {Variables[line.ElementAt(1)]}"};

            Warn($"Invalid argument for '{funcName}'\nVariable '{line.ElementAt(1)}' does not exist");
            return new string[0];

        }
    }
}
