using System;

namespace OMODFramework.Example
{
    public class Progress : ICodeProgress
    {
        private long _total;
        private bool _compressing;

        public void Init(long totalSize, bool compressing)
        {
            _total = totalSize;
            _compressing = compressing;
        }

        public void Dispose() 
        {
            Console.WriteLine(_compressing 
                ? $"Compressing finished."
                : $"Decompressing finished.");
        }

        public void SetProgress(long inSize, long outSize)
        {
            Console.WriteLine(_compressing
                ? $"Compressing: {outSize} of {_total}"
                : $"Decompressing: {inSize} of {_total}");
        }
    }
}
