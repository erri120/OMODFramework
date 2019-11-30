namespace OMODFramework
{
    public interface ICodeProgress : SevenZip.ICodeProgress
    {
        /// <summary>
        /// Initialization
        /// </summary>
        /// <param name="totalSize">Total size of the archive in bytes</param>
        /// <param name="compressing">Whether you are compressing or decompressing</param>
        void Init(long totalSize, bool compressing);
    }
}
