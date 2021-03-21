using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using OMODFramework.Compression;
using OMODFramework.Scripting.Data;

namespace OMODFramework.Scripting
{
    internal static class Utils
    {
        internal static bool TryGetEnum<T>(string s, [MaybeNullWhen(false)] out T type)
        {
            type = default;
            if (!Enum.TryParse(typeof(T), s, true, out var result))
                return false;

            if (!(result is T token)) return false;
            type = token;
            return true;
        }

        internal static void AddOrReplace<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key,
            TValue value)
        {
            if (dictionary.ContainsKey(key))
                dictionary.Remove(key);
            dictionary.Add(key, value);
        }

        internal static bool EqualsPath(this string path1, string path2)
        {
            return Path.GetFullPath(path1).Equals(Path.GetFullPath(path2), StringComparison.OrdinalIgnoreCase);
        }
        
        private static bool IsMatchingFile(OMODCompressedFile compressedFile, string path, string pattern,
            bool recursive)
        {
            var name = compressedFile.Name;

            //check if the path is in the current name aka checking if the current file is in the provided folder
            if (!name.StartsWith(path, StringComparison.OrdinalIgnoreCase))
                return false;

            var dirName = Path.GetDirectoryName(name);
            if (dirName == null) return false;

            /*
             * If we are not searching recursively, we can filter out the files where the directory is not what we
             * search for.
             */
                
            if (!recursive && !dirName.EqualsPath(path))
                return false;

            /*
             * Pattern is empty or '*' and we are looking recursively means we can return true for all since we
             * already guarantee that the current file is somewhere in the provided path.
             */
            if (string.IsNullOrWhiteSpace(pattern) || pattern == "*")
                return true;

            return name.Contains(pattern);
        }
        
        internal static IEnumerable<OMODCompressedFile> FileEnumeration(this IEnumerable<OMODCompressedFile> files,
            string path, string pattern, bool recursive)
        {
            return files.Where(x => IsMatchingFile(x, path, pattern, recursive));
        }

        internal static IEnumerable<T> FileEnumeration<T>(this IEnumerable<T> files,
            string path, string pattern, bool recursive) where T : ScriptReturnFile
        {
            return files.Where(x => IsMatchingFile(x.Input, path, pattern, recursive));
        }

        internal static IEnumerable<T> NotNull<T>(this IEnumerable<T?> enumerable) where T : class
        {
            return enumerable.Where(x => x != null).Select(x => x!);
        }

        internal static void AddOrChange<T>(this HashSet<T> set, T obj, Action<T> change) where T : class
        {
            if (set.TryGetValue(obj, out var actualValue))
            {
                change(actualValue);
            }
            else
            {
                set.Add(obj);
            }
        }
    }
}
