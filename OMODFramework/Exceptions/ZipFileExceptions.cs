using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;
using JetBrains.Annotations;

namespace OMODFramework.Exceptions
{
    /// <summary>
    /// The Exception that is thrown when the archive test for a <see cref="ZipFile"/> failed
    /// </summary>
    [PublicAPI]
    public class ZipFileIntegrityException : OMODException
    {
        public ZipFileIntegrityException(string s) 
            : base(s){}

        public ZipFileIntegrityException(int errorCount, IEnumerable<string> entryList) 
            : base($"Encountered {errorCount} errors in the following entries: {entryList.Aggregate((x, y) => $"{x},{y}")}") {}
    }

    /// <summary>
    /// The Exception that is thrown when an entry was not found in a <see cref="ZipFile"/>
    /// </summary>
    [PublicAPI]
    public class ZipFileEntryNotFoundException : OMODException
    {
        public ZipFileEntryNotFoundException(string name) 
            : base($"Could not find entry {name}") {}

        public ZipFileEntryNotFoundException(string name, ZipFile file) 
            : base($"Could not find entry {name} in {file.Name}") {}
    }

    /// <summary>
    /// The Exception that is thrown when a file could not be extracted from a <see cref="ZipFile"/>
    /// </summary>
    [PublicAPI]
    public class ZipFileExtractionException : OMODException
    {
        public ZipFileExtractionException(string name) 
            : base($"Could not extract file {name}!"){}

        public ZipFileExtractionException(string name, string output) 
            : base($"Could not extract file {name} to {output}!"){}

        public ZipFileExtractionException(string name, string output, long sizeA, long sizeB) 
            : base($"Could not extract file {name} to {output}, the length of the files mismatched after extraction: {sizeA} != {sizeB}"){}
    }
}
