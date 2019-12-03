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
using System.IO;

namespace OMODFramework.Scripting
{
    /// <summary>
    ///     Interface for functions to be called during script execution.
    ///     <para>
    ///         Some of those will not be called depending on your settings
    ///         (see <see cref="ScriptExecutionSettings" /> for all available options)
    ///     </para>
    /// </summary>
    public interface IScriptFunctions
    {
        /// <summary>
        ///     Only called if <see cref="ScriptExecutionSettings.EnableWarnings" /> is set to <c>true</c>
        /// </summary>
        /// <param name="msg">Warn message with line number</param>
        void Warn(string msg);

        /// <summary>
        ///     Message the user, preferably in an extra window
        /// </summary>
        /// <param name="msg">The message to be displayed</param>
        void Message(string msg);

        /// <summary>
        ///     Message the user, preferably in an extra window
        /// </summary>
        /// <param name="msg">The message to be displayed</param>
        /// <param name="title">The title of the window</param>
        void Message(string msg, string title);

        /// <summary>
        ///     Let the user select one or multiple items from a list
        ///     <para>
        ///         The preview image and description of an item will be at the index position of
        ///         the item within the items list.
        ///     </para>
        ///     <para>
        ///         You need to return either null, if the user canceled, or a list containing the indices of the
        ///         selected items.
        ///     </para>
        ///     <para>If the user selected the first item than that list will be <c>{ 0 }</c></para>
        ///     <para>If the user selected the first and second item than <c>{ 0, 1 }</c></para>
        /// </summary>
        /// <param name="items">List of items </param>
        /// <param name="title">Title of the form</param>
        /// <param name="isMultiSelect">Whether the user can select multiple things</param>
        /// <param name="previews">List with absolute paths to pictures of previews, can be null when no previews exist</param>
        /// <param name="descriptions">List with descriptions for each item, can be null if there are no descriptions</param>
        /// <returns>List with the indices of the selected items or null if canceled</returns>
        List<int> Select(
            List<string> items, string title, bool isMultiSelect, List<string> previews, List<string> descriptions);

        /// <summary>
        ///     Let the user input a string
        /// </summary>
        /// <param name="title">Title of the popup, is never null</param>
        /// <param name="initialText">Initial contents of the text box, is never null</param>
        /// <param name="useRTF">Whether to use a System.Windows.Forms.RichTextBox or a normal TextBox</param>
        /// <returns>Contents of the text box or null if the user canceled</returns>
        string InputString(string title, string initialText, bool useRTF);

        /// <summary>
        ///     Displays a Yes/No dialog
        /// </summary>
        /// <param name="title">Title of the dialog</param>
        /// <returns>1 for yes, 0 for no and null for canceled</returns>
        int DialogYesNo(string title);

        /// <summary>
        ///     Displays a Yes/No dialog
        /// </summary>
        /// <param name="title">Title of the dialog</param>
        /// <param name="message">Prompt-message</param>
        /// <returns>1 for yes, 0 for no and null for canceled</returns>
        int DialogYesNo(string title, string message);

        /// <summary>
        ///     Display an image to the user
        /// </summary>
        /// <param name="path">Absolute path to the image</param>
        /// <param name="title">Title of the window</param>
        void DisplayImage(string path, string title);

        /// <summary>
        ///     Display RTF text to the user, do note that the RTF text is supposed to be
        ///     displayed using a System.Windows.Forms.RichTextBox
        /// </summary>
        /// <param name="text">Text to be displayed</param>
        /// <param name="title">Title of the window</param>
        void DisplayText(string text, string title);

        /// <summary>
        ///     Patch a data file from inside the data folder. Only called when
        ///     <see cref="ScriptExecutionSettings.PatchWithInterface" />
        ///     is set to <c>true</c>.
        /// </summary>
        /// <param name="from">Absolute path to the file from the OMOD</param>
        /// <param name="to">Relative path to the file inside the data folder which may or may not exist</param>
        void Patch(string from, string to);

        /// <summary>
        ///     Read the oblivion.ini file and return the value of a field using its key and section name.
        ///     Only called when <see cref="ScriptExecutionSettings.ReadINIWithInterface" /> is set to <c>true</c>.
        /// </summary>
        /// <param name="section"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        string ReadOblivionINI(string section, string name);

        /// <summary>
        ///     Reads the RendererInfo.txt file and returns the value of the field using its name.
        ///     Only called when <see cref="ScriptExecutionSettings.ReadRendererInfoWithInterface" /> is set to
        ///     <c>true</c>.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        string ReadRendererInfo(string name);

        /// <summary>
        ///     Check if a file inside the Oblivion data folder exists
        ///     <para>
        ///         This looks easier said than done but remember that OBMM placed all mod files
        ///         inside the Oblivion data folder.
        ///     </para>
        ///     <para>
        ///         This means that you have to also check if any installed mod has this file if those are not
        ///         inside the data folder.
        ///     </para>
        /// </summary>
        /// <param name="path">Relative path to the file based on the Oblivion data folder. Eg: textures\\something</param>
        /// <returns></returns>
        bool DataFileExists(string path);

        /// <summary>
        ///     Check if the Oblivion Script Extender is installed
        /// </summary>
        /// <returns></returns>
        bool HasScriptExtender();

        /// <summary>
        ///     Check if the Oblivion Graphics Extender is installed
        /// </summary>
        /// <returns></returns>
        bool HasGraphicsExtender();

        /// <summary>
        ///     Gets the <see cref="Version" /> of the Oblivion Script Extender
        /// </summary>
        /// <returns></returns>
        Version ScriptExtenderVersion();

        /// <summary>
        ///     Gets the <see cref="Version" /> of the Oblivion Graphics Extender
        /// </summary>
        /// <returns></returns>
        Version GraphicsExtenderVersion();

        /// <summary>
        ///     Gets the <see cref="Version" /> of the oblivion.exe
        /// </summary>
        /// <returns></returns>
        Version OblivionVersion();

        /// <summary>
        ///     Gets the <see cref="Version" /> of a plugin in data\\obse\\plugins\\
        /// </summary>
        /// <param name="path">Relative path to the plugin based from data\\obse\\plugins\\</param>
        /// <returns></returns>
        Version OBSEPluginVersion(string path);

        /// <summary>
        ///     Gets a <see cref="HashSet{T}" /> of all ESPs, see <see cref="ScriptESP" /> for more info, this should
        ///     include all ESPs
        /// </summary>
        /// <returns></returns>
        HashSet<ScriptESP> GetESPs();

        /// <summary>
        ///     Gets a <see cref="HashSet{T}" /> with the name of all active OMODs.
        /// </summary>
        /// <returns></returns>
        HashSet<string> GetActiveOMODNames();

        /// <summary>
        ///     Use <see cref="File.ReadAllBytes" /> to read the bytes of a file located inside the data folder
        /// </summary>
        /// <param name="file">Relative path to the file inside the data folder</param>
        /// <returns></returns>
        byte[] ReadExistingDataFile(string file);

        /// <summary>
        ///     Gets the contents of a file within a BSA, will only get called when
        ///     <see cref="ScriptExecutionSettings.HandleBSAsWithInterface" /> is set to <c>true</c>
        /// </summary>
        /// <param name="file">name of the file within a bsa</param>
        /// <returns></returns>
        byte[] GetDataFileFromBSA(string file);

        /// <summary>
        ///     Gets the content of a file within a specific bsa, will only get called when
        ///     <see cref="ScriptExecutionSettings.HandleBSAsWithInterface" /> is set to <c>true</c>
        /// </summary>
        /// <param name="bsa">name of the bsa</param>
        /// <param name="file">name of the file within a bsa</param>
        /// <returns></returns>
        byte[] GetDataFileFromBSA(string bsa, string file);
    }

    /// <summary>
    ///     Simple struct for storing information about an ESP used during script executing
    /// </summary>
    public struct ScriptESP
    {
        /// <summary>
        ///     Name of the ESP without extension
        /// </summary>
        public string Name;

        /// <summary>
        ///     Whether the ESP is active or not
        /// </summary>
        public bool Active;
    }
}
