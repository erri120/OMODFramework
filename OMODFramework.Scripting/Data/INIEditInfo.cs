using System;
using System.IO;
using JetBrains.Annotations;
using OMODFramework.Oblivion;

namespace OMODFramework.Scripting.Data
{
    /// <summary>
    /// Represents an edit to the Oblivion.ini file.
    /// </summary>
    [PublicAPI]
    public class INIEditInfo : IEquatable<INIEditInfo>
    {
        /// <summary>
        /// Name of the section where <see cref="Name"/> can be found.
        /// </summary>
        public readonly string Section;

        /// <summary>
        /// Name of the key to find.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// New value of the key <see cref="Name"/>.
        /// </summary>
        public string NewValue { get; set; }

        internal INIEditInfo(string section, string name, string newValue)
        {
            Section = section;
            Name = name;
            NewValue = newValue;
        }

        /// <summary>
        /// Executes the edit and replaces a key in a section in the Oblivion.ini file with a new value.
        /// </summary>
        /// <param name="iniPath">Path to the Oblivion.ini file</param>
        /// <exception cref="ArgumentException">File does not exist</exception>
        public void ExecuteEdit(string iniPath)
        {
            if (!File.Exists(iniPath))
                throw new ArgumentException($"File does not exist: {iniPath}", nameof(iniPath));
            
            OblivionINI.SetINIValue(iniPath, Section, Name, NewValue);
        }
        
        /// <inheritdoc />
        public override string ToString()
        {
            return $"[{Section}]{Name}:{NewValue}";
        }
        
        /// <inheritdoc />
        public bool Equals(INIEditInfo? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Section, other.Section, StringComparison.OrdinalIgnoreCase) && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((INIEditInfo) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Section, StringComparer.OrdinalIgnoreCase);
            hashCode.Add(Name, StringComparer.OrdinalIgnoreCase);
            return hashCode.ToHashCode();
        }
    }
}
