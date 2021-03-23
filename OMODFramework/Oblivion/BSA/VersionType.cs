using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace OMODFramework.Oblivion.BSA
{
    [PublicAPI]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal enum VersionType : uint
    {
        TES4 = 0x67,
        FO3 = 0x68, // FO3, FNV, TES5
        SSE = 0x69,
        FO4 = 0x01,
        TES3 = 0xFF // Not a real Bethesda version number
    }
}
