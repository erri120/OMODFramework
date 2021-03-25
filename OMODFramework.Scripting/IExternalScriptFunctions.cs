using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using JetBrains.Annotations;
using OMODFramework.Oblivion;

namespace OMODFramework.Scripting
{
    /// <summary>
    /// Possible results when showing a dialog.
    /// </summary>
    [PublicAPI]
    public enum DialogResult
    {
        /// <summary>
        /// The user accepted the dialog.
        /// </summary>
        Yes,
        
        /// <summary>
        /// The user rejected the dialog.
        /// </summary>
        No,
        
        /// <summary>
        /// The user canceled the dialog or the dialog was closed in a different way.
        /// </summary>
        Cancel
    }
    
    /// <summary>
    /// Represents all possible external script functions you have to implement.
    /// </summary>
    [PublicAPI]
    public interface IExternalScriptFunctions
    {
        /// <summary>
        /// Display a message to the user.
        /// </summary>
        /// <param name="message">The message to display.</param>
        void Message(string message);

        /// <summary>
        /// Display a message to the user.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="title">The title of the dialog box.</param>
        void Message(string message, string title);

        /// <summary>
        /// Let the user input a string.
        /// </summary>
        /// <param name="title">The title of the dialog.</param>
        /// <param name="initialText">The initial text of the input text-box.</param>
        /// <returns></returns>
        string InputString(string? title, string? initialText);
        
        /// <summary>
        /// Create a Yes/No dialog prompt.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <returns></returns>
        DialogResult DialogYesNo(string message);

        /// <summary>
        /// Create a Yes/No dialog prompt.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="title">The title of the dialog prompt.</param>
        /// <returns></returns>
        DialogResult DialogYesNo(string message, string title);

        /// <summary>
        /// Display an Image to the user. Only used if <see cref="OMODScriptSettings.UseBitmapOverloads"/> is set to false.
        /// </summary>
        /// <param name="imagePath">Path to the image.</param>
        /// <param name="title">Title of the window.</param>
        void DisplayImage(string imagePath, string? title);
        
        /// <summary>
        /// Display an Image to the user. Only used if <see cref="OMODScriptSettings.UseBitmapOverloads"/> is set to true.
        /// You do not have to dispose of the Bitmap after the user closed the window.
        /// </summary>
        /// <param name="image">Image to display.</param>
        /// <param name="title">Title of the window.</param>
        void DisplayImage(Bitmap image, string? title);
        
        /// <summary>
        /// Display text to the user.
        /// </summary>
        /// <param name="text">Text to display.</param>
        /// <param name="title">Title of the window.</param>
        void DisplayText(string text, string? title);

        /// <summary>
        /// Let the user select one or multiple items. Only used if <see cref="OMODScriptSettings.UseBitmapOverloads"/>
        /// is set to false.
        /// </summary>
        /// <param name="items">Enumerable to all items to select from.</param>
        /// <param name="title">Title of the dialog.</param>
        /// <param name="isMany">Whether the user can select one or many options.</param>
        /// <param name="previews">Path to the preview images for each item.</param>
        /// <param name="descriptions">Descriptions for each item.</param>
        /// <returns></returns>
        IEnumerable<int> Select(IEnumerable<string> items, string title, bool isMany, IEnumerable<string> previews,
            IEnumerable<string> descriptions);
        
        /// <summary>
        /// Let the user select one or multiple items. Only used if <see cref="OMODScriptSettings.UseBitmapOverloads"/>
        /// is set to false.
        /// </summary>
        /// <param name="items">Enumerable to all items to select from.</param>
        /// <param name="title">Title of the dialog.</param>
        /// <param name="isMany">Whether the user can select one or many options.</param>
        /// <param name="previews">Preview images for each item. You do not have to dispose of the bitmaps after using them.
        /// </param>
        /// <param name="descriptions">Descriptions for each item.</param>
        /// <returns></returns>
        IEnumerable<int> Select(IEnumerable<string> items, string title, bool isMany, IEnumerable<Bitmap> previews,
            IEnumerable<string> descriptions);
        
        /// <summary>
        /// Whether the Oblivion Script Extender is installed. Check if <code>obse_loader.exe</code> is present in the
        /// Oblivion game directory.
        /// </summary>
        /// <returns></returns>
        bool HasScriptExtender();

        /// <summary>
        /// Whether the Oblivion Graphics Extender is installed. Check if <code>data\obse\plugins\obge.dll</code> is
        /// present in the Oblivion game directory.
        /// </summary>
        /// <returns></returns>
        bool HasGraphicsExtender();

        /// <summary>
        /// Returns the <see cref="FileVersionInfo.FileVersion"/> of the Oblivion Script Extender at
        /// <code>obse_loader.exe</code> in the Oblivion game directory.
        /// </summary>
        /// <returns></returns>
        Version GetScriptExtenderVersion();

        /// <summary>
        /// Returns the <see cref="FileVersionInfo.FileVersion"/> of the Oblivion Graphics Extender at
        /// <code>data\obse\plugins\obge.dll</code> in the Oblivion game directory.
        /// </summary>
        /// <returns></returns>
        Version GetGraphicsExtenderVersion();

        /// <summary>
        /// Returns the <see cref="FileVersionInfo.FileVersion"/> of <code>oblivion.exe</code> in the Oblivion game
        /// directory.
        /// </summary>
        /// <returns></returns>
        Version GetOblivionVersion();

        /// <summary>
        /// Returns the <see cref="FileVersionInfo.FileVersion"/> of an Oblivion Script Extender Plugin.
        /// </summary>
        /// <param name="file">Name of the plugin, without the extension. The file should be at
        /// <code>data\obse\plugins\{file}.dll</code>.</param>
        /// <returns></returns>
        Version GetOBSEPluginVersion(string file);

        /// <summary>
        /// Returns all plugins in the load order.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Plugin> GetPlugins();

        /// <summary>
        /// Returns all names of active OMODs.
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetActiveOMODNames();

        /// <summary>
        /// Returns the binary contents of an existing data file in the Oblivion data directory.
        /// </summary>
        /// <param name="file">Relative path to the file in the data directory.</param>
        /// <returns></returns>
        byte[] ReadExistingDataFile(string file);

        /// <summary>
        /// Returns whether or not a file in the Oblivion data directory already exists.
        /// </summary>
        /// <param name="path">Relative path to the file in the data directory.</param>
        /// <returns></returns>
        bool DataFileExists(string path);

        /// <summary>
        /// Returns the value of key in the Oblivion.ini file. <see cref="OblivionINI.GetINIValue(string,string,string)"/>
        /// can be used if you don't have your own implementation.
        /// </summary>
        /// <param name="section">Section the key is in.</param>
        /// <param name="valueName">The key to look for.</param>
        /// <returns></returns>
        string ReadINI(string section, string valueName);

        /// <summary>
        /// Returns the value of a key in an Oblivion Renderer Info file. <see cref="OblivionRendererInfo.GetRendererInfo(string,string)"/>
        /// can be used if you don't have your own implementation.
        /// </summary>
        /// <param name="valueName">The key to look for.</param>
        /// <returns></returns>
        string ReadRendererInfo(string valueName);

        /// <summary>
        /// Sets a new load order based on the provided plugin names.
        /// </summary>
        /// <param name="plugins"></param>
        void SetNewLoadOrder(string[] plugins);

        /// <summary>
        /// Returns the binary data of a file in a BSA. Only called when <see cref="OMODScriptSettings.UseInternalBSAFunctions"/>
        /// is set to false. OBMM loads all BSAs in the Oblivion data folder at startup and builds a cache containing all
        /// files in all BSAs. When this function is called, OBMM will simply check the cache for the file name and knows
        /// from what BSA it came from. We can't really replicate this behaviour in the OMODFramework so you have to
        /// implement this yourself. Do note that no inlined-script calls this function.
        /// </summary>
        /// <param name="file">Relative path of the file in the BSA</param>
        /// <returns></returns>
        byte[] GetDataFileFromBSA(string file);
        
        /// <summary>
        /// Returns the binary data of a file in a BSA. Only called when <see cref="OMODScriptSettings.UseInternalBSAFunctions"/>
        /// is set to false. This is very different from <see cref="GetDataFileFromBSA(string)"/> as it explicitly
        /// sets the BSA from where the file came from.
        /// </summary>
        /// <param name="bsa">BSA containing <paramref name="file"/></param>
        /// <param name="file">Relative path of teh file in the BSA</param>
        /// <returns></returns>
        byte[] GetDataFileFromBSA(string bsa, string file);

        /// <summary>
        /// Returns the path to an existing BSA archive. Only called when <see cref="OMODScriptSettings.UseInternalBSAFunctions"/>
        /// is set to true. The OMODFramework will not change this archive but simply read from it using the internal
        /// implementation of <see cref="GetDataFileFromBSA(string, string)"/>.
        /// </summary>
        /// <param name="bsa"></param>
        /// <returns></returns>
        string GetExistingBSAPath(string bsa);
    }

    /// <summary>
    /// Represents a plugin in the load order. Used by <see cref="IExternalScriptFunctions.GetPlugins"/>.
    /// </summary>
    [PublicAPI]
    public struct Plugin
    {
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Whether the plugin is active or not.
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> structure. 
        /// </summary>
        /// <param name="name">Name of the plugin</param>
        public Plugin (string name)
        {
            Name = name;
            Active = true;
        }
    }
}
