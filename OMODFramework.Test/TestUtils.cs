using System;

namespace OMODFramework.Test
{
    internal static class TestUtils
    {
        internal static readonly bool IsCI;
        
        static TestUtils()
        {
            var env = Environment.GetEnvironmentVariable("CI");
            if (env == null)
            {
                IsCI = false;
                return;
            }

            IsCI = env.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase);
        }
    }
}
