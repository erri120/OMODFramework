// ReSharper disable CheckNamespace

using JetBrains.Annotations;

namespace OblivionModManager.Scripting
{
    [PublicAPI]
    public enum ConflictLevel
    {
        Active,
        NoConflict,
        MinorConflict,
        MajorConflict,
        Unusable
    }
}
