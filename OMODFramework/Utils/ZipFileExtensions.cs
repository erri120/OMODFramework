using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using OMODFramework.Exceptions;

namespace OMODFramework
{
    internal static partial class Utils
    {
        /// <summary>
        /// Checks if the given <see cref="ZipFile"/> contains a file
        /// </summary>
        /// <param name="zipFile">The ZipFile</param>
        /// <param name="file">The file to find</param>
        /// <param name="ignoreCase">Whether to ignore case or not</param>
        /// <returns></returns>
        internal static bool HasFile(this ZipFile zipFile, string file, bool ignoreCase = true)
        {
            var index = zipFile.FindEntry(file, ignoreCase);
            return index != -1;
        }

        /// <summary>
        /// Tests the <see cref="ZipFile"/> for integrity
        /// </summary>
        /// <param name="file">The ZipFile to test</param>
        /// <param name="throwIfNotValid">Whether to throw a <see cref="ZipFileIntegrityException"/> if the archive is not valid</param>
        /// <returns></returns>
        /// <exception cref="ZipFileIntegrityException"></exception>
        internal static bool CheckIntegrity(this ZipFile file, bool throwIfNotValid = true)
        {
            var errorCount = 0;
            var badEntries = new List<string>();
            var valid = file.TestArchive(false, TestStrategy.FindAllErrors, (status, message) =>
            {
                if (status.EntryValid)
                    return;

                errorCount = status.ErrorCount;

                if (status.Entry == null)
                    return;

                if (badEntries.Contains(status.Entry.Name))
                    return;

                badEntries.Add(status.Entry.Name);
            });

            if (!valid && throwIfNotValid)
                throw new ZipFileIntegrityException(errorCount, badEntries);
            return valid;
        }

        /// <summary>
        /// Extracts a file from <see cref="ZipFile"/> and returns a <see cref="Stream"/>
        /// </summary>
        /// <param name="file">The archive</param>
        /// <param name="name">The file to extract</param>
        /// <returns></returns>
        /// <exception cref="ZipFileEntryNotFoundException"></exception>
        internal static Stream ExtractFile(this ZipFile file, string name)
        {
            var index = file.FindEntry(name, true);
            if (index == -1)
                throw new ZipFileEntryNotFoundException(name, file);

            var entry = file[index];
            using var stream = file.GetInputStream(index);
            var memoryStream = new MemoryStream((int) entry.Size);
            stream.CopyTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            return memoryStream;
        }
    }
}
