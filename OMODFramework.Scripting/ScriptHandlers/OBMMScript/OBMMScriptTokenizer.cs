using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using OMODFramework.Scripting.Exceptions;
using OMODFramework.Scripting.ScriptHandlers.OBMMScript.Tokenizer;

namespace OMODFramework.Scripting.ScriptHandlers.OBMMScript
{
    internal partial class OBMMScriptHandler
    {
        internal static IEnumerable<Token> TokenizeScript(string script)
        {
            Logger.Trace("Starting Script Tokenization");
            var sw = new Stopwatch();
            sw.Start();

            var lines = script
                //remove carriage return
                .Replace("\r", "")
                //split lines
                .Split("\n")
                .Select(x => x.Trim())
                //remove empty lines
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
            
            /*
             * The code above only works if you have one statement per line but you can extends a line with the '\'
             * character:
Select "Install body textures?" \
	   "Yes" \
	   "No"
	Case Yes
		CopyDataFolder "Body Textures\\Textures" "Textures" True
		Break
	Case No
		Break
EndSelect
             * The Select statement goes for 3 lines which we need to process as 1. We can achieve this by using a queue
             * and enqueue all lines that have the '\' character at the end (without the '\' of course) and aggregating
             * the contents of the queue into a new line once we reach a line that does not have this character at
             * the end.
             */
            
            var queue = new Queue<string>();
            var list = new List<string>();
            /*
             * Example:
Select "Install body textures?" \
	   "Yes" \
	   "No"
             */
            foreach (var currentLine in lines)
            {
                //in the example, the first and second line match this
                if (currentLine.EndsWith("\\"))
                {
                    //we enqueue the first and second example lines without the '\' character at the end
                    queue.Enqueue(currentLine[..^1].Trim());
                }
                else
                {
                    //we encounter a line that does not have the '\' character at the end
                    
                    /*
                     * The queue is empty when the current line is not part of a multi-line statement so we can just
                     * add the current line to our new list and continue.
                     */
                    if (queue.Count == 0)
                    {
                        list.Add(currentLine);
                        continue;
                    }

                    /*
                     * The queue is not empty so we need to combine all elements into one new line. The current line
                     * is not in the queue so we just append it afterwards and clear the queue.
                     */
                    var s = queue.Aggregate((x, y) => $"{x} {y}");
                    s += $" {currentLine}";
                    //the final output for s would be "Select "Install body textures?" "Yes" "No"" (using the example)
                    list.Add(s);
                    queue.Clear();
                }
            }

            lines = list;

            var tokens = lines
                .Select(TokenizeLine)
                .ToList();
            
            sw.Stop();
            Logger.Trace($"Finished Script Tokenization after {sw.ElapsedMilliseconds}ms");

            return tokens;
        }

        [SuppressMessage("ReSharper", "CommentTypo")]
        internal static Line ParseLine(string l)
        {
            var split = l.Split(" ");
            var tokenName = split.Length == 0 ? l : split.First();
            
            Logger.Debug("Parsing line {0}", l);
            
            if (!Utils.TryGetEnum<TokenType>(tokenName, out var token))
                throw new OBMMScriptTokenizerException($"Unable to parse string as Token! {tokenName}");

            var line = new Line(token);

            //line only contains the TokenType and no arguments
            if (split.Length == 0) return line;
            
            /*
             * making a new array where we remove the first element which is the TokenType we just parsed. This new
             * array only contains the arguments
             */
            split = split.TakeLast(split.Length - 1).ToArray();

            //no quoted strings mean we don't have to process the line-string further
            if (!l.Contains("\""))
            {
                line.Arguments = new List<string>(split);
                return line;
            }

            /*
             * create a new string from the split array using LINQ Aggregate. This is simply the entire line without
             * the Token at the beginning.
             *
             * Alternative: l.Substring(tokenName.Length + 1, l.Length - tokenName.Length - 1)
             */
            var splitAggregation = split.Aggregate((x, y) => $"{x} {y}");
            
            //using the new string we simply separate by a double-quote character
            var quoteSplit = splitAggregation.Split('\"').ToList();

            /*
             * Example line:
             *
             * CopyDataFolder "Subtile Breathing - No BBB\\Data\\Meshes" "Meshes" True
             *
             * The split array would look the following at this point:
             * - [0] = ""Subtile"
             * - [1] = "Breathing"
             * - [2] = "-"
             * - [3] = "No"
             * - [4] = "BBB\\Data\\Meshes""
             * - [5] = ""Meshes""
             * - [6] = "True""
             *
             * and the quote split array would look like this:
             * - [0] = ""
             * - [1] = "Subtile Breathing - No BBB\\Data\\Meshes"
             * - [2] = " "
             * - [3] = "Meshes"
             * - [4] = " True"
             * 
             */
            
            line.Arguments = new List<string>();
            var j = 0;
            for (var i = 0; i < split.Length; i++)
            {
                var current = split[i];
                if (current.StartsWith('\"'))
                {
                    //remove the starting double-quote character from the string, ""Meshes"" would become "Meshes""
                    current = current[1..];

                    /*
                     * The following condition is true when we get an empty argument aka \"\" like this:
                     * 
                     * SetVar Something ""
                     *
                     * In this case we can just add an empty string to the arguments list and continue
                     */
                    if (current.StartsWith('\"'))
                    {
                        line.Arguments.Add(string.Empty);
                        continue;
                    }

                    //TODO: find example where this is true and add docs
                    if (current.EndsWith("\","))
                        current = current[..^2];
                    
                    //remove the trailing double-quote character, eg ""Meshes"" would be only "Meshes" at this point
                    if (current.EndsWith('\"'))
                        current = current[..^1];

                    /*
                     * Here we try to find the actual value in quotes, eg:
                     * 
                     * "Bob Ross"
                     * 
                     * would split into
                     *
                     * - [0] = "Bob
                     * - [1] = Ross"
                     *
                     * So we search for the first element in our quoteSplit array that starts with "Bob".
                     * This is problematic when multiple elements start with the same string:
                     *
                     * "Bob Ross"
                     * "Bob Marley"
                     *
                     * would split into
                     *
                     * - [0] = "Bob
                     * - [1] = Ross"
                     * - [2] = "Bob
                     * - [3] = Marley"
                     *
                     * The first search will be successful and we will find "Bob Ross" the second one where we want to
                     * get "Bob Marley" will also find "Bob Ross". The fix is a simple helper variable j which is set
                     * to the index of the last element we found. This way we will only go forward in the array when
                     * searching.
                     */
                    
                    //TODO: why are we not using quoteSplit from the start?
                    var first = quoteSplit.First(x => x.StartsWith(current) && quoteSplit.LastIndexOf(x) > j);
                    j = quoteSplit.IndexOf(first);
                    line.Arguments.Add(first);
                    //since we split by ' ', we need to advance by the amount of spaces in the string we found
                    i += first.Count(x => x == ' ');
                    continue;
                }
                
                line.Arguments.Add(current);
            }
            
            return line;
        }

        internal static void ValidateLine(Line line)
        {
            Logger.Debug("Validataing line {0}", line);

            var type = line.TokenType.GetType();

            var memberInfo = type.GetMember(line.TokenType.ToString());
            var attributes = memberInfo[0].GetCustomAttributes(typeof(LineValidationAttribute), false);
            
            if (!(attributes[0] is LineValidationAttribute validation))
            {
                Debugger.Break();
                throw new OBMMScriptLineValidationException($"Could not get LineValidationAttribute for token {line.TokenType}. This should be impossible to reach, please report this on GitHub", line);
            }
            
            if (line.Arguments == null)
            {
                if (validation.Min != validation.Max)
                    throw new OBMMScriptLineValidationException($"Line does not have any arguments but is supposed to have at least {validation.Min}{(validation.Max == -1 ? "" : $" and max {validation.Max}")}!", line);

                if (validation.Min != 0)
                    throw new OBMMScriptLineValidationException($"Line does not have any arguments but is supposed to have exactly {validation.Min}", line);
            }
            else
            {
                if (validation.Min == validation.Max)
                {
                    if (validation.Min == -1)
                    {
                        //should be impossible to reach
                        Debugger.Break();
                        throw new OBMMScriptLineValidationException("Min and Max are both -1, this should be impossible. Please report this on GitHub and start blaming me (erri120) for missing this", line);
                    }

                    if (line.Arguments.Count != validation.Min)
                        throw new OBMMScriptLineValidationException($"Line can only have {validation.Min} arguments but has {line.Arguments.Count}", line);
                }
                
                if (line.Arguments.Count < validation.Min)
                    throw new OBMMScriptLineValidationException($"Line is supposed to have at least {validation.Min} arguments but has {line.Arguments.Count}", line);

                if (validation.Max == -1) return;

                if (line.Arguments.Count > validation.Max)
                    throw new OBMMScriptLineValidationException($"Line is supposed to have at max {validation.Max} arguments but has {line.Arguments.Count}", line);
            }
        }
        
        internal static Token TokenizeLine(string l)
        {
            //don't parse comments because they don't have a TokenType at the start
            if (l.StartsWith(";"))
            {
                return new CommentToken
                {
                    Comment = l.Replace(";", "").Trim()
                };
            }

            /*
             * Game plan is as follows:
             *
             * 1) Convert plain text into a Line object
             * 2) Validate the Line
             * 3) Create a new Token depending on the TokenType
             *
             * The original version of OBMM and previous versions of this library worked with strings the entire time.
             * This was not really pleasant to look at, had tons of bugs and was not really debuggable. The new
             * approach parses, validates and then tokenizes each line so we get strongly typed objects instead of
             * just plain strings. One of the big reasons of doing this is making function class easier as we don't
             * have to parse the string again and can call the same functions as the C# Script Handler.
             *
             * Example on how this works:
             *
             *  If VersionLessThan 0.9.13
             *      Message "Some Message"
             *      FatalError
             *  EndIf
             *
             * becomes:
             *
             *  Line: Type: If; Arguments: ["VersionLessThan", "0.9.13"]
             *  Line: Type: Message; Arguments: ["Some Message"]
             *  Line: Type: FatalError
             *  Line: Type: EndIf
             *
             * After validation we create a new token:
             *
             *  Token: Type: If; ConditionType: VersionLessThan; Arguments: ["0.9.13"]
             *  Token: Type: Message; Value: "Some Message"
             *  Token: Type: FatalError
             *  Token: Type: EndIf
             *
             * This method does not have many advantages when it comes to single-statement lines like FatalError or
             * simple instructions like Message but it makes working with If statements and especially For and Select
             * statements really easy.
             */
            
            var line = ParseLine(l);
            ValidateLine(line);

            var token = line.TokenType;

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (token)
            {
                case TokenType.EndIf:
                case TokenType.EndFor:
                case TokenType.EndSelect:
                case TokenType.Break:
                    return new EndFlowToken(token);
                case TokenType.If:
                case TokenType.IfNot:
                    return new IfToken(line, token);
                case TokenType.Else:
                    return new StartFlowToken {Type = token};
                case TokenType.For:
                    return new ForToken(line);
                case TokenType.Case:
                    return new CaseToken(line);
                case TokenType.Select:
                case TokenType.SelectMany:
                case TokenType.SelectManyWithDescriptions:
                case TokenType.SelectManyWithDescriptionsAndPreviews:
                case TokenType.SelectManyWithPreview:
                case TokenType.SelectWithDescriptions:
                case TokenType.SelectWithDescriptionsAndPreviews:
                case TokenType.SelectWithPreview:
                    return new SelectToken(line, token);
                case TokenType.SelectVar:
                    return new SelectVarToken(line);
                case TokenType.SelectString:
                    return new SelectStringToken(line);
                case TokenType.Label:
                case TokenType.Goto:
                    return new GotoLabelToken(line, token);
                case TokenType.SetVar:
                    return new SetVarToken(line);
                case TokenType.iSet:
                case TokenType.fSet:
                    return new SetToken(line, token);
                default:
                    return new InstructionToken(line, true) {Type = token};
            }
        }
    }
}
