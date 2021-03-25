using System;
using System.Text;
using JetBrains.Annotations;
using OMODFramework.Scripting.Exceptions;

namespace OMODFramework.Scripting.Data
{
    /// <summary>
    /// Represents an edit to a XML file.
    /// </summary>
    [PublicAPI]
    public class EditXMLInfo : IEquatable<EditXMLInfo>
    {
        /// <summary>
        /// The XML file to edit.
        /// </summary>
        public readonly ScriptReturnFile File;

        /// <summary>
        /// Whether to find and replace
        /// </summary>
        public readonly bool IsReplace;
        /// <summary>
        /// Whether to replace a Line
        /// </summary>
        public readonly bool IsEditLine;

        /// <summary>
        /// Only set if <see cref="IsEditLine"/> is set to <c>true</c>. Line number
        /// to replace.
        /// </summary>
        public readonly int Line;
        /// <summary>
        /// Only set if <see cref="IsEditLine"/> is set to <c>true</c>. Line replacement.
        /// </summary>
        public string Value { get; internal set; }

        /// <summary>
        /// Only set if <see cref="IsReplace"/> is set to <c>true</c>. String to find.
        /// </summary>
        public readonly string Find;
        /// <summary>
        /// Only set if <see cref="IsReplace"/> is set to <c>true</c>. String to replace <see cref="Find"/>with.
        /// </summary>
        public string Replace { get; internal set; }
        
        internal EditXMLInfo(ScriptReturnFile file, int line, string value)
        {
            File = file;
            IsReplace = false;
            IsEditLine = true;

            Line = line;
            Value = value;

            Find = string.Empty;
            Replace = string.Empty;
        }

        internal EditXMLInfo(ScriptReturnFile file, string find, string replace)
        {
            File = file;
            IsReplace = true;
            IsEditLine = false;

            Line = -1;
            Value = string.Empty;

            Find = find;
            Replace = replace;
        }

        /// <summary>
        /// Executes the edit and changes the XML file. This function will copy the file to the output folder and then
        /// either replace <see cref="Find"/> with <see cref="Replace"/> if <see cref="IsReplace"/> or change line
        /// <see cref="Line"/> with <see cref="Value"/> if <see cref="IsEditLine"/>.
        /// </summary>
        /// <param name="extractionFolder">Path to the extraction folder. This should be <see cref="ScriptReturnData.DataFolder"/>.</param>
        /// <param name="outputFolder">Path to the output folder.</param>
        /// <exception cref="OBMMScriptHandlerException">Line <see cref="Line"/> does not exist</exception>
        public void ExecuteEdit(string extractionFolder, string outputFolder)
        {
            var path = File.CopyToOutput(extractionFolder, outputFolder);

            if (IsReplace)
            {
                var contents = System.IO.File.ReadAllText(path, Encoding.UTF8);
                contents = contents.Replace(Find, Replace);
                System.IO.File.WriteAllText(path, contents, Encoding.UTF8);
            }
            else
            {
                var lines = System.IO.File.ReadAllLines(path, Encoding.UTF8);
                if (Line < 0 || Line >= lines.Length)
                    throw new OBMMScriptHandlerException($"Unable to edit XML {path}, line {Line} does not exist!");
                lines[Line] = Value;
                System.IO.File.WriteAllLines(path, lines, Encoding.UTF8);
            }
        }
        
        /// <inheritdoc />
        public bool Equals(EditXMLInfo? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return File.Equals(other.File) 
                   && IsReplace == other.IsReplace 
                   && IsEditLine == other.IsEditLine 
                   && Line == other.Line 
                   && string.Equals(Find, other.Find, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((EditXMLInfo) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(File);
            hashCode.Add(IsReplace);
            hashCode.Add(IsEditLine);
            hashCode.Add(Line);
            hashCode.Add(Find, StringComparer.OrdinalIgnoreCase);
            return hashCode.ToHashCode();
        }
    }
}
