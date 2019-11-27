﻿using System.Collections.Generic;

namespace OMODFramework.Scripting
{
    public interface IScriptFunctions
    {
        /// <summary>
        /// Warn the user about something. This will only be called if <c>Framework.EnableWarnings = true</c>
        /// </summary>
        /// <param name="msg">The message, will always contain the line number</param>
        void Warn(string msg);

        /// <summary>
        /// Inform the user about something
        /// </summary>
        /// <param name="msg">The message to be displayed</param>
        void Message(string msg);

        /// <summary>
        /// Inform the user about something
        /// </summary>
        /// <param name="msg">The message to be displayed</param>
        /// <param name="title">The title of the popup</param>
        void Message(string msg, string title);

        /// <summary>
        /// This gets called when the user needs to select something.
        /// <para>The preview image and description of an item will be at the index position of
        /// the item within the items list.</para>
        /// <para>You need to return either null, if the user canceled, or a list containing the indices of the
        /// selected items.</para>
        /// <para>If the user selected the first item than that list will be <c>{ 0 }</c></para>
        /// <para>If the user selected the first and second item than <c>{ 0, 1 }</c></para>
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
        /// Gets called when the user needs to input something.
        /// </summary>
        /// <param name="title">Title of the popup, is never null</param>
        /// <param name="initialText">Initial contents of the text box, is never null</param>
        /// <param name="useRTF">Whether to use a System.Windows.Forms.RichTextBox or a normal TextBox</param>
        /// <returns>Contents of the text box or null if the user canceled</returns>
        string InputString(string title, string initialText, bool useRTF);

        /// <summary>
        /// Display an image to the user
        /// </summary>
        /// <param name="path">Absolute path to the image</param>
        /// <param name="title">Title of the window</param>
        void DisplayImage(string path, string title);

        /// <summary>
        /// Display RTF text to the user, do note that the RTF text is supposed to be
        /// displayed using a System.Windows.Forms.RichTextBox
        /// </summary>
        /// <param name="text">Text to be displayed</param>
        /// <param name="title">Title of the window</param>
        void DisplayText(string text, string title);

        /// <summary>
        /// This function will only be called if Framework.CurrentPatchMethod is set to PatchWithInterface.
        /// </summary>
        /// <param name="from">Absolute path to the file from the OMOD</param>
        /// <param name="to">Relative path to the file inside the data folder which may or may not exist</param>
        void Patch(string from, string to);

        /// <summary>
        /// Read the oblivion.ini file and return the value of a field using its key and section name
        /// </summary>
        /// <param name="section"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        string ReadOblivionINI(string section, string name);

        /// <summary>
        /// Reads the RendererInfo.txt file and returns the value of the field using its name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        string ReadRendererInfo(string name);
    }
}
