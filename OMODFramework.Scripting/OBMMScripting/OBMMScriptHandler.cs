using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace OMODFramework.Scripting
{
    public partial class OBMMScriptHandler : AScriptHandler
    {
        private ScriptReturnData _srd = null!;
        private IScriptSettings _settings = null!;
        private ScriptFunctions _scriptFunctions = null!;
        private OMOD _omod = null!;

        private HashSet<Token> _tokens = new HashSet<Token>();
        private readonly Dictionary<string, string> _variables = new Dictionary<string, string>();
        private readonly Stack<StartFlowToken> _stack = new Stack<StartFlowToken>();
        private readonly List<GotoLabelToken> LabelTokens = new List<GotoLabelToken>();

        private bool Return { get; set; }

        internal override ScriptReturnData Execute(OMOD omod, string script, IScriptSettings settings)
        {
            _settings = settings;
            _srd = new ScriptReturnData();
            _omod = omod;
            _scriptFunctions = new ScriptFunctions(_settings, omod, _srd);

            TokenizeScript(script);
            ParseScript();

            FinishUpReturnData();

            return _srd;
        }

        /// <summary>
        /// Utility Function that will clean up the script return data
        /// </summary>
        private void FinishUpReturnData()
        {
            _srd.DataFiles = _srd.DataFiles.DistinctBy(x => x.Output).ToList();
            _srd.PluginFiles = _srd.PluginFiles.DistinctBy(x => x.Output).ToList();

            if (_srd.UnCheckedPlugins.Count != 0)
            {
                if (_omod.PluginsList == null)
                    throw new ScriptingNullListException(false);

                _srd.UnCheckedPlugins.Do(p =>
                {
                    if (_srd.PluginFiles.Any(x => x.Output.Equals(p, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        _srd.PluginFiles.First(x => x.Output.Equals(p, StringComparison.InvariantCultureIgnoreCase))
                            .IsUnchecked = true;
                    }
                    else
                    {
                        var first = _omod.PluginsList.First(x =>
                            x.Name.Equals(p, StringComparison.InvariantCultureIgnoreCase));
                        _srd.PluginFiles.Add(new PluginFile(first){IsUnchecked = true});
                    }
                });
            }
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
                if(!_variables.TryGetValue(current, out var value))
                    throw new OBMMScriptingVariableNotFoundException(current);
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

                //the Return variable is being set by the Return instruction
                //when that thing is called, the entire script exists
                if (Return) return;

                if (token is GotoLabelToken gotoLabelToken)
                {
                    //Label and Goto tokens are actually simpler than I thought they would be
                    //we just add the Label token to the LabelTokens list with i as Index
                    //and when we encounter a Goto we simply jump to that Index

                    //ofc this might fuck up a ton of things such as the stack and whatnot
                    //but you should not do that anyways, I think...

                    if (gotoLabelToken.Type == TokenType.Label)
                    {
                        if(!LabelTokens.Contains(gotoLabelToken))
                        {
                            gotoLabelToken.Index = i;
                            LabelTokens.Add(gotoLabelToken);
                        }
                        continue;
                    }

                    if(LabelTokens.All(x => x.Label != gotoLabelToken.Label))
                        throw new OBMMScriptingParseException(gotoLabelToken.ToString(), $"Unable to find Label token with label {gotoLabelToken.Label}");

                    var first = LabelTokens.First(x => x.Label == gotoLabelToken.Label);
                    i = first.Index;

                    continue;
                }

                if (token.Type == TokenType.EndFor)
                {
                    if(_stack.All(x => x.Type != TokenType.For))
                        throw new OBMMScriptingParseException(token.ToString(), "The stack does not contain a For Token but EndFor was still called");
                    var forToken = (ForToken)_stack.First(x => x.Type == TokenType.For);

                    //for token not active means that either Exit or Continue was called
                    if (!forToken.Active)
                    {
                        //if Exit was called, we simply pop the for element from the stack
                        //and continue with the next token outside the for loop
                        if (forToken.Exit)
                        {
                            _stack.Pop();
                            continue;
                        }

                        //if it was continue we set active back to true, setting active to
                        //false was just needed so that  if (_stack.Any(x => !x.Active))
                        //in the ParseToken function would trigger and we wouldn't
                        //be executing anything in the for loop
                        forToken.Active = true;
                    }

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
                            i -= forToken.Children.Count+2;
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
            //When we hit an EndFlowToken we pop the top StartFlowToken in the stack

            if (token is EndFlowToken)
            {
                if (_stack.Count == 0)
                    throw new OBMMScriptingParseException(token.ToString(),
                        "Hit an EndFlowToken but the stack is empty!");

                var peek = _stack.Peek();
                if (token.Type == TokenType.EndIf && peek.Type != TokenType.If && peek.Type != TokenType.IfNot)
                    throw new OBMMScriptingParseException(token.ToString(), $"Type of top token in stack should be If or IfNot but is {peek.Type}");

                if (token.Type == TokenType.EndFor && peek.Type != TokenType.For)
                    throw new OBMMScriptingParseException(token.ToString(), $"Type of top token in stack should be For but is {peek.Type}");

                if (token.Type == TokenType.EndSelect && !(peek is SelectiveToken))
                    throw new OBMMScriptingParseException(token.ToString(), $"Type of top token in stack should be Select but is {peek.Type}");

                if(token.Type == TokenType.Break && !(peek is CaseToken))
                    throw new OBMMScriptingParseException(token.ToString(), $"Type of top token in stack should be Case or Default but is {peek.Type}");

                _stack.Pop();
                return;
            }

            //If the stack is not empty then that means we are in some form of
            //control structure like For, If, Select, Case, Default...
            //we will add the current token as a child to top element and 
            //check whether or not we actually wanna execute the instruction

            if (_stack.Count != 0)
            {
                var peek = _stack.Peek();
                if(!peek.Children.Contains(token))
                    peek.Children.Add(token);

                if (_stack.Any(x => !x.Active))
                {
                    //we slap start flow tokens in the stack so that
                    //end flow tokens will still pop them even if we
                    //are not executing any instructions
                    if (token is StartFlowToken startFlowToken)
                        _stack.Push(startFlowToken);
                    return;
                }
            }

            switch (token.Type)
            {
                case TokenType.FatalError:
                    throw new ScriptingFatalErrorException();
                case TokenType.If:
                case TokenType.IfNot:
                {
                    var ifToken = (IfToken)token;
                    var args = ifToken.Arguments.Select(ReplaceWithVariable).ToList();
                    switch (ifToken.ConditionType)
                    {
                        case IfToken.IfConditionType.DialogYesNo:
                            ifToken.Active = args.Count == 1
                                ? _scriptFunctions.DialogYesNo(args[0])
                                : _scriptFunctions.DialogYesNo(args[0], args[1]);
                            break;

                        case IfToken.IfConditionType.DataFileExists:
                            ifToken.Active = _scriptFunctions.DataFileExists(args[0]);
                            break;

                        case IfToken.IfConditionType.VersionGreaterThan:
                        case IfToken.IfConditionType.VersionLessThan:
                            {
                                var version = new Version(ifToken.Arguments[0]);
                                ifToken.Active = ifToken.ConditionType == IfToken.IfConditionType.VersionGreaterThan
                                    ? version < _scriptFunctions.GetOBMMVersion()
                                    : version > _scriptFunctions.GetOBMMVersion();
                                break;
                            }

                        case IfToken.IfConditionType.ScriptExtenderPresent:
                            ifToken.Active = _settings.ScriptFunctions.HasScriptExtender();
                            break;

                        case IfToken.IfConditionType.ScriptExtenderNewerThan:
                            {
                                var version = new Version(args[0]);
                                ifToken.Active = version < _settings.ScriptFunctions.ScriptExtenderVersion();
                                break;
                            }

                        case IfToken.IfConditionType.GraphicsExtenderPresent:
                            ifToken.Active = _settings.ScriptFunctions.HasGraphicsExtender();
                            break;

                        case IfToken.IfConditionType.GraphicsExtenderNewerThan:
                            {
                                var version = new Version(args[0]);
                                ifToken.Active = version < _settings.ScriptFunctions.GraphicsExtenderVersion();
                                break;
                            }

                        case IfToken.IfConditionType.OblivionNewerThan:
                            {
                                var version = new Version(args[0]);
                                ifToken.Active = version < _settings.ScriptFunctions.OblivionVersion();
                                break;
                            }

                        case IfToken.IfConditionType.Equal:
                        case IfToken.IfConditionType.GreaterThan:
                        case IfToken.IfConditionType.GreaterEqual:
                        {
                            if (!int.TryParse(args[0], out var i1))
                                throw new OBMMScriptingNumberParseException(token.ToString(), args[0], typeof(int));
                            if (!int.TryParse(args[1], out var i2))
                                throw new OBMMScriptingNumberParseException(token.ToString(), args[1], typeof(int));

                            ifToken.Active = ifToken.ConditionType switch
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
                                throw new OBMMScriptingNumberParseException(token.ToString(), args[0], typeof(float));
                            if (!float.TryParse(args[1], out var f2))
                                throw new OBMMScriptingNumberParseException(token.ToString(), args[1], typeof(float));

                            ifToken.Active = ifToken.ConditionType switch
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
                        ifToken.Active = !ifToken.Active;
                    _stack.Push(ifToken);
                    break;
                }
                case TokenType.Else:
                {
                    var elseToken = (StartFlowToken) token;
                    var peek = _stack.Peek();
                    if (!(peek is IfToken ifToken))
                        throw new OBMMScriptingParseException(token.ToString(), $"Else token encountered but top element in stack is not an If or IfNot token but {peek.Type}!");

                    elseToken.Active = !ifToken.Active;
                    
                    _stack.Push(elseToken);
                    break;
                }
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
                case TokenType.Case:
                {
                    var caseToken = (CaseToken) token;
                    var peek = _stack.Peek();

                    if (peek is SelectToken selectToken)
                    {
                        if (selectToken.Results.Contains(caseToken.Value))
                        {
                            caseToken.Active = true;
                            selectToken.FoundCase = true;
                        }
                    }
                    else if (peek is SelectVarToken selectVarToken)
                    {
                        if (caseToken.Value == selectVarToken.Value)
                        {
                            caseToken.Active = true;
                            selectVarToken.FoundCase = true;
                        }
                    }
                    else
                    {
                        throw new OBMMScriptingParseException(token.ToString(), $"The top token in the stack is not a Selective Token but {peek}");
                    }

                    _stack.Push(caseToken);
                    break;
                }
                case TokenType.Default:
                {
                    var peek = _stack.Peek();
                    //default is basically just a Case token that only gets
                    //triggered when all other cases did not, we fake this
                    //by creating a case token with the value of the current
                    //value/result of the selective token

                    if (peek is SelectiveToken selectiveToken)
                    {
                        if (selectiveToken.FoundCase)
                            return;

                        if (peek is SelectToken selectToken)
                        {
                            _stack.Push(new CaseToken(selectToken.Results[0]));
                        } else if (peek is SelectVarToken selectVarToken)
                        {
                            _stack.Push(new CaseToken(selectVarToken.Value));
                        }
                    }
                    else
                    {
                        throw new OBMMScriptingParseException(token.ToString(), $"The top token in the stack is not a Selective Token but {peek}");
                    }

                    break;
                }
                case TokenType.Continue:
                case TokenType.Exit:
                {
                    if(_stack.All(x => x.Type != TokenType.For))
                        throw new OBMMScriptingParseException(token.ToString(), $"Stack does not contain a For Token but {(token.Type == TokenType.Continue ? "Continue" : "Exit")} was still called");

                    var forToken = (ForToken)_stack.First(x => x is ForToken);
                    //check the main loop for more info on this
                    //we basically disable the execution of all coming instructions when setting
                    //Active to false and use the Exit variable to specify if it was Continue or
                    //Exit that set Active to false
                    forToken.Active = false;
                    forToken.Exit = token.Type == TokenType.Exit;
                    break;
                }
                case TokenType.Select:
                case TokenType.SelectMany:
                case TokenType.SelectWithPreview:
                case TokenType.SelectManyWithPreview:
                case TokenType.SelectWithDescriptions:
                case TokenType.SelectManyWithDescriptions:
                case TokenType.SelectWithDescriptionsAndPreviews:
                case TokenType.SelectManyWithDescriptionsAndPreviews:
                {
                    //the vertical bar | at the beginning of an item means it should be auto selected
                    //this is problematic since we compare strings with strings in the case token
                    //so we want to remove this bar before continuing
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
                {
                    var selectToken = (SelectVarToken) token;
                    if (!_variables.TryGetValue(selectToken.Variable, out var value))
                        throw new OBMMScriptingVariableNotFoundException(selectToken.Variable);
                    selectToken.Value = value;
                    _stack.Push(selectToken);
                    break;
                }
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
                case TokenType.InstallDataFolder:
                case TokenType.InstallDataFile:
                case TokenType.InstallPlugin:
                case TokenType.DontInstallPlugin:
                case TokenType.DontInstallDataFile:
                case TokenType.DontInstallDataFolder:
                    {
                    var iToken = (InstructionToken) token;
                    var path = iToken.Instructions[0];

                    if (iToken.Type == TokenType.InstallPlugin)
                    {
                        _scriptFunctions.InstallPlugin(path);
                    } else if (iToken.Type == TokenType.InstallDataFile)
                    {
                        _scriptFunctions.InstallDataFile(path);
                    } else if (iToken.Type == TokenType.InstallDataFolder)
                    {
                        _scriptFunctions.InstallDataFolder(path,
                            iToken.Instructions.Count != 1 && iToken.Instructions[1]
                                .Equals("true", StringComparison.InvariantCultureIgnoreCase));
                    } else if (iToken.Type == TokenType.DontInstallPlugin)
                    {
                        _scriptFunctions.DontInstallPlugin(path);
                    } else if (iToken.Type == TokenType.DontInstallDataFile)
                    {
                        _scriptFunctions.DontInstallDataFile(path);
                    } else if (iToken.Type == TokenType.DontInstallDataFolder)
                    {
                        _scriptFunctions.InstallDataFolder(path,
                            iToken.Instructions.Count != 1 && iToken.Instructions[1]
                                .Equals("true", StringComparison.InvariantCultureIgnoreCase));
                    }

                    break;
                }
                case TokenType.RegisterBSA:
                case TokenType.UnregisterBSA:
                    throw new NotImplementedException();
                case TokenType.Return:
                {
                    Return = true;
                    break;
                }
                case TokenType.UncheckESP:
                {
                    var iToken = (InstructionToken) token;
                    _scriptFunctions.UncheckEsp(iToken.Instructions[0]);
                    break;
                }
                case TokenType.SetDeactivationWarning:
                    throw new NotImplementedException();
                case TokenType.CopyDataFile:
                case TokenType.CopyPlugin:
                case TokenType.CopyDataFolder:
                {
                    var iToken = (InstructionToken) token;
                    var from = iToken.Instructions[0];
                    var to = iToken.Instructions[1];

                    if (token.Type == TokenType.CopyDataFile)
                        _scriptFunctions.CopyDataFile(from, to);
                    else if (token.Type == TokenType.CopyPlugin)
                        _scriptFunctions.CopyPlugin(from, to);
                    else
                        _scriptFunctions.CopyDataFolder(from, to,
                            iToken.Instructions.Count == 3 && iToken.Instructions[2]
                                .Equals("true", StringComparison.InvariantCultureIgnoreCase));

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
                case TokenType.Substring:
                case TokenType.RemoveString:
                case TokenType.StringLength:
                {
                    var iToken = (InstructionToken) token;
                    var instructions = iToken.Instructions.Select(ReplaceWithVariable).ToList();
                    var variable = instructions[0];

                    var first = instructions[1];

                    if (token.Type == TokenType.CombinePaths)
                    {
                        var second = instructions[2];
                            _variables.AddOrReplace(variable, Path.Combine(first, second));
                    } else if (token.Type == TokenType.Substring || token.Type == TokenType.RemoveString)
                    {
                        var second = instructions[2];
                        if (!int.TryParse(second, out var start))
                            throw new OBMMScriptingNumberParseException(token.ToString(), second, typeof(int));

                        if (token.Type == TokenType.Substring)
                        {
                            if (instructions.Count == 4)
                            {
                                if (!int.TryParse(instructions[3], out var end))
                                    throw new OBMMScriptingNumberParseException(token.ToString(), instructions[3], typeof(int));
                                _variables.AddOrReplace(variable, first.Substring(start, end));
                            }
                            else
                            {
                                _variables.AddOrReplace(variable, first.Substring(start));
                            }
                        }
                        else
                        {
                            if (instructions.Count == 4)
                            {
                                if (!int.TryParse(instructions[3], out var end))
                                    throw new OBMMScriptingNumberParseException(token.ToString(), instructions[3], typeof(int));
                                _variables.AddOrReplace(variable, first.Remove(start, end));
                            }
                            else
                            {
                                _variables.AddOrReplace(variable, first.Remove(start));
                            }
                        }

                    } else if (token.Type == TokenType.StringLength)
                    {
                        _variables.AddOrReplace(variable, first.Length.ToString());
                    }

                    break;
                }
                case TokenType.InputString:
                {
                    var iToken = (InstructionToken) token;
                    var variable = iToken.Instructions[0];

                    var title = "";
                    var initial = "";
                    if (iToken.Instructions.Count > 1)
                        title = iToken.Instructions[1];
                    if (iToken.Instructions.Count > 2)
                        initial = iToken.Instructions[2];

                    var result = _scriptFunctions.InputString(title, initial);
                    _variables.AddOrReplace(variable, result);

                    break;
                }
                case TokenType.ReadINI:
                    throw new NotImplementedException();
                case TokenType.ReadRendererInfo:
                    throw new NotImplementedException();
                case TokenType.ExecLines:
                    throw new NotImplementedException();
                case TokenType.iSet:
                case TokenType.fSet:
                {
                    var setToken = (SetToken) token;
                    
                    var isInt = token.Type == TokenType.iSet;
                    var iRes = 0;
                    var fRes = 0.0;

                    var instructions = setToken.Instructions.Select(ReplaceWithVariable).ToList();

                    if (isInt)
                        iRes = EvaluateIntExpression(instructions);
                    else
                        fRes = EvaluateFloatExpression(instructions);

                    _variables.AddOrReplace(setToken.Variable, isInt ? iRes.ToString() : fRes.ToString(CultureInfo.InvariantCulture));

                    break;
                }
                case TokenType.EditXMLLine:
                    throw new NotImplementedException();
                case TokenType.EditXMLReplace:
                    throw new NotImplementedException();
                case TokenType.AllowRunOnLines:
                    //TODO
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal static int EvaluateIntExpression(List<string> list)
        {
            /*
             * nice function that will evaluate an expression from iSet
             * the same part in OBMM was very repetitive and I replaced
             * all the same stuff with a simple calc Action that will
             * do everything for us.
             *
             * Main problem here is having to deal with expressions in
             * brackets. Our first while loop will simply grab the
             * expression in brackets, put it inside this function (recursive)
             * and put the result back into the list. eg:
             *
             *  1 + ( 2 + 4 )
             *
             * the stuff in brackets will get send to this function:
             *
             *  2 + 4
             *
             * and the result (6) will be put back into the expression
             *
             *  1 + 6
             *
             * Since this is recursive it can handle any amount of bracket-ception.
             */

            var index = list.IndexOf("(");

            while (index != -1)
            {
                var newFunc = new List<string>();
                var count = 1;
                for (var i = index + 1; i < list.Count; i++)
                {
                    var current = list[i];
                    if (current == "(") count++;
                    else if (current == ")") count--;
                    if (count == 0)
                    {
                        list.RemoveRange(index, i - index + 1);
                        list.Insert(index, EvaluateIntExpression(newFunc).ToString());
                        break;
                    }
                    newFunc.Add(current);
                }

                index = list.IndexOf("(");
            }

            var calc = new Action<string, bool, Func<int, int, int>>(
                (search, single, calcFunc) =>
                {
                    index = list.IndexOf(search);
                    while (index != -1)
                    {
                        var i1 = int.Parse(single ? list[index + 1] : list[index - 1]);
                        var i2 = 0;
                        if (!single)
                        {
                            i2 = int.Parse(list[index + 1]);
                        }

                        var res = calcFunc(i1, i2);
                        list[index + 1] = res.ToString();
                        if(single)
                            list.RemoveAt(index);
                        else
                            list.RemoveRange(index-1, 2);
                        index = list.IndexOf(search);
                    }
                });

            calc("not", true, (o1, o2) => ~o1);
            calc("and", false, (o1, o2) => o1 & o2);
            calc("or", false, (o1, o2) => o1 | o2);
            calc("xor", false, (o1, o2) => o1 ^ o2);
            calc("mod", false, (o1, o2) => o1 % o2);
            calc("%", false, (o1, o2) => o1 % o2);
            calc("^", false, (o1, o2) => (int)Math.Pow(o1, o2));
            calc("/", false, (o1, o2) => o1 / o2);
            calc("*", false, (o1, o2) => o1 * o2);
            calc("+", false, (o1, o2) => o1 + o2);
            calc("-", false, (o1, o2) => o1 - o2);

            return int.Parse(list[0]);
        }

        internal static double EvaluateFloatExpression(List<string> list)
        {
            //see comment in EvaluateIntExpression as that basically the same function

            var index = list.IndexOf("(");

            while (index != -1)
            {
                var newFunc = new List<string>();
                var count = 1;
                for (var i = index + 1; i < list.Count; i++)
                {
                    var current = list[i];
                    if (current == "(") count++;
                    else if (current == ")") count--;
                    if (count == 0)
                    {
                        list.RemoveRange(index, i - index + 1);
                        list.Insert(index, EvaluateFloatExpression(newFunc).ToString(CultureInfo.InvariantCulture));
                        break;
                    }
                    newFunc.Add(current);
                }

                index = list.IndexOf("(");
            }

            var calc = new Action<string, bool, Func<double, double, double>>(
                (search, single, calcFunc) =>
                {
                    index = list.IndexOf(search);
                    while (index != -1)
                    {
                        var i1 = double.Parse(single ? list[index + 1] : list[index - 1]);
                        double i2 = 0;
                        if (!single)
                        {
                            i2 = double.Parse(list[index + 1]);
                        }

                        var res = calcFunc(i1, i2);
                        list[index + 1] = res.ToString(CultureInfo.InvariantCulture);
                        if (single)
                            list.RemoveAt(index);
                        else
                            list.RemoveRange(index - 1, 2);
                        index = list.IndexOf(search);
                    }
                });

            calc("sin", true, (f, f1) => Math.Sin(f));
            calc("cos", true, (f, f1) => Math.Cos(f));
            calc("tan", true, (f, f1) => Math.Tan(f));
            calc("sinh", true, (f, f1) => Math.Sinh(f));
            calc("cosh", true, (f, f1) => Math.Cosh(f));
            calc("tanh", true, (f, f1) => Math.Tanh(f));
            calc("exp", true, (f, f1) => Math.Exp(f));
            calc("log", true, (f, f1) => Math.Log10(f));
            calc("ln", true, (f, f1) => Math.Log(f));
            calc("mod", false, (f, f1) => f%f1);
            calc("%", false, (f, f1) => f%f1);
            calc("^", false, Math.Pow);
            calc("/", false, (f, f1) => f/f1);
            calc("*", false, (f, f1) => f*f1);
            calc("+", false, (f, f1) => f+f1);
            calc("-", false, (f, f1) => f-f1);

            return double.Parse(list[0]);
        }
    }
}
