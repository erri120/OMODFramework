using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace OMODFramework.Oblivion
{
    /// <summary>
    /// Provides static functions for reading and modifying the Oblivion.ini file.
    /// </summary>
    [PublicAPI]
    public static class OblivionINI
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Cache = new Dictionary<string, Dictionary<string, string>>();

        private static string FixSection(string section)
        {
            if (!section.Contains('['))
                section = $"[{section}";
            if (!section.Contains("]"))
                section = $"{section}]";

            return section;
        }
        
        /// <summary>
        /// Returns the value of a key in an INI file.
        /// </summary>
        /// <param name="file">Path to the INI file</param>
        /// <param name="section">Name of the section the key is in</param>
        /// <param name="key">Name of the key to find</param>
        /// <returns></returns>
        public static string? GetINIValue(string file, string section, string key)
        {
            section = FixSection(section);

            if (Cache.TryGetValue(file, out var cachedValues))
                return cachedValues.TryGetValue(key, out var cachedValue) ? cachedValue : null;

            var values = GetINISection(file, section);
            if (values.Count == 0)
                return null;

            return values.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// Returns the value of a key in an INI Stream.
        /// </summary>
        /// <param name="iniStream">Stream of the INI file</param>
        /// <param name="section">Name of the section the key is in</param>
        /// <param name="key">Name of the key to find</param>
        /// <param name="cacheName">Optional cache name to use. Setting this will make subsequent calls with the
        /// same Stream faster.</param>
        /// <returns></returns>
        public static string? GetINIValue(Stream iniStream, string section, string key, string? cacheName = null)
        {
            section = FixSection(section);

            if (cacheName != null)
            {
                if (Cache.TryGetValue(cacheName, out var cachedValues))
                    return cachedValues.TryGetValue(key, out var cachedValue) ? cachedValue : null;
            }
            
            var values = GetINISection(iniStream, section);
            if (values.Count == 0)
                return null;

            return values.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// Edits the provided ini to replace a key in a section with a new value.
        /// </summary>
        /// <param name="file">Path to the ini file.</param>
        /// <param name="section">Section where the key is located in</param>
        /// <param name="key">Key to replace it's value</param>
        /// <param name="newValue">New value of the key</param>
        /// <exception cref="ArgumentException">File does not exist</exception>
        public static void SetINIValue(string file, string section, string key, string newValue)
        {
            if (!File.Exists(file))
                throw new ArgumentException($"File does not exist: {file}", nameof(file));
            
            var lines = File.ReadAllLines(file).Select(x => x.Trim()).ToList();

            var sectionString = FixSection(section);
            var sectionIndex = lines.IndexOf(sectionString);
            if (sectionIndex == -1)
                throw new ArgumentException("Unable to find section in ini!", nameof(section));

            var nextSection = lines.Skip(sectionIndex+1).FirstOrDefault(x => x.StartsWith('[') && x.EndsWith(']'));
            var nextSectionIndex = nextSection == null ? lines.Count : lines.IndexOf(nextSection);

            var keySpan = key.AsSpan();
            
            for (var i = sectionIndex; i < nextSectionIndex; i++)
            {
                var span = lines[i].AsSpan();
                
                var splitIndex = span.IndexOf('=');
                if (splitIndex == -1) continue;
                var commentIndex = span.IndexOf(';');

                //example line: "myKey=SomeValue ;I'm a comment"
                    
                //splitting from index 0 till index of "=" (non-inclusive)
                var currentKey = span[..splitIndex];

                if (!currentKey.Equals(keySpan, StringComparison.OrdinalIgnoreCase)) continue;

                var newLine = $"{key}={newValue}";
                
                if (commentIndex != -1)
                {
                    var comment = span.Slice(commentIndex + 1, span.Length - commentIndex - 1);
                    newLine = $"{newLine} ;{comment.ToString()}";
                }
                
                lines[i] = newLine;
                break;
            }
            
            File.WriteAllLines(file, lines);
        }
        
        private static Dictionary<string, string> GetINISection(string file, string section)
        {
            if (!File.Exists(file))
                throw new ArgumentException($"File does not exist! {file}", nameof(file));

            using var fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            return GetINISection(fs, section, file);
        }

        private static Dictionary<string, string> GetINISection(Stream iniStream, string section, string? cacheName = null)
        {
            if (!iniStream.CanRead)
                throw new ArgumentException("Stream is not readable!", nameof(iniStream));
            if (!iniStream.CanSeek)
                throw new ArgumentException("Stream is not seekable!", nameof(iniStream));
            
            var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var sectionSpan = section.AsSpan();

            var initialPosition = iniStream.Position;
            
            //the StreamReader constructor is different between .NET Standard 2.1 and .NET 5.0
            using var sr = new StreamReader(iniStream, Encoding.UTF8, true, -1, true);

            var inSection = false;
            while (sr.Peek() != -1)
            {
                var line = sr.ReadLine();
                if (line == null) break;

                var span = line.AsSpan().Trim();

                if (inSection)
                {
                    //this condition is true if we found the section we are looking for and then encounter another
                    //section start meaning the section we are looking for is parsed and we can break
                    if (span[0].Equals('[') && span[^1].Equals(']')) break;

                    var splitIndex = span.IndexOf('=');
                    if (splitIndex == -1) continue;
                    var commentIndex = span.IndexOf(';');

                    //example line: "myKey=SomeValue ;I'm a comment"
                    
                    //splitting from index 0 till index of "=" (non-inclusive)
                    var key = span[..splitIndex];
                    
                    //second argument of Slice is the length, calculated by subtracting the length of the entire thing
                    //with the index of "=" (extra -1 because zero-indexed)
                    var value = span.Slice(splitIndex + 1,
                        commentIndex == -1
                            ? span.Length - splitIndex - 1
                            //comment is always after the value so we have to further reduce the length of the value
                            //by the length of the comment
                            : span.Length - splitIndex - 1 - (span.Length - commentIndex));

                    //trimming the end of the value if we have a comment because the comment might be directly after
                    //the value or with spaces in between
                    if (commentIndex != -1)
                        value = value.TrimEnd();
                    
                    dictionary.Add(key.ToString(), value.ToString());
                }
                else
                {
                    if (span.Equals(sectionSpan, StringComparison.OrdinalIgnoreCase))
                        inSection = true;
                }
            }

            //resetting the Stream position so sequential calls are possible
            iniStream.Position = initialPosition;
            
            if (cacheName != null)
                Cache.Add(cacheName, dictionary);
            return dictionary;
        }
    }
}
