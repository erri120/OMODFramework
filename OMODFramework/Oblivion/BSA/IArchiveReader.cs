using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace OMODFramework.Oblivion.BSA
{
    [PublicAPI]
    internal interface IArchiveReader
    {
        bool TryGetFolder(string path, [MaybeNullWhen(false)] out IArchiveFolder folder);
    }
}
