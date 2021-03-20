using System;
using JetBrains.Annotations;

namespace OMODFramework.Scripting.Exceptions
{
    [PublicAPI]
    public class OMODScriptException : OMODException
    {
        internal OMODScriptException() { }
        internal OMODScriptException(string message) : base(message) { }
        internal OMODScriptException(string message, Exception e) : base(message, e) { }
    }
}
