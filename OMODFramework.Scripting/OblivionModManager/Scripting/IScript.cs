// ReSharper disable CheckNamespace

using JetBrains.Annotations;

namespace OblivionModManager.Scripting
{
    [PublicAPI]
    public interface IScript
    {
        void Execute(IScriptFunctions sf);
    }
}
