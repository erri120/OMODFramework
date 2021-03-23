using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using OMODFramework.Scripting.Exceptions;

namespace OMODFramework.Scripting.ScriptHandlers.OBMMScript.Tokenizer
{
    internal class Token
    {
        internal virtual TokenType Type { get; set; }

        public override string ToString()
        {
            return $"{Type}";
        }
    }

    /// <summary>
    /// A comment without the starting ';'
    /// </summary>
    internal sealed class CommentToken : Token
    {
        internal override TokenType Type => TokenType.Comment;

        internal string Comment { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"Comment: {Comment}";
        }
    }

    /// <summary>
    /// Token that marks the start of a new flow block. A block has a start token and an end token, eg:
    /// If (<see cref="StartFlowToken"/>) and EndIf (<see cref="EndFlowToken"/>). This token also contains all children
    /// that are in the block as well as a bool whether or not the block is active
    /// (eg for If token: if the condition is true).
    /// </summary>
    internal class StartFlowToken : Token
    {
        internal virtual bool Active { get; set; }
        internal List<Token> Children { get; } = new List<Token>();

        /// <summary>
        /// Recursive function that will give the total number of children the StartFlowToken has. This also includes
        /// the amount of children the children have.
        /// </summary>
        /// <returns></returns>
        internal int GetTotalChildrenCount()
        {
            var count = 0;
            foreach (var child in Children)
            {
                count++;
                if (child is StartFlowToken startFlowToken)
                    count += startFlowToken.GetTotalChildrenCount();
            }
            return count;
        }
    }

    /// <summary>
    /// Token that marks the end of a flow block.
    /// </summary>
    internal sealed class EndFlowToken : Token {
        
        internal EndFlowToken(TokenType tokenType)
        {
            Type = tokenType;
        }
    }

    /// <summary>
    /// Basic parent class for all instruction based tokens
    /// </summary>
    internal class InstructionToken : Token
    {
        internal IReadOnlyList<string> Instructions { get; set; }

        internal InstructionToken(Line line, bool argsNull = false)
        {
            Instructions = line.Arguments ?? (argsNull
                ? new List<string>()
                : throw new ArgumentException("Arguments List of Line is null!", nameof(line)));
        }

        public override string ToString()
        {
            return $"{Type}{(Instructions.Count == 0 ? "" : " "+Instructions.Aggregate((x, y) => $"{x},{y}"))}";
        }
    }

    /// <summary>
    /// <see cref="InstructionToken"/> for the <see cref="TokenType.SetVar"/> token.
    /// </summary>
    internal sealed class SetVarToken : InstructionToken
    {
        internal override TokenType Type => TokenType.SetVar;

        /// <summary>
        /// The name of the variable we want to change.
        /// </summary>
        internal string Variable => Instructions[0];
        
        /// <summary>
        /// The new value we want to change the variable to.
        /// </summary>
        internal string Value => Instructions[1];

        internal SetVarToken(Line line) : base(line) { }

        public override string ToString()
        {
            return $"{Type}: {Variable} to {Value}";
        }
    }

    /// <summary>
    /// <see cref="StartFlowToken"/> for the <see cref="TokenType.If"/> and <see cref="TokenType.IfNot"/> tokens.
    /// </summary>
    internal sealed class IfToken : StartFlowToken
    {
        /// <summary>
        /// All possible condition types.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        internal enum IfConditionType
        {
            /// <summary>
            /// Displays a dialog box with a message and Yes/No buttons. Condition is met if user clicked on Yes.
            /// </summary>
            DialogYesNo,
            /// <summary>
            /// Condition is met if the provided data file (Arguments[0]) exists.
            /// </summary>
            DataFileExists,
            /// <summary>
            /// Condition is met if the current OBMM version is greater than the version provided (Arguments[0]).
            /// </summary>
            VersionGreaterThan,
            /// <summary>
            /// Condition is met if the current OBMM version is less than the version provided (Arguments[0]).
            /// </summary>
            VersionLessThan,
            /// <summary>
            /// Condition is met if the Oblivion Script Extender is present. OBMM looks for <code>obse_loader.exe</code>
            /// in the Oblivion game directory.
            /// </summary>
            ScriptExtenderPresent,
            /// <summary>
            /// Condition is met if the Oblivion Script Extender has a newer/greater version than the provided one
            /// (Arguments[0]). OBMM uses <see cref="FileVersionInfo.GetVersionInfo"/> on <code>obse_loader.exe</code>
            /// in the Oblivion game directory.
            /// </summary>
            ScriptExtenderNewerThan,
            /// <summary>
            /// Condition is met if the Oblivion Graphics Extender is present. OBMM looks for
            /// <code>data\obse\plugins\obge.dll</code> in the Oblivion game directory.
            /// </summary>
            GraphicsExtenderPresent,
            /// <summary>
            /// Condition is met if the Oblivion Graphics Extender has a newer/greater version than the provided one
            /// (Arguments[0]). OBMM uses <see cref="FileVersionInfo.GetVersionInfo"/> on
            /// <code>data\obse\plugins\obge.dll</code> in the Oblivion game directory.
            /// </summary>
            GraphicsExtenderNewerThan,
            /// <summary>
            /// Condition is met if Oblivion has a newer/greater version than the provided one (Arguments[0]). OBMM uses
            /// <see cref="FileVersionInfo.GetVersionInfo"/> on <code>Oblivion.exe</code> in the Oblivion game
            /// directory. 
            /// </summary>
            OblivionNewerThan,
            /// <summary>
            /// Condition is met if two arguments are the same. Those 2 Arguments (Arguments[0] and Arguments[1]) are
            /// both strings and <see cref="string.Equals(string?)"/> is used for equality checking.
            /// </summary>
            Equal,
            /// <summary>
            /// Parsed both Arguments (Arguments[0] and Arguments[1]) as integers using <see cref="int.TryParse(string?,out int)"/>
            /// and comparing them using <code>arg1 &gt;= arg2</code>
            /// </summary>
            GreaterEqual,
            /// <summary>
            /// Parsing both Arguments (Arguments[0] and Arguments[1]) as integers using <see cref="int.TryParse(string?,out int)"/>
            /// and comparing them using <code>arg1 &lt; arg2</code>
            /// </summary>
            GreaterThan,
            /// <summary>
            /// Parsing both Arguments (Arguments[0] and Arguments[1]) as floats using <see cref="float.TryParse(string?,out float)"/>
            /// and comparing them using <code>arg1 &gt;= arg2</code>
            /// </summary>
            fGreaterEqual,
            /// <summary>
            /// Parsing both Arguments (Arguments[0] and Arguments[1]) as floats using <see cref="float.TryParse(string?,out float)"/>
            /// and comparing them using <code>arg1 &lt; arg2</code>
            /// </summary>
            fGreaterThan
        }

        /// <summary>
        /// Type of the condition.
        /// </summary>
        internal readonly IfConditionType ConditionType;
        
        /// <summary>
        /// List of all Arguments
        /// </summary>
        internal readonly IReadOnlyList<string> Arguments;
        
        /// <summary>
        /// Whether the condition should be negated (IfNot)
        /// </summary>
        internal readonly bool Not;

        internal IfToken(Line line, TokenType tokenType)
        {
            if (line.Arguments == null)
                throw new ArgumentException("Arguments List of Line is null!", nameof(line));

            var conditionName = line.Arguments[0];
            if (!Utils.TryGetEnum<IfConditionType>(conditionName, out var conditionType))
                throw new OBMMScriptTokenizerException($"Unable to parse {conditionName} as IfConditionType!");
            
            ConditionType = conditionType;
            Arguments = line.Arguments.TakeLast(line.Arguments.Count - 1).ToList();
            Type = tokenType;
            Not = Type == TokenType.IfNot;
        }
    }

    /// <summary>
    /// Parent <see cref="StartFlowToken"/> class for all Select-type tokens
    /// </summary>
    internal class SelectiveToken : StartFlowToken
    {
        /// <summary>
        /// Helper variable so we don't trigger and evaluate subsequent "Case" tokens if we already found one that
        /// satisfied its condition.
        /// </summary>
        internal bool FoundCase { get; set; }
        
        /// <summary>
        /// Select tokens are always active
        /// </summary>
        internal override bool Active => true;
        
        /// <summary>
        /// List of all Arguments
        /// </summary>
        protected readonly IReadOnlyList<string> Arguments;

        internal SelectiveToken(Line line)
        {
            Arguments = line.Arguments 
                        ?? throw new ArgumentException("Arguments List of Line is null!", nameof(line));
        }
    }

    /// <summary>
    /// <see cref="SelectiveToken"/> for <see cref="TokenType.SelectVar"/> token.
    /// </summary>
    internal sealed class SelectVarToken : SelectiveToken
    {
        internal override TokenType Type => TokenType.SelectVar;

        /// <summary>
        /// Name of the variable we select
        /// </summary>
        internal string Variable => Arguments[0];

        /// <summary>
        /// Value of the variable
        /// </summary>
        internal string Value { get; set; } = string.Empty;

        internal SelectVarToken(Line line) : base(line) { }
    }

    /// <summary>
    /// <see cref="SelectiveToken"/> for <see cref="TokenType.SelectString"/> token.
    /// </summary>
    internal sealed class SelectStringToken : SelectiveToken
    {
        internal override TokenType Type => TokenType.SelectString;

        /// <summary>
        /// Value we select
        /// </summary>
        internal string Value => Arguments[0];

        internal SelectStringToken(Line line) : base(line) { }
    }

    /// <summary>
    /// <see cref="SelectiveToken"/> for all Select tokens (without <see cref="TokenType.SelectVar"/> and
    /// <see cref="TokenType.SelectString"/>, those have <see cref="SelectVarToken"/> and <see cref="SelectStringToken"/>)
    /// </summary>
    internal sealed class SelectToken : SelectiveToken
    {
        internal string Title => Arguments[0];
        internal readonly bool IsMany;
        private readonly bool _hasPreviews;
        private readonly bool _hasDescriptions;

        internal readonly IReadOnlyList<string> Items;
        internal readonly IReadOnlyList<string> Previews;
        internal readonly IReadOnlyList<string> Descriptions;

        internal List<string> Results { get; set; } = new List<string>();
        
        internal SelectToken(Line line, TokenType tokenType) : base(line)
        {
            Type = tokenType;
            
            if (line.Arguments == null)
                throw new ArgumentException("Arguments of line is null!", nameof(line));

            Type = line.TokenType;
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (Type)
            {
                case TokenType.Select:
                    break;
                case TokenType.SelectMany:
                    IsMany = true;
                    break;
                case TokenType.SelectWithPreview:
                    _hasPreviews = true;
                    break;
                case TokenType.SelectManyWithPreview:
                    IsMany = true;
                    _hasPreviews = true;
                    break;
                case TokenType.SelectWithDescriptions:
                    _hasDescriptions = true;
                    break;
                case TokenType.SelectManyWithDescriptions:
                    IsMany = true;
                    _hasDescriptions = true;
                    break;
                case TokenType.SelectWithDescriptionsAndPreviews:
                    _hasDescriptions = true;
                    _hasPreviews = true;
                    break;
                case TokenType.SelectManyWithDescriptionsAndPreviews:
                    IsMany = true;
                    _hasDescriptions = true;
                    _hasPreviews = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Type), $"Unknown Type: {Type}");
            }

            /*
             * A Select statement has multiple options you can select. Each option can have different amounts of
             * additional arguments depending on the token type, eg:
             *
             *  Select "Your Name" "Bob" "Peter"
             *
             * argsPerOptions is going to be 1 because we use the base Select statement. SelectWithPreview has more:
             *
             *  SelectWithPreview "Your Name" "Bob" "bob.jpg" "Peter" "peter.jpg"
             *
             * Here each option takes up 2 arguments, one for the actual value and one for the image path.
             */
            var argsPerOptions = 1 + (_hasPreviews ? 1 : 0) + (_hasDescriptions ? 1 : 0);
            //-1 because the title is at [0]
            if ((line.Arguments.Count - 1) % argsPerOptions != 0)
                throw new OBMMScriptTokenizerException($"Select has too many arguments. Amount of arguments: {line.Arguments.Count - 1}, has previews: {(_hasPreviews ? "true" : "false")}, has descriptions: {(_hasDescriptions ? "true" : "false")}, argsPerOptions: {argsPerOptions}. This usually means the script is broken.");
            
            var items = new List<string>();
            var previews = new List<string>();
            var descriptions = new List<string>();
            
            var l = line.Arguments.Count - 1 / argsPerOptions;
            for (var i = 1; i <= l; i += argsPerOptions)
            {
                if (line.Arguments.Count == i) break;
                
                items.Add(line.Arguments[i]);
                
                //the image path for the preview always goes before the description
                if (_hasPreviews)
                {
                    previews.Add(line.Arguments[i + 1]);
                    if (_hasDescriptions)
                        descriptions.Add(line.Arguments[i + 2]);
                }
                else
                {
                    if (_hasDescriptions)
                        descriptions.Add(line.Arguments[i + 1]);
                }
            }

            Items = items;
            Previews = previews;
            Descriptions = descriptions;
        }

        public override string ToString()
        {
            return $"{Type}: {Title}";
        }
    }

    /// <summary>
    /// <see cref="StartFlowToken"/> for <see cref="TokenType.For"/> token.
    /// </summary>
    internal sealed class ForToken : StartFlowToken
    {
        internal override TokenType Type => TokenType.For;
        
        internal override bool Active { get; set; } = true;
        
        /// <summary>
        /// Will be set to true if we have to exit out of the for loop (condition not met or break called). Used to
        /// script execution of child-statements once we are done. 
        /// </summary>
        internal bool Exit { get; set; }
        
        internal enum ForEnumerationType
        {
            /// <summary>
            /// Similar to a typical for i loop: counts from Start (Arguments[1]) till End (Arguments[2]),
            /// incrementing the variable by the optional value Step (Arguments[3], 1 is used if not specified) and
            /// putting the value in variable (Arguments[0]). Example:
            /// <code>
            /// For Count var1 0 10 2
            /// ...
            /// EndFor
            /// </code>
            /// in code:
            /// <code>
            /// for (var var1 = 0; i &lt; 10; i += 2) {
            ///     ...
            /// }
            /// </code>
            /// </summary>
            Count,
            /// <summary>
            /// Iterates over the subdirectories in the extracted data folder of the OMOD, not the game folder!
            /// <list type="bullet">
            /// <item>
            ///     <description>Arguments[0]: Variable that holds the current path.</description>
            /// </item>
            /// <item>
            ///     <description>Arguments[1]: Variable that holds the current path.</description>
            /// </item>
            /// <item>
            ///     <description>
            ///         Arguments[2]: (Optional, False is assumed if undefined) Boolean to set if the
            ///         search should be recursive or not.
            ///     </description>
            /// </item>
            /// <item>
            ///     <description>
            ///         Arguments[3]: (Optional, "*" is assumed if undefined) Search string with wildcards being
            ///         allowed.
            ///     </description>
            /// </item>
            /// </list>
            /// </summary>
            DataFolder,
            /// <summary>
            /// Iterates over the subdirectories in the extracted plugins folder of the OMOD, not the game folder!
            /// Do note that this function is completely useless as there are no subdirectories in the plugins folder.
            /// <list type="bullet">
            /// <item>
            ///     <description>Arguments[0]: Variable that holds the current path.</description>
            /// </item>
            /// <item>
            ///     <description>Arguments[1]: Variable that holds the current path.</description>
            /// </item>
            /// <item>
            ///     <description>
            ///         Arguments[2]: (Optional, False is assumed if undefined) Boolean to set if the
            ///         search should be recursive or not.
            ///     </description>
            /// </item>
            /// <item>
            ///     <description>
            ///         Arguments[3]: (Optional, "*" is assumed if undefined) Search string with wildcards being
            ///         allowed.
            ///     </description>
            /// </item>
            /// </list>
            /// </summary>
            PluginFolder,
            /// <summary>
            /// Iterates over the files in the extracted data folder of the OMOD, not the game folder!
            /// <list type="bullet">
            /// <item>
            ///     <description>Arguments[0]: Variable that holds the current path.</description>
            /// </item>
            /// <item>
            ///     <description>Arguments[1]: Variable that holds the current path.</description>
            /// </item>
            /// <item>
            ///     <description>
            ///         Arguments[2]: (Optional, False is assumed if undefined) Boolean to set if the
            ///         search should be recursive or not.
            ///     </description>
            /// </item>
            /// <item>
            ///     <description>
            ///         Arguments[3]: (Optional, "*" is assumed if undefined) Search string with wildcards being
            ///         allowed.
            ///     </description>
            /// </item>
            /// </list>
            /// </summary>
            DataFile,
            /// <summary>
            /// Iterates over the plugin-files in the extracted plugins folder of the OMOD, not the game folder!
            /// <list type="bullet">
            /// <item>
            ///     <description>Arguments[0]: Variable that holds the current path.</description>
            /// </item>
            /// <item>
            ///     <description>Arguments[1]: Variable that holds the current path.</description>
            /// </item>
            /// <item>
            ///     <description>
            ///         Arguments[2]: (Optional, False is assumed if undefined) Boolean to set if the
            ///         search should be recursive or not.
            ///     </description>
            /// </item>
            /// <item>
            ///     <description>
            ///         Arguments[3]: (Optional, "*" is assumed if undefined) Search string with wildcards being
            ///         allowed.
            ///     </description>
            /// </item>
            /// </list>
            /// </summary>
            Plugin
        }

        internal readonly ForEnumerationType EnumerationType;
        internal readonly string Variable;

        internal List<string> Enumerable { get; set; } = new List<string>();
        internal int Current { get; set; }
        
        #region For Count Arguments
        
        /// <summary>
        /// Start value of <see cref="Variable"/> in a For Count loop.
        /// </summary>
        internal readonly int Start;
        
        /// <summary>
        /// Max value of <see cref="Variable"/> in a For Count loop. 
        /// </summary>
        internal readonly int End;
        
        /// <summary>
        /// Step size in a For Count loop. 
        /// </summary>
        internal readonly int Step;

        #endregion

        #region For DataFolder, DataFile, PluginFolder, Plugin Arguments

        /// <summary>
        /// Folder to search in.
        /// </summary>
        internal readonly string FolderPath;
        
        /// <summary>
        /// Whether to search recursively or not.
        /// </summary>
        internal readonly bool Recursive;
        
        /// <summary>
        /// Search string, defaults to "*"
        /// </summary>
        internal readonly string SearchString;

        #endregion

        /// <summary>
        /// Index of the first instruction that we jump to on each iteration.
        /// </summary>
        internal int StartingIndex { get; set; } = -1;
        
        internal ForToken(Line line)
        {
            if (line.Arguments == null)
                throw new ArgumentException("Arguments List of Line is null!", nameof(line));

            var args = line.Arguments.ToList();
            
            //"Each" appears to be completely useless, "For Each" is the same as "For" so we just remove it
            if (args[0].Equals("Each", StringComparison.OrdinalIgnoreCase))
                args = args.TakeLast(args.Count - 1).ToList();

            var enumerationTypeName = args[0];
            if (!Utils.TryGetEnum<ForEnumerationType>(enumerationTypeName, out var enumerationType))
                throw new OBMMScriptTokenizerException($"Unable to parse {enumerationTypeName} as ForEnumerationType!");

            EnumerationType = enumerationType;
            Variable = args[1];
            args = args.TakeLast(args.Count - 2).ToList();

            FolderPath = string.Empty;
            SearchString = "*";

            switch (enumerationType)
            {
                case ForEnumerationType.Count:
                    if (!int.TryParse(args[0], out Start))
                        throw new OBMMScriptTokenizerException($"Unable to parse {args[0]} as int!");
                    if (!int.TryParse(args[1], out End))
                        throw new OBMMScriptTokenizerException($"Unable to parse {args[1]} as int!");
                    
                    //no optional Step argument defined
                    if (args.Count == 2)
                    {
                        Step = 1;
                        break;
                    }

                    if (!int.TryParse(args[2], out Step))
                        throw new OBMMScriptTokenizerException($"Unable to parse {args[2]} as int!");
                    break;
                case ForEnumerationType.DataFolder:
                case ForEnumerationType.PluginFolder:
                case ForEnumerationType.DataFile:
                case ForEnumerationType.Plugin:
                    FolderPath = args[0];
                    
                    if (args.Count == 1) break;
                    Recursive = args[1].Equals("True", StringComparison.OrdinalIgnoreCase);
                    
                    if (args.Count == 2) break;
                    SearchString = args[2];

                    if (args.Count == 3) break;
                    throw new OBMMScriptTokenizerException($"Unexpected extra arguments on a For line: {line}");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        public override string ToString()
        {
            return EnumerationType == ForEnumerationType.Count 
                ? $"{Type}: Type: {EnumerationType}, Variable: {Variable}, Count from {Start} to {End} with {Step} step(s)" 
                : $"{Type}: Type: {EnumerationType}, Variable: {Variable}, Folder {FolderPath}, Recursive: {Recursive}, Search: {SearchString}";
        }
    }

    /// <summary>
    /// <see cref="StartFlowToken"/> for <see cref="TokenType.Case"/> token.
    /// </summary>
    internal sealed class CaseToken : StartFlowToken
    {
        internal override TokenType Type => TokenType.Case;

        /// <summary>
        /// Value to make this case block active. Will be compared with the current variable in the parent Select block.
        /// </summary>
        internal readonly string Value;

        internal CaseToken(Line line)
        {
            if (line.Arguments == null)
                throw new ArgumentException("Arguments List of Line is null!", nameof(line));

            Value = line.Arguments.Aggregate((x, y) => $"{x} {y}");
        }

        /// <summary>
        /// Constructor for use in <see cref="OBMMScriptHandler"/> during script execution to construct a new Case token.
        /// </summary>
        /// <param name="s"></param>
        internal CaseToken(string s)
        {
            Value = s;
        }
    }

    /// <summary>
    /// <see cref="InstructionToken"/> for <see cref="TokenType.iSet"/> and <see cref="TokenType.fSet"/> tokens.
    /// </summary>
    internal sealed class SetToken : InstructionToken
    {
        internal readonly string Variable;
        internal readonly IReadOnlyList<string> Expression;

        internal SetToken(Line line, TokenType token) : base(line)
        {
            Type = token;
            
            /*
             * Variable is the first argument, the rest is the expression we later have to evaluate.
             * We pass the instructions list without the variable to the parent class and use
             * InstructionToken.Instructions for evaluation later on. We could also just create a new field for the
             * expression but whatever.
             */
            
            Variable = Instructions[0];
            Expression = Instructions.TakeLast(Instructions.Count - 1).ToList();
        }
    }

    /// <summary>
    /// <see cref="InstructionToken"/> for <see cref="TokenType.Goto"/> and <see cref="TokenType.Label"/> tokens.
    /// </summary>
    internal sealed class GotoLabelToken : InstructionToken
    {
        /// <summary>
        /// Name of the Label we define/goto.
        /// </summary>
        internal readonly string Label;
        
        /// <summary>
        /// Index of the Label token we jump to.
        /// </summary>
        internal int Index { get; set; }

        internal GotoLabelToken(Line line, TokenType tokenType) : base(line)
        {
            Type = tokenType;
            
            Label = Instructions[0];
        }

        public override string ToString()
        {
            return $"{Type}: {Label}";
        }
    }
}
