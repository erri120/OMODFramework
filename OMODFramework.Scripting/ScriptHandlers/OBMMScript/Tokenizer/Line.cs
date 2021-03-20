using System.Collections.Generic;
using System.Linq;

namespace OMODFramework.Scripting.ScriptHandlers.OBMMScript.Tokenizer
{
    internal class Line
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
            return $"{TokenType.ToString()}{(Arguments == null ? "" : " "+Arguments.Aggregate((x, y) => $"{x} {y}"))}";
        }
    }
}
