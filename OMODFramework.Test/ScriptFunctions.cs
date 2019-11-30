/*
    Copyright (C) 2019  erri120

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using OMODFramework.Scripting;

namespace OMODFramework.Test
{
    internal class ScriptFunctions : IScriptFunctions
    {
        public void Warn(string msg)
        {
            Debug.WriteLine($"[Warning]: {msg}");
        }

        public void Message(string msg)
        {
            Debug.WriteLine($"[Message]: {msg}");
        }

        public void Message(string msg, string title)
        {
            Debug.WriteLine($"[Message]: {title} - {msg}");
        }

        public List<int> Select(List<string> items, string title, bool isMultiSelect, List<string> previews, List<string> descriptions)
        {
            Debug.WriteLine($"[Select]: {title} multi: {isMultiSelect}");
            for (var i = 0; i < items.Count; i++)
            {
                var preview = previews?[i] != null ? previews[i] : "empty";
                var description = descriptions?[i] != null ? descriptions[i] : "empty";
                Debug.Write($"Items: {items[i]} | preview: {preview} | description: {description}");
            }
            return new List<int>{0};
        }

        public string InputString(string title, string initialText, bool useRTF)
        {
            Debug.WriteLine($"[InputString]: {title}, text: {initialText}, rtf: {useRTF}");
            return "Hello World";
        }

        public int DialogYesNo(string title)
        {
            Debug.WriteLine($"[DialogYesNo]: {title}");
            return 1;
        }

        public int DialogYesNo(string title, string message)
        {
            Debug.WriteLine($"[DialogYesNo]: {title} - {message}");
            return 1;
        }

        public void DisplayImage(string path, string title)
        {
            Debug.WriteLine($"[Image]: {title} - {path}");
        }

        public void DisplayText(string text, string title)
        {
            Debug.WriteLine($"[Text]: {title} - {text}");
        }

        public void Patch(string from, string to)
        {
            Debug.WriteLine($"[Patch]: {from} - {to}");
        }

        public string ReadOblivionINI(string section, string name)
        {
            Debug.WriteLine($"[ReadOblivionINI]: {section} - {name}");
            return "";
        }

        public string ReadRendererInfo(string name)
        {
            Debug.WriteLine($"[ReadRendererInfo]: {name}");
            return "";
        }

        public bool DataFileExists(string path)
        {
            Debug.WriteLine($"[DataFileExists]: {path}");
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
            throw new NotImplementedException();
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
