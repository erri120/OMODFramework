using System;
using JetBrains.Annotations;

namespace OMODFramework
{
    [PublicAPI]
    public class OMODException : Exception
    {
        public OMODException() { }
        public OMODException(string message) : base(message) { }
        public OMODException(string message, Exception e) : base(message, e) { }
    }

    [PublicAPI]
    public class OMODValidationException : OMODException
    {
        public OMODValidationException(string message) : base(message) { }
    }

    [PublicAPI]
    public class OMODEntryNotFoundException : OMODException
    {
        public OMODEntryNotFoundException(OMODEntryFileType entryFileType) : base($"OMOD does not contain a {entryFileType.ToFileString()} file") { }
    }
}
