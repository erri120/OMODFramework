using System;
using System.Collections.Generic;
using System.Drawing;
using JetBrains.Annotations;

namespace OMODFramework.Scripting
{
    [PublicAPI]
    public enum DialogResult
    {
        Yes,
        No,
        Cancel
    }
    
    [PublicAPI]
    public interface IExternalScriptFunctions
    {
        void Message(string message);

        void Message(string message, string title);

        string InputString(string? title, string? initialText);
        
        DialogResult DialogYesNo(string message);

        DialogResult DialogYesNo(string message, string title);

        void DisplayImage(string imagePath, string? title);
        void DisplayImage(Bitmap image, string? title);
        
        void DisplayText(string text, string? title);

        IEnumerable<int> Select(IEnumerable<string> items, string title, bool isMany, IEnumerable<string> previews,
            IEnumerable<string> descriptions);
        
        IEnumerable<int> Select(IEnumerable<string> items, string title, bool isMany, IEnumerable<Bitmap> previews,
            IEnumerable<string> descriptions);
        
        bool HasScriptExtender();

        bool HasGraphicsExtender();

        Version GetScriptExtenderVersion();

        Version GetGraphicsExtenderVersion();

        Version GetOblivionVersion();

        Version GetOBSEPluginVersion(string file);

        IEnumerable<Plugin> GetPlugins();

        IEnumerable<string> GetActiveOMODNames();

        byte[] ReadExistingDataFile(string file);

        bool DataFileExists(string path);

        string ReadINI(string section, string valueName);

        string ReadRendererInfo(string valueName);

        void SetNewLoadOrder(string[] plugins);
    }

    [PublicAPI]
    public struct Plugin
    {
        public string Name { get; set; }
        
        public bool Active { get; set; }

        public Plugin (string name)
        {
            Name = name;
            Active = true;
        }
    }
}
