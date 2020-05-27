using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace OMODFramework.Test
{
    internal static class Utils
    {
        internal static void Do<T>(this IEnumerable<T> col, [InstantHandle] Action<T> a)
        {
            foreach (var item in col) a(item);
        }
    }
}
