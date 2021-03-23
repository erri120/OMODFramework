using System;
using JetBrains.Annotations;
using OMODFramework.Scripting.ScriptHandlers.OBMMScript.Tokenizer;

namespace OMODFramework.Scripting.Exceptions
{
    /// <summary>
    /// Represents errors that occur during OBMM script execution.
    /// </summary>
    [PublicAPI]
    public class OBMMScriptHandlerException : OMODScriptException
    {
        internal OBMMScriptHandlerException(string message) : base(message) { }
        internal OBMMScriptHandlerException(string message, Exception e) : base(message, e) { }
    }

    /// <summary>
    /// Represents errors that occur during tokenization of an OBMM script.
    /// </summary>
    [PublicAPI]
    public class OBMMScriptTokenizerException : OBMMScriptHandlerException
    {
        internal OBMMScriptTokenizerException(string message) : base(message) { }
        internal OBMMScriptTokenizerException(string message, Exception e) : base(message, e) { }
    }

    /// <summary>
    /// Represents errors that occur during line validation of OBMM script lines.
    /// </summary>
    [PublicAPI]
    public class OBMMScriptLineValidationException : OBMMScriptTokenizerException
    {
        internal OBMMScriptLineValidationException(string message, Line line) : base($"{message}\n{line}") { }
    }
}
