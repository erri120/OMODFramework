using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace OMODFramework.Oblivion.BSA
{
    [PublicAPI]
    internal interface IArchiveFile
    {
        /// <summary>
        /// The path of the file inside the archive
        /// </summary>
        string Path { get; }

        /// <summary>
        /// The uncompressed file size
        /// </summary>
        uint Size { get; }

        /// <summary>
        /// Copies this entry to the given stream
        /// </summary>
        void CopyDataTo(Stream output);

        /// <summary>
        /// Copies this entry to the given stream
        /// </summary>
        ValueTask CopyDataToAsync(Stream output);
    }
}
