using System;
using System.IO;
using System.Text;
using JetBrains.Annotations;

namespace OMODFramework.Oblivion
{
    [PublicAPI]
    public static class OblivionSDP
    {
        /// <summary>
        /// Replaces a shader in a Shader Package file (.sdp)
        /// </summary>
        /// <param name="file">Path to the sdp file</param>
        /// <param name="shaderName">Name of the Shader to replace</param>
        /// <param name="newData">New data</param>
        /// <param name="outputFile">Optional output file location. The input file will the overwritten if not specified</param>
        /// <exception cref="ArgumentException">Input file does not exist</exception>
        public static void EditShaderPackage(string file, string shaderName, byte[] newData, string? outputFile = null)
        {
            if (!File.Exists(file))
                throw new ArgumentException($"File does not exist! {file}", nameof(file));
            
            var tmpFile = Path.GetTempFileName();
            try
            {
                //copy input file to temp file
                File.Copy(file, tmpFile, true);

                //open temp file and read from it
                using var fs = File.Open(tmpFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                EditShaderPackage(fs, shaderName, newData, out var ms);

                //create output file stream at either the specified location or overwriting the input file
                using var outputFs = File.Open(outputFile ?? file, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                
                //write updated shader package to output stream
                ms.CopyTo(outputFs);
            }
            finally
            {
                //important that we delete the tmp file once we are done with it
                if (File.Exists(tmpFile))
                    File.Delete(tmpFile);
            }
        }

        /// <summary>
        /// Replaces a shader in a Shader Package Stream
        /// </summary>
        /// <param name="inputStream">Input Stream</param>
        /// <param name="shaderName">Name of the Shader to replace</param>
        /// <param name="newData">New data</param>
        /// <param name="outputStream">Output Stream</param>
        /// <exception cref="ArgumentException">Stream is not readable or seekable</exception>
        /// <exception cref="NotImplementedException">Position mismatch due to bad data</exception>
        public static void EditShaderPackage(Stream inputStream, string shaderName, byte[] newData,
            out MemoryStream outputStream)
        {
            if (!inputStream.CanRead)
                throw new ArgumentException("Stream is not readable!", nameof(inputStream));
            if (!inputStream.CanSeek)
                throw new ArgumentException("Stream is not seekable!", nameof(inputStream));

            var shaderNameSpan = shaderName.AsSpan();
            
            var initialPosition = inputStream.Position;
            
            outputStream = new MemoryStream((int) (inputStream.Length - initialPosition));

            using var br = new BinaryReader(inputStream, Encoding.UTF8, true);
            using var bw = new BinaryWriter(outputStream, Encoding.UTF8, true);

            bw.Write(br.ReadUInt32());

            var num = br.ReadUInt32();
            bw.Write(num);
            
            var sizeOffset = br.BaseStream.Position;
            //this field contains the size of the entire shader package - 12 (starting bytes)
            //we need to later change this with the new size
            var packageSize = br.ReadUInt32();
            bw.Write(packageSize);

            var found = false;
            for (var i = 0; i < num; i++)
            {
                //the name of the shader package has a static size of 0x100 filled with the name and then zeroes
                var nameChars = br.ReadChars(0x100);
                var size = br.ReadUInt32();
                byte[] data = br.ReadBytes((int) size);
                
                bw.Write(nameChars);

                var nameSpan = (ReadOnlySpan<char>) nameChars.AsSpan();
                nameSpan = nameSpan[..nameSpan.IndexOf('\0')];

                if (!found && nameSpan.Equals(shaderNameSpan, StringComparison.OrdinalIgnoreCase))
                {
                    bw.Write(newData.Length);
                    bw.Write(newData);
                    found = true;
                }
                else
                {
                    bw.Write(size);
                    bw.Write(data);
                }
            }

            if (br.BaseStream.Position - 12 != initialPosition + packageSize)
                throw new NotImplementedException($"Position in Stream does not equal expected position: {br.BaseStream.Position} - 12 != {initialPosition} + {packageSize}");

            //re-write the package size with a new one
            bw.BaseStream.Position = sizeOffset;
            bw.Write((uint) bw.BaseStream.Length - 12);

            //reset stream positions
            inputStream.Position = initialPosition;
            outputStream.Position = 0;
        }
    }
}
