using System;
using System.Collections.Generic;
using OMODFramework.Scripting;

namespace OMODFramework.Example
{
    public class ScriptFunctions : IScriptFunctions //IMPORTANT: DO NOT USE OblivionModManager.Scripting.IScriptFunctions
    {
        public void Warn(string msg)
        {
            Console.WriteLine($"[Warning]: {msg}");
        }

        public void Message(string msg)
        {
            Console.WriteLine($"[Message]: {msg}");
        }

        public void Message(string msg, string title)
        {
            Console.WriteLine($"[Message]: {title} - {msg}");
        }

        public List<int> Select(List<string> items, string title, bool isMultiSelect, List<string> previews, List<string> descriptions)
        {
            Console.WriteLine($"[Select]: {title} multi: {isMultiSelect}");
            for (var i = 0; i < items.Count; i++)
            {
                var preview = previews?[i] != null ? previews[i] : "empty";
                var description = descriptions?[i] != null ? descriptions[i] : "empty";
                Console.Write($"Items: {items[i]} | preview: {preview} | description: {description}");
            }
            return new List<int>{0};
        }

        public string InputString(string title, string initialText, bool useRTF)
        {
            Console.WriteLine($"[InputString]: {title}, text: {initialText}, rtf: {useRTF}");
            return "Hello World";
        }

        public int DialogYesNo(string title)
        {
            Console.WriteLine($"[DialogYesNo]: {title}");
            return 1;
        }

        public int DialogYesNo(string title, string message)
        {
            Console.WriteLine($"[DialogYesNo]: {title} - {message}");
            return 1;
        }

        public void DisplayImage(string path, string title)
        {
            Console.WriteLine($"[Image]: {title} - {path}");
        }

        public void DisplayText(string text, string title)
        {
            Console.WriteLine($"[Text]: {title} - {text}");
        }

        public void Patch(string from, string to)
        {
            Console.WriteLine($"[Patch]: {from} - {to}");
        }

        public string ReadOblivionINI(string section, string name)
        {
            Console.WriteLine($"[ReadOblivionINI]: {section} - {name}");
            return "";
        }

        public string ReadRendererInfo(string name)
        {
            Console.WriteLine($"[ReadRendererInfo]: {name}");
            return "";
        }

        public bool DataFileExists(string path)
        {
            Console.WriteLine($"[DataFileExists]: {path}");
            return false;
        }

        public bool HasScriptExtender()
        {
            return true;
        }

        public bool HasGraphicsExtender()
        {
            return false;
        }

        public Version ScriptExtenderVersion()
        {
            return new Version(0, 0, 21, 4);
        }

        public Version GraphicsExtenderVersion()
        {
            return new Version("");
        }

        public Version OblivionVersion()
        {
            return new Version(1, 2, 416, 0);
        }

        public Version OBSEPluginVersion(string path)
        {
            return new Version(1,0,0,0);
        }

        public HashSet<ScriptESP> GetESPs()
        {
            return new HashSet<ScriptESP>
            {
                new ScriptESP
                {
                    Active = true,
                    Name = "test"
                }
            };
        }

        public HashSet<string> GetActiveOMODNames()
        {
            throw new NotImplementedException();
        }

        public byte[] ReadExistingDataFile(string file)
        {
            throw new NotImplementedException();
        }

        public byte[] GetDataFileFromBSA(string file)
        {
            throw new NotImplementedException();
        }

        public byte[] GetDataFileFromBSA(string bsa, string file)
        {
            throw new NotImplementedException();
        }
    }
}
