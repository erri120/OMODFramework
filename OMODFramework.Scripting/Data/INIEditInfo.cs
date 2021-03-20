using System;
using JetBrains.Annotations;

namespace OMODFramework.Scripting.Data
{
    [PublicAPI]
    public struct INIEditInfo : IEquatable<INIEditInfo>
    {
        public readonly string Section;

        public readonly string Name;

        public readonly string NewValue;

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

        public bool Equals(INIEditInfo other)
        {
            return string.Equals(Section, other.Section, StringComparison.OrdinalIgnoreCase) && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            return obj is INIEditInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Section, StringComparer.OrdinalIgnoreCase);
            hashCode.Add(Name, StringComparer.OrdinalIgnoreCase);
            return hashCode.ToHashCode();
        }

        public static bool operator ==(INIEditInfo left, INIEditInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(INIEditInfo left, INIEditInfo right)
        {
            return !(left == right);
        }
    }
}
