﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace OMODFramework.Scripting
{
    public partial class OBMMScriptHandler
    {
        /// <summary>
        /// Utility attribute for <see cref="TokenType"/>. Used in <see cref="OBMMScriptHandler.ValidateLine"/>
        /// to see if a line has the required amount of arguments.
        /// </summary>
        [AttributeUsage(AttributeTargets.Field)]
        private class LineAttribute : Attribute
        {
            internal readonly int Min;
            internal readonly int Max;

            internal LineAttribute(int length)
            {
                Min = length;
                Max = length;
            }

            internal LineAttribute(int min, int max)
            {
                Min = min;
                Max = max;
            }
        }

        /// <summary>
        /// All possible tokens
        /// </summary>
        private enum TokenType
        {
            //Logic
            [Line(2, 3)]
            If,
            [Line(2, 3)]
            IfNot,
            [Line(0)]
            Else,
            [Line(0)]
            EndIf,
            [Line(1, -1)]
            Case,
            [Line(0)]
            Default,
            [Line(1, -1)]
            For,
            [Line(0)]
            EndFor,
            [Line(0)]
            Break,
            [Line(0)]
            Continue,
            [Line(0)]
            Exit,
            [Line(1)]
            Label,
            [Line(1)]
            Goto,
            [Line(1)]
            Commend,

            //Select
            [Line(2, -1)]
            Select,
            [Line(2, -1)]
            SelectMany,
            [Line(2, -1)]
            SelectWithPreview,
            [Line(2, -1)]
            SelectManyWithPreview,
            [Line(2, -1)]
            SelectWithDescriptions,
            [Line(2, -1)]
            SelectManyWithDescriptions,
            [Line(2, -1)]
            SelectWithDescriptionsAndPreviews,
            [Line(2, -1)]
            SelectManyWithDescriptionsAndPreviews,
            [Line(1)]
            SelectVar,
            [Line(1)]
            SelectString,
            [Line(0)]
            EndSelect,

            //Functions
            [Line(1)]
            Message,
            [Line(1)]
            LoadEarly,
            [Line(2)]
            LoadBefore,
            [Line(2)]
            LoadAfter,
            [Line(1, -1)]
            ConflictsWith,
            [Line(1, -1)]
            DependsOn,
            [Line(1, -1)]
            ConflictsWithRegex,
            [Line(1, -1)]
            DependsOnRegex,
            [Line(0)]
            DontInstallAnyPlugins,
            [Line(0)]
            DontInstallAnyDataFiles,
            [Line(0)]
            InstallAllPlugins,
            [Line(0)]
            InstallAllDataFiles,
            [Line(1)]
            InstallPlugin,
            [Line(1)]
            DontInstallPlugin,
            [Line(1)]
            InstallDataFile,
            [Line(1)]
            DontInstallDataFile,
            [Line(2, 3)]
            DontInstallDataFolder,
            [Line(2, 3)]
            InstallDataFolder,
            [Line(2)]
            RegisterBSA,
            [Line(2)]
            UnregisterBSA,
            [Line(0)]
            FatalError,
            [Line(0)]
            Return,
            [Line(1)]
            UncheckESP,
            [Line(3)]
            SetDeactivationWarning,
            [Line(2)]
            CopyDataFile,
            [Line(2)]
            CopyPlugin,
            [Line(2, 3)]
            CopyDataFolder,
            [Line(2, 3)]
            PatchPlugin,
            [Line(2, 3)]
            PatchDataFile,
            [Line(3)]
            EditINI,
            [Line(3)]
            EditSDP,
            [Line(3)]
            EditShader,
            [Line(3)]
            SetGMST,
            [Line(3)]
            SetGlobal,
            [Line(3)]
            SetPluginByte,
            [Line(3)]
            SetPluginShort,
            [Line(3)]
            SetPluginInt,
            [Line(3)]
            SetPluginLong,
            [Line(3)]
            SetPluginFloat,
            [Line(2, 3)]
            DisplayImage,
            [Line(2, 3)]
            DisplayText,
            [Line(2)]
            SetVar,
            [Line(3)]
            GetFolderName,
            [Line(3)]
            GetDirectoryName,
            [Line(2)]
            GetFileName,
            [Line(2)]
            GetFileNameWithoutExtension,
            [Line(3)]
            CombinePaths,
            [Line(3, 4)]
            Substring,
            [Line(3, 4)]
            RemoveString,
            [Line(2)]
            StringLength,
            [Line(1, 3)]
            InputString,
            [Line(3)]
            ReadINI,
            [Line(2)]
            ReadRendererInfo,
            [Line(1)]
            ExecLines,
            [Line(2, -1)]
            iSet,
            [Line(2, -1)]
            fSet,
            [Line(3)]
            EditXMLLine,
            [Line(3)]
            EditXMLReplace,
            [Line(0)]
            AllowRunOnLines,
        }

        private class Token
        {
            internal virtual TokenType Type { get; set; }

            public override string ToString()
            {
                return $"{Type}";
            }
        }

        private class FatalErrorToken : Token { }

        private sealed class CommentToken : Token
        {
            internal override TokenType Type => TokenType.Commend;
            internal string Comment { get; set; } = string.Empty;

            public override string ToString()
            {
                return $"Comment: {Comment}";
            }
        }

        private class StartFlowToken : Token { }
        private class MidFlowToken : Token { }
        private class EndFlowToken : Token { }

        private class InstructionToken : Token
        {
            internal readonly IReadOnlyList<string> Instructions;

            internal InstructionToken(IReadOnlyList<string> instructions)
            {
                Instructions = instructions;
            }

            public override string ToString()
            {
                return Instructions.Count == 0 ? $"{Type}" : $"{Type}: {Instructions.ToAggregatedString()}";
            }
        }

        private sealed class SetVarToken : InstructionToken
        {
            internal override TokenType Type => TokenType.SetVar;

            internal readonly string Variable;
            internal readonly string Value;

            internal SetVarToken(IReadOnlyList<string> instructions) : base(instructions)
            {
                Variable = instructions[0];
                Value = instructions[1];
            }

            public override string ToString()
            {
                return $"{Type}: {Variable} to {Value}";
            }
        }

        private sealed class IfToken : StartFlowToken
        {
            internal enum IfConditionType
            {
                DialogYesNo,
                DataFileExists,
                VersionGreaterThan,
                VersionLessThan,
                ScriptExtenderPresent,
                ScriptExtenderNewerThan,
                GraphicsExtenderPresent,
                GraphicsExtenderNewerThan,
                OblivionNewerThan,
                Equal,
                GreaterEqual,
                GreaterThan,
                fGreaterEqual,
                fGreaterThan
            }

            internal override TokenType Type => TokenType.If;
            internal readonly IfConditionType ConditionType;
            internal readonly IReadOnlyList<string> Arguments;
            internal readonly bool Not;

            internal IfToken(IfConditionType type, IReadOnlyList<string> arguments, bool not = false)
            {
                ConditionType = type;
                Arguments = arguments;
                Not = not;
            }

            public override string ToString()
            {
                return $"If {ConditionType} {Arguments.ToAggregatedString()}";
            }
        }

        private sealed class SelectToken : StartFlowToken
        {
            internal readonly string Title;
            internal readonly bool IsMany;
            internal readonly bool HasPreviews;
            internal readonly bool HasDescriptions;

            internal readonly IReadOnlyList<string> Items;
            internal readonly IReadOnlyList<string> Previews;
            internal readonly IReadOnlyList<string> Descriptions;

            internal SelectToken(TokenType type, IReadOnlyList<string> arguments)
            {
                Title = arguments[0];
                Type = type;
                // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                switch (type)
                {
                    case TokenType.Select:
                        break;
                    case TokenType.SelectMany:
                        IsMany = true;
                        break;
                    case TokenType.SelectWithPreview:
                        HasPreviews = true;
                        break;
                    case TokenType.SelectManyWithPreview:
                        IsMany = true;
                        HasPreviews = true;
                        break;
                    case TokenType.SelectWithDescriptions:
                        HasDescriptions = true;
                        break;
                    case TokenType.SelectManyWithDescriptions:
                        IsMany = true;
                        HasDescriptions = true;
                        break;
                    case TokenType.SelectWithDescriptionsAndPreviews:
                        HasDescriptions = true;
                        HasPreviews = true;
                        break;
                    case TokenType.SelectManyWithDescriptionsAndPreviews:
                        IsMany = true;
                        HasDescriptions = true;
                        HasPreviews = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                var argsPerOptions = 1 + (HasPreviews ? 1 : 0) + (HasDescriptions ? 1 : 0);
                if((arguments.Count-1) % argsPerOptions != 0)
                    throw new OBMMScriptingTokenizationException(new Line(type){Arguments = arguments.ToList() }.ToString(), $"Select has too many arguments. Amount of arguments: {arguments.Count-1}, has previews: {(HasPreviews ? "true" : "false")}, has descriptions: {(HasDescriptions ? "true" : "false")}, argsPerOptions: {argsPerOptions}. This usually means the script is broken.");

                var l = arguments.Count - 1 / argsPerOptions;

                var items = new List<string>();
                var previews = new List<string>();
                var descriptions = new List<string>();

                for (var i = 1; i <= l; i += argsPerOptions)
                {
                    if (arguments.Count == i)
                        break;
                    items.Add(arguments.ElementAt(i));
                    if (HasPreviews)
                    {
                        previews.Add(arguments.ElementAt(i + 1));
                        if(HasDescriptions)
                            descriptions.Add(arguments.ElementAt(i + 2));
                    }
                    else
                    {
                        if(HasDescriptions)
                            descriptions.Add(arguments.ElementAt(i + 1));
                    }
                }

                Items = items;
                Previews = previews;
                Descriptions = descriptions;
            }
        }

        private void TokenizeScript(string script)
        {
            var lines = script
                .Replace("\r", "")
                .Split("\n")
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            //the code above would work if we have one statement per line but
            //you can use the \ character at the end of a line to include the
            //next line in the same statement. This means we have to make it one line

            var stack = new Stack<string>();
            var list = new List<string>();
            foreach (var current in lines)
            {
                if (current.EndsWith("\\"))
                {
                    stack.Push(current[..^1].Trim());
                }
                else
                {
                    if (stack.Count == 0)
                    {
                        list.Add(current);
                        continue;
                    }

                    var s = stack.Reverse().ToAggregatedString(" ");
                    s += $" {current}";
                    list.Add(s);
                    stack.Clear();
                }
            }

            lines = list;
            _tokens = lines.Select(TokenizeLine).ToHashSet();
        }

        private struct Line
        {
            internal readonly TokenType TokenType;
            internal List<string>? Arguments;

            internal Line(TokenType tokenType)
            {
                TokenType = tokenType;
                Arguments = null;
            }

            public override string ToString()
            {
                return Arguments == null ? TokenType.ToString() : $"{TokenType} {Arguments.ToAggregatedString(" ")}";
            }
        }

        private static Line ParseLine(string l)
        {
            var split = l.Split(" ");
            var tokenName = split.Length == 0 ? l : split.First();

            if (!Utils.TryGetEnum<TokenType>(tokenName, out var token))
                throw new OBMMScriptingTokenParseException(tokenName);

            var line = new Line(token);

            if(split.Length == 0)
                return line;

            split = split.TakeLast(split.Length - 1).ToArray();

            if (!l.Contains("\""))
            {
                line.Arguments = new List<string>(split);
                return line;
            }

            var quoteSplit = l.Substring(tokenName.Length+1, l.Length-tokenName.Length-1)
                .Split("\"")
                .ToArray();
            line.Arguments = new List<string>();

            for (var i = 0; i < split.Length; i++)
            {
                var current = split[i];
                if (current.StartsWith("\""))
                {
                    current = current[1..];
                    if (current.StartsWith("\""))
                    {
                        //in this case we got \"\" meaning an empty string eg:
                        //SetVar Something ""
                        //in this case we add the empty string and continue
                        line.Arguments.Add(string.Empty);
                        continue;
                    }

                    if (current.EndsWith("\","))
                        current = current[..^2];

                    if (current.EndsWith("\""))
                        current = current[..^1];

                    var first = quoteSplit.First(x => x.StartsWith(current) && line.Arguments.All(y => y != x));
                    line.Arguments.Add(first);
                    i += first.Count(x => x == ' ');
                    continue;
                }

                line.Arguments.Add(current);
            }

            return line;
        }

        private static void ValidateLine(Line line)
        {
            var type = line.TokenType.GetType();
            if (!type.IsEnum)
                throw new ArgumentException($"{nameof(line.TokenType)} must be of type Enum", nameof(line.TokenType));

            var memberInfo = type.GetMember(line.TokenType.ToString());
            var attributes = memberInfo[0].GetCustomAttributes(typeof(LineAttribute), false);

            if (!(attributes[0] is LineAttribute attribute))
                throw new OBMMScriptingTokenizationException(line.ToString(), $"Could not parse line attribute for token: {line.TokenType}. This should not have happened, please report this.");

            if (line.Arguments == null)
            {
                if (attribute.Min != attribute.Max)
                    throw new OBMMScriptingTokenizationException(line.ToString(),
                        $"Line does not have any arguments but is supposed to have at least {attribute.Min}{(attribute.Max == -1 ? "" : $" and max {attribute.Max}")}!");
                
                if (attribute.Min != 0)
                    throw new OBMMScriptingTokenizationException(line.ToString(), $"Line does not have any arguments but is supposed to have exactly {attribute.Min}!");
                return;
            }

            if (attribute.Min == attribute.Max)
            {
                if(attribute.Min == -1)
                    throw new NotImplementedException();

                if(line.Arguments.Count != attribute.Min)
                    throw new OBMMScriptingTokenizationException(line.ToString(), $"Line can only have {attribute.Min} attributes but has {line.Arguments.Count}!");
                return;
            }

            if(line.Arguments.Count < attribute.Min)
                throw new OBMMScriptingTokenizationException(line.ToString(), $"Line is supposed to have at least {attribute.Min} attributes but has {line.Arguments.Count}!");

            if (attribute.Max == -1)
                return;

            if(line.Arguments.Count > attribute.Max)
                throw new OBMMScriptingTokenizationException(line.ToString(), $"Line is supposed to have max {attribute.Max} attributes but has {line.Arguments.Count}!");
        }

        private static Token TokenizeLine(string l)
        {
            //comments start with a ;
            if (l.StartsWith(";"))
            {
                return new CommentToken
                {
                    Comment = l.Replace(";", "").Trim()
                };
            }

            /*
             * Plan is to read the script line by line and tokenize it.
             * First we convert a plain string to a Line.
             *
             * This means that:
             *
             *  If VersionLessThan 0.9.13
             *      Message "Something"
             *      FatalError
             *  EndIf
             *
             *  becomes:
             *
             *  Line: Type: If Arguments:{VersionLessThan, 0.9.13}
             *  Line: Type: Message Arguments:{Something}
             *  Line: Type: FatalError
             *  Line: Type: EndIf
             *
             * The line will then be validated where we check the amount of
             * arguments required. After validation create a new token based on the
             * line:
             *
             *  Token: Type: If ConditionType: VersionLessThan Arguments:{0.9.13}
             *  Token: Type: Message Value: Something
             *  Token: Type: FatalError
             *  Token: Type: EndIf
             *
             * This might seem a bit over-engineered but I wanted to create something
             * strongly typed instead of using plain strings for everything. This ensures
             * that the script is not broken and that we can use the same functions that
             * other ScriptHandlers (eg C#) also use.
             */

            var line = ParseLine(l);

            ValidateLine(line);

            var token = line.TokenType;

            if (token == TokenType.EndIf || token == TokenType.EndFor || token == TokenType.EndSelect)
                return new EndFlowToken{Type = token};

            if (token == TokenType.If || token == TokenType.If)
            {
                var conditionName = line.Arguments!.First();
                if(!Utils.TryGetEnum<IfToken.IfConditionType>(conditionName, out var conditionType))
                    throw new OBMMScriptingTokenParseException(conditionName);

                return new IfToken(conditionType, line.Arguments.TakeLast(line.Arguments!.Count-1).ToList(), token == TokenType.IfNot);
            }

            if (token == TokenType.Select || token == TokenType.SelectMany ||
                token == TokenType.SelectManyWithDescriptions ||
                token == TokenType.SelectManyWithDescriptionsAndPreviews ||
                token == TokenType.SelectWithDescriptions || token == TokenType.SelectWithDescriptionsAndPreviews ||
                token == TokenType.SelectWithPreview)
            {
                return new SelectToken(token, line.Arguments!);
            }

            if(token == TokenType.Else || token == TokenType.Exit || token == TokenType.Goto || token == TokenType.Return || token == TokenType.Break)
                return new MidFlowToken { Type = token };

            if (token == TokenType.FatalError)
                return new FatalErrorToken { Type = token };

            if (token == TokenType.SetVar)
                return new SetVarToken(line.Arguments!);

            return new InstructionToken(line.Arguments ?? new List<string>())
            {
                Type = token
            };
        }
    }
}
