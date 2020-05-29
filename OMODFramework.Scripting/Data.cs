using JetBrains.Annotations;

namespace OMODFramework.Scripting
{
    [PublicAPI]
    public enum ScriptType : byte
    {
        OBMMScript,
        Python,
        CSharp,
        VB
    }

    [PublicAPI]
    public interface IScriptSettings
    {

    }

    [PublicAPI]
    public class ScriptReturnData
    {

    }
}
