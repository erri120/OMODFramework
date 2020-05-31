using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OMODFramework.Scripting
{
    public partial class OBMMScriptHandler : AScriptHandler
    {
        private ScriptReturnData _srd = null!;
        private IScriptSettings _settings = null!;
        private ScriptFunctions _scriptFunctions = null!;

        private HashSet<Token> _tokens = new HashSet<Token>();
        private readonly Dictionary<string, string> _variables = new Dictionary<string, string>();
        private readonly Stack<Token> _stack = new Stack<Token>();
        //private readonly Queue<Token> _queue = new Queue<Token>();

        internal override ScriptReturnData Execute(OMOD omod, string script, IScriptSettings settings)
        {
            _settings = settings;
            _srd = new ScriptReturnData();
            _scriptFunctions = new ScriptFunctions(_settings, omod, _srd);

            TokenizeScript(script);
            ParseScript();

            return _srd;
        }

        private string ReplaceWithVariable(string s)
        {
            if (!s.Contains("%") || s.Count(x => x == '%') < 2)
                return s;

            var split = s.Split("%");
            var result = s;
            for (var i = 1; i < split.Length; i += 2)
            {
                var current = split[i];
                if (!_variables.ContainsKey(current))
                    throw new NotImplementedException();
                if(!_variables.TryGetValue(current, out var value))
                    throw new NotImplementedException();
                result = result.Replace($"%{current}%", value);
            }

            return result;
        }

        private void ParseScript()
        {
            var tokens = _tokens.Where(x => x.Type != TokenType.Commend).ToHashSet();
            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens.ElementAt(i);
                /*if (token.Type == TokenType.Commend)
                    continue;*/

                if (token.Type == TokenType.EndFor)
                {
                    if(_stack.All(x => x.Type != TokenType.For))
                        throw new NotImplementedException();
                    var forToken = (ForToken)_stack.First(x => x.Type == TokenType.For);
                    if (forToken.EnumerationType == ForToken.ForEnumerationType.Count)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        if (forToken.Enumerable.Count() - 1 != forToken.Current)
                        {
                            /*
                             * +2 because +1 for the EndToken and another +1 because the for loop adds +1 on each iteration
                             * meaning that if we are at index 11 and token is EndFor with For being at index 6
                             * we want to reduce i (atm 11) by 5 so we end up at 6 which becomes 7 in the next iteration
                             */
                            i -= forToken.ChildTokens+2;
                            forToken.ChildTokens = 0;
                            forToken.Current++;
                            _variables.AddOrReplace(forToken.Variable, forToken.Enumerable.ElementAt(forToken.Current));
                            continue;
                        }
                    }
                }

                ParseToken(token);
            }
        }

        private void ParseToken(Token token)
        {
            if (token is EndFlowToken)
            {
                _stack.Pop();
                return;
            }

            if (_stack.Count != 0)
            {
                if (_stack.Any(x => x.Type == TokenType.For))
                {
                    var forToken = (ForToken) _stack.First(x => x.Type == TokenType.For);
                    forToken.ChildTokens++;
                }

                if (_stack.Peek() is IfToken ifToken)
                {
                    if (!ifToken.IsTrue && token.Type != TokenType.Else)
                        return;
                }

                if (_stack.Peek() is CaseToken caseToken)
                {
                    if (!caseToken.IsTrue && token.Type != TokenType.Break)
                        return;
                }
            }

            if (token is FatalErrorToken)
                throw new NotImplementedException();

            switch (token.Type)
            {
                case TokenType.If:
                case TokenType.IfNot:
                {
                    var ifToken = (IfToken)token;
                    var args = ifToken.Arguments.Select(ReplaceWithVariable).ToList();
                    switch (ifToken.ConditionType)
                    {
                        case IfToken.IfConditionType.DialogYesNo:
                            ifToken.IsTrue = args.Count == 1
                                ? _scriptFunctions.DialogYesNo(args[0])
                                : _scriptFunctions.DialogYesNo(args[0], args[1]);
                            break;

                        case IfToken.IfConditionType.DataFileExists:
                            ifToken.IsTrue = _scriptFunctions.DataFileExists(args[0]);
                            break;

                        case IfToken.IfConditionType.VersionGreaterThan:
                        case IfToken.IfConditionType.VersionLessThan:
                            {
                                var version = new Version(ifToken.Arguments[0]);
                                ifToken.IsTrue = ifToken.ConditionType == IfToken.IfConditionType.VersionGreaterThan
                                    ? version < _scriptFunctions.GetOBMMVersion()
                                    : version > _scriptFunctions.GetOBMMVersion();
                                break;
                            }

                        case IfToken.IfConditionType.ScriptExtenderPresent:
                            ifToken.IsTrue = _settings.ScriptFunctions.HasScriptExtender();
                            break;

                        case IfToken.IfConditionType.ScriptExtenderNewerThan:
                            {
                                var version = new Version(args[0]);
                                ifToken.IsTrue = version < _settings.ScriptFunctions.ScriptExtenderVersion();
                                break;
                            }

                        case IfToken.IfConditionType.GraphicsExtenderPresent:
                            ifToken.IsTrue = _settings.ScriptFunctions.HasGraphicsExtender();
                            break;

                        case IfToken.IfConditionType.GraphicsExtenderNewerThan:
                            {
                                var version = new Version(args[0]);
                                ifToken.IsTrue = version < _settings.ScriptFunctions.GraphicsExtenderVersion();
                                break;
                            }

                        case IfToken.IfConditionType.OblivionNewerThan:
                            {
                                var version = new Version(args[0]);
                                ifToken.IsTrue = version < _settings.ScriptFunctions.OblivionVersion();
                                break;
                            }

                        case IfToken.IfConditionType.Equal:
                        case IfToken.IfConditionType.GreaterThan:
                        case IfToken.IfConditionType.GreaterEqual:
                        {
                            if (!int.TryParse(args[0], out var i1))
                                throw new NotImplementedException();
                            if (!int.TryParse(args[1], out var i2))
                                throw new NotImplementedException();

                            ifToken.IsTrue = ifToken.ConditionType switch
                            {
                                IfToken.IfConditionType.Equal => i1 == i2,
                                IfToken.IfConditionType.GreaterThan => i1 < i2,
                                IfToken.IfConditionType.GreaterEqual => i1 <= i2,
                                _ => throw new ArgumentOutOfRangeException()
                            };

                            break;
                        }
                        case IfToken.IfConditionType.fGreaterEqual:
                        case IfToken.IfConditionType.fGreaterThan:
                        {
                            if (!float.TryParse(args[0], out var f1))
                                throw new NotImplementedException();
                            if (!float.TryParse(args[1], out var f2))
                                throw new NotImplementedException();

                            ifToken.IsTrue = ifToken.ConditionType switch
                            {
                                IfToken.IfConditionType.fGreaterEqual => f1 <= f2,
                                IfToken.IfConditionType.fGreaterThan => f1 < f2,
                                _ => throw new ArgumentOutOfRangeException()
                            };

                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (ifToken.Not)
                        ifToken.IsTrue = !ifToken.IsTrue;
                    _stack.Push(ifToken);
                    break;
                }
                case TokenType.Else:
                    throw new NotImplementedException();
                case TokenType.For:
                {
                    var forToken = (ForToken)token;
                    switch (forToken.EnumerationType)
                    {
                        case ForToken.ForEnumerationType.Count:
                            throw new NotImplementedException();

                        case ForToken.ForEnumerationType.DataFolder:
                            throw new NotImplementedException();

                        case ForToken.ForEnumerationType.PluginFolder:
                            throw new NotImplementedException();

                        case ForToken.ForEnumerationType.DataFile:
                        {
                            forToken.Enumerable = _scriptFunctions.GetDataFiles(forToken.FolderPath,
                                forToken.SearchString, forToken.Recursive);
                            _variables.AddOrReplace(forToken.Variable, forToken.Enumerable.ElementAt(0));
                            break;
                        }

                        case ForToken.ForEnumerationType.Plugin:
                            throw new NotImplementedException();
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    _stack.Push(forToken);
                    break;
                }
                case TokenType.Break:
                {
                    if (!(_stack.Peek() is CaseToken))
                        throw new NotImplementedException();

                    _stack.Pop();
                    break;
                }
                case TokenType.Case:
                {
                    var caseToken = (CaseToken) token;
                    if (!(_stack.Peek() is SelectToken selectToken))
                        throw new NotImplementedException();

                    if (selectToken.Results.Contains(caseToken.Value))
                        caseToken.IsTrue = true;

                    _stack.Push(caseToken);
                    break;
                }
                case TokenType.Default:
                    throw new NotImplementedException();
                case TokenType.Continue:
                    throw new NotImplementedException();
                case TokenType.Exit:
                    throw new NotImplementedException();
                case TokenType.Label:
                    throw new NotImplementedException();
                case TokenType.Goto:
                    throw new NotImplementedException();
                case TokenType.Select:
                case TokenType.SelectMany:
                case TokenType.SelectWithPreview:
                case TokenType.SelectManyWithPreview:
                case TokenType.SelectWithDescriptions:
                case TokenType.SelectManyWithDescriptions:
                case TokenType.SelectWithDescriptionsAndPreviews:
                case TokenType.SelectManyWithDescriptionsAndPreviews:
                {
                    var selectToken = (SelectToken) token;
                    selectToken.Results = _scriptFunctions.Select(selectToken.Items, selectToken.Previews,
                        selectToken.Descriptions, selectToken.Title, selectToken.IsMany)
                        .Select(x => x.StartsWith("|") ? x[1..] : x)
                        .ToList();

                    _stack.Push(selectToken);
                    break;
                }
                case TokenType.SelectVar:
                case TokenType.SelectString:
                    throw new NotImplementedException();
                case TokenType.Message:
                {
                    var iToken = (InstructionToken) token;
                    if(iToken.Instructions.Count == 1)
                        _settings.ScriptFunctions.Message(iToken.Instructions[0]);
                    else
                        _settings.ScriptFunctions.Message(iToken.Instructions[0], iToken.Instructions[1]);
                    break;
                }
                case TokenType.LoadEarly:
                    throw new NotImplementedException();
                case TokenType.LoadBefore:
                    throw new NotImplementedException();
                case TokenType.LoadAfter:
                    throw new NotImplementedException();
                case TokenType.ConflictsWith:
                    throw new NotImplementedException();
                case TokenType.DependsOn:
                    throw new NotImplementedException();
                case TokenType.ConflictsWithRegex:
                    throw new NotImplementedException();
                case TokenType.DependsOnRegex:
                    throw new NotImplementedException();
                case TokenType.DontInstallAnyPlugins:
                    _scriptFunctions.DontInstallAnyPlugins();
                    break;
                case TokenType.DontInstallAnyDataFiles:
                    _scriptFunctions.DontInstallAnyDataFiles();
                    break;
                case TokenType.InstallAllPlugins:
                    _scriptFunctions.InstallAllPlugins();
                    break;
                case TokenType.InstallAllDataFiles:
                    _scriptFunctions.InstallAllDataFiles();
                    break;
                case TokenType.InstallPlugin:
                    throw new NotImplementedException();
                case TokenType.DontInstallPlugin:
                    throw new NotImplementedException();
                case TokenType.InstallDataFile:
                    throw new NotImplementedException();
                case TokenType.DontInstallDataFile:
                    throw new NotImplementedException();
                case TokenType.DontInstallDataFolder:
                    throw new NotImplementedException();
                case TokenType.InstallDataFolder:
                    throw new NotImplementedException();
                case TokenType.RegisterBSA:
                    throw new NotImplementedException();
                case TokenType.UnregisterBSA:
                    throw new NotImplementedException();
                case TokenType.Return:
                    throw new NotImplementedException();
                case TokenType.UncheckESP:
                    throw new NotImplementedException();
                case TokenType.SetDeactivationWarning:
                    throw new NotImplementedException();
                case TokenType.CopyDataFile:
                case TokenType.CopyPlugin:
                case TokenType.CopyDataFolder:
                {
                    var iToken = (InstructionToken) token;
                    var from = iToken.Instructions[0];
                    var to = iToken.Instructions[1];

                    switch (token.Type)
                    {
                        case TokenType.CopyDataFile:
                            _scriptFunctions.CopyDataFile(from, to);
                            break;
                        case TokenType.CopyPlugin:
                            _scriptFunctions.CopyPlugin(from, to);
                            break;
                        case TokenType.CopyDataFolder:
                            if(iToken.Instructions.Count == 3)
                                _scriptFunctions.CopyDataFolder(from, to, iToken.Instructions[2].ToLower() == "true");
                            else
                                _scriptFunctions.CopyDataFolder(from, to, false);
                            break;
                    }

                    break;
                }
                case TokenType.PatchPlugin:
                    throw new NotImplementedException();
                case TokenType.PatchDataFile:
                    throw new NotImplementedException();
                case TokenType.EditINI:
                    throw new NotImplementedException();
                case TokenType.EditSDP:
                    throw new NotImplementedException();
                case TokenType.EditShader:
                    throw new NotImplementedException();
                case TokenType.SetGMST:
                    throw new NotImplementedException();
                case TokenType.SetGlobal:
                    throw new NotImplementedException();
                case TokenType.SetPluginByte:
                    throw new NotImplementedException();
                case TokenType.SetPluginShort:
                    throw new NotImplementedException();
                case TokenType.SetPluginInt:
                    throw new NotImplementedException();
                case TokenType.SetPluginLong:
                    throw new NotImplementedException();
                case TokenType.SetPluginFloat:
                    throw new NotImplementedException();
                case TokenType.DisplayImage:
                    throw new NotImplementedException();
                case TokenType.DisplayText:
                    throw new NotImplementedException();
                case TokenType.SetVar:
                {
                    var setVarToken = (SetVarToken)token;
                    _variables.AddOrReplace(setVarToken.Variable, setVarToken.Value);
                    break;
                }
                case TokenType.GetFolderName:
                case TokenType.GetDirectoryName:
                case TokenType.GetFileName:
                case TokenType.GetFileNameWithoutExtension:
                    {
                    var getFileNameToken = (InstructionToken) token;
                    var instructions = getFileNameToken.Instructions.Select(ReplaceWithVariable).ToList();
                    var variable = instructions[0];
                    if(token.Type == TokenType.GetFolderName || token.Type == TokenType.GetDirectoryName)
                        _variables.AddOrReplace(variable, Path.GetFileName(instructions[1]));
                    else
                    {
                        var value = token.Type == TokenType.GetFileName
                            ? Path.GetFileName(instructions[1])
                            : Path.GetFileNameWithoutExtension(instructions[1]);
                        _variables.AddOrReplace(variable, value);
                    }
                    break;
                }
                case TokenType.CombinePaths:
                    throw new NotImplementedException();
                case TokenType.Substring:
                    throw new NotImplementedException();
                case TokenType.RemoveString:
                    throw new NotImplementedException();
                case TokenType.StringLength:
                    throw new NotImplementedException();
                case TokenType.InputString:
                    throw new NotImplementedException();
                case TokenType.ReadINI:
                    throw new NotImplementedException();
                case TokenType.ReadRendererInfo:
                    throw new NotImplementedException();
                case TokenType.ExecLines:
                    throw new NotImplementedException();
                case TokenType.iSet:
                    throw new NotImplementedException();
                case TokenType.fSet:
                    throw new NotImplementedException();
                case TokenType.EditXMLLine:
                    throw new NotImplementedException();
                case TokenType.EditXMLReplace:
                    throw new NotImplementedException();
                case TokenType.AllowRunOnLines:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
