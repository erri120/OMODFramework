using System;
using JetBrains.Annotations;

namespace OMODFramework.Exceptions
{
    [PublicAPI]
    public class OMODException : Exception
    {
        public OMODException(string s) : base(s) { }
        public OMODException(string s, Exception innerException) : base(s, innerException) { }
    }

    /// <summary>
    /// The Exception that is thrown when the parsed config of an OMOD is not valid
    /// </summary>
    [PublicAPI]
    public class OMODInvalidConfigException : OMODException
    {
        public OMODInvalidConfigException(string s) : base(s){ }
        public OMODInvalidConfigException(Config config) : base("Bad config!") { }
        public OMODInvalidConfigException(Config config, string s) : base(s) { }
    }
}
