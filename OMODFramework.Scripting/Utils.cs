using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace OMODFramework.Scripting
{
    internal static partial class Utils
    {
        internal static string ToAggregatedString(this IEnumerable<string> col, string separator = ",")
        {
            return col.Aggregate((x, y) => $"{x}{separator}{y}");
        }

        public static void AddArguments(this IList<string> arguments, IReadOnlyCollection<string> line, int start, int end)
        {
            if (start == 0 && end == 0)
                return;

            if (start == end)
            {
                arguments.Add(line.ElementAt(start - 1));
                return;
            }

            for (var i = start - 1; i < end; i++)
            {
                arguments.Add(line.ElementAt(i));
            }
        }

        public static bool TryGetEnum<T>(string s,[MaybeNullWhen(false)] out T type)
        {
            type = default;
            if (!Enum.TryParse(typeof(T), s, true, out var result))
                return false;

            if (result == null)
                return false;

            if (!(result is T token))
                return false;

            type = token;
            return true;
        }
    }
}
