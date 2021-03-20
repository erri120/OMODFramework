using System;
using JetBrains.Annotations;

namespace OMODFramework.Scripting.Exceptions
{
    [PublicAPI]
    public class OMODScriptException : OMODException
    {
        internal OMODScriptException(string message) : base(message) { }
        internal OMODScriptException(string message, Exception e) : base(message, e) { }
    }

    [PublicAPI]
    public class OMODScriptFunctionException : OMODScriptException
    {
        internal OMODScriptFunctionException(string message) : base(message) { }

        internal OMODScriptFunctionException(string message, Exception e) : base(message, e) { }
    }
}
