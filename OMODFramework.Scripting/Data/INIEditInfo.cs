using System;
using JetBrains.Annotations;

namespace OMODFramework.Scripting.Data
{
    [PublicAPI]
    public class INIEditInfo : IEquatable<INIEditInfo>
    {
        public readonly string Section;

        public readonly string Name;

        public string NewValue { get; set; }

        internal INIEditInfo(string section, string name, string newValue)
        {
            Section = section;
            Name = name;
            NewValue = newValue;
        }

        public override string ToString()
        {
            return $"[{Section}]{Name}:{NewValue}";
        }


        public bool Equals(INIEditInfo? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Section, other.Section, StringComparison.OrdinalIgnoreCase) && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((INIEditInfo) obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Section, StringComparer.OrdinalIgnoreCase);
            hashCode.Add(Name, StringComparer.OrdinalIgnoreCase);
            return hashCode.ToHashCode();
        }
    }
}
