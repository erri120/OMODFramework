using System.Collections.Generic;

namespace OMODFramework.Scripting
{
    public interface IScriptFunctions
    {
        /// <summary>
        /// Warn the user about something. This will only be called if Framework.EnableWarnings = true
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
        /// The preview image and description of an item will be at the index position of
        /// the item within the items list.
        /// You need to return either null, if the user canceled, or a list containing the indices of the
        /// selected items.
        /// If the user selected the first item than that list will be {0}
        /// If the user selected the first and second item than {0, 1}
        /// </summary>
        /// <param name="items">List of items </param>
        /// <param name="title">Title of the form</param>
        /// <param name="isMultiSelect">Whether the user can select multiple things</param>
        /// <param name="previews">List with absolute paths to pictures of previews, can be null when no previews exist</param>
        /// <param name="descriptions">List with descriptions for each item, can be null if there are no descriptions</param>
        /// <returns>List with the indices of the selected items or null if canceled</returns>
        List<int> Select(
            List<string> items, string title, bool isMultiSelect, List<string> previews, List<string> descriptions);
    }
}
