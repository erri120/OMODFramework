using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using OblivionModManager.Scripting;

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
        private readonly List<GotoLabelToken> _labelTokens = new List<GotoLabelToken>();

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

        /// <summary>
        /// Utility function for replacing variable placeholders with the actual value
        /// of the variable from the _variables dictionary
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
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
                        if(!_labelTokens.Contains(gotoLabelToken))
                        {
                            gotoLabelToken.Index = i;
                            _labelTokens.Add(gotoLabelToken);
                        }
                        continue;
                    }

                    if(_labelTokens.All(x => x.Label != gotoLabelToken.Label))
                        throw new OBMMScriptingParseException(gotoLabelToken.ToString(), $"Unable to find Label token with label {gotoLabelToken.Label}");

                    var first = _labelTokens.First(x => x.Label == gotoLabelToken.Label);
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
                        if (forToken.Current != forToken.End)
                        {
                            i -= forToken.Children.Count + 2;
                            forToken.Current += forToken.Step;
                            _variables.AddOrReplace(forToken.Variable, forToken.Current.ToString());
                            continue;
                        }
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

            {
                if (token is InstructionToken iToken)
                {
                    iToken.Instructions = iToken.Instructions.Select(ReplaceWithVariable).ToList();
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
                        {
                            forToken.Current = forToken.Start;
                            _variables.AddOrReplace(forToken.Variable, forToken.Start.ToString());
                            break;
                        }


                        case ForToken.ForEnumerationType.DataFolder:
                        {
                            forToken.Enumerable = _scriptFunctions.GetDataFolders(forToken.FolderPath,
                                forToken.SearchString, forToken.Recursive);
                            _variables.AddOrReplace(forToken.Variable, forToken.Enumerable.ElementAt(0));
                            break;
                        }

                        case ForToken.ForEnumerationType.PluginFolder:
                        {
                            forToken.Enumerable = _scriptFunctions.GetPluginFolders(forToken.FolderPath,
                                forToken.SearchString, forToken.Recursive);
                            _variables.AddOrReplace(forToken.Variable, forToken.Enumerable.ElementAt(0));
                            break;
                        }

                        case ForToken.ForEnumerationType.DataFile:
                        {
                            forToken.Enumerable = _scriptFunctions.GetDataFiles(forToken.FolderPath,
                                forToken.SearchString, forToken.Recursive);
                            _variables.AddOrReplace(forToken.Variable, forToken.Enumerable.ElementAt(0));
                            break;
                        }

                        case ForToken.ForEnumerationType.Plugin:
                        {
                            forToken.Enumerable = _scriptFunctions.GetPlugins(forToken.FolderPath,
                                forToken.SearchString, forToken.Recursive);
                            _variables.AddOrReplace(forToken.Variable, forToken.Enumerable.ElementAt(0));
                            break;
                        }

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
                {
                    var iToken = (InstructionToken) token;
                    var plugin = iToken.Instructions[0];
                    _scriptFunctions.LoadEarly(plugin);
                    break;
                }
                case TokenType.LoadBefore:
                case TokenType.LoadAfter:
                {
                    var iToken = (InstructionToken) token;
                    var plugin = iToken.Instructions[0];
                    var target = iToken.Instructions[1];

                    if(token.Type == TokenType.LoadBefore)
                        _scriptFunctions.LoadBefore(plugin, target);
                    else
                        _scriptFunctions.LoadAfter(plugin, target);

                    break;
                }
                case TokenType.ConflictsWith:
                case TokenType.ConflictsWithRegex:
                case TokenType.DependsOn:
                case TokenType.DependsOnRegex:
                {
                    var iToken = (InstructionToken) token;
                    var cd = new ConflictData
                    {
                        Level = ConflictLevel.MajorConflict,
                        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
                        Type = token.Type switch
                        {
                            TokenType.ConflictsWith => ConflictType.Conflicts,
                            TokenType.ConflictsWithRegex => ConflictType.Conflicts,
                            TokenType.DependsOn => ConflictType.Depends,
                            TokenType.DependsOnRegex => ConflictType.Depends,
                            _ => throw new ArgumentOutOfRangeException(nameof(token.Type), token.Type.ToString(), null)
                        }
                    };

                    switch (iToken.Instructions.Count)
                    {
                        case 1:
                            cd.File = iToken.Instructions[0];
                            break;
                        case 2:
                            cd.Comment = iToken.Instructions[1];
                            goto case 1;
                        case 3:
                        {
                            var levelName = iToken.Instructions[2];
                            if (!Utils.TryGetEnum<ConflictLevel>(levelName, out var level))
                                throw new OBMMScriptingParseException(iToken.ToString(), $"Unable to parse ConflictLevel {levelName}!");
                            if(level == ConflictLevel.Active || level == ConflictLevel.NoConflict)
                                throw new OBMMScriptingParseException(iToken.ToString(), $"ConflictLevel {level} is not allowed!");
                            cd.Level = level;
                            goto case 2;
                        }
                        case 5:
                        {
                            cd.File = iToken.Instructions[0];
                            var sMinMajor = iToken.Instructions[1];
                            var sMinMinor = iToken.Instructions[2];
                            var sMaxMajor = iToken.Instructions[3];
                            var sMaxMinor = iToken.Instructions[4];

                            if (!int.TryParse(sMinMajor, out var minMajor))
                                throw new OBMMScriptingNumberParseException(iToken.ToString(), sMinMajor, typeof(int));
                            if (!int.TryParse(sMinMinor, out var minMinor))
                                throw new OBMMScriptingNumberParseException(iToken.ToString(), sMinMinor, typeof(int));
                            if (!int.TryParse(sMaxMajor, out var maxMajor))
                                throw new OBMMScriptingNumberParseException(iToken.ToString(), sMaxMajor, typeof(int));
                            if (!int.TryParse(sMaxMinor, out var maxMinor))
                                throw new OBMMScriptingNumberParseException(iToken.ToString(), sMaxMinor, typeof(int));

                            cd.MinVersion = new Version(minMajor, minMinor);
                            cd.MaxVersion = new Version(maxMajor, maxMinor);

                            break;
                        }
                        case 6:
                            cd.Comment = iToken.Instructions[5];
                            goto case 5;
                        case 7:
                        {
                            var levelName = iToken.Instructions[6];
                            if (!Utils.TryGetEnum<ConflictLevel>(levelName, out var level))
                                throw new OBMMScriptingParseException(iToken.ToString(), $"Unable to parse ConflictLevel {levelName}!");
                            if (level == ConflictLevel.Active || level == ConflictLevel.NoConflict)
                                throw new OBMMScriptingParseException(iToken.ToString(), $"ConflictLevel {level} is not allowed!");
                            cd.Level = level;
                            goto case 6;
                        }
                    }

                    cd.Partial = token.Type == TokenType.DependsOnRegex || token.Type == TokenType.ConflictsWithRegex;
                    _srd.Conflicts.Add(cd);
                    break;
                }
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
                {
                    var iToken = (InstructionToken) token;
                    var path = iToken.Instructions[0];

                    if(token.Type == TokenType.RegisterBSA)
                        _scriptFunctions.RegisterBSA(path);
                    else
                        _scriptFunctions.UnregisterBSA(path);

                    break;
                }
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
                {
                    var iToken = (InstructionToken) token;
                    var plugin = iToken.Instructions[0];
                    var warningType = iToken.Instructions[1];
                    if (!Utils.TryGetEnum<DeactiveStatus>(warningType, out var status))
                        throw new OBMMScriptingParseException(token.ToString(), $"Unable to parse enum {warningType}!");

                    _scriptFunctions.SetDeactivationWarning(plugin, status);
                    break;
                }
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
                case TokenType.PatchDataFile:
                {
                    var iToken = (InstructionToken) token;
                    var from = iToken.Instructions[0];
                    var to = iToken.Instructions[1];

                    var create = iToken.Instructions.Count == 3 && iToken.Instructions[2] == "True";

                    if(token.Type == TokenType.PatchPlugin)
                        _scriptFunctions.PatchPlugin(from, to, create);
                    else
                        _scriptFunctions.PatchDataFile(from, to, create);

                    break;
                }
                case TokenType.EditINI:
                {
                    var iToken = (InstructionToken) token;

                    var section = iToken.Instructions[0];
                    var key = iToken.Instructions[1];
                    var newValue = iToken.Instructions[2];

                    _scriptFunctions.EditINI(section, key, newValue);

                    break;
                }
                case TokenType.EditSDP:
                case TokenType.EditShader:
                {
                    var iToken = (InstructionToken) token;

                    var shaderPackage = iToken.Instructions[0];
                    var shaderName = iToken.Instructions[1];
                    var binaryObjectPath = iToken.Instructions[2];

                    //TODO: EditShader requires the binary data
                    throw new NotImplementedException();
                }
                case TokenType.SetGMST:
                case TokenType.SetGlobal:
                {
                    var iToken = (InstructionToken) token;
                    var file = iToken.Instructions[0];
                    var edid = iToken.Instructions[1];
                    var value = iToken.Instructions[2];

                    if(token.Type == TokenType.SetGMST)
                        _scriptFunctions.SetGMST(file, edid, value);
                    else
                        _scriptFunctions.SetGlobal(file, edid, value);

                    break;
                }
                case TokenType.SetPluginByte:
                case TokenType.SetPluginShort:
                case TokenType.SetPluginInt:
                case TokenType.SetPluginLong:
                case TokenType.SetPluginFloat:
                {
                    var iToken = (InstructionToken) token;

                    var file = iToken.Instructions[0];
                    var sOffset = iToken.Instructions[1];

                    if(!long.TryParse(sOffset, out var offset))
                        throw new OBMMScriptingNumberParseException(iToken.ToString(), sOffset, typeof(long));

                    var sData = iToken.Instructions[2];

                    //still haven't found a better solution for this mess

                    if (token.Type == TokenType.SetPluginByte)
                    {
                        if(!byte.TryParse(sData, out var value))
                            throw new OBMMScriptingNumberParseException(iToken.ToString(), sData, typeof(byte));
                        
                        _scriptFunctions.SetPluginByte(file, offset, value);
                    } else if (token.Type == TokenType.SetPluginShort)
                    {
                        if (!short.TryParse(sData, out var value))
                            throw new OBMMScriptingNumberParseException(iToken.ToString(), sData, typeof(short));

                        _scriptFunctions.SetPluginShort(file, offset, value);
                    }
                    else if (token.Type == TokenType.SetPluginInt)
                    {
                        if (!int.TryParse(sData, out var value))
                            throw new OBMMScriptingNumberParseException(iToken.ToString(), sData, typeof(int));

                        _scriptFunctions.SetPluginInt(file, offset, value);
                    }
                    else if (token.Type == TokenType.SetPluginLong)
                    {
                        if (!long.TryParse(sData, out var value))
                            throw new OBMMScriptingNumberParseException(iToken.ToString(), sData, typeof(long));

                        _scriptFunctions.SetPluginLong(file, offset, value);
                    }
                    else if (token.Type == TokenType.SetPluginFloat)
                    {
                        if (!float.TryParse(sData, out var value))
                            throw new OBMMScriptingNumberParseException(iToken.ToString(), sData, typeof(float));

                        _scriptFunctions.SetPluginFloat(file, offset, value);
                    }

                    break;
                }
                case TokenType.DisplayImage:
                case TokenType.DisplayText:
                {
                    var iToken = (InstructionToken) token;
                    var path = iToken.Instructions[0];

                    if(token.Type == TokenType.DisplayImage)
                        if(iToken.Instructions.Count == 2)
                            _scriptFunctions.DisplayImage(path, iToken.Instructions[1]);
                        else
                            _scriptFunctions.DisplayImage(path);
                    else
                        if (iToken.Instructions.Count == 2)
                            _scriptFunctions.DisplayText(path, iToken.Instructions[1]);
                        else
                            _scriptFunctions.DisplayText(path);

                    break;
                }
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
                    var variable = getFileNameToken.Instructions[0];
                    if(token.Type == TokenType.GetFolderName || token.Type == TokenType.GetDirectoryName)
                        _variables.AddOrReplace(variable, Path.GetFileName(getFileNameToken.Instructions[1]));
                    else
                    {
                        var value = token.Type == TokenType.GetFileName
                            ? Path.GetFileName(getFileNameToken.Instructions[1])
                            : Path.GetFileNameWithoutExtension(getFileNameToken.Instructions[1]);
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
                    var variable = iToken.Instructions[0];

                    var first = iToken.Instructions[1];

                    if (token.Type == TokenType.CombinePaths)
                    {
                        var second = iToken.Instructions[2];
                            _variables.AddOrReplace(variable, Path.Combine(first, second));
                    } else if (token.Type == TokenType.Substring || token.Type == TokenType.RemoveString)
                    {
                        var second = iToken.Instructions[2];
                        if (!int.TryParse(second, out var start))
                            throw new OBMMScriptingNumberParseException(token.ToString(), second, typeof(int));

                        if (token.Type == TokenType.Substring)
                        {
                            if (iToken.Instructions.Count == 4)
                            {
                                if (!int.TryParse(iToken.Instructions[3], out var end))
                                    throw new OBMMScriptingNumberParseException(token.ToString(), iToken.Instructions[3], typeof(int));
                                _variables.AddOrReplace(variable, first.Substring(start, end));
                            }
                            else
                            {
                                _variables.AddOrReplace(variable, first.Substring(start));
                            }
                        }
                        else
                        {
                            if (iToken.Instructions.Count == 4)
                            {
                                if (!int.TryParse(iToken.Instructions[3], out var end))
                                    throw new OBMMScriptingNumberParseException(token.ToString(), iToken.Instructions[3], typeof(int));
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
                case TokenType.ReadRendererInfo:
                {
                    var iToken = (InstructionToken) token;
                    var variable = iToken.Instructions[0];

                    if (token.Type == TokenType.ReadINI)
                    {
                        var section = iToken.Instructions[1];
                        var value = iToken.Instructions[2];
                        var result = "";
                        try
                        {
                            result = _scriptFunctions.ReadINI(section, value);
                        }
                        catch (Exception e)
                        {
                            result = e.Message;
                        }
                        finally
                        {
                            _variables.AddOrReplace(variable, result);
                        }
                    }
                    else
                    {
                        var value = iToken.Instructions[1];
                        var result = "";
                        try
                        {
                            result = _scriptFunctions.ReadRendererInfo(value);
                        }
                        catch (Exception e)
                        {
                            result = e.Message;
                        }
                        finally
                        {
                            _variables.AddOrReplace(variable, result);
                        }
                    }

                    break;
                }
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
                case TokenType.EditXMLReplace:
                {
                    var iToken = (InstructionToken) token;
                    var file = iToken.Instructions[0];
                    var extension = Path.GetExtension(file);

                    if(extension != ".xml" && extension != ".txt" && extension != ".ini" && extension != ".bat")
                        throw new OBMMScriptingParseException(iToken.ToString(), $"Extension of file {file} is not allowed! Allowed are .xml, .txt, .ini and .bat files!");

                    if (token.Type == TokenType.EditXMLLine)
                    {
                        var sLineNumber = iToken.Instructions[1];
                        var line = iToken.Instructions[2];

                        if(!int.TryParse(sLineNumber, out var lineNumber))
                            throw new OBMMScriptingNumberParseException(iToken.ToString(), sLineNumber, typeof(int));

                        _scriptFunctions.EditXMLLine(file, lineNumber, line);
                    }
                    else
                    {
                        var toFind = iToken.Instructions[1];
                        var toReplace = iToken.Instructions[2];

                        _scriptFunctions.EditXMLReplace(file, toFind, toReplace);
                    }

                    break;
                }
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
