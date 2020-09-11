// /*
//     Copyright (C) 2020  erri120
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <https://www.gnu.org/licenses/>.
// */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OMODFramework
{
    internal static class OblivionINI
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Cache = new Dictionary<string, Dictionary<string, string>>();

        internal static string? GetINIValue(string file, string section, string name)
        {
            if (Cache.TryGetValue(file, out var cachedValues))
                return cachedValues.TryGetValue(name, out var cachedValue) ? cachedValue : null;
            
            Dictionary<string, string> values = GetINISection(file, section);
            if (values.Count == 0)
                return null;

            return values.TryGetValue(name, out var value) ? value : null;
        }

        private static Dictionary<string, string> GetINISection(string file, string section)
        {
            var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ReadOnlySpan<char> sectionSpan = section.AsSpan();

            using var fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var sr = new StreamReader(fs, Encoding.UTF8);

            var inSection = false;
            while (sr.Peek() != -1)
            {
                var line = sr.ReadLine();
                if (line == null) break;
                ReadOnlySpan<char> span = line.AsSpan().Trim();
                
                if (inSection)
                {
                    if (span[0].Equals('[') && span[^1].Equals(']')) break;

                    var splitIndex = span.IndexOf('=');
                    if (splitIndex == -1) continue;
                    var commentIndex = span.IndexOf(';');

                    ReadOnlySpan<char> key = span.Slice(0, splitIndex);
                    ReadOnlySpan<char> value = span.Slice(splitIndex + 1,
                        commentIndex == -1
                            ? span.Length - splitIndex - 1
                            : span.Length - splitIndex - 1 - (span.Length - commentIndex - 1));

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

            Cache.Add(file, dictionary);
            return dictionary;
        }
    }

    internal static class OblivionRendererInfo
    {
        internal static string? GetInfo(string file, string search)
        {
            /*
             * Example file (note the spaces):
	Water shader       		: yes
	Water reflections  		: yes
	Water displacement 		: yes
	Water high res     		: yes
	MultiSample Type   		: 0
	Shader Package     		: 13
             */
            
            var result = "";
            
            ReadOnlySpan<char> searchSpan = search.AsSpan();
            
            string[] lines = File.ReadAllLines(file, Encoding.UTF8);

            foreach (var line in lines)
            {
                ReadOnlySpan<char> span = line.AsSpan();
                var index = span.IndexOf(':');
                if (index == -1) continue;

                ReadOnlySpan<char> key = span.Slice(0, index).Trim();
                if (!key.Equals(searchSpan, StringComparison.OrdinalIgnoreCase))
                    continue;

                ReadOnlySpan<char> value = span.Slice(index + 1, span.Length - index -1);
                result = value.ToString();
            }

            return result;
        }
    }

    internal static class OblivionSDP
    {
        internal static void EditShader(string shaderFile, string shaderName, byte[] newData, string? outputFile)
        {
            var output = outputFile ?? shaderFile;
            if (File.Exists(output))
                File.Delete(output);

            using var tempFile = Utils.GetTempFile(FileMode.CreateNew, copyFile: shaderFile);
            using var outputFileStream = File.Open(output, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);

            using var br = tempFile.GetBinaryReader();
            using var bw = new BinaryWriter(outputFileStream, Encoding.UTF8, true);
            
            bw.Write(br.ReadInt32());
            
            var num = br.ReadInt32();
            bw.Write(num);

            var sizeOffset = br.BaseStream.Position;
            
            bw.Write(br.ReadInt32());

            var found = false;
            for (var i = 0; i < num; i++)
            {
                char[] nameChars = br.ReadChars(0x100);
                var size = br.ReadInt32();
                byte[] data = br.ReadBytes(size);
                
                bw.Write(nameChars);

                ReadOnlySpan<char> nameSpan = nameChars.AsSpan();
                nameSpan.TrimEnd();

                var sName = nameSpan.ToString();
                if (!found && sName.Equals(shaderName))
                {
                    bw.Write(newData.Length);
                    bw.Write(newData);
                    found = true;
                }
                else
                {
                    bw.Write(size);
                    bw.Write(data);
                }
            }

            bw.BaseStream.Position = sizeOffset;
            bw.Write(bw.BaseStream.Length - 12);
        }
    }
}
