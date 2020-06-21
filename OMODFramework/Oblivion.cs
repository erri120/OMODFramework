using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OMODFramework.Exceptions;

namespace OMODFramework
{
    internal static class OblivionINI
    {
        internal static string GetINIValue(string file, string section, string name)
        {
            var result = "";
            List<string>? list = GetINISection(file, section);
            if (list == null)
                throw new OMODException($"Oblivion.ini section {section} does not exist!");

            list.Where(s => s.Trim().ToLower().StartsWith($"{name.ToLower()}=")).Do(s =>
            {
                var res = s.Substring(s.IndexOf('=') + 1).Trim();
                var i = res.IndexOf(';');
                if (i != -1)
                    res = res.Substring(0, i - 1);
                result = res;
            });

            return result;
        }

        private static List<string>? GetINISection(string file, string section)
        {
            var contents = new List<string>();
            var inSection = false;
            using var sr = new StreamReader(File.OpenRead(file), Encoding.UTF8);
            try
            {
                while (sr.Peek() != -1)
                {
                    var s = sr.ReadLine();
                    if (s == null)
                        break;
                    if (inSection)
                    {
                        if (s.Trim().StartsWith("[") && s.Trim().EndsWith("]")) break;
                        contents.Add(s);
                    }
                    else
                    {
                        if (s.Trim().ToLower() == section)
                            inSection = true;
                    }
                }
            }
            catch (Exception e)
            {
                throw new OMODException($"Could not read from oblivion.ini at {file}\n{e}");
            }

            return !inSection ? null : contents;
        }
    }

    internal static class OblivionRenderInfo
    {
        internal static string GetInfo(string file, string s)
        {
            var result = $"Value {s} not found";

            try
            {
                var lines = File.ReadAllLines(file);
                lines.Where(t => t.Trim().ToLower().StartsWith(s)).Do(t =>
                {
                    var split = t.Split(':');
                    if (split.Length != 2) result = "-1";
                    result = split[1].Trim();
                });
            }
            catch (Exception e)
            {
                throw new OMODException($"Could not read from RenderInfo.txt file at {file}\n{e}");
            }

            return result;
        }
    }

    internal static class OblivionSDP
    {
        internal static void EditShader(FileInfo shaderFile, string shaderName, byte[] newData, FileInfo? outputFile)
        {
            var temp = Utils.CreateTempFile();
            shaderFile.CopyTo(temp, true);

            var output = outputFile == null ? shaderFile.FullName : outputFile.FullName;
            if(File.Exists(output))
                File.Delete(output);

            {
                using var br = new BinaryReader(File.OpenRead(temp));
                using var bw = new BinaryWriter(File.Create(output));

                bw.Write(br.ReadInt32());
                var num = br.ReadInt32();
                bw.Write(num);

                var sizeOffset = br.BaseStream.Position;
                bw.Write(br.ReadInt32());

                var found = false;
                for (var i = 0; i < num; i++)
                {
                    char[] name = br.ReadChars(0x100);
                    var size = br.ReadInt32();
                    byte[] data = br.ReadBytes(size);
                    bw.Write(name);
                    var sName = "";
                    for (var j = 0; j < 100; j++)
                    {
                        if (name[j] == '\0')
                            break;
                        sName += name[j];
                    }

                    if (!found && sName == shaderName)
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
                bw.Write(bw.BaseStream.Length-12);
            }

            File.Delete(temp);
        }
    }
}
