using System;
using JetBrains.Annotations;

namespace OMODFramework.Scripting.Exceptions
{
    /// <summary>
    /// Represents errors that occur during script execution.
    /// </summary>
    [PublicAPI]
    public class OMODScriptException : OMODException
    {
        internal OMODScriptException(string message) : base(message) { }
        internal OMODScriptException(string message, Exception e) : base(message, e) { }
    }

    /// <summary>
    /// Represents errors that occur in script functions during script execution.
    /// </summary>
    [PublicAPI]
    public class OMODScriptFunctionException : OMODScriptException
    {
        internal OMODScriptFunctionException(string message) : base(message) { }

        internal OMODScriptFunctionException(string message, Exception e) : base(message, e) { }
    }
}
