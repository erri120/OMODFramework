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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OMODFramework.Classes;

namespace OMODFramework.Scripting
{
    internal class SharedFunctionsHandler
    {
        internal readonly IScriptFunctions ScriptFunctions;

        private readonly ScriptType _type;

        internal SharedFunctionsRegistry Registry;

        internal SharedFunctionsHandler(ScriptType type, ref IScriptFunctions scriptFunctions)
        {
            _type = type;
            ScriptFunctions = scriptFunctions;
        }

        internal void Warn(string msg)
        {
            if (!Framework.Settings.ScriptExecutionSettings.EnableWarnings)
                return;

            Utils.Warn($"Script warning: '{msg}'");

            if (_type == ScriptType.OBMMScript)
                ScriptFunctions.Warn($"'{msg}' at {OBMMScriptHandler.CLine}");
        }
    }

    internal class SharedFunctionsRegistry
    {
        private readonly HashSet<Lazy<SharedFunction>> _functions;

        internal SharedFunctionsRegistry(SharedFunctionsHandler handler)
        {
            _functions = new HashSet<Lazy<SharedFunction>>
            {
                new Lazy<SharedFunction>(() => new FunctionMessage(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionSetVar(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionCombinePaths(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionSubRemoveString(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionStringLength(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionSet(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionExecLines(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionLoadOrder(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionConflicts(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionUncheckESP(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionSetDeactivationWarning(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionEditXML(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionModifyInstall(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionModifyInstallFolder(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionCopyDataFile(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionCopyDataFolder(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionGetDirectoryFileName(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionPatch(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionEditShader(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionEditINI(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionSetESPVar(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionSetESPData(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionInputString(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionDisplayFile(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionRegisterBSA(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionReadINI(ref handler)),
                new Lazy<SharedFunction>(() => new FunctionReadRenderer(ref handler))
            };
        }

        internal SharedFunction GetFunctionByName(string name)
        {
            return _functions.FirstOrDefault(f =>
            {
                if (f.Value.FuncNames != null && f.Value.FuncNames.Count >= 1)
                    return f.Value.FuncNames.Contains(name);

                return f.Value.FuncName == name;
            })?.Value;
        }
    }

    internal abstract class SharedFunction
    {
        protected SharedFunctionsHandler Handler;

        public abstract List<string> FuncNames { get; set; }
        public abstract string FuncName { get; set; }
        public abstract int MinArgs { get; set; }
        public abstract int MaxArgs { get; set; }

        public abstract void Run(ref IReadOnlyCollection<string> line);
        public abstract void Execute(ref IReadOnlyCollection<object> args);

        protected SharedFunction(ref SharedFunctionsHandler handler)
        {
            Handler = handler;
        }
    }

    internal class FunctionMessage : SharedFunction
    {
        public FunctionMessage(ref SharedFunctionsHandler handler) : base(ref handler) { }
        public override List<string> FuncNames { get; set; }
        public override string FuncName { get; set; } = "Message";
        public override int MinArgs { get; set; }
        public override int MaxArgs { get; set; }

        public override void Run(ref IReadOnlyCollection<string> line)
        {
            switch (line.Count)
            {
                case 2:
                    Handler.ScriptFunctions.Message(line.ElementAt(1));
                    break;
                case 3:
                    Handler.ScriptFunctions.Message(line.ElementAt(1), line.ElementAt(2));
                    break;
                default:
                    goto case 3;
            }
        }

        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }
    }

    internal class FunctionSetVar : SharedFunction
    {
        public override List<string> FuncNames { get; set; }
        public override string FuncName { get; set; } = "SetVar";
        public override int MinArgs { get; set; } = 3;
        public override int MaxArgs { get; set; } = 3;

        public override void Run(ref IReadOnlyCollection<string> line)
        {
            OBMMScriptHandler.Variables[line.ElementAt(1)] = line.ElementAt(2);
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }

        public FunctionSetVar(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionCombinePaths : SharedFunction
    {
        public override List<string> FuncNames { get; set; }
        public override string FuncName { get; set; } = "CombinePaths";
        public override int MinArgs { get; set; } = 4;
        public override int MaxArgs { get; set; } = 4;

        public override void Run(ref IReadOnlyCollection<string> line)
        {
            try
            {
                OBMMScriptHandler.Variables[line.ElementAt(1)] = Path.Combine(line.ElementAt(2), line.ElementAt(3));
            }
            catch
            {
                Handler.Warn("Invalid arguments for 'CombinePaths'");
            }
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }

        public FunctionCombinePaths(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionSubRemoveString : SharedFunction
    {
        public override List<string> FuncNames { get; set; } = new List<string> {"RemoveString", "Substring"};
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
                    Handler.Warn($"Invalid arguments for '{FuncName}'");
                    return;
                }

                OBMMScriptHandler.Variables[line.ElementAt(1)] =
                    remove ? line.ElementAt(2).Remove(start) : line.ElementAt(2).Substring(start);
            }
            else
            {
                if (!int.TryParse(line.ElementAt(3), out int start) || !int.TryParse(line.ElementAt(4), out int end))
                {
                    Handler.Warn($"Invalid arguments for '{FuncName}'");
                    return;
                }

                OBMMScriptHandler.Variables[line.ElementAt(1)] = remove
                    ? line.ElementAt(2).Remove(start, end)
                    : line.ElementAt(2).Substring(start, end);
            }
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }

        public FunctionSubRemoveString(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionStringLength : SharedFunction
    {
        public override List<string> FuncNames { get; set; }
        public override string FuncName { get; set; } = "StringLength";
        public override int MinArgs { get; set; } = 3;
        public override int MaxArgs { get; set; } = 3;

        public override void Run(ref IReadOnlyCollection<string> line)
        {
            OBMMScriptHandler.Variables[line.ElementAt(1)] = line.ElementAt(2).Length.ToString();
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }

        public FunctionStringLength(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionSet : SharedFunction
    {
        private static int Set(List<string> func)
        {
            if (func.Count == 0) throw new OMODFrameworkException($"Empty iSet in script at {OBMMScriptHandler.CLine}");
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

                    func.RemoveRange(index, i - index + 1);
                    func.Insert(index, Set(newFunc).ToString());
                    break;
                }

                if (count != 0)
                    throw new OMODFrameworkException($"Mismatched brackets in script at {OBMMScriptHandler.CLine}");
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

            if (func.Count != 1)
                throw new OMODFrameworkException($"Leftovers in iSet function for script at {OBMMScriptHandler.CLine}");
            return int.Parse(func[0]);
        }

        private static double FSet(List<string> func)
        {
            if (func.Count == 0) throw new OMODFrameworkException($"Empty fSet in script at {OBMMScriptHandler.CLine}");
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

                if (count != 0)
                    throw new OMODFrameworkException($"Mismatched brackets in script at {OBMMScriptHandler.CLine}");
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

            if (func.Count != 1)
                throw new OMODFrameworkException($"Leftovers in iSet function for script at {OBMMScriptHandler.CLine}");
            return double.Parse(func[0]);
        }

        public override List<string> FuncNames { get; set; } = new List<string> {"iSet", "fSet"};
        public override string FuncName { get; set; } = "iSet OR fSet";
        public override int MinArgs { get; set; } = 3;
        public override int MaxArgs { get; set; } = 0;

        public override void Run(ref IReadOnlyCollection<string> line)
        {
            var integer = line.ElementAt(0) == "iSet";
            FuncName = integer ? "iSet" : "fSet";

            var func = new List<string>();
            for (int i = 2; i < line.Count; i++) func.Add(line.ElementAt(i));
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

                OBMMScriptHandler.Variables[line.ElementAt(1)] = result;
            }
            catch
            {
                Handler.Warn($"Invalid arguments for {FuncName}");
            }
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }

        public FunctionSet(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionExecLines : SharedFunction
    {
        public override List<string> FuncNames { get; set; }
        public override string FuncName { get; set; } = "ExecLines";
        public override int MinArgs { get; set; } = 2;
        public override int MaxArgs { get; set; } = 2;

        public override void Run(ref IReadOnlyCollection<string> line)
        {
            line.ElementAt(1).Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)
                .Do(OBMMScriptHandler.ExtraLines.Enqueue);
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }

        public FunctionExecLines(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionLoadOrder : SharedFunction
    {
        public override List<string> FuncNames { get; set; } =
            new List<string> {"LoadEarly", "LoadAfter", "LoadBefore"};

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
                Handler.Warn($"Missing arguments for '{FuncName}'");
                return;
            }

            if (loadEarly && line.Count > 2)
                Handler.Warn($"Unexpected arguments for '{FuncName}'");

            if (loadEarly)
            {
                var plugin = line.ElementAt(1);
                plugin = plugin.ToLower();
                if (!OBMMScriptHandler.Srd.EarlyPlugins.Contains(plugin))
                    OBMMScriptHandler.Srd.EarlyPlugins.Add(plugin);
            }
            else
            {
                OBMMScriptHandler.Srd.LoadOrderSet.Add(new PluginLoadInfo(line.ElementAt(1), line.ElementAt(2),
                    loadAfter));
            }
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }

        public FunctionLoadOrder(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionConflicts : SharedFunction
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
                            Handler.Warn($"Unknown conflict level after '{FuncName}'");
                            break;
                    }

                    goto case 3;
                case 5:
                    Handler.Warn($"Unexpected arguments for '{FuncName}'");
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
                        Handler.Warn($"Arguments for '{FuncName}' could not been parsed");
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
                            Handler.Warn($"Unknown conflict level after '{FuncName}'");
                            break;
                    }

                    goto case 7;
                default:
                    Handler.Warn($"Unexpected arguments for '{FuncName}'");
                    goto case 8;
            }

            cd.Partial = regex;
            if (conflicts)
                OBMMScriptHandler.Srd.ConflictsWith.Add(cd);
            else
                OBMMScriptHandler.Srd.DependsOn.Add(cd);
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }
        public FunctionConflicts(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionUncheckESP : SharedFunction
    {
        public override List<string> FuncNames { get; set; }
        public override string FuncName { get; set; } = "UncheckESP";
        public override int MinArgs { get; set; } = 2;
        public override int MaxArgs { get; set; } = 2;

        public override void Run(ref IReadOnlyCollection<string> line)
        {
            var plugin = line.ElementAt(1);
            if (!File.Exists(Path.Combine(OBMMScriptHandler.Plugins, plugin)))
            {
                Handler.Warn($"Invalid argument for 'UncheckESP': {plugin} does not exist");
                return;
            }

            plugin = plugin.ToLower();
            if (!OBMMScriptHandler.Srd.UncheckedPlugins.Contains(plugin))
                OBMMScriptHandler.Srd.UncheckedPlugins.Add(plugin);
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }
        public FunctionUncheckESP(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionSetDeactivationWarning : SharedFunction
    {
        public override List<string> FuncNames { get; set; }
        public override string FuncName { get; set; } = "SetDeactivationHandler.Warning";
        public override int MinArgs { get; set; } = 3;
        public override int MaxArgs { get; set; } = 3;

        public override void Run(ref IReadOnlyCollection<string> line)
        {
            var plugin = line.ElementAt(1);
            if (!File.Exists(Path.Combine(OBMMScriptHandler.Plugins, plugin)))
            {
                Handler.Warn($"Invalid argument for 'SetDeactivationHandler.Warning'\nFile '{plugin}' does not exist");
                return;
            }

            plugin = plugin.ToLower();

            OBMMScriptHandler.Srd.ESPDeactivation.RemoveWhere(a => a.Plugin == plugin);
            switch (line.ElementAt(2))
            {
                case "Allow":
                    OBMMScriptHandler.Srd.ESPDeactivation.Add(
                        new ScriptESPWarnAgainst(plugin, DeactivationStatus.Allow));
                    break;
                case "Handler.WarnAgainst":
                    OBMMScriptHandler.Srd.ESPDeactivation.Add(new ScriptESPWarnAgainst(plugin,
                        DeactivationStatus.WarnAgainst));
                    break;
                case "Disallow":
                    OBMMScriptHandler.Srd.ESPDeactivation.Add(new ScriptESPWarnAgainst(plugin,
                        DeactivationStatus.Disallow));
                    break;
                default:
                    Handler.Warn("Invalid argument for 'SetDeactivationHandler.Warning'");
                    return;
            }
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }
        public FunctionSetDeactivationWarning(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionEditXML : SharedFunction
    {
        public override List<string> FuncNames { get; set; } = new List<string> {"EditXMLLine", "EditXMLReplace"};
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
                var file = Path.Combine(OBMMScriptHandler.DataFiles, path);
                path = path.ToLower();
                if (!Utils.IsSafeFileName(path) || !File.Exists(file))
                {
                    Handler.Warn($"Invalid filename supplied for '{FuncName}'");
                    return;
                }

                var ext = Path.GetExtension(file);
                if (ext != ".xml" && ext != ".txt" && ext != ".ini" && ext != ".bat")
                {
                    Handler.Warn($"Invalid filename supplied for '{FuncName}'");
                    return;
                }

                var text = File.ReadAllText(file);
                text = text.Replace(line.ElementAt(2), line.ElementAt(3));
                File.WriteAllText(file, text);
            }
            else
            {
                var path = line.ElementAt(1);
                var file = Path.Combine(OBMMScriptHandler.DataFiles, path);

                path = path.ToLower();
                if (!Utils.IsSafeFileName(path) || !File.Exists(file))
                {
                    Handler.Warn($"Invalid filename supplied for '{FuncName}'");
                    return;
                }

                var ext = Path.GetExtension(path);
                if (ext != ".xml" && ext != ".txt" && ext != ".ini" && ext != ".bat")
                {
                    Handler.Warn($"Invalid filename supplied for '{FuncName}'");
                    return;
                }

                if (!int.TryParse(line.ElementAt(2), out var index) || index < 1)
                {
                    Handler.Warn($"Invalid line number supplied for '{FuncName}'");
                    return;
                }

                index -= 1;
                var lines = File.ReadAllLines(file);
                if (lines.Length <= index)
                {
                    Handler.Warn($"Invalid line number supplied for '{FuncName}'");
                    return;
                }

                lines[index] = line.ElementAt(3);
                File.WriteAllLines(file, lines);
            }
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }
        public FunctionEditXML(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionModifyInstall : SharedFunction
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
                var path = Path.Combine(OBMMScriptHandler.Plugins, l);
                if (!File.Exists(path))
                {
                    Handler.Warn($"Invalid argument for '{FuncName}'\nFile '{path}' does not exist");
                    return;
                }

                if (l.IndexOfAny(new[] {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar}) != -1)
                {
                    Handler.Warn(
                        $"Invalid argument for '{FuncName}'\nThis function cannot be used on OBMMScriptHandler.Plugins stored in subdirectories");
                }

                if (install)
                {
                    OBMMScriptHandler.Srd.IgnorePlugins.RemoveWhere(s => s == l);
                    if (!OBMMScriptHandler.Srd.InstallPlugins.Contains(l))
                        OBMMScriptHandler.Srd.InstallPlugins.Add(l);
                }
                else
                {
                    OBMMScriptHandler.Srd.InstallPlugins.RemoveWhere(s => s == l);
                    if (!OBMMScriptHandler.Srd.IgnorePlugins.Contains(l))
                        OBMMScriptHandler.Srd.IgnorePlugins.Add(l);
                }
            }
            else
            {
                var path = Path.Combine(OBMMScriptHandler.DataFiles, l);
                if (!File.Exists(path))
                {
                    Handler.Warn($"Invalid argument for '{FuncName}'\nFile '{path}' does not exist");
                    return;
                }

                if (install)
                {
                    OBMMScriptHandler.Srd.IgnoreData.RemoveWhere(s => s == l);
                    if (!OBMMScriptHandler.Srd.InstallData.Contains(l))
                        OBMMScriptHandler.Srd.InstallData.Add(l);
                }
                else
                {
                    OBMMScriptHandler.Srd.InstallData.RemoveWhere(s => s == l);
                    if (!OBMMScriptHandler.Srd.IgnoreData.Contains(l))
                        OBMMScriptHandler.Srd.IgnoreData.Add(l);
                }
            }
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }
        public FunctionModifyInstall(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionModifyInstallFolder : SharedFunction
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
            var path = Path.Combine(OBMMScriptHandler.DataFiles, validFolderPath);

            if (!Directory.Exists(path))
            {
                Handler.Warn($"Invalid argument for '{FuncName}'\nFolder '{path}' does not exist");
                return;
            }

            if (line.Count > 2)
            {
                switch (line.ElementAt(2))
                {
                    case "True":
                        Directory.GetDirectories(path).Do(d =>
                        {
                            var a = (IReadOnlyCollection<string>)new List<string>
                            {
                                "InstallDataFolder", d.Substring(OBMMScriptHandler.DataFiles.Length), "True"
                            };
                            Run(ref a);
                        });
                        break;
                    case "False":
                        break;
                    default:
                        Handler.Warn($"Invalid argument for '{FuncName}'\nExpected True or False");
                        break;
                }
            }

            Directory.GetFiles(path).Do(f =>
            {
                var name = Path.GetFileName(f);
                if (install)
                {
                    OBMMScriptHandler.Srd.IgnoreData.RemoveWhere(s => s == name);
                    if (!OBMMScriptHandler.Srd.InstallData.Contains(name))
                        OBMMScriptHandler.Srd.InstallData.Add(name);
                }
                else
                {
                    OBMMScriptHandler.Srd.InstallData.RemoveWhere(s => s == name);
                    if (!OBMMScriptHandler.Srd.IgnoreData.Contains(name))
                        OBMMScriptHandler.Srd.IgnoreData.Add(name);
                }
            });
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }
        public FunctionModifyInstallFolder(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionCopyDataFile : SharedFunction
    {
        public override List<string> FuncNames { get; set; } = new List<string> {"CopyPlugin", "CopyDataFile"};
        public override string FuncName { get; set; }
        public override int MinArgs { get; set; } = 3;
        public override int MaxArgs { get; set; } = 3;

        public override void Run(ref IReadOnlyCollection<string> line)
        {
            var plugin = line.ElementAt(0).Contains("Plugin");
            FuncName = plugin ? "CopyPlugin" : "CopyDataFile";

            var from = line.ElementAt(1);
            var to = line.ElementAt(2);

            if (!Utils.IsSafeFileName(from) || !Utils.IsSafeFileName(to))
            {
                Handler.Warn($"Invalid argument for '{FuncName}'");
                return;
            }

            if (from == to)
            {
                Handler.Warn($"Invalid argument for '{FuncName}'\nYou can not copy a file over itself");
                return;
            }

            if (plugin)
            {
                var path = Path.Combine(OBMMScriptHandler.Plugins, from);
                if (!File.Exists(path))
                {
                    Handler.Warn($"Invalid argument for '{FuncName}'\nFile '{from}' does not exist");
                    return;
                }

                if (to.IndexOfAny(new[] {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar}) != -1)
                {
                    Handler.Warn("OBMMScriptHandler.Plugins cannot be copied to subdirectories of the data folder");
                    return;
                }

                if (!(to.EndsWith("esp") || to.EndsWith(".esm")))
                {
                    Handler.Warn("Copied OBMMScriptHandler.Plugins must have a .esp or .esm extension");
                    return;
                }
            }
            else
            {
                var path = Path.Combine(OBMMScriptHandler.DataFiles, from);
                if (!File.Exists(path))
                {
                    Handler.Warn($"Invalid argument for '{FuncName}'\nFile '{from}' does not exist");
                    return;
                }

                if (to.EndsWith("esp") || to.EndsWith(".esm"))
                {
                    Handler.Warn("Copied data files cannot have a .esp or .esm extension");
                    return;
                }
            }

            if (plugin)
            {
                OBMMScriptHandler.Srd.CopyPlugins.RemoveWhere(s => s.CopyTo == to.ToLower());
                OBMMScriptHandler.Srd.CopyPlugins.Add(new ScriptCopyDataFile(from.ToLower(), to.ToLower()));
            }
            else
            {
                OBMMScriptHandler.Srd.CopyDataFiles.RemoveWhere(s => s.CopyTo == to.ToLower());
                OBMMScriptHandler.Srd.CopyDataFiles.Add(new ScriptCopyDataFile(from.ToLower(), to.ToLower()));
            }
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }
        public FunctionCopyDataFile(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionCopyDataFolder : SharedFunction
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
                Handler.Warn("Invalid argument for 'CopyDataFolder'");
                return;
            }

            var from = Path.Combine(OBMMScriptHandler.DataFiles, validFrom);
            var to = Path.Combine(OBMMScriptHandler.DataFiles, validTo);

            if (!Directory.Exists(from))
            {
                Handler.Warn($"Invalid argument for 'CopyDataFolder'\nFolder '{from}' does not exist!");
                return;
            }

            if (from == to)
            {
                Handler.Warn("Invalid argument for 'CopyDataFolder'\nYou cannot copy a folder over itself");
                return;
            }

            var collection = line;
            if (line.Count >= 4)
            {
                switch (line.ElementAt(3))
                {
                    case "True":
                        Directory.GetDirectories(from).Do(d =>
                        {
                            var arg2 = d.Substring(OBMMScriptHandler.DataFiles.Length);
                            var l = OBMMScriptHandler.DataFiles.Length + collection.ElementAt(1).Length;
                            if (arg2.StartsWith("\\"))
                            {
                                arg2 = arg2.Substring(1);
                                l++;
                            }
                            var t = d.Substring(l);
                            var arg3 = collection.ElementAt(2) + t;
                            var arg = (IReadOnlyCollection<string>)new List<string> {"", arg2, arg3, "True"};
                            Run(ref arg);
                        });
                        break;
                    case "False":
                        break;
                    default:
                        Handler.Warn("Invalid argument for 'CopyDataFolder'\nExpected True or False");
                        return;
                }
            }

            Directory.GetFiles(from).Do(f =>
            {
                var fFrom = Path.Combine(collection.ElementAt(1), Path.GetFileName(f));
                var fTo = Path.Combine(collection.ElementAt(2), Path.GetFileName(f)).ToLower();

                OBMMScriptHandler.Srd.CopyDataFiles.RemoveWhere(s => s.CopyTo == fTo);

                OBMMScriptHandler.Srd.CopyDataFiles.Add(new ScriptCopyDataFile(fFrom, fTo));
            });
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }
        public FunctionCopyDataFolder(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionGetDirectoryFileName : SharedFunction
    {
        public override List<string> FuncNames { get; set; } = new List<string>
        {
            "GetDirectoryName", "GetFileName", "GetFolderName", "GetFileNameWithoutExtension"
        };

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
                if (file)
                    OBMMScriptHandler.Variables[line.ElementAt(1)] = Path.GetFileName(line.ElementAt(2));
                else
                    OBMMScriptHandler.Variables[line.ElementAt(1)] = Path.GetDirectoryName(line.ElementAt(2));

                if (extension)
                    OBMMScriptHandler.Variables[line.ElementAt(1)] =
                        Path.GetFileNameWithoutExtension(line.ElementAt(2));
            }
            catch
            {
                Handler.Warn($"Invalid argument for '{FuncName}'");
            }
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }
        public FunctionGetDirectoryFileName(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionPatch : SharedFunction
    {
        public override List<string> FuncNames { get; set; } = new List<string> {"PatchPlugin", "PatchDataFile"};
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
                Handler.Warn($"Invalid argument for '{FuncName}'");
                return;
            }

            var pathFrom = plugin
                ? Path.Combine(OBMMScriptHandler.Plugins, from)
                : Path.Combine(OBMMScriptHandler.DataFiles, from);
            if (plugin)
            {
                if (!File.Exists(pathFrom))
                {
                    Handler.Warn($"Invalid argument for 'PatchPlugin'\nFile '{from}' does not exist");
                    return;
                }

                if (to.IndexOfAny(new[] {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar}) != -1)
                {
                    Handler.Warn("OBMMScriptHandler.Plugins cannot be copied to subdirectories of the data folder");

                    return;
                }

                if (!(to.EndsWith(".esp") || to.EndsWith(".esm")))
                {
                    Handler.Warn("OBMMScriptHandler.Plugins must have a .esp or .esm extension");
                    return;
                }
            }
            else
            {
                if (!File.Exists(pathFrom))
                {
                    Handler.Warn($"Invalid argument to PatchDataFile\nFile '{from}' does not exist");
                    return;
                }

                if (to.EndsWith(".esp") || to.EndsWith(".esm"))
                {
                    Handler.Warn("Data files cannot have a .esp or .esm extension");
                    return;
                }
            }

            Work(from, to, plugin, OBMMScriptHandler.Plugins, OBMMScriptHandler.DataFiles, ref OBMMScriptHandler.Srd, ref line, true);
        }

        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            if (args.Count != 6)
                return;

            if (!(args.ElementAt(0) is string from))
                return;

            if (!(args.ElementAt(1) is string to))
                return;

            if (!(args.ElementAt(2) is bool plugin))
                return;

            if (!(args.ElementAt(3) is string dataPath))
                return;

            if (!(args.ElementAt(4) is string pluginPath))
                return;

            if (!(args.ElementAt(5) is ScriptReturnData srd))
                return;

            IReadOnlyCollection<string> a = null;

            Work(from, to, plugin, dataPath, pluginPath, ref srd, ref a);
        }

        private void Work(string from, string to, bool plugin, string dataFilesPath, string pluginPath, ref ScriptReturnData srd, ref IReadOnlyCollection<string> line, bool obmm = false)
        {
            var pathFrom = plugin
                ? Path.Combine(pluginPath, from)
                : Path.Combine(dataFilesPath, from);

            if (Framework.Settings.ScriptExecutionSettings.PatchWithInterface)
            {
                Handler.ScriptFunctions.Patch(pathFrom, to);
            }
            else
            {
                if (Framework.Settings.ScriptExecutionSettings.UseSafePatching)
                {
                    if (string.IsNullOrWhiteSpace(Framework.Settings.ScriptExecutionSettings.OblivionGamePath))
                        throw new OMODFrameworkException(
                            $"{Framework.Settings.ScriptExecutionSettings.OblivionGamePath} can not be null or whitespace!");

                    var patchFolder = Path.Combine(Framework.Settings.ScriptExecutionSettings.OblivionDataPath, "Patch");

                    if (!Directory.Exists(patchFolder))
                        Directory.CreateDirectory(patchFolder);

                    var patchPath = Path.Combine(patchFolder, to);
                    if (File.Exists(patchPath))
                        throw new OMODFrameworkException($"The file {patchPath} already exists");

                    var toDataPath = Path.Combine(Framework.Settings.ScriptExecutionSettings.OblivionDataPath, to);
                    DateTime toTimeStamp = default;
                    if (File.Exists(toDataPath))
                    {
                        toTimeStamp = File.GetLastWriteTime(toDataPath);
                    }

                    try
                    {
                        File.Copy(pathFrom, patchPath);
                        if (toTimeStamp != default)
                            File.SetLastWriteTime(patchPath, toTimeStamp);
                    }
                    catch (Exception e)
                    {
                        throw new OMODFrameworkException(
                            $"The file {pathFrom} could not be copied to {patchPath}\n{e}");
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(Framework.Settings.ScriptExecutionSettings.OblivionGamePath))
                        throw new OMODFrameworkException(
                            $"{Framework.Settings.ScriptExecutionSettings.OblivionGamePath} can not be null or whitespace!");

                    var dataPath = Path.Combine(Framework.Settings.ScriptExecutionSettings.OblivionDataPath, to);
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
                    else if (obmm && (line.Count < 4 || line.ElementAt(3) != "True")) return;

                    try
                    {
                        File.Move(pathFrom, dataPath);
                        File.SetLastWriteTime(dataPath, timeStamp);
                    }
                    catch (Exception e)
                    {
                        throw new OMODFrameworkException($"The file {pathFrom} could not be moved to {dataPath}\n{e}");
                    }
                }
            }
        }

        public FunctionPatch(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionEditShader : SharedFunction
    {
        public override List<string> FuncNames { get; set; } = new List<string> {"EditSDP", "EditShader"};
        public override string FuncName { get; set; }
        public override int MinArgs { get; set; } = 4;
        public override int MaxArgs { get; set; } = 4;

        public override void Run(ref IReadOnlyCollection<string> line)
        {
            FuncName = line.ElementAt(0);
            var shaderPath = Path.Combine(OBMMScriptHandler.DataFiles, line.ElementAt(3));
            if (!Utils.IsSafeFileName(line.ElementAt(3)))
            {
                Handler.Warn($"Invalid argument for 'EditShader'\n'{line.ElementAt(3)}' is not a valid file name");
                return;
            }

            if (!File.Exists(shaderPath))
            {
                Handler.Warn($"Invalid argument for 'EditShader'\nFile '{line.ElementAt(3)}' does not exist");
                return;
            }

            if (!byte.TryParse(line.ElementAt(1), out var package))
            {
                Handler.Warn(
                    $"Invalid argument for function 'EditShader'\n'{line.ElementAt(1)}' is not a valid shader package ID");
                return;
            }

            OBMMScriptHandler.Srd.SDPEdits.Add(new SDPEditInfo(package, line.ElementAt(2), shaderPath));
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }

        public FunctionEditShader(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionEditINI : SharedFunction
    {
        public override List<string> FuncNames { get; set; }
        public override string FuncName { get; set; } = "EditINI";
        public override int MinArgs { get; set; } = 4;
        public override int MaxArgs { get; set; } = 4;

        public override void Run(ref IReadOnlyCollection<string> line)
        {
            OBMMScriptHandler.Srd.INIEdits.Add(new INIEditInfo(line.ElementAt(1), line.ElementAt(2),
                line.ElementAt(3)));
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }

        public FunctionEditINI(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionSetESPVar : SharedFunction
    {
        public override List<string> FuncNames { get; set; } = new List<string> {"SetGMST", "SetGlobal"};
        public override string FuncName { get; set; }
        public override int MinArgs { get; set; } = 4;
        public override int MaxArgs { get; set; } = 4;

        public override void Run(ref IReadOnlyCollection<string> line)
        {
            var gmst = line.ElementAt(0) == "SetGMST";
            FuncName = line.ElementAt(0);

            if (!Utils.IsSafeFileName(line.ElementAt(1)))
            {
                Handler.Warn($"Illegal plugin name supplied to '{FuncName}'");
                return;
            }

            if (!File.Exists(Path.Combine(OBMMScriptHandler.Plugins, line.ElementAt(1))))
            {
                Handler.Warn($"Invalid argument for '{FuncName}'\nFile '{line.ElementAt(1)}' does not exist");
                return;
            }

            OBMMScriptHandler.Srd.ESPEdits.Add(new ScriptESPEdit(gmst, line.ElementAt(1).ToLower(),
                line.ElementAt(2).ToLower(),
                line.ElementAt(3)));
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }

        public FunctionSetESPVar(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionSetESPData : SharedFunction
    {
        public override List<string> FuncNames { get; set; } = new List<string>
        {
            "SetPluginByte",
            "SetPluginShort",
            "SetPluginLong",
            "SetPluginFloat",
            "SetPluginInt"
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
                Handler.Warn($"Illegal plugin name supplied to '{FuncName}'");
                return;
            }

            var pluginPath = Path.Combine(OBMMScriptHandler.Plugins, plugin);
            if (!File.Exists(pluginPath))
            {
                Handler.Warn($"Invalid argument for '{FuncName}'\nFile {plugin} does not exist");
                return;
            }

            byte[] data = null;
            if (!long.TryParse(line.ElementAt(2), out var offset) || offset < 0)
            {
                Handler.Warn($"Invalid argument for '{FuncName}'\nOffset {line.ElementAt(2)} is not valid");
                return;
            }

            var val = line.ElementAt(3);
            if (FuncName.EndsWith("Byte"))
            {
                if (!byte.TryParse(val, out var value))
                {
                    Handler.Warn($"Invalid argument for '{FuncName}'\nValue '{val}' is not valid");
                    return;
                }

                data = BitConverter.GetBytes(value);
            }

            if (FuncName.EndsWith("Short"))
            {
                if (!short.TryParse(val, out var value))
                {
                    Handler.Warn($"Invalid argument for '{FuncName}'\nValue '{val}' is not valid");
                    return;
                }

                data = BitConverter.GetBytes(value);
            }

            if (FuncName.EndsWith("Int"))
            {
                if (!int.TryParse(val, out var value))
                {
                    Handler.Warn($"Invalid argument for '{FuncName}'\nValue '{val}' is not valid");
                    return;
                }

                data = BitConverter.GetBytes(value);
            }

            if (FuncName.EndsWith("Long"))
            {
                if (!long.TryParse(val, out var value))
                {
                    Handler.Warn($"Invalid argument for '{FuncName}'\nValue '{val}' is not valid");
                    return;
                }

                data = BitConverter.GetBytes(value);
            }

            if (FuncName.EndsWith("Float"))
            {
                if (!float.TryParse(val, out var value))
                {
                    Handler.Warn($"Invalid argument for '{FuncName}'\nValue '{val}' is not valid");
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
                    Handler.Warn($"Invalid argument for '{FuncName}'\nOffset {line.ElementAt(2)} is out of range");
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
                        $"Could not write to file {pluginPath} in '{FuncName}' at {OBMMScriptHandler.CLine}\n{e}");
                }
            }
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }

        public FunctionSetESPData(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionInputString : SharedFunction
    {
        public override List<string> FuncNames { get; set; }
        public override string FuncName { get; set; } = "InputString";
        public override int MinArgs { get; set; } = 2;
        public override int MaxArgs { get; set; } = 4;

        public override void Run(ref IReadOnlyCollection<string> line)
        {
            var title = line.Count > 2 ? line.ElementAt(2) : "";
            var initialText = line.Count > 3 ? line.ElementAt(3) : "";

            var result = Handler.ScriptFunctions.InputString(title, initialText);
            OBMMScriptHandler.Variables[line.ElementAt(1)] = result ?? "";
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }

        public FunctionInputString(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionDisplayFile : SharedFunction
    {
        public override List<string> FuncNames { get; set; } = new List<string> {"DisplayText", "DisplayImage"};
        public override string FuncName { get; set; }
        public override int MinArgs { get; set; } = 2;
        public override int MaxArgs { get; set; } = 3;

        public override void Run(ref IReadOnlyCollection<string> line)
        {
            var image = line.ElementAt(0) == "DisplayImage";
            FuncName = line.ElementAt(0);

            if (!Utils.IsSafeFileName(line.ElementAt(1)))
            {
                Handler.Warn($"Illegal path supplied to '{FuncName}'");
                return;
            }

            var path = Path.Combine(OBMMScriptHandler.DataFiles, line.ElementAt(1));
            if (!File.Exists(path))
            {
                Handler.Warn($"Invalid argument for '{FuncName}'\nFile {path} does not exist");
                return;
            }

            var title = line.Count > 2 ? line.ElementAt(2) : line.ElementAt(1);

            if (image)
                Handler.ScriptFunctions.DisplayImage(path, title);
            else
            {
                var text = File.ReadAllText(path, Encoding.UTF8);
                Handler.ScriptFunctions.DisplayText(text, title);
            }
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }

        public FunctionDisplayFile(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionRegisterBSA : SharedFunction
    {
        public override List<string> FuncNames { get; set; } = new List<string> {"RegisterBSA", "UnregisterBSA"};
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
                Handler.Warn(
                    $"Invalid argument for '{FuncName}'\nBSA file names are not allowed to include the characters ',' '=' or ';'");
                return;
            }

            if (register && !OBMMScriptHandler.Srd.RegisterBSASet.Contains(esp))
                OBMMScriptHandler.Srd.RegisterBSASet.Add(esp);
            else
                OBMMScriptHandler.Srd.RegisterBSASet.RemoveWhere(s => s == esp);
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }

        public FunctionRegisterBSA(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionReadINI : SharedFunction
    {
        public override List<string> FuncNames { get; set; }
        public override string FuncName { get; set; } = "ReadINI";
        public override int MinArgs { get; set; } = 4;
        public override int MaxArgs { get; set; } = 4;

        public override void Run(ref IReadOnlyCollection<string> line)
        {
            if (Framework.Settings.ScriptExecutionSettings.ReadINIWithInterface)
            {
                var s = Handler.ScriptFunctions.ReadOblivionINI(line.ElementAt(2), line.ElementAt(3));
                OBMMScriptHandler.Variables[line.ElementAt(1)] =
                    s ?? throw new OMODFrameworkException(
                        "Could not read the oblivion.ini file using the function IScriptFunctions.ReadOblivionINI");
            }
            else
            {
                OBMMScriptHandler.Variables[line.ElementAt(1)] =
                    OblivionINI.GetINIValue(line.ElementAt(2), line.ElementAt(3));
            }
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }

        public FunctionReadINI(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }

    internal class FunctionReadRenderer : SharedFunction
    {
        public override List<string> FuncNames { get; set; }
        public override string FuncName { get; set; } = "ReadRendererInfo";
        public override int MinArgs { get; set; } = 3;
        public override int MaxArgs { get; set; } = 3;

        public override void Run(ref IReadOnlyCollection<string> line)
        {
            if (Framework.Settings.ScriptExecutionSettings.ReadRendererInfoWithInterface)
            {
                var s = Handler.ScriptFunctions.ReadRendererInfo(line.ElementAt(2));
                OBMMScriptHandler.Variables[line.ElementAt(1)] =
                    s ?? throw new OMODFrameworkException(
                        "Could not read the RenderInfo.txt file using the function IScriptFunctions.ReadRendererInfo");
            }
            else
            {
                OBMMScriptHandler.Variables[line.ElementAt(1)] = OblivionRenderInfo.GetInfo(line.ElementAt(2));
            }
        }
        public override void Execute(ref IReadOnlyCollection<object> args)
        {
            throw new NotImplementedException();
        }

        public FunctionReadRenderer(ref SharedFunctionsHandler handler) : base(ref handler) { }
    }
}
