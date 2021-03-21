using System.Collections.Generic;
using JetBrains.Annotations;

namespace OMODFramework.Oblivion.BSA
{
    [PublicAPI]
    public interface IArchiveFolder
    {
        string? Path { get; }
        IReadOnlyCollection<IArchiveFile> Files { get; }
    }
}
