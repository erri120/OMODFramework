using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using NLog;
using OblivionModManager.Scripting;
using OMODFramework.Scripting.Data;
using OMODFramework.Scripting.Exceptions;
using OMODFramework.Scripting.ScriptHandlers.OBMMScript.Tokenizer;

namespace OMODFramework.Scripting.ScriptHandlers.OBMMScript
{
    internal partial class OBMMScriptHandler : AScriptHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<string, string> _variables = new Dictionary<string, string>();
        private readonly Stack<StartFlowToken> _stack = new Stack<StartFlowToken>();
        private readonly List<GotoLabelToken> _labelTokens = new List<GotoLabelToken>();

        private bool Return;
        
        internal OBMMScriptHandler(OMOD omod, string script, OMODScriptSettings settings, string? extractionFolder) 
            : base(omod, script, settings, extractionFolder) { }

        /// <summary>
        /// Utility function that replaces a variable placeholder with its value from the <see cref="_variables"/>
        /// Dictionary.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private string ReplaceWithVariable(string s)
        {
            if (!s.Contains('%') || s.Count(x => x == '%') < 2)
                return s;

            var split = s.Split('%');
            var result = s;
            for (var i = 1; i < split.Length; i += 2)
            {
                var current = split[i];
                if (!_variables.TryGetValue(current, out var value))
                    throw new OBMMScriptHandlerException($"Unable to find value of variable {current}");
                result = result.Replace($"%{current}%", value);
            }

            return result;
        }
        
        private protected override void PrivateRunScript()
        {
            Logger.Info("Starting OBMM Script Execution");
            
            var tokens = TokenizeScript(Script).ToList();
            
            /*
             * This is very irritating and took me way to long to figure out:
             * OBMM installs everything by default. This is why every script in existence calls DontInstallAnyDataFiles
             * at the start of the script...
             * It's kinda dumb but whatever, we can just mimic this behaviour by calling these two functions at the
             * start of the script execution.
             */
            ScriptFunctions.InstallAllDataFiles();
            if (OMOD.HasEntryFile(OMODEntryFileType.PluginsCRC))
                ScriptFunctions.InstallAllPlugins();

            ExecuteScript(tokens);
            
            Logger.Info("Finished OBMM Script Execution");
        }

        private void ExecuteScript(IEnumerable<Token> scriptTokens)
        {
            //ignore comments, who needs them
            var tokens = scriptTokens
                .Where(x => x.Type != TokenType.Comment)
                .ToList();
            
            /*
             * Script execution. The base idea is simple: go through every token and execute a function. The biggest
             * problem are control structures like For loops, Select statements, Label/Goto statements and so on. These
             * all change the flow of the script execution and make simple sequential execution harder.
             */

            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                Logger.Debug($"Current token: {token}");

                if (token is GotoLabelToken gotoLabelToken)
                {
                    /*
                     * Goto and Label tokens are very hacky itself so the solution for dealing with them is on a similar
                     * level. The approach used is probably the simplest one: we store the label token with the current
                     * index in a list and change our index to the index of the label token if we encounter a matching
                     * Goto statement.
                     *
                     * This will of course break basically everything if you jump from control structure to control
                     * structure but using label and goto itself is already bad practise so whatever.
                     */
                    
                    if (gotoLabelToken.Type == TokenType.Label)
                    {
                        if (!_labelTokens.Contains(gotoLabelToken))
                        {
                            gotoLabelToken.Index = i;
                            _labelTokens.Add(gotoLabelToken);
                        }
                        continue;
                    }

                    if (_labelTokens.All(x => x.Label != gotoLabelToken.Label))
                        throw new OBMMScriptHandlerException($"Unable to find matching Label for Goto-Token: {gotoLabelToken.Label}");

                    var first = _labelTokens.First(x => x.Label == gotoLabelToken.Label);
                    
                    Logger.Debug($"Encountered goto token, jumping to {first.Index} was {i}");
                    i = first.Index;

                    continue;
                }

                if (token.Type == TokenType.For)
                {
                    var forToken = (ForToken) token;
                    //set the starting index so we can easily jump to it
                    forToken.StartingIndex = i + 1;
                }
                
                if (token.Type == TokenType.EndFor)
                {
                    if (_stack.All(x => x.Type != TokenType.For))
                        throw new OBMMScriptHandlerException("Encountered EndFor-Token but stack does not contain a For-Token!");
                    var forToken = (ForToken) _stack.First(x => x.Type == TokenType.For);
                    
                    if (!forToken.Active)
                    {
                        //For-Token is not active, this means we either hit Continue or Exit
                        if (forToken.Exit)
                        {
                            /*
                             * If it was an Exit-Token that deactivated the For-Token then we just pop the For-Token
                             * from the Stack and continue code execution since this means we should exit the for-loop.
                             */
                            _stack.Pop();
                            continue;
                        }

                        /*
                         * If it was a Continue-Token that deactivated the For-Token then we need to re-activate it
                         * so we can "continue" with the code execution of the next loop-iteration. Since ForToken
                         * is a StartFlowToken, setting this back to true is important so we don't skip it.
                         */
                        forToken.Active = true;
                    }
                    
                    /*
                     * We enumerate by checking if we can continue enumerating (depending on EnumerationType) and
                     * then setting our index to the start of the for-loop.
                     * Do note that we set i to StartingIndex - 1 because we immediately call "continue" and then
                     * i will be StartingIndex in the next iteration.
                     */

                    /*if (forToken.StartingIndex == -1)
                        throw new NotImplementedException();
                    if (forToken.StartingIndex > i)
                        throw new NotImplementedException();*/
                    
                    if (forToken.EnumerationType == ForToken.ForEnumerationType.Count)
                    {
                        if (forToken.Current != forToken.End)
                        {
                            i = forToken.StartingIndex - 1;
                            forToken.Current += forToken.Step;
                            _variables.AddOrReplace(forToken.Variable, forToken.Current.ToString());
                            continue;
                        }
                    }
                    else
                    {
                        if (forToken.Enumerable.Count - 1 != forToken.Current)
                        {
                            i = forToken.StartingIndex - 1;
                            forToken.Current += 1;
                            _variables.AddOrReplace(forToken.Variable, forToken.Enumerable[forToken.Current]);
                            continue;
                        }
                    }
                }
                
                ExecuteToken(token);
                
                //literally return if we hit the Return token
                if (Return) return;
            }
        }

        private void ExecuteToken(Token token)
        {
            /*
             * Executing one single token is fairly straight forward: we get the token type, do a massive switch operation
             * on it and do something with the arguments depending on the type.
             */

            if (token is EndFlowToken)
            {
                /*
                 * Encountering an EndFlowToken means there is/should be a StartFlowToken in the stack. We do some
                 * safety checks to make sure the script is not completely broken and the correct StartFlowToken is
                 * present and then pop it from the stack.
                 */

                if (_stack.Count == 0)
                    throw new OBMMScriptHandlerException("Encountered an EndFlowToken but the stack is empty!");

                var peek = _stack.Peek();
                
                if (token.Type == TokenType.EndIf && peek.Type != TokenType.If && peek.Type != TokenType.IfNot && peek.Type != TokenType.Else)
                    throw new OBMMScriptHandlerException($"Top token is supposed to be {TokenType.If}, {TokenType.IfNot} or {TokenType.Else} but is {peek.Type}!");

                if (token.Type == TokenType.EndFor && peek.Type != TokenType.For)
                    throw new OBMMScriptHandlerException($"Top token is supposed to be {TokenType.For} but is {peek.Type}!");
                
                if (token.Type == TokenType.EndSelect && !(peek is SelectiveToken))
                    throw new OBMMScriptHandlerException($"Top token is supposed to be a Select token but is {peek.Type}!");
                
                if (token.Type == TokenType.Break && peek.Type != TokenType.Case)
                    throw new OBMMScriptHandlerException($"Top token is supposed to be {TokenType.Case} but is {peek.Type}!");

                if (token.Type == TokenType.EndIf)
                {
                    /*
                     * Special case for EndIf because you can have If+Else which are both closed by 1 EndIf. We need to
                     * make sure that we pop both of them and not only 1.
                     */

                    //top element being of type Else means the following one will always be If or IfNot
                    if (peek.Type == TokenType.Else)
                        _stack.Pop();
                }
                
                _stack.Pop();

                if (_stack.Count == 0) return;
                
                /*
                 * If we were to simply return like above then you would not add the current token as a child to the
                 * next control structure in the stack like what happens below with any other token.
                 */
                peek = _stack.Peek();
                if (!peek.Children.Contains(token))
                    peek.Children.Add(token);
                return;
            }
            
            if (_stack.Count != 0)
            {
                /*
                 * The stack is not empty meaning we are in a control structure like For, If, Select, Case, Default...
                 * The current token will be added as a child to the top element and we check if the control structure
                 * is active or not. Control structures that are not active have their children-instructions not
                 * executed, eg: an if-statement that is false.
                 */

                var peek = _stack.Peek();
                if (!peek.Children.Contains(token))
                    peek.Children.Add(token);

                if (_stack.Any(x => !x.Active))
                {
                    /*
                     * This push operation is important because we handle EndFlowTokens prior to this. If we encounter
                     * an EndFlowToken we pop the top element from the stack so we have to make sure we always add
                     * all StartFlowTokens to the stack even if they are in an inactive control structure.
                     */
                    if (token is StartFlowToken startFlowToken)
                        _stack.Push(startFlowToken);
                    return;
                }
            }

            {
                /*
                 * Simple helper so we don't have to do this manually for every InstructionToken we encounter. Tho
                 * we still need to do this for every other token that might have variables as arguments.
                 */
                if (token is InstructionToken iToken)
                {
                    iToken.Instructions = iToken.Instructions.Select(ReplaceWithVariable).ToList();
                }
            }
            
            switch (token.Type)
            {
                case TokenType.If:
                case TokenType.IfNot:
                {
                    var ifToken = (IfToken) token;
                    var args = ifToken.Arguments.Select(ReplaceWithVariable).ToList();
                    switch (ifToken.ConditionType)
                    {
                        case IfToken.IfConditionType.DialogYesNo:
                            ifToken.Active = args.Count == 1
                                ? ScriptFunctions.DialogYesNo(args[0])
                                : ScriptFunctions.DialogYesNo(args[0], args[1]);
                            break;
                        
                        case IfToken.IfConditionType.DataFileExists:
                            ifToken.Active = ScriptFunctions.DataFileExists(args[0]);
                            break;
                        
                        case IfToken.IfConditionType.VersionGreaterThan:
                        case IfToken.IfConditionType.VersionLessThan:
                        case IfToken.IfConditionType.ScriptExtenderNewerThan:
                        case IfToken.IfConditionType.GraphicsExtenderNewerThan:
                        case IfToken.IfConditionType.OblivionNewerThan:
                        {
                            var version = new Version(args[0]);
                            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                            switch (ifToken.ConditionType)
                            {
                                case IfToken.IfConditionType.VersionGreaterThan:
                                case IfToken.IfConditionType.VersionLessThan:
                                    ifToken.Active = ifToken.ConditionType == IfToken.IfConditionType.VersionGreaterThan
                                        ? version < ScriptFunctions.GetOBMMVersion()
                                        : version > ScriptFunctions.GetOBMMVersion();
                                    break;
                                case IfToken.IfConditionType.ScriptExtenderNewerThan:
                                    ifToken.Active = version < ExternalScriptFunctions.GetScriptExtenderVersion();
                                    break;
                                case IfToken.IfConditionType.GraphicsExtenderNewerThan:
                                    ifToken.Active = version < ExternalScriptFunctions.GetGraphicsExtenderVersion();
                                    break;
                                case IfToken.IfConditionType.OblivionNewerThan:
                                    ifToken.Active = version < ExternalScriptFunctions.GetOblivionVersion();
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            break;
                        }
                        
                        case IfToken.IfConditionType.ScriptExtenderPresent:
                            ifToken.Active = ExternalScriptFunctions.HasScriptExtender();
                            break;

                        case IfToken.IfConditionType.GraphicsExtenderPresent:
                            ifToken.Active = ExternalScriptFunctions.HasGraphicsExtender();
                            break;

                        case IfToken.IfConditionType.Equal:
                        case IfToken.IfConditionType.GreaterEqual:
                        case IfToken.IfConditionType.GreaterThan:
                            var i1 = int.Parse(args[0]);
                            var i2 = int.Parse(args[1]);

                            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
                            ifToken.Active = ifToken.ConditionType switch
                            {
                                IfToken.IfConditionType.Equal => i1 == i2,
                                IfToken.IfConditionType.GreaterThan => i1 > i2,
                                IfToken.IfConditionType.GreaterEqual => i1 >= i2,
                                _ => throw new ArgumentOutOfRangeException()
                            };
                            break;
                        case IfToken.IfConditionType.fGreaterEqual:
                        case IfToken.IfConditionType.fGreaterThan:
                            var f1 = float.Parse(args[0]);
                            var f2 = float.Parse(args[1]);
                            
                            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
                            ifToken.Active = ifToken.ConditionType switch
                            {
                                IfToken.IfConditionType.fGreaterThan => f1 < f2,
                                IfToken.IfConditionType.fGreaterEqual => f1 <= f2,
                                _ => throw new ArgumentOutOfRangeException()
                            };
                            
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    ifToken.Active = ifToken.Not 
                        ? !ifToken.Active 
                        : ifToken.Active;
                    _stack.Push(ifToken);
                    
                    break;
                }
                case TokenType.Else:
                {
                    var elseToken = (StartFlowToken) token;
                    var peek = _stack.Peek();
                    if (!(peek is IfToken ifToken))
                        throw new OBMMScriptHandlerException($"Else token encountered but top element in stack is not an If or IfNot token but {peek.Type}!");

                    elseToken.Active = !ifToken.Active;
                    
                    _stack.Push(elseToken);
                    break;
                }
                case TokenType.Case:
                {
                    var caseToken = (CaseToken) token;
                    var peek = _stack.Peek();

                    if (!(peek is SelectiveToken selectiveToken))
                        throw new OBMMScriptHandlerException($"Case encountered but top element in stack is not a Select token but {peek.Type}!");
                    
                    //we already found a case statement that is true so we can just skip this one
                    if (selectiveToken.FoundCase)
                    {
                        _stack.Push(caseToken);
                        break;
                    }

                    switch (peek)
                    {
                        case SelectToken selectToken:
                        {
                            if (selectToken.Results.Contains(caseToken.Value))
                            {
                                caseToken.Active = true;
                                /*
                                 * We can't set FoundCase to true if you can select multiple items (SelectMany). Since
                                 * you can select multiple items we also have to make sure multiple Case-Tokens are
                                 * active and executed which can't happen if we set FoundCase to true.
                                 */
                                selectToken.FoundCase = !selectToken.IsMany;
                            }

                            break;
                        }
                        case SelectVarToken selectVarToken:
                        {
                            if (selectVarToken.Value == caseToken.Value)
                            {
                                caseToken.Active = true;
                                selectVarToken.FoundCase = true;
                            }

                            break;
                        }
                        case SelectStringToken selectStringToken:
                        {
                            //SelectString can have a variable as it's value which we need to replace
                            if (ReplaceWithVariable(selectStringToken.Value) == caseToken.Value)
                            {
                                caseToken.Active = true;
                                selectStringToken.FoundCase = true;
                            }

                            break;
                        }
                        default:
                            throw new OBMMScriptHandlerException($"Case encountered but top element in stack is not a Select token but {peek.Type}!");
                    }
                    
                    _stack.Push(caseToken);
                    break;
                }
                case TokenType.Default:
                {
                    var peek = _stack.Peek();

                    if (!(peek is SelectiveToken selectiveToken))
                        throw new OBMMScriptHandlerException($"Default encountered but top element in stack is not a Select token but {peek.Type}!");

                    //already found a case that is true so we just skip this
                    //TODO: find out if the Default case is also triggered when you have a SelectMany token
                    if (selectiveToken.FoundCase) break;

                    /*
                     * Default is just another Case token that is active when all other Case tokens are not active. We
                     * can simply fake this by creating a new CaseToken that is active but has no value and push that
                     * onto the stack. The rest of our code will just assume this is an active case statement and will
                     * continue working as usual.
                     */
                    _stack.Push(new CaseToken(string.Empty) { Active = true});
                    break;
                }
                case TokenType.For:
                {
                    var forToken = (ForToken) token;
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
                            forToken.Enumerable = ScriptFunctions
                                .GetDataFolders(forToken.FolderPath, forToken.SearchString, forToken.Recursive)
                                .ToList();
                            _variables.AddOrReplace(forToken.Variable, forToken.Enumerable[0]);
                            break;
                        }
                        case ForToken.ForEnumerationType.PluginFolder:
                        {
                            forToken.Enumerable = ScriptFunctions
                                .GetPluginFolders(forToken.FolderPath, forToken.SearchString, forToken.Recursive)
                                .ToList();
                            _variables.AddOrReplace(forToken.Variable, forToken.Enumerable[0]);
                            break;
                        }

                        case ForToken.ForEnumerationType.DataFile:
                        {
                            forToken.Enumerable = ScriptFunctions
                                .GetDataFiles(forToken.FolderPath, forToken.SearchString, forToken.Recursive)
                                .ToList();
                            _variables.AddOrReplace(forToken.Variable, forToken.Enumerable[0]);
                            break;
                        }
                        case ForToken.ForEnumerationType.Plugin:
                        {
                            forToken.Enumerable = ScriptFunctions
                                .GetPlugins(forToken.FolderPath, forToken.SearchString, forToken.Recursive)
                                .ToList();
                            _variables.AddOrReplace(forToken.Variable, forToken.Enumerable[0]);
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    
                    _stack.Push(forToken);
                    break;
                }
                case TokenType.Continue:
                case TokenType.Exit:
                {
                    if (_stack.All(x => x.Type != TokenType.For))
                        throw new OBMMScriptHandlerException("Exit or Continue token encountered but stack does not contain a For-Token!");

                    var forToken = (ForToken) _stack.First(x => x.Type == TokenType.For);

                    /*
                     * In normal programming a continue in a for-loop will make the pointer jump back to the start of
                     * the for-loop. Here we are doing something simpler: we simply deactivate the For-token so no more
                     * instructions will be executed. The main loop has more information on this but we are basically
                     * just deactivating everything and when we reach the EndFor-Token we check if Exit has been set and
                     * then exit the for-loop or re-activate the For-Token and continue the loop.
                     * Setting Exit is important so we know if it was a Continue or Exit Token that deactivated the For-
                     * Token.
                     */
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
                    var selectToken = (SelectToken) token;
                    
                    if (selectToken.Title.Equals("Install blank age maps for humans and elves?"))
                        Debugger.Break();
                    
                    /*
                     * The pipe character '|' at the beginning of one of the items means it should be auto-selected/be
                     * set as a default option. Since we compare the result with our Case value we need to remove this
                     * pipe after the user selected the option.
                     */
                    selectToken.Results = ScriptFunctions
                        //this is not the LINQ IEnumerable<T>.Select extension but the Select function
                        .Select(
                            selectToken.Items, 
                            selectToken.Previews, 
                            selectToken.Descriptions, 
                            selectToken.Title, 
                            selectToken.IsMany)
                        //this is the LINQ IEnumerable<T>.Select extension where we remove the pipe
                        .Select(x => x[0] == '|' ? x[1..] : x)
                        .ToList();
                    
                    _stack.Push(selectToken);
                    break;
                }
                case TokenType.SelectVar:
                {
                    var selectVarToken = (SelectVarToken) token;
                    if (!_variables.TryGetValue(selectVarToken.Variable, out var value))
                        throw new OBMMScriptHandlerException($"Unable to find variable {selectVarToken.Variable} in dictionary!");
                    selectVarToken.Value = value;
                    _stack.Push(selectVarToken);
                    break;
                }
                case TokenType.SelectString:
                {
                    /*
                     * We actually don't need to do anything here. Most of the stuff that needs to be done for SelectString
                     * is done in the Case token.
                     */
                    break;
                }
                case TokenType.Message:
                {
                    var iToken = (InstructionToken) token;
                    if (iToken.Instructions.Count == 1)
                        ExternalScriptFunctions.Message(iToken.Instructions[0]);
                    else
                        ExternalScriptFunctions.Message(iToken.Instructions[0], iToken.Instructions[1]);
                    break;
                }
                case TokenType.LoadEarly:
                case TokenType.LoadBefore:
                case TokenType.LoadAfter:
                {
                    var iToken = (InstructionToken) token;

                    var plugin = iToken.Instructions[0];
                    var target = new Lazy<string>(() => iToken.Instructions[1]);

                    // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                    switch (token.Type)
                    {
                        case TokenType.LoadEarly:
                            ScriptFunctions.LoadEarly(plugin);
                            break;
                        case TokenType.LoadBefore:
                            ScriptFunctions.LoadBefore(plugin, target.Value);
                            break;
                        case TokenType.LoadAfter:
                            ScriptFunctions.LoadAfter(plugin, target.Value);
                            break;
                    }
                    
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
                            _ => throw new ArgumentOutOfRangeException()
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
                                throw new OBMMScriptHandlerException($"Unable to parse {levelName} as ConflictLevel");
                            if (level == ConflictLevel.Active || level == ConflictLevel.NoConflict)
                                throw new OBMMScriptHandlerException($"Parsed ConflictLevel {level} is {ConflictLevel.Active} or {ConflictLevel.NoConflict}");
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

                            var minMajor = int.Parse(sMinMajor);
                            var minMinor = int.Parse(sMinMinor);
                            var maxMajor = int.Parse(sMaxMajor);
                            var maxMinor = int.Parse(sMaxMinor);

                            cd.MinVersion = new Version(minMajor, minMinor);
                            cd.MaxVersion = new Version(maxMajor, maxMinor);
                            break;
                        }
                        case 6:
                            cd.Comment = iToken.Instructions[5];
                            goto case 5;
                        case 7:
                        {
                            var levelName = iToken.Instructions[2];
                            if (!Utils.TryGetEnum<ConflictLevel>(levelName, out var level))
                                throw new OBMMScriptHandlerException($"Unable to parse {levelName} as ConflictLevel");
                            if (level == ConflictLevel.Active || level == ConflictLevel.NoConflict)
                                throw new OBMMScriptHandlerException($"Parsed ConflictLevel {level} is {ConflictLevel.Active} or {ConflictLevel.NoConflict}");
                            cd.Level = level;
                            goto case 6;
                        }
                    }

                    cd.Partial = token.Type == TokenType.DependsOnRegex || token.Type == TokenType.ConflictsWithRegex;
                    ScriptReturnData.Conflicts.Add(cd);
                    break;
                }
                case TokenType.DontInstallAnyPlugins:
                    ScriptFunctions.DontInstallAnyPlugins();
                    break;
                case TokenType.DontInstallAnyDataFiles:
                    ScriptFunctions.DontInstallAnyDataFiles();
                    break;
                case TokenType.InstallAllPlugins:
                    ScriptFunctions.InstallAllPlugins();
                    break;
                case TokenType.InstallAllDataFiles:
                    ScriptFunctions.InstallAllDataFiles();
                    break;
                case TokenType.InstallPlugin:
                case TokenType.DontInstallPlugin:
                case TokenType.InstallDataFile:
                case TokenType.DontInstallDataFile:
                case TokenType.InstallDataFolder:
                case TokenType.DontInstallDataFolder:
                {
                    var iToken = (InstructionToken) token;

                    var path = iToken.Instructions[0];
                    var recurse = iToken.Instructions.Count >= 2 &&
                                  iToken.Instructions[1].Equals("True", StringComparison.OrdinalIgnoreCase);

                    // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                    switch (token.Type)
                    {
                        case TokenType.InstallPlugin:
                            ScriptFunctions.InstallPlugin(path);
                            break;
                        case TokenType.InstallDataFile:
                            ScriptFunctions.InstallDataFile(path);
                            break;
                        case TokenType.InstallDataFolder:
                            ScriptFunctions.InstallDataFolder(path, recurse);
                            break;
                        case TokenType.DontInstallPlugin:
                            ScriptFunctions.DontInstallPlugin(path);
                            break;
                        case TokenType.DontInstallDataFile:
                            ScriptFunctions.DontInstallDataFile(path);
                            break;
                        case TokenType.DontInstallDataFolder:
                            ScriptFunctions.DontInstallDataFolder(path, recurse);
                            break;
                    }
                    
                    break;
                }
                case TokenType.RegisterBSA:
                case TokenType.UnregisterBSA:
                {
                    var iToken = (InstructionToken) token;
                    var fileName = iToken.Instructions[0];
                    
                    if (token.Type == TokenType.RegisterBSA)
                        ScriptFunctions.RegisterBSA(fileName);
                    else
                        ScriptFunctions.UnregisterBSA(fileName);
                    
                    break;
                }
                case TokenType.FatalError:
                    throw new OBMMScriptHandlerException("Fatal Error called from script!");
                case TokenType.Return:
                {
                    Return = true;
                    return;
                }
                case TokenType.UncheckESP:
                {
                    var iToken = (InstructionToken) token;
                    ScriptFunctions.UncheckEsp(iToken.Instructions[0]);
                    break;
                }
                case TokenType.SetDeactivationWarning:
                {
                    var iToken = (InstructionToken) token;
                    var plugin = iToken.Instructions[0];
                    var warningType = iToken.Instructions[1];

                    if (!Utils.TryGetEnum<DeactiveStatus>(warningType, out var status))
                        throw new OBMMScriptHandlerException($"Unable to parse {warningType} as DeactiveStatus!");
                    
                    ScriptFunctions.SetDeactivationWarning(plugin, status);
                    break;
                }
                case TokenType.CopyDataFile:
                case TokenType.CopyPlugin:
                case TokenType.CopyDataFolder:
                {
                    var iToken = (InstructionToken) token;

                    var from = iToken.Instructions[0];
                    var to = iToken.Instructions[1];
                    var recursive = iToken.Instructions.Count == 3 &&
                                    iToken.Instructions[2].Equals("True", StringComparison.OrdinalIgnoreCase);

                    // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                    switch (token.Type)
                    {
                        case TokenType.CopyDataFile:
                            ScriptFunctions.CopyDataFile(from, to);
                            break;
                        case TokenType.CopyPlugin:
                            ScriptFunctions.CopyPlugin(from, to);
                            break;
                        case TokenType.CopyDataFolder:
                            ScriptFunctions.CopyDataFolder(from, to, recursive);
                            break;
                    }
                    
                    break;
                }
                case TokenType.PatchPlugin:
                case TokenType.PatchDataFile:
                {
                    var iToken = (InstructionToken) token;

                    var from = iToken.Instructions[0];
                    var to = iToken.Instructions[1];
                    var create = iToken.Instructions.Count == 3 &&
                                 iToken.Instructions[2].Equals("True", StringComparison.OrdinalIgnoreCase);
                    
                    if (token.Type == TokenType.PatchPlugin)
                        ScriptFunctions.PatchPlugin(from, to, create);
                    else
                        ScriptFunctions.PatchDataFile(from, to, create);

                    break;
                }
                case TokenType.EditINI:
                {
                    var iToken = (InstructionToken) token;

                    var section = iToken.Instructions[0];
                    var key = iToken.Instructions[1];
                    var newValue = iToken.Instructions[2];
                    
                    ScriptFunctions.EditINI(section, key, newValue);
                    break;
                }
                case TokenType.EditSDP:
                case TokenType.EditShader:
                {
                    var iToken = (InstructionToken) token;

                    var shaderPackage = iToken.Instructions[0];
                    var shaderName = iToken.Instructions[1];
                    var binaryObjectPath = iToken.Instructions[2];
                    var package = byte.Parse(shaderPackage);
                    
                    ScriptFunctions.EditShader(package, shaderName, binaryObjectPath);
                    break;
                }
                case TokenType.SetGMST:
                case TokenType.SetGlobal:
                {
                    var iToken = (InstructionToken) token;

                    var file = iToken.Instructions[0];
                    var editorId = iToken.Instructions[1];
                    var value = iToken.Instructions[2];
                    
                    if (token.Type == TokenType.SetGMST)
                        ScriptFunctions.SetGMST(file, editorId, value);
                    else
                        ScriptFunctions.SetGlobal(file, editorId, value);

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
                    var sData = iToken.Instructions[2];
                    
                    var offset = long.Parse(sOffset);

                    // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                    switch (token.Type)
                    {
                        case TokenType.SetPluginByte:
                            ScriptFunctions.SetPluginByte(file, offset, byte.Parse(sData));
                            break;
                        case TokenType.SetPluginShort:
                            ScriptFunctions.SetPluginShort(file, offset, short.Parse(sData));
                            break;
                        case TokenType.SetPluginInt:
                            ScriptFunctions.SetPluginInt(file, offset, int.Parse(sData));
                            break;
                        case TokenType.SetPluginLong:
                            ScriptFunctions.SetPluginLong(file, offset, long.Parse(sData));
                            break;
                        case TokenType.SetPluginFloat:
                            ScriptFunctions.SetPluginFloat(file, offset, float.Parse(sData));
                            break;
                    }
                    
                    break;
                }
                case TokenType.DisplayImage:
                case TokenType.DisplayText:
                {
                    var iToken = (InstructionToken) token;
                    
                    var path = iToken.Instructions[0];
                    var lazyTitle = new Lazy<string>(() => iToken.Instructions[1]);

                    if (token.Type == TokenType.DisplayImage)
                    {
                        if (iToken.Instructions.Count == 2)
                            ScriptFunctions.DisplayImage(path, lazyTitle.Value);
                        else
                            ScriptFunctions.DisplayImage(path);
                    }
                    else
                    {
                        if (iToken.Instructions.Count == 2)
                            ScriptFunctions.DisplayText(path, lazyTitle.Value);
                        else
                            ScriptFunctions.DisplayText(path);
                    }
                    
                    break;
                }
                case TokenType.SetVar:
                {
                    var setVarToken = (SetVarToken) token;
                    _variables.AddOrReplace(setVarToken.Variable, setVarToken.Value);
                    break;
                }
                case TokenType.GetFolderName:
                case TokenType.GetDirectoryName:
                case TokenType.GetFileName:
                case TokenType.GetFileNameWithoutExtension:
                {
                    var iToken = (InstructionToken) token;

                    var variable = iToken.Instructions[0];
                    var path = iToken.Instructions[1];

                    if (token.Type == TokenType.GetFolderName || token.Type == TokenType.GetDirectoryName)
                    {
                        _variables.AddOrReplace(variable, Path.GetFileName(path));
                    }
                    else
                    {
                        _variables.AddOrReplace(variable, token.Type == TokenType.GetFileName
                            ? Path.GetFileName(path)
                            : Path.GetFileNameWithoutExtension(path));
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
                    var firstArgument = iToken.Instructions[1];
                    var secondArgument = new Lazy<string>(() => iToken.Instructions[2]);
                    var thirdArgument = new Lazy<string>(() => iToken.Instructions[3]);

                    if (token.Type == TokenType.CombinePaths)
                    {
                        _variables.AddOrReplace(variable, Path.Combine(firstArgument, secondArgument.Value));
                    } else if (token.Type == TokenType.StringLength)
                    {
                        _variables.AddOrReplace(variable, firstArgument.Length.ToString());
                    }
                    else
                    {
                        var startFrom = int.Parse(secondArgument.Value);
                        var length = new Lazy<int>(() => int.Parse(thirdArgument.Value));
                        
                        if (token.Type == TokenType.Substring)
                        {
                            _variables.AddOrReplace(variable, firstArgument.Substring(startFrom,
                                iToken.Instructions.Count == 4 ? length.Value : firstArgument.Length - startFrom));
                        }
                        else
                        {
                            _variables.AddOrReplace(variable, firstArgument.Remove(startFrom, 
                                iToken.Instructions.Count == 4 ? length.Value : firstArgument.Length - startFrom));
                        }
                    }
                    
                    break;
                }
                case TokenType.InputString:
                {
                    var iToken = (InstructionToken) token;

                    var variable = iToken.Instructions[0];
                    var title = iToken.Instructions.Count > 1 ? iToken.Instructions[1] : string.Empty;
                    var initial = iToken.Instructions.Count > 2 ? iToken.Instructions[2] : string.Empty;

                    var result = ScriptFunctions.InputString(title, initial);
                    _variables.AddOrReplace(variable, result);

                    break;
                }
                case TokenType.ReadINI:
                case TokenType.ReadRendererInfo:
                {
                    var iToken = (InstructionToken) token;
                    
                    var variable = iToken.Instructions[0];
                    var firstArgument = iToken.Instructions[1];
                    var secondArgument = new Lazy<string>(() => iToken.Instructions[2]);

                    var result = token.Type == TokenType.ReadINI
                        ? ScriptFunctions.ReadINI(firstArgument, secondArgument.Value)
                        : ScriptFunctions.ReadRendererInfo(firstArgument);
                    
                    _variables.AddOrReplace(variable, result);
                    
                    break;
                }
                case TokenType.ExecLine:
                {
                    /*
                     * This dynamically executes the provided arguments as if it was part of the script. Not sure how
                     * go about implementing this as it would require tokenizing the line, inserting it into the current
                     * token-list and then letting it execute...
                     * Not impossible but very hacky so will probably implement once I find a script that uses this.
                     */
                    throw new NotImplementedException("ExecLine is not supported by OMODFramework at the moment." +
                                                      "Please open an issue on GitHub with the current script attached.");
                }
                case TokenType.iSet:
                case TokenType.fSet:
                {
                    var setToken = (SetToken) token;

                    var isInt = token.Type == TokenType.iSet;
                    var iRes = 0;
                    var fRes = 0.0;
                    
                    var expression = setToken.Expression.Select(ReplaceWithVariable).ToList();

                    if (isInt)
                        iRes = NumericExpressionHandler.EvaluateIntExpression(expression);
                    else
                        fRes = NumericExpressionHandler.EvaluateFloatExpression(expression);
                    
                    _variables.AddOrReplace(setToken.Variable, isInt ? iRes.ToString() : fRes.ToString(CultureInfo.InvariantCulture));
                    
                    break;
                }
                case TokenType.EditXMLLine:
                case TokenType.EditXMLReplace:
                {
                    var iToken = (InstructionToken) token;
                    
                    var file = iToken.Instructions[0];
                    var firstArgument = iToken.Instructions[1];
                    var secondArgument = iToken.Instructions[2];

                    if (token.Type == TokenType.EditXMLLine)
                    {
                        var lineNumber = int.Parse(firstArgument);
                        ScriptFunctions.EditXMLLine(file, lineNumber, secondArgument);
                    }
                    else
                    {
                        ScriptFunctions.EditXMLReplace(file, firstArgument, secondArgument);
                    }
                    
                    break;
                }
                case TokenType.AllowRunOnLines:
                    //TODO: AllowRunOnLines
                    /*
                     * This enables the use of '\' characters at the end of a line to make it multi-line. The problem is
                     * that we don't evaluate this before we transform the script since we don't parse the script line
                     * by line.
                     * Don't know what should be done with this so leaving this as it is for now.
                     */
                    break;
                case TokenType.EndIf:
                case TokenType.EndFor:
                case TokenType.Break:
                case TokenType.EndSelect:
                case TokenType.Label:
                case TokenType.Goto:
                case TokenType.Comment:
                    throw new OBMMScriptHandlerException("Impossible to reach!");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
