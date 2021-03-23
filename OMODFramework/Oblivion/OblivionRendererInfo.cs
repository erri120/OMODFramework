using System;
using System.IO;
using System.Text;
using JetBrains.Annotations;

namespace OMODFramework.Oblivion
{
    /// <summary>
    /// Provides static functions for reading and modifying Oblivion Renderer-Info files.
    /// </summary>
    [PublicAPI]
    public static class OblivionRendererInfo
    {
        /// <summary>
        /// Returns the value of a key in a Renderer-Info file. 
        /// </summary>
        /// <param name="file">Path to the file</param>
        /// <param name="keyName">Key name to search for</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">File does not exist</exception>
        public static string? GetRendererInfo(string file, string keyName)
        {
            if (!File.Exists(file))
                throw new ArgumentException($"File does not exist! {file}", nameof(file));

            using var fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            return GetRendererInfo(fs, keyName);
        }

        /// <summary>
        /// Returns the value of a key in a Renderer-Info Stream.
        /// </summary>
        /// <param name="stream">Stream of the Renderer-Info file</param>
        /// <param name="keyName">Key name to search for</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Stream is not readable or seekable</exception>
        public static string? GetRendererInfo(Stream stream, string keyName)
        {
            if (!stream.CanRead)
                throw new ArgumentException("Stream is not readable!", nameof(stream));
            if (!stream.CanSeek)
                throw new ArgumentException("Stream is not seekable!", nameof(stream));
            
            var keyNameSpan = keyName.AsSpan();
            
            //the StreamReader constructor is different between .NET Standard 2.1 and .NET 5.0
            using var sr = new StreamReader(stream, Encoding.UTF8, true, -1, true);

            var initialPosition = stream.Position;
            
            /*
             * Example file (note the spaces):
	Water shader       		: yes
	Water reflections  		: yes
	Water displacement 		: yes
	Water high res     		: yes
	MultiSample Type   		: 0
	Shader Package     		: 13
             */
            
            while (sr.Peek() != -1)
            {
                var lines = sr.ReadLine();
                if (lines == null) continue;

                var span = lines.AsSpan();
                var separatorIndex = span.IndexOf(':');
                if (separatorIndex == -1) continue;

                var key = span[..separatorIndex].Trim();
                if (!key.Equals(keyNameSpan, StringComparison.OrdinalIgnoreCase))
                    continue;

                var value = span
                    .Slice(separatorIndex + 1, span.Length - separatorIndex - 1)
                    .Trim();

                //reset position
                stream.Position = initialPosition;
                
                return value.ToString();
            }

            return null;
        }
    }
}
