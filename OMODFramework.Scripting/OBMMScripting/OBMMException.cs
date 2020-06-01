namespace OMODFramework.Scripting
{
    public class OBMMScriptingTokenParseException : ScriptException
    {
        public OBMMScriptingTokenParseException(string token) : base($"Unable to parse token {token}"){}
    }

    public class OBMMScriptingTokenizationException : ScriptException
    {
        public OBMMScriptingTokenizationException(string line) : base(line){ }
        public OBMMScriptingTokenizationException(string line, string msg) : base($"{msg}\n{line}") { }
    }

    public class OBMMScriptingParseException : ScriptException
    {
        public OBMMScriptingParseException(string token) : base(token) { }
        public OBMMScriptingParseException(string token, string msg) : base($"{msg}\n{token}") { }
    }

    public class OBMMScriptingVariableNotFoundException : ScriptException
    {
        public OBMMScriptingVariableNotFoundException(string variable) : base($"Variable {variable} was not found!") { }
        public OBMMScriptingVariableNotFoundException(string token, string variable) : base($"{token}\nVariable: {variable} was not found!") { }
    }
}
