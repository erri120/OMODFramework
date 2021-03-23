// ReSharper disable CheckNamespace

using JetBrains.Annotations;

namespace OblivionModManager.Scripting
{
    /// <summary>
    /// Conflict levels.
    /// </summary>
    [PublicAPI]
    public enum ConflictLevel
    {
        /// <summary>
        /// Conflict when other is active.
        /// </summary>
        Active,
        
        /// <summary>
        /// No conflict at all.
        /// </summary>
        NoConflict,
        
        /// <summary>
        /// Minor conflict.
        /// </summary>
        MinorConflict,
        
        /// <summary>
        /// Major Conflict.
        /// </summary>
        MajorConflict,
        
        /// <summary>
        /// Unusable, higher priority than <see cref="MajorConflict"/>.
        /// </summary>
        Unusable
    }
}
