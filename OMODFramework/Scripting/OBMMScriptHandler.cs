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
using System.Text;
using OMODFramework.Classes;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using File = Alphaleonis.Win32.Filesystem.File;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace OMODFramework.Scripting
{
    internal abstract class OBMMFunction
    {
        public abstract List<string> FuncNames { get; set; }
        public abstract string FuncName { get; set; }
        public abstract int MinArgs { get; set; }
        public abstract int MaxArgs { get; set; }

        public abstract void Run(ref IReadOnlyCollection<string> line);
    }

    internal static class OBMMScriptHandler
    {

        private class OBMMFunctionRegistry
        {
            private readonly HashSet<Lazy<OBMMFunction>> _functions;

            public OBMMFunctionRegistry()
            {
                _functions = new HashSet<Lazy<OBMMFunction>>
                {
                    new Lazy<OBMMFunction>(() => new FunctionMessage()),
                    new Lazy<OBMMFunction>(() => new FunctionSetVar()),
                    new Lazy<OBMMFunction>(() => new FunctionCombinePaths()),
                    new Lazy<OBMMFunction>(() => new FunctionSubRemoveString()),
                    new Lazy<OBMMFunction>(() => new FunctionStringLength()),
                    new Lazy<OBMMFunction>(() => new FunctionSet()),
                    new Lazy<OBMMFunction>(() => new FunctionExecLines()),
                    new Lazy<OBMMFunction>(() => new FunctionLoadOrder()),
                    new Lazy<OBMMFunction>(() => new FunctionConflicts()),
                    new Lazy<OBMMFunction>(() => new FunctionUncheckESP()),
                    new Lazy<OBMMFunction>(() => new FunctionSetDeactivationWarning()),
                    new Lazy<OBMMFunction>(() => new FunctionEditXML()),
                    new Lazy<OBMMFunction>(() => new FunctionModifyInstall()),
                    new Lazy<OBMMFunction>(() => new FunctionModifyInstallFolder()),
                    new Lazy<OBMMFunction>(() => new FunctionCopyDataFile()),
                    new Lazy<OBMMFunction>(() => new FunctionCopyDataFolder()),
                    new Lazy<OBMMFunction>(() => new FunctionGetDirectoryFileName()),
                    new Lazy<OBMMFunction>(() => new FunctionPatch()),
                    new Lazy<OBMMFunction>(() => new FunctionEditShader()),
                    new Lazy<OBMMFunction>(() => new FunctionEditINI()),
                    new Lazy<OBMMFunction>(() => new FunctionSetESPVar()),
                    new Lazy<OBMMFunction>(() => new FunctionSetESPData()),
                    new Lazy<OBMMFunction>(() => new FunctionInputString()),
                    new Lazy<OBMMFunction>(() => new FunctionDisplayFile()),
                    new Lazy<OBMMFunction>(() => new FunctionRegisterBSA()),
                    new Lazy<OBMMFunction>(() => new FunctionReadINI()),
                    new Lazy<OBMMFunction>(() => new FunctionReadRenderer())
                };
            }

            public OBMMFunction GetFunctionByName(string name)
            {
                return _functions.FirstOrDefault(f =>
                {
                    if(f.Value.FuncNames != null && f.Value.FuncNames.Count >= 1)
                        return f.Value.FuncNames.Contains(name);

                    return f.Value.FuncName == name;
                })?.Value;
            }
        }

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

        private static ScriptReturnData _srd;
        private static Dictionary<string, string> _variables;

        private static readonly OBMMFunctionRegistry Registry = new OBMMFunctionRegistry();
        private static readonly Queue<string> ExtraLines = new Queue<string>();

        private static string _dataFiles;
        private static string _plugins;
        private static string _cLine = "0";

        private static IScriptFunctions _scriptFunctions;

        internal static ScriptReturnData Execute(string inputScript, string dataPath, string pluginsPath, IScriptFunctions scriptFunctions)
        {
            _srd = new ScriptReturnData();
            if (string.IsNullOrWhiteSpace(inputScript))
                return _srd;

            _scriptFunctions = scriptFunctions ?? throw new OMODFrameworkException("The provided script functions can not be null!");

            _dataFiles = dataPath;
            _plugins = pluginsPath;
            _variables = new Dictionary<string, string>();

            var flowControl = new Stack<FlowControlStruct>();

            _variables["NewLine"] = Environment.NewLine;
            _variables["Tab"] = "\t";

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

                _cLine = i.ToString();
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
                    var function = Registry.GetFunctionByName(line.ElementAt(0));
                    if (function != null)
                    {
                        if (line.Count < function.MinArgs)
                        {
                            Warn($"Missing arguments for '{function.FuncName}'");
                            break;
                        }

                        if(function.MaxArgs != 0 && line.Count > function.MaxArgs)
                            Warn($"Unexpected arguments for '{function.FuncName}'");

                        function.Run(ref line);
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
                            _variables[fc.Var] = fc.Values[0];
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
                                _variables[fcs[k].Var] = fcs[k].Values[fcs[k].ForCount];
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
                                _variables[fc.Var] = fc.Values[fc.ForCount];
                            }
                        }
                        else Warn("Unexpected EndFor");
                        break;
                    //Functions
                    case "DontInstallAnyPlugins":
                        _srd.InstallAllPlugins = false;
                        break;
                    case "DontInstallAnyDataFiles":
                        _srd.InstallAllData = false;
                        break;
                    case "InstallAllPlugins":
                        _srd.InstallAllPlugins = true;
                        break;
                    case "InstallAllDataFiles":
                        _srd.InstallAllData = true;
                        break;
                    case "FatalError":
                        _srd.CancelInstall = true;
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

                if (Break || _srd.CancelInstall) break;
            }

            if (skipTo != null) Warn($"Expected: {skipTo}!");

            var temp = _srd;
            _srd = null;
            _variables = null;

            return temp;
        }

        private static void Warn(string msg)
        {
            if(Framework.EnableWarnings)
                _scriptFunctions.Warn($"'{msg}' at {_cLine}");
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
                            if (_variables.ContainsKey(currentWord))
                                currentWord = currentVar + _variables[currentWord];
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
                    dialogResult = _scriptFunctions.DialogYesNo(line.ElementAt(2));
                    if (dialogResult == -1)
                    {
                        _srd.CancelInstall = true;
                        return false;
                    }
                    else
                        return dialogResult == 1;
                case 4:
                    dialogResult = _scriptFunctions.DialogYesNo(line.ElementAt(2), line.ElementAt(3));
                    if (dialogResult == -1)
                    {
                        _srd.CancelInstall = true;
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
                    return _scriptFunctions.DataFileExists(line.ElementAt(2));

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
                    var v2 = new Version($"{Framework.Version}.0");
                    return line.ElementAt(1) == "VersionGreaterThan" ? v2 > v : v2 < v;
                }
                catch
                {
                    Warn($"Invalid argument for 'If {funcName}'");
                    return false;
                }
            case "ScriptExtenderPresent":
                if (line.Count > 2) Warn("Unexpected extra arguments for 'If ScriptExtenderPresent'");
                return _scriptFunctions.HasScriptExtender();
            case "ScriptExtenderNewerThan":
                if (line.Count == 2)
                {
                    Warn("Missing arguments for 'If ScriptExtenderNewerThan'");
                    return false;
                }
                if(line.Count > 3) Warn("Unexpected extra arguments for 'If ScriptExtenderNewerThan'");
                if (!_scriptFunctions.HasScriptExtender()) return false;
                try
                {
                    var v = _scriptFunctions.ScriptExtenderVersion();
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
                return _scriptFunctions.HasGraphicsExtender();
            case "GraphicsExtenderNewerThan":
                if (line.Count == 2)
                {
                    Warn("Missing arguments for 'If GraphicsExtenderNewerThan'");
                    return false;
                }
                if(line.Count > 3) Warn("Unexpected extra arguments for 'If GraphicsExtenderNewerThan'");
                if (!_scriptFunctions.HasGraphicsExtender()) return false;
                try
                {
                    var v = _scriptFunctions.GraphicsExtenderVersion();
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
                    var v = _scriptFunctions.OblivionVersion();
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
                    root = _dataFiles;
                else
                    root = _plugins;

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
                    } else if (!File.Exists(Path.Combine(_dataFiles, previews[i]))) {
                        Warn($"Preview file path '{previews[i]}' does not exist");
                        previews[i] = null;
                    }
                    else
                    {
                        previews[i] = Path.Combine(_dataFiles, previews[i]);
                    }
                }
            }

            var selectedIndex = _scriptFunctions.Select(items, title, isMultiSelect, previews, descriptions);
            if (selectedIndex == null || selectedIndex.Count == 0)
            {
                _srd.CancelInstall = true;
                return new string[0];
            }

            var result = new string[selectedIndex.Count];
            for (int i = 0; i < selectedIndex.Count; i++)
            {
                result[i] = $"Case {items[selectedIndex[i]]}";
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

            if (_variables.ContainsKey(line.ElementAt(1)))
                return new[] {$"Case {_variables[line.ElementAt(1)]}"};

            Warn($"Invalid argument for '{funcName}'\nVariable '{line.ElementAt(1)}' does not exist");
            return new string[0];

        }

        private static int Set(List<string> func)
        {
            if (func.Count == 0) throw new OMODFrameworkException($"Empty iSet in script at {_cLine}");
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

                if(count != 0) throw new OMODFrameworkException($"Mismatched brackets in script at {_cLine}");
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

            if(func.Count != 1) throw new OMODFrameworkException($"Leftovers in iSet function for script at {_cLine}");
            return int.Parse(func[0]);
        }

        private static double FSet(List<string> func)
        {
            if (func.Count == 0) throw new OMODFrameworkException($"Empty fSet in script at {_cLine}");
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

                if (count != 0) throw new OMODFrameworkException($"Mismatched brackets in script at {_cLine}");
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

            if (func.Count != 1) throw new OMODFrameworkException($"Leftovers in iSet function for script at {_cLine}");
            return double.Parse(func[0]);
        }

        private class FunctionMessage : OBMMFunction
        {
            public override List<string> FuncNames { get; set; }
            public override string FuncName { get; set; } = "Message";
            public override int MinArgs { get; set; } = 2;
            public override int MaxArgs { get; set; } = 3;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                switch (line.Count)
                {
                case 2:
                    _scriptFunctions.Message(line.ElementAt(1));
                    break;
                case 3:
                    _scriptFunctions.Message(line.ElementAt(1), line.ElementAt(2));
                    break;
                default:
                    goto case 3;
                }
            }
        }

        private class FunctionSetVar : OBMMFunction
        {
            public override List<string> FuncNames { get; set; }
            public override string FuncName { get; set; } = "SetVar";
            public override int MinArgs { get; set; } = 3;
            public override int MaxArgs { get; set; } = 3;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                _variables[line.ElementAt(1)] = line.ElementAt(2);
            }
        }

        private class FunctionCombinePaths : OBMMFunction
        {
            public override List<string> FuncNames { get; set; }
            public override string FuncName { get; set; } = "CombinePaths";
            public override int MinArgs { get; set; } = 4;
            public override int MaxArgs { get; set; } = 4;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                try
                {
                    _variables[line.ElementAt(1)] = Path.Combine(line.ElementAt(2), line.ElementAt(3));
                }
                catch
                {
                    Warn("Invalid arguments for 'CombinePaths'");
                }
            }
        }

        private class FunctionSubRemoveString : OBMMFunction
        {
            public override List<string> FuncNames { get; set; } = new List<string>{"RemoveString", "Substring"};
            public override string FuncName { get; set; } = "RemoveString OR Substring";
            public override int MinArgs { get; set; } = 4;
            public override int MaxArgs { get; set; } = 5;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                var remove = line.ElementAt(0) == "RemoveString";
                FuncName = line.ElementAt(0);

                if (line.Count == 4)
                {
                    if (!int.TryParse(line.ElementAt(3), out int start))
                    {
                        Warn($"Invalid arguments for '{FuncName}'");
                        return;
                    }

                    _variables[line.ElementAt(1)] = remove ? line.ElementAt(2).Remove(start) : line.ElementAt(2).Substring(start);
                }
                else
                {
                    if (!int.TryParse(line.ElementAt(3), out int start) || !int.TryParse(line.ElementAt(4), out int end))
                    {
                        Warn($"Invalid arguments for '{FuncName}'");
                        return;
                    }
                    _variables[line.ElementAt(1)] = remove ? line.ElementAt(2).Remove(start,end) : line.ElementAt(2).Substring(start, end);
                }
            }
        }

        private class FunctionStringLength : OBMMFunction
        {
            public override List<string> FuncNames { get; set; }
            public override string FuncName { get; set; } = "StringLength";
            public override int MinArgs { get; set; } = 3;
            public override int MaxArgs { get; set; } = 3;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                _variables[line.ElementAt(1)] = line.ElementAt(2).Length.ToString();
            }
        }

        private class FunctionSet : OBMMFunction
        {
            public override List<string> FuncNames { get; set; } = new List<string>{"iSet", "fSet"};
            public override string FuncName { get; set; } = "iSet OR fSet";
            public override int MinArgs { get; set; } = 3;
            public override int MaxArgs { get; set; } = 0;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                var integer = line.ElementAt(0) == "iSet";
                FuncName = integer ? "iSet" : "fSet";

                var func = new List<string>();
                for(int i = 2; i < line.Count; i++) func.Add(line.ElementAt(i));
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

                    _variables[line.ElementAt(1)] = result;
                } 
                catch
                {
                    Warn($"Invalid arguments for {FuncName}");
                }
            }
        }

        private class FunctionExecLines : OBMMFunction
        {
            public override List<string> FuncNames { get; set; }
            public override string FuncName { get; set; } = "ExecLines";
            public override int MinArgs { get; set; } = 2;
            public override int MaxArgs { get; set; } = 2;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                line.ElementAt(1).Split(new [] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries).Do(ExtraLines.Enqueue);
            }
        }

        private class FunctionLoadOrder : OBMMFunction
        {
            public override List<string> FuncNames { get; set; } = new List<string>{"LoadEarly", "LoadAfter", "LoadBefore"};
            public override string FuncName { get; set; } = "LoadEarly OR LoadAfter OR LoadBefore";
            public override int MinArgs { get; set; } = 3;
            public override int MaxArgs { get; set; } = 3;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                var loadEarly = line.ElementAt(0) == "LoadEarly";
                var loadAfter = line.ElementAt(0) == "LoadAfter";
                FuncName = loadEarly ? "LoadEarly" : loadAfter ? "LoadAfter" : "LoadBefore";

                if (loadEarly && line.Count < 2)
                {
                    Warn($"Missing arguments for '{FuncName}'");
                    return;
                }

                if (loadEarly && line.Count > 2)
                    Warn($"Unexpected arguments for '{FuncName}'");

                if (loadEarly)
                {
                    var plugin = line.ElementAt(1);
                    plugin = plugin.ToLower();
                    if (!_srd.EarlyPlugins.Contains(plugin))
                        _srd.EarlyPlugins.Add(plugin);
                }
                else
                {
                    _srd.LoadOrderSet.Add(new PluginLoadInfo(line.ElementAt(1), line.ElementAt(2), loadAfter));
                }
            }
        }

        private class FunctionConflicts : OBMMFunction
        {
            public override List<string> FuncNames { get; set; } = new List<string>
            {
                "ConflictsWith", "DependsOn", "ConflictsWithRegex", "DependsOnRegex"
            };

            public override string FuncName { get; set; } =
                "ConflictsWith OR DependsOn OR ConflictsWithRegex OR DependsOnRegex";

            public override int MinArgs { get; set; } = 2;
            public override int MaxArgs { get; set; } = 8;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                var conflicts = line.ElementAt(0).StartsWith("ConflictsWith");
                var regex = line.ElementAt(0).EndsWith("Regex");
                FuncName = conflicts ? "ConflictsWith" : "DependsOn";
                FuncName += regex ? "Regex" : "";

                var cd = new ConflictData {Level = ConflictLevel.MajorConflict};
            switch (line.Count)
            { 
            case 2:
                cd.File = line.ElementAt(1);
                break;
            case 3:
                cd.Comment = line.ElementAt(2);
                goto case 2;
            case 4:
                switch (line.ElementAt(3))
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
                    Warn($"Unknown conflict level after '{FuncName}'");
                    break;
                }

                goto case 3;
            case 5:
                Warn($"Unexpected arguments for '{FuncName}'");
                break;
            case 6:
                cd.File = line.ElementAt(1);
                try
                {
                    cd.MinMajorVersion = Convert.ToInt32(line.ElementAt(2));
                    cd.MinMinorVersion = Convert.ToInt32(line.ElementAt(3));
                    cd.MaxMajorVersion = Convert.ToInt32(line.ElementAt(4));
                    cd.MaxMinorVersion = Convert.ToInt32(line.ElementAt(5));
                }
                catch
                {
                    Warn($"Arguments for '{FuncName}' could not been parsed");
                }

                break;
            case 7:
                cd.Comment = line.ElementAt(6);
                goto case 6;
            case 8:
                switch (line.ElementAt(7))
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
                    Warn($"Unknown conflict level after '{FuncName}'");
                    break;
                }

                goto case 7;
            default:
                Warn($"Unexpected arguments for '{FuncName}'");
                goto case 8;
            }

            cd.Partial = regex;
            if (conflicts)
                _srd.ConflictsWith.Add(cd);
            else
                _srd.DependsOn.Add(cd);
            }
        }

        private class FunctionUncheckESP : OBMMFunction
        {
            public override List<string> FuncNames { get; set; }
            public override string FuncName { get; set; } = "UncheckESP";
            public override int MinArgs { get; set; } = 2;
            public override int MaxArgs { get; set; } = 2;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                var plugin = line.ElementAt(1);
                if (!File.Exists(Path.Combine(_plugins, plugin)))
                {
                    Warn($"Invalid argument for 'UncheckESP': {plugin} does not exist");
                    return;
                }

                plugin = plugin.ToLower();
                if (!_srd.UncheckedPlugins.Contains(plugin))
                    _srd.UncheckedPlugins.Add(plugin);
            }
        }

        private class FunctionSetDeactivationWarning : OBMMFunction
        {
            public override List<string> FuncNames { get; set; }
            public override string FuncName { get; set; } = "SetDeactivationWarning";
            public override int MinArgs { get; set; } = 3;
            public override int MaxArgs { get; set; } = 3;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                var plugin = line.ElementAt(1);
                if (!File.Exists(Path.Combine(_plugins, plugin)))
                {
                    Warn($"Invalid argument for 'SetDeactivationWarning'\nFile '{plugin}' does not exist");
                    return;
                }

                plugin = plugin.ToLower();

                _srd.ESPDeactivation.RemoveWhere(a => a.Plugin == plugin);
                switch (line.ElementAt(2))
                {
                    case "Allow":
                        _srd.ESPDeactivation.Add(new ScriptESPWarnAgainst(plugin, DeactivationStatus.Allow));
                        break;
                    case "WarnAgainst":
                        _srd.ESPDeactivation.Add(new ScriptESPWarnAgainst(plugin, DeactivationStatus.WarnAgainst));
                        break;
                    case "Disallow":
                        _srd.ESPDeactivation.Add(new ScriptESPWarnAgainst(plugin, DeactivationStatus.Disallow));
                        break;
                    default:
                        Warn("Invalid argument for 'SetDeactivationWarning'");
                        return;
                }
            }
        }

        private class FunctionEditXML : OBMMFunction
        {
            public override List<string> FuncNames { get; set; } = new List<string>{"EditXMLLine", "EditXMLReplace"};
            public override string FuncName { get; set; } = "EditXMLLine OR EditXMLReplace";
            public override int MinArgs { get; set; } = 4;
            public override int MaxArgs { get; set; } = 4;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                var replace = line.ElementAt(0) == "EditXMLReplace";
                FuncName = replace ? "EditXMLReplace" : "EditXMLLine";

                if (replace)
                {
                    var path = line.ElementAt(1);
                    var file = Path.Combine(_dataFiles, path);
                    path = path.ToLower();
                    if (!Utils.IsSafeFileName(path) || !File.Exists(file))
                    {
                        Warn($"Invalid filename supplied for '{FuncName}'");
                        return;
                    }

                    var ext = Path.GetExtension(file);
                    if (ext != ".xml" && ext != ".txt" && ext != ".ini" && ext != ".bat")
                    {
                        Warn($"Invalid filename supplied for '{FuncName}'");
                        return;
                    }

                    var text = File.ReadAllText(file);
                    text = text.Replace(line.ElementAt(2), line.ElementAt(3));
                    File.WriteAllText(file, text);
                }
                else
                {
                    var path = line.ElementAt(1);
                    var file = Path.Combine(_dataFiles, path);

                    path = path.ToLower();
                    if (!Utils.IsSafeFileName(path) || !File.Exists(file))
                    {
                        Warn($"Invalid filename supplied for '{FuncName}'");
                        return;
                    }

                    var ext = Path.GetExtension(path);
                    if (ext != ".xml" && ext != ".txt" && ext != ".ini" && ext != ".bat")
                    {
                        Warn($"Invalid filename supplied for '{FuncName}'");
                        return;
                    }

                    if (!int.TryParse(line.ElementAt(2), out var index) || index < 1)
                    {
                        Warn($"Invalid line number supplied for '{FuncName}'");
                        return;
                    }

                    index -= 1;
                    var lines = File.ReadAllLines(file);
                    if (lines.Length <= index)
                    {
                        Warn($"Invalid line number supplied for '{FuncName}'");
                        return;
                    }

                    lines[index] = line.ElementAt(3);
                    File.WriteAllLines(file, lines);
                }
            }
        }

        private class FunctionModifyInstall : OBMMFunction
        {
            public override List<string> FuncNames { get; set; } = new List<string>
            {
                "InstallPlugin", "InstallDataFile", "DontInstallPlugin", "DontInstallDataFile"
            };

            public override string FuncName { get; set; } =
                "InstallPlugin OR InstallDataFile OR DontInstallPlugin OR DontInstallDataFile";

            public override int MinArgs { get; set; } = 2;
            public override int MaxArgs { get; set; } = 2;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                var install = line.ElementAt(0).StartsWith("Install");
                var plugin = line.ElementAt(0).EndsWith("Plugin");

                FuncName = install ? "Install" : "DontInstall";
                FuncName += plugin ? "Plugin" : "DataFile";

                var l = line.ElementAt(1).ToLower();
                if (plugin)
                {
                    var path = Path.Combine(_plugins, l);
                    if (!File.Exists(path))
                    {
                        Warn($"Invalid argument for '{FuncName}'\nFile '{path}' does not exist");
                        return;
                    }

                    if (l.IndexOfAny(new [] {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar}) != -1)
                    {
                        Warn($"Invalid argument for '{FuncName}'\nThis function cannot be used on plugins stored in subdirectories");
                    }

                    if (install)
                    {
                        _srd.IgnorePlugins.RemoveWhere(s => s == l);
                        if (!_srd.InstallPlugins.Contains(l))
                            _srd.InstallPlugins.Add(l);
                    }
                    else
                    {
                        _srd.InstallPlugins.RemoveWhere(s => s == l);
                        if (!_srd.IgnorePlugins.Contains(l))
                            _srd.IgnorePlugins.Add(l);
                    }
                }
                else
                {
                    var path = Path.Combine(_dataFiles, l);
                    if(!File.Exists(path)) {
                        Warn($"Invalid argument for '{FuncName}'\nFile '{path}' does not exist");
                        return;
                    }

                    if (install)
                    {
                        _srd.IgnoreData.RemoveWhere(s => s == l);
                        if (!_srd.InstallData.Contains(l))
                            _srd.InstallData.Add(l);
                    } else
                    {
                        _srd.InstallData.RemoveWhere(s => s == l);
                        if (!_srd.IgnoreData.Contains(l))
                            _srd.IgnoreData.Add(l);
                    }
                }
            }
        }

        private class FunctionModifyInstallFolder : OBMMFunction
        {
            public override List<string> FuncNames { get; set; } =
                new List<string> {"InstallDataFolder", "DontInstallDataFolder"};

            public override string FuncName { get; set; } = "InstallDataFolder OR DontInstallDataFolder";
            public override int MinArgs { get; set; } = 2;
            public override int MaxArgs { get; set; } = 3;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                var install = line.ElementAt(0).StartsWith("Install");
                FuncName = install ? "InstallDataFolder" : "DontInstallDataFolder";

                var validFolderPath = Utils.MakeValidFolderPath(line.ElementAt(1));
                var path = Path.Combine(_dataFiles, validFolderPath);

                if (!Directory.Exists(path))
                {
                    Warn($"Invalid argument for '{FuncName}'\nFolder '{path}' does not exist");
                    return;
                }

                if (line.Count >= 2)
                {
                    switch (line.ElementAt(2))
                    {
                        case "True":
                            Directory.GetDirectories(path).Do(d =>
                            {
                                var a = (IReadOnlyCollection<string>)new List<string>
                                {
                                    "InstallDataFolder", d.Substring(_dataFiles.Length), "True"
                                };
                                Run(ref a);
                            });
                            break;
                        case "False":
                            break;
                        default:
                            Warn($"Invalid argument for '{FuncName}'\nExpected True or False");
                            break;
                    }
                }
                Directory.GetFiles(path).Do(f =>
                {
                    var name = Path.GetFileName(f);
                    if (install)
                    {
                        _srd.IgnoreData.RemoveWhere(s => s == name);
                        if (!_srd.InstallData.Contains(name))
                            _srd.InstallData.Add(name);
                    }
                    else
                    {
                        _srd.InstallData.RemoveWhere(s => s == name);
                        if (!_srd.IgnoreData.Contains(name))
                            _srd.IgnoreData.Add(name);
                    }
                });
            }
        }

        private class FunctionCopyDataFile : OBMMFunction
        {
            public override List<string> FuncNames { get; set; } = new List<string>{"CopyPlugin", "CopyDataFile"};
            public override string FuncName { get; set; }
            public override int MinArgs { get; set; } = 3;
            public override int MaxArgs { get; set; } = 3;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                var plugin = line.ElementAt(0).StartsWith("Plugin");
                FuncName = plugin ? "CopyPlugin" : "CopyDataFile";

                var from = line.ElementAt(1);
            var to = line.ElementAt(2);

            if (!Utils.IsSafeFileName(from) || !Utils.IsSafeFileName(to))
            {
                Warn($"Invalid argument for '{FuncName}'");
                return;
            }

            if (from == to)
            {
                Warn($"Invalid argument for '{FuncName}'\nYou can not copy a file over itself");
                return;
            }

            if(plugin)
            {
                var path = Path.Combine(_plugins, from);
                if (!File.Exists(path))
                {
                    Warn($"Invalid argument for '{FuncName}'\nFile '{from}' does not exist");
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
                var path = Path.Combine(_dataFiles, from);
                if (!File.Exists(path))
                {
                    Warn($"Invalid argument for '{FuncName}'\nFile '{from}' does not exist");
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
                _srd.CopyPlugins.RemoveWhere(s => s.CopyTo == to.ToLower());
                _srd.CopyPlugins.Add(new ScriptCopyDataFile(from.ToLower(), to.ToLower()));
            }
            else
            {
                _srd.CopyDataFiles.RemoveWhere(s => s.CopyTo == to.ToLower());
                _srd.CopyDataFiles.Add(new ScriptCopyDataFile(from.ToLower(), to.ToLower()));
            }
            }
        }

        private class FunctionCopyDataFolder : OBMMFunction
        {
            public override List<string> FuncNames { get; set; }
            public override string FuncName { get; set; } = "CopyDataFolder";
            public override int MinArgs { get; set; } = 3;
            public override int MaxArgs { get; set; } = 4;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                var validFrom = Utils.MakeValidFolderPath(line.ElementAt(1).ToLower());
                var validTo = Utils.MakeValidFolderPath(line.ElementAt(2).ToLower());

                if (!Utils.IsSafeFolderName(validFrom) || !Utils.IsSafeFolderName(validTo))
                {
                    Warn("Invalid argument for 'CopyDataFolder'");
                    return;
                }

                var from = Path.Combine(_dataFiles, validFrom);
                var to = Path.Combine(_dataFiles, validTo);

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

                var collection = line;
                if (line.Count >= 4)
                {
                    switch(line.ElementAt(3)) {
                    case "True":
                        Directory.GetDirectories(from).Do(d =>
                        {
                            var arg2 = d.Substring(_dataFiles.Length);
                            if (arg2.StartsWith("\\"))
                                arg2 = arg2.Substring(1);
                            var l = _dataFiles.Length + collection.ElementAt(1).Length;
                            var t = d.Substring(l);
                            var arg3 = collection.ElementAt(2) + t;
                            var arg = (IReadOnlyCollection<string>)new List<string> {"", arg2, arg3, "True"};
                            Run(ref arg);
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
                    var fFrom = Path.Combine(collection.ElementAt(1), Path.GetFileName(f));
                    var fTo = Path.Combine(collection.ElementAt(2), Path.GetFileName(f)).ToLower();

                    _srd.CopyDataFiles.RemoveWhere(s => s.CopyTo == fTo);

                    _srd.CopyDataFiles.Add(new ScriptCopyDataFile(fFrom, fTo));
                });
            }
        }

        private class FunctionGetDirectoryFileName : OBMMFunction
        {
            public override List<string> FuncNames { get; set; } = new List<string>{"GetDirectoryName", "GetFileName", "GetFolderName", "GetFileNameWithoutExtension"};
            public override string FuncName { get; set; }
            public override int MinArgs { get; set; } = 3;
            public override int MaxArgs { get; set; } = 3;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                var file = line.ElementAt(0) == "GetFileName";
                var extension = line.ElementAt(0) == "GetFileNameWithoutExtension";
                FuncName = file ? "GetFileName" : line.ElementAt(0);

                try
                {
                    if(file)
                        _variables[line.ElementAt(1)] = Path.GetFileName(line.ElementAt(2));
                    else
                        _variables[line.ElementAt(1)] = Path.GetDirectoryName(line.ElementAt(2));

                    if(extension)
                        _variables[line.ElementAt(1)] = Path.GetFileNameWithoutExtension(line.ElementAt(2));
                }
                catch
                {
                    Warn($"Invalid argument for '{FuncName}'");
                }
            }
        }

        private class FunctionPatch : OBMMFunction
        {
            public override List<string> FuncNames { get; set; } = new List<string>{"PatchPlugin", "PatchDataFile"};
            public override string FuncName { get; set; }
            public override int MinArgs { get; set; } = 3;
            public override int MaxArgs { get; set; } = 4;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                var plugin = line.ElementAt(0) == "PatchPlugin";
                FuncName = plugin ? "PatchPlugin" : "PatchDataFile";

                var from = line.ElementAt(1);
            var to = line.ElementAt(2);

            if (!Utils.IsSafeFileName(from) || !Utils.IsSafeFileName(to))
            {
                Warn($"Invalid argument for '{FuncName}'");
                return;
            }

            var pathFrom = plugin ? Path.Combine(_plugins, from) : Path.Combine(_dataFiles, from);
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
                _srd.PatchFiles.RemoveWhere(s => s.CopyTo == to.ToLower());
                _srd.PatchFiles.Add(new ScriptCopyDataFile(from.ToLower(), to.ToLower()));
                break;
            case Framework.PatchMethod.PatchWithInterface:
                _scriptFunctions.Patch(pathFrom, to);
                break;
            default:
                throw new OMODFrameworkException("Unknown PatchMethod for Framework.CurrentPatchMethod!");
            }
            }
        }

        private class FunctionEditShader : OBMMFunction
        {
            public override List<string> FuncNames { get; set; } = new List<string>{"EditSDP", "EditShader"};
            public override string FuncName { get; set; }
            public override int MinArgs { get; set; } = 4;
            public override int MaxArgs { get; set; } = 4;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                FuncName = line.ElementAt(0);
                var shaderPath = Path.Combine(_dataFiles, line.ElementAt(3));
                if (!Utils.IsSafeFileName(line.ElementAt(3)))
                {
                    Warn($"Invalid argument for 'EditShader'\n'{line.ElementAt(3)}' is not a valid file name");
                    return;
                }

                if (!File.Exists(shaderPath))
                {
                    Warn($"Invalid argument for 'EditShader'\nFile '{line.ElementAt(3)}' does not exist");
                    return;
                }

                if (!byte.TryParse(line.ElementAt(1), out var package))
                {
                    Warn($"Invalid argument for function 'EditShader'\n'{line.ElementAt(1)}' is not a valid shader package ID");
                    return;
                }

                _srd.SDPEdits.Add(new SDPEditInfo(package, line.ElementAt(2), shaderPath));
            }
        }

        private class FunctionEditINI : OBMMFunction
        {
            public override List<string> FuncNames { get; set; }
            public override string FuncName { get; set; } = "EditINI";
            public override int MinArgs { get; set; } = 4;
            public override int MaxArgs { get; set; } = 4;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                _srd.INIEdits.Add(new INIEditInfo(line.ElementAt(1), line.ElementAt(2), line.ElementAt(3)));
            }
        }

        private class FunctionSetESPVar : OBMMFunction
        {
            public override List<string> FuncNames { get; set; } = new List<string>{"SetGMST", "SetGlobal"};
            public override string FuncName { get; set; }
            public override int MinArgs { get; set; } = 4;
            public override int MaxArgs { get; set; } = 4;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                var gmst = line.ElementAt(0) == "SetGMST";
                FuncName = line.ElementAt(0);

                if (!Utils.IsSafeFileName(line.ElementAt(1)))
                {
                    Warn($"Illegal plugin name supplied to '{FuncName}'");
                    return;
                }

                if (!File.Exists(Path.Combine(_plugins, line.ElementAt(1))))
                {
                    Warn($"Invalid argument for '{FuncName}'\nFile '{line.ElementAt(1)}' does not exist");
                    return;
                }

                _srd.ESPEdits.Add(new ScriptESPEdit(gmst, line.ElementAt(1).ToLower(), line.ElementAt(2).ToLower(),
                    line.ElementAt(3)));
            }
        }

        private class FunctionSetESPData : OBMMFunction
        {
            public override List<string> FuncNames { get; set; } = new List<string>
            {
                "SetPluginByte", "SetPluginShort", "SetPluginLong", "SetPluginFloat", "SetPluginInt"
            };
            public override string FuncName { get; set; }
            public override int MinArgs { get; set; } = 4;
            public override int MaxArgs { get; set; } = 4;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                FuncName = line.ElementAt(0);

                var plugin = line.ElementAt(1);
                if (!Utils.IsSafeFileName(plugin))
                {
                    Warn($"Illegal plugin name supplied to '{FuncName}'");
                    return;
                }

                var pluginPath = Path.Combine(_plugins, plugin);
                if (!File.Exists(pluginPath))
                {
                    Warn($"Invalid argument for '{FuncName}'\nFile {plugin} does not exist");
                    return;
                }

                byte[] data = null;
                if (!long.TryParse(line.ElementAt(2), out var offset) || offset < 0)
                {
                    Warn($"Invalid argument for '{FuncName}'\nOffset {line.ElementAt(2)} is not valid");
                    return;
                }

                var val = line.ElementAt(3);
                if (FuncName.EndsWith("Byte"))
                {
                    if (!byte.TryParse(val, out var value))
                    {
                        Warn($"Invalid argument for '{FuncName}'\nValue '{val}' is not valid");
                        return;
                    }

                    data = BitConverter.GetBytes(value);
                }

                if (FuncName.EndsWith("Short"))
                {
                    if (!short.TryParse(val, out var value))
                    {
                        Warn($"Invalid argument for '{FuncName}'\nValue '{val}' is not valid");
                        return;
                    }

                    data = BitConverter.GetBytes(value);
                }

                if (FuncName.EndsWith("Int"))
                {
                    if (!int.TryParse(val, out var value))
                    {
                        Warn($"Invalid argument for '{FuncName}'\nValue '{val}' is not valid");
                        return;
                    }

                    data = BitConverter.GetBytes(value);
                }

                if (FuncName.EndsWith("Long"))
                {
                    if (!long.TryParse(val, out var value))
                    {
                        Warn($"Invalid argument for '{FuncName}'\nValue '{val}' is not valid");
                        return;
                    }

                    data = BitConverter.GetBytes(value);
                }

                if (FuncName.EndsWith("Float"))
                {
                    if (!float.TryParse(val, out var value))
                    {
                        Warn($"Invalid argument for '{FuncName}'\nValue '{val}' is not valid");
                        return;
                    }

                    data = BitConverter.GetBytes(value);
                }

                if (data == null)
                {
                    throw new OMODFrameworkException($"Data in '{FuncName}' can not be null!");
                }

                using (var fs = File.OpenWrite(pluginPath))
                {
                    if (offset + data.Length >= fs.Length)
                    {
                        Warn($"Invalid argument for '{FuncName}'\nOffset {line.ElementAt(2)} is out of range");
                        return;
                    }

                    fs.Position = offset;

                    try
                    {
                        fs.Write(data, 0, data.Length);
                    }
                    catch (Exception e)
                    {
                        throw new OMODFrameworkException(
                            $"Could not write to file {pluginPath} in '{FuncName}' at {_cLine}\n{e}");
                    }
                }
            }
        }

        private class FunctionInputString : OBMMFunction
        {
            public override List<string> FuncNames { get; set; }
            public override string FuncName { get; set; } = "InputString";
            public override int MinArgs { get; set; } = 2;
            public override int MaxArgs { get; set; } = 4;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                var title = line.Count > 2 ? line.ElementAt(2) : "";
                var initialText = line.Count > 3 ? line.ElementAt(3) : "";

                var result = _scriptFunctions.InputString(title, initialText, false);
                _variables[line.ElementAt(1)] = result ?? "";
            }
        }

        private class FunctionDisplayFile : OBMMFunction
        {
            public override List<string> FuncNames { get; set; } = new List<string>{"DisplayText", "DisplayImage"};
            public override string FuncName { get; set; }
            public override int MinArgs { get; set; } = 2;
            public override int MaxArgs { get; set; } = 3;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                var image = line.ElementAt(0) == "DisplayImage";
                FuncName = line.ElementAt(0);

                if (!Utils.IsSafeFileName(line.ElementAt(1)))
                {
                    Warn($"Illegal path supplied to '{FuncName}'");
                    return;
                }

                var path = Path.Combine(_dataFiles, line.ElementAt(1));
                if (!File.Exists(path))
                {
                    Warn($"Invalid argument for '{FuncName}'\nFile {path} does not exist");
                    return;
                }

                var title = line.Count > 2 ? line.ElementAt(2) : line.ElementAt(1);

                if(image)
                    _scriptFunctions.DisplayImage(path, title);
                else
                {
                    var text = File.ReadAllText(path, Encoding.UTF8);
                    _scriptFunctions.DisplayText(text, title);
                }
            }
        }

        private class FunctionRegisterBSA : OBMMFunction
        {
            public override List<string> FuncNames { get; set; } = new List<string>{"RegisterBSA", "UnregisterBSA"};
            public override string FuncName { get; set; }
            public override int MinArgs { get; set; } = 2;
            public override int MaxArgs { get; set; } = 2;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                var register = line.ElementAt(0) == "RegisterBSA";
                FuncName = line.ElementAt(0);

                var esp = line.ElementAt(1).ToLower();
                if (esp.Contains(",") || esp.Contains(";") || esp.Contains("="))
                {
                    Warn($"Invalid argument for '{FuncName}'\nBSA file names are not allowed to include the characters ',' '=' or ';'");
                    return;
                }

                if (register && !_srd.RegisterBSASet.Contains(esp))
                    _srd.RegisterBSASet.Add(esp);
                else
                    _srd.RegisterBSASet.RemoveWhere(s => s == esp);
            }
        }

        private class FunctionReadINI : OBMMFunction
        {
            public override List<string> FuncNames { get; set; }
            public override string FuncName { get; set; } = "ReadINI";
            public override int MinArgs { get; set; } = 4;
            public override int MaxArgs { get; set; } = 4;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                switch (Framework.CurrentReadINIMethod)
                {
                    case Framework.ReadINIMethod.ReadOriginalINI:
                        _variables[line.ElementAt(1)] = OblivionINI.GetINIValue(line.ElementAt(2), line.ElementAt(3));
                        break;
                    case Framework.ReadINIMethod.ReadWithInterface:
                        var s = _scriptFunctions.ReadOblivionINI(line.ElementAt(2), line.ElementAt(3));
                        _variables[line.ElementAt(1)] = s ?? throw new OMODFrameworkException("Could not read the oblivion.ini file using the function IScriptFunctions.ReadOblivionINI");
                        break;
                    default:
                        throw new OMODFrameworkException("Unknown ReadINIMethod for Framework.CurrentReadINIMethod!");
                }
            }
        }

        private class FunctionReadRenderer : OBMMFunction
        {
            public override List<string> FuncNames { get; set; }
            public override string FuncName { get; set; } = "ReadRendererInfo";
            public override int MinArgs { get; set; } = 3;
            public override int MaxArgs { get; set; } = 3;

            public override void Run(ref IReadOnlyCollection<string> line)
            {
                switch (Framework.CurrentReadRendererMethod)
                {
                    case Framework.ReadRendererMethod.ReadOriginalRenderer:
                        _variables[line.ElementAt(1)] = OblivionRenderInfo.GetInfo(line.ElementAt(2));
                        break;
                    case Framework.ReadRendererMethod.ReadWithInterface:
                        var s = _scriptFunctions.ReadRendererInfo(line.ElementAt(2));
                        _variables[line.ElementAt(1)] = s ?? throw new OMODFrameworkException("Could not read the RenderInfo.txt file using the function IScriptFunctions.ReadRendererInfo");
                        break;
                    default:
                        throw new OMODFrameworkException("Unknown ReadRendererMethod for Framework.CurrentReadRendererMethod!");
                }
            }
        }
    }
}
