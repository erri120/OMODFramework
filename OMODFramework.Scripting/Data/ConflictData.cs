using System;
using JetBrains.Annotations;
using OblivionModManager.Scripting;

namespace OMODFramework.Scripting.Data
{
    /// <summary>
    /// Represents a conflict between the current OMOD and another file.
    /// </summary>
    [PublicAPI]
    public class ConflictData
    {
        /// <summary>
        /// Type of Conflict
        /// </summary>
        public ConflictType Type { get; set; }
        
        /// <summary>
        /// Level of the Conflict
        /// </summary>
        public ConflictLevel Level { get; set; }
        
        /// <summary>
        /// File that the current OMOD conflicts with/depends on
        /// </summary>
        public string File { get; set; } = string.Empty;
        
        /// <summary>
        /// Conflict is only viable if the <see cref="File"/> has this minimum version
        /// </summary>
        public Version? MinVersion { get; set; }
        
        /// <summary>
        /// Conflict is only viable if the <see cref="File"/> has this maximum version
        /// </summary>
        public Version? MaxVersion { get; set; }

        /// <summary>
        /// (Can be null) Comment
        /// </summary>
        public string? Comment { get; set; }
        
        /// <summary>
        /// Whether <see cref="File"/> is a regex
        /// </summary>
        public bool Partial { get; set; }
    }

    /// <summary>
    /// Possible types of conflicts.
    /// </summary>
    [PublicAPI]
    public enum ConflictType
    {
        /// <summary>
        /// Conflict with another file
        /// </summary>
        Conflicts, 
        /// <summary>
        /// Depends on another file
        /// </summary>
        Depends
    }
}
