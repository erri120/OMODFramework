// ReSharper disable CheckNamespace
// ReSharper disable IdentifierTypo

using JetBrains.Annotations;

namespace OblivionModManager.Scripting
{
    /// <summary>
    /// Possible types of deactivation statuses.
    /// </summary>
    [PublicAPI]
    public enum DeactiveStatus
    {
        /// <summary>
        /// Allow deactivation.
        /// </summary>
        Allow,
        
        /// <summary>
        /// Warn against deactivation. 
        /// </summary>
        WarnAgainst,
        
        /// <summary>
        /// Disallow deactivation.
        /// </summary>
        Disallow
    }
}
