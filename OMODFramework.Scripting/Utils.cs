using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace OMODFramework.Scripting
{
    internal static partial class Utils
    {
        internal static string ToAggregatedString(this IEnumerable<string> col, string separator = ",")
        {
            return col.Aggregate((x, y) => $"{x}{separator}{y}");
        }

        internal static IEnumerable<T> DistinctBy<T, V>(this IEnumerable<T> vs, Func<T, V> select)
        {
            var set = new HashSet<V>();
            foreach (var v in vs)
            {
                var key = select(v);
                if (set.Contains(key)) continue;
                set.Add(key);
                yield return v;
            }
        }

        internal static IEnumerable<string> FileEnumeration(this IEnumerable<string> enumerable, string path, string pattern,
            bool recurse)
        {
            return enumerable.Where(x =>
            {
                if (!x.StartsWith(path, true, null))
                    return false;

                var dirName = Path.GetDirectoryName(x);
                if (dirName == null)
                    return false;

                if (!recurse && !dirName.Equals(path, StringComparison.InvariantCultureIgnoreCase))
                    return false;

                if (pattern == string.Empty || pattern == "*")
                {
                    return true;
                }

                return x.Contains(pattern);
            });
        }

        internal static void AddOrReplace<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key, TValue value) where TKey : notnull
        {
            if (dic.ContainsKey(key))
                dic.Remove(key);
            dic.Add(key, value);
        }

        internal static void Do<T>(this IEnumerable<T> col,[InstantHandle] Action<T> a)
        {
            foreach (var item in col) a(item);
        }

        internal static void AddArguments(this IList<string> arguments, IReadOnlyCollection<string> line, int start, int end)
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

        internal static bool TryGetEnum<T>(string s,[MaybeNullWhen(false)] out T type)
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
