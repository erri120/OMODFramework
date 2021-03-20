using System;
using JetBrains.Annotations;

namespace OMODFramework
{
    [PublicAPI]
    public class OMODException : Exception
    {
        internal OMODException() { }
        internal OMODException(string message) : base(message) { }
        internal OMODException(string message, Exception e) : base(message, e) { }
    }

    [PublicAPI]
    public class OMODValidationException : OMODException
    {
        internal OMODValidationException(string message) : base(message) { }
    }

    [PublicAPI]
    public class OMODEntryNotFoundException : OMODException
    {
        internal OMODEntryNotFoundException(OMODEntryFileType entryFileType) : base($"OMOD does not contain a {entryFileType.ToFileString()} file") { }
    }
}
