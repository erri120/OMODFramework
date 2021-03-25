using System;
using JetBrains.Annotations;
using OMODFramework.Oblivion;

namespace OMODFramework.Scripting.Data
{
    /// <summary>
    /// Represents an edit to an Oblivion Shader Package.
    /// </summary>
    [PublicAPI]
    public class SDPEditInfo : IEquatable<SDPEditInfo>
    {
        /// <summary>
        /// ID of the Package to change.
        /// </summary>
        public readonly byte Package;

        /// <summary>
        /// Name of the shader to edit.
        /// </summary>
        public readonly string Shader;

        /// <summary>
        /// File containing the new binary data that should replace the existing shader.
        /// </summary>
        public ScriptReturnFile File { get; set; }

        internal SDPEditInfo(byte package, string shader, ScriptReturnFile file)
        {
            Package = package;
            Shader = shader;
            File = file;
        }

        /// <summary>
        /// Executes the edit and changes a shader package.
        /// </summary>
        /// <param name="extractionFolder">Path to the extraction folder, this should be <see cref="ScriptReturnData.DataFolder"/></param>.
        /// <param name="shaderPath">Path to the shader file that should be edited.</param>
        /// <param name="outputPath">Optional output path if you don't want the shader to be overwritten.</param>
        /// <exception cref="ArgumentException">The file does not exist</exception>
        public void ExecuteEdit(string extractionFolder, string shaderPath, string? outputPath = null)
        {
            var filePath = File.GetFileInFolder(extractionFolder);
            if (!System.IO.File.Exists(filePath))
                throw new ArgumentException($"File does not exist: {filePath}");
            if (!System.IO.File.Exists(shaderPath))
                throw new ArgumentException($"File does not exist: {shaderPath}");
            
            var bytes = System.IO.File.ReadAllBytes(filePath);
            OblivionSDP.EditShaderPackage(shaderPath, Shader, bytes, outputPath);
        }
        
        /// <inheritdoc />
        public bool Equals(SDPEditInfo? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Package == other.Package && string.Equals(Shader, other.Shader, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((SDPEditInfo) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Package);
            hashCode.Add(Shader, StringComparer.OrdinalIgnoreCase);
            return hashCode.ToHashCode();
        }
    }
}
