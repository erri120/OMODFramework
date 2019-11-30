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

        public void SetProgress(long inSize, long outSize)
        {
            if (_compressing)
            {
                Console.WriteLine($"Compressing: {outSize} of {_total}");
            }
            else
            {
                Console.WriteLine($"Decompressing: {inSize} of {_total}");
            }
        }
    }
}
