using System;

namespace OMODFramework.Scripting
{
    public class OBMMScriptingTokenParseException : Exception
    {
        public OBMMScriptingTokenParseException(string token) : base($"Unable to parse token {token}"){}
    }

    public class OBMMScriptingTokenizationException : Exception
    {
        public OBMMScriptingTokenizationException(string line) : base(line){ }
        public OBMMScriptingTokenizationException(string line, string msg) : base($"{msg}\n{line}") { }
    }
}
