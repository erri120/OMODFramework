using System;
using JetBrains.Annotations;

namespace OMODFramework
{
    /// <summary>
    /// Represents errors that occur when dealing with OMODs.
    /// </summary>
    [PublicAPI]
    public class OMODException : Exception
    {
        internal OMODException() { }
        internal OMODException(string message) : base(message) { }
        internal OMODException(string message, Exception e) : base(message, e) { }
    }

    /// <summary>
    /// Represents errors that occur during OMOD validation.
    /// </summary>
    [PublicAPI]
    public class OMODValidationException : OMODException
    {
        internal OMODValidationException(string message) : base(message) { }
    }

    /// <summary>
    /// Represents an error where a <see cref="OMODEntryFileType"/> was not found.
    /// </summary>
    [PublicAPI]
    public class OMODEntryNotFoundException : OMODException
    {
        internal OMODEntryNotFoundException(OMODEntryFileType entryFileType) : base($"OMOD does not contain a {entryFileType.ToFileString()} file") { }
    }
}
