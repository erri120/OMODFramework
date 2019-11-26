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
    }
}
