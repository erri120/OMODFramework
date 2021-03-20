using System;

namespace OMODFramework.Scripting.ScriptHandlers.OBMMScript.Tokenizer
{
    [AttributeUsage(AttributeTargets.Field)]
    internal class LineValidationAttribute : Attribute
    {
        internal readonly int Min;
        internal readonly int Max;
        
        internal LineValidationAttribute(int length)
        {
            Min = length;
            Max = length;
        }

        internal LineValidationAttribute(int min, int max)
        {
            Min = min;
            Max = max;
        }
    }
}
