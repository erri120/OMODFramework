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
                
            if (!recursive && !dirName.Equals(path, StringComparison.OrdinalIgnoreCase))
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
            var newPath = path.MakePath();
            return files.Where(x => IsMatchingFile(x, newPath, pattern, recursive));
        }

        internal static IEnumerable<T> FileEnumeration<T>(this IEnumerable<T> files,
            string path, string pattern, bool recursive) where T : ScriptReturnFile
        {
            var newPath = path.MakePath();
            return files.Where(x => IsMatchingFile(x.Input, newPath, pattern, recursive));
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

        internal static OMODCompressedFile GetDataFile(this OMOD omod, string file)
        {
            var compressedFile = omod.GetDataFiles().First(x => x.Name.Equals(file.MakePath(), StringComparison.OrdinalIgnoreCase));
            return compressedFile;
        }
        
        internal static OMODCompressedFile GetPluginFile(this OMOD omod, string file)
        {
            var compressedFile = omod.GetPluginFiles().First(x => x.Name.Equals(file.MakePath(), StringComparison.OrdinalIgnoreCase));
            return compressedFile;
        }
        
        internal static DataFile GetDataFile(this ScriptReturnData srd, string file, bool byInput = true)
        {
            var dataFile = srd.DataFiles.First(x => byInput 
                ? x.Input.Name.Equals(file.MakePath(), StringComparison.OrdinalIgnoreCase) 
                : x.Output.Equals(file.MakePath(), StringComparison.OrdinalIgnoreCase));
            return dataFile;
        }

        internal static PluginFile GetPluginFile(this ScriptReturnData srd, string file, bool byInput = true)
        {
            var pluginFile = srd.PluginFiles.First(x => byInput 
                ? x.Input.Name.Equals(file.MakePath(), StringComparison.OrdinalIgnoreCase) 
                : x.Output.Equals(file.MakePath(), StringComparison.OrdinalIgnoreCase));
            return pluginFile;
        }
        
        internal static DataFile GetOrAddDataFile(this ScriptReturnData srd, string file, OMOD omod, bool byInput = true)
        {
            var filePath = file.MakePath();
            var dataFile = srd.DataFiles.FirstOrDefault(x => byInput 
                ? x.Input.Name.Equals(filePath, StringComparison.OrdinalIgnoreCase) 
                : x.Output.Equals(filePath, StringComparison.OrdinalIgnoreCase));
            if (dataFile != null) return dataFile;

            var compressedFile = GetDataFile(omod, filePath);
            dataFile = new DataFile(compressedFile);
            srd.DataFiles.Add(dataFile);
            
            return dataFile;
        }

        internal static PluginFile GetOrAddPluginFile(this ScriptReturnData srd, string file, OMOD omod, bool byInput = true)
        {
            var filePath = file.MakePath();
            var pluginFile = srd.PluginFiles.FirstOrDefault(x => byInput 
                ? x.Input.Name.Equals(filePath, StringComparison.OrdinalIgnoreCase) 
                : x.Output.Equals(filePath, StringComparison.OrdinalIgnoreCase));
            if (pluginFile != null) return pluginFile;

            var compressedFile = GetPluginFile(omod, filePath);
            pluginFile = new PluginFile(compressedFile);
            srd.PluginFiles.Add(pluginFile);
            
            return pluginFile;
        }
    }
}
