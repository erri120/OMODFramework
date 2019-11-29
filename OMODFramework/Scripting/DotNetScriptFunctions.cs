using System;
using System.Windows.Forms;

namespace OMODFramework.Scripting
{
    internal class DotNetScriptFunctions : OblivionModManager.Scripting.IScriptFunctions
    {
        private readonly ScriptReturnData _srd;
        private readonly string _dataFiles;
        private readonly string _plugins;
        private readonly SharedFunctionsHandler _handler;

        internal DotNetScriptFunctions(ScriptReturnData srd, string dataFilesPath, string pluginsPath, ref SharedFunctionsHandler handler)
        {
            _srd = srd;
            _dataFiles = dataFilesPath;
            _plugins = pluginsPath;
            _handler = handler;
        }

        public bool GetDisplayWarnings()
        {
            throw new NotImplementedException();
        }

        public bool DialogYesNo(string msg)
        {
            throw new NotImplementedException();
        }

        public bool DialogYesNo(string msg, string title)
        {
            throw new NotImplementedException();
        }

        public bool DataFileExists(string path)
        {
            throw new NotImplementedException();
        }

        public Version GetOBMMVersion()
        {
            throw new NotImplementedException();
        }

        public Version GetOBSEVersion()
        {
            throw new NotImplementedException();
        }

        public Version GetOBGEVersion()
        {
            throw new NotImplementedException();
        }

        public Version GetOblivionVersion()
        {
            throw new NotImplementedException();
        }

        public Version GetOBSEPluginVersion(string plugin)
        {
            throw new NotImplementedException();
        }

        public string[] GetPlugins(string path, string pattern, bool recurse)
        {
            throw new NotImplementedException();
        }

        public string[] GetDataFiles(string path, string pattern, bool recurse)
        {
            throw new NotImplementedException();
        }

        public string[] GetPluginFolders(string path, string pattern, bool recurse)
        {
            throw new NotImplementedException();
        }

        public string[] GetDataFolders(string path, string pattern, bool recurse)
        {
            throw new NotImplementedException();
        }

        public string[] GetActiveEspNames()
        {
            throw new NotImplementedException();
        }

        public string[] GetExistingEspNames()
        {
            throw new NotImplementedException();
        }

        public string[] GetActiveOmodNames()
        {
            throw new NotImplementedException();
        }

        public string[] Select(string[] items, string[] previews, string[] descs, string title, bool many)
        {
            throw new NotImplementedException();
        }

        public void Message(string msg)
        {
            throw new NotImplementedException();
        }

        public void Message(string msg, string title)
        {
            throw new NotImplementedException();
        }

        public void DisplayImage(string path)
        {
            throw new NotImplementedException();
        }

        public void DisplayImage(string path, string title)
        {
            throw new NotImplementedException();
        }

        public void DisplayText(string path)
        {
            throw new NotImplementedException();
        }

        public void DisplayText(string path, string title)
        {
            throw new NotImplementedException();
        }

        public void LoadEarly(string plugin)
        {
            throw new NotImplementedException();
        }

        public void LoadBefore(string plugin1, string plugin2)
        {
            throw new NotImplementedException();
        }

        public void LoadAfter(string plugin1, string plugin2)
        {
            throw new NotImplementedException();
        }

        public void SetNewLoadOrder(string[] plugins)
        {
            throw new NotImplementedException();
        }

        public void UncheckEsp(string plugin)
        {
            throw new NotImplementedException();
        }

        public void SetDeactivationWarning(string plugin, DeactivationStatus warning)
        {
            throw new NotImplementedException();
        }

        public void ConflictsWith(string filename)
        {
            throw new NotImplementedException();
        }

        public void ConslictsWith(string filename, string comment)
        {
            throw new NotImplementedException();
        }

        public void ConflictsWith(string filename, string comment, ConflictLevel level)
        {
            throw new NotImplementedException();
        }

        public void ConflictsWith(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion)
        {
            throw new NotImplementedException();
        }

        public void ConflictsWith(
            string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment)
        {
            throw new NotImplementedException();
        }

        public void ConflictsWith(
            string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment,
            ConflictLevel level)
        {
            throw new NotImplementedException();
        }

        public void ConflictsWith(
            string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment,
            ConflictLevel level, bool regex)
        {
            throw new NotImplementedException();
        }

        public void DependsOn(string filename)
        {
            throw new NotImplementedException();
        }

        public void DependsOn(string filename, string comment)
        {
            throw new NotImplementedException();
        }

        public void DependsOn(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion)
        {
            throw new NotImplementedException();
        }

        public void DependsOn(
            string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment)
        {
            throw new NotImplementedException();
        }

        public void DependsOn(
            string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment,
            bool regex)
        {
            throw new NotImplementedException();
        }

        public void DontInstallAnyPlugins()
        {
            throw new NotImplementedException();
        }

        public void DontInstallAnyDataFiles()
        {
            throw new NotImplementedException();
        }

        public void InstallAllPlugins()
        {
            throw new NotImplementedException();
        }

        public void InstallAllDataFiles()
        {
            throw new NotImplementedException();
        }

        public void DontInstallPlugin(string name)
        {
            throw new NotImplementedException();
        }

        public void DontInstallDataFile(string name)
        {
            throw new NotImplementedException();
        }

        public void DontInstallDataFolder(string folder, bool recurse)
        {
            throw new NotImplementedException();
        }

        public void InstallPlugin(string name)
        {
            throw new NotImplementedException();
        }

        public void InstallDataFile(string name)
        {
            throw new NotImplementedException();
        }

        public void InstallDataFolder(string folder, bool recurse)
        {
            throw new NotImplementedException();
        }

        public void CopyPlugin(string @from, string to)
        {
            throw new NotImplementedException();
        }

        public void CopyDataFile(string @from, string to)
        {
            throw new NotImplementedException();
        }

        public void CopyDataFolder(string @from, string to, bool recurse)
        {
            throw new NotImplementedException();
        }

        public void PatchPlugin(string @from, string to, bool create)
        {
            throw new NotImplementedException();
        }

        public void PatchDataFile(string @from, string to, bool create)
        {
            throw new NotImplementedException();
        }

        public void RegisterBSA(string path)
        {
            throw new NotImplementedException();
        }

        public void UnregisterBSA(string path)
        {
            throw new NotImplementedException();
        }

        public void EditINI(string section, string key, string value)
        {
            throw new NotImplementedException();
        }

        public void EditShader(byte package, string name, string path)
        {
            throw new NotImplementedException();
        }

        public void FatalError()
        {
            throw new NotImplementedException();
        }

        public void SetGMST(string file, string edid, string value)
        {
            throw new NotImplementedException();
        }

        public void SetGlobal(string file, string edid, string value)
        {
            throw new NotImplementedException();
        }

        public void SetPluginByte(string file, long offset, byte value)
        {
            throw new NotImplementedException();
        }

        public void SetPluginShort(string file, long offset, short value)
        {
            throw new NotImplementedException();
        }

        public void SetPluginInt(string file, long offset, int value)
        {
            throw new NotImplementedException();
        }

        public void SetPluginLong(string file, long offset, long value)
        {
            throw new NotImplementedException();
        }

        public void SetPluginFloat(string file, long offset, float value)
        {
            throw new NotImplementedException();
        }

        public string InputString()
        {
            throw new NotImplementedException();
        }

        public string InputString(string title)
        {
            throw new NotImplementedException();
        }

        public string InputString(string title, string initial)
        {
            throw new NotImplementedException();
        }

        public string ReadINI(string section, string value)
        {
            throw new NotImplementedException();
        }

        public string ReadRendererInfo(string value)
        {
            throw new NotImplementedException();
        }

        public void EditXMLLine(string file, int line, string value)
        {
            throw new NotImplementedException();
        }

        public void EditXMLReplace(string file, string find, string replace)
        {
            throw new NotImplementedException();
        }

        public Form CreateCustomDialog()
        {
            throw new NotImplementedException();
        }

        public byte[] ReadDataFile(string file)
        {
            throw new NotImplementedException();
        }

        public byte[] ReadExistingDataFile(string file)
        {
            throw new NotImplementedException();
        }

        public byte[] GetDataFileFromBSA(string file)
        {
            throw new NotImplementedException();
        }

        public byte[] GetDataFileFromBSA(string bsa, string file)
        {
            throw new NotImplementedException();
        }

        public void GenerateNewDataFile(string file, byte[] data)
        {
            throw new NotImplementedException();
        }

        public void CancelDataFileCopy(string file)
        {
            throw new NotImplementedException();
        }

        public void CancelDataFolderCopy(string folder)
        {
            throw new NotImplementedException();
        }

        public void GenerateBSA(string file, string path, string prefix, int cRatio, int cLevel)
        {
            throw new NotImplementedException();
        }

        public bool IsSimulation()
        {
            throw new NotImplementedException();
        }
    }
}
