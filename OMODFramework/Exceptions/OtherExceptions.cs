using System;

namespace OMODFramework.Exceptions
{
    public class BadSettingsException : ArgumentException
    {
        public BadSettingsException(string s) : base(s) { }
        public BadSettingsException(string message, string parameterName) : base(message, parameterName) { }
    }
}
