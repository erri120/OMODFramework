using System;
using System.Collections.Generic;
using System.Linq;
using OblivionModManager.Scripting;

namespace OMODFramework.Scripting
{
    internal class ScriptFunctions : OblivionModManager.Scripting.IScriptFunctions
    {
        private readonly IScriptSettings _settings;
        private readonly OMOD _omod;
        private readonly ScriptReturnData _srd;
        
        internal ScriptFunctions(IScriptSettings settings, OMOD omod, ScriptReturnData srd)
        {
            _settings = settings;
            _omod = omod;
            _srd = srd;
        }

        public bool GetDisplayWarnings()
        {
            return true;
        }

        public bool DialogYesNo(string msg)
        {
            var result = _settings.ScriptFunctions.DialogYesNo("", msg);
            if (result == DialogResult.Canceled)
                throw new ScriptingCanceledException($"DialogYesNo returned {result}");
            return result == DialogResult.Yes;
        }

        public bool DialogYesNo(string msg, string title)
        {
            var result = _settings.ScriptFunctions.DialogYesNo(title, msg);
            if (result == DialogResult.Canceled)
                throw new ScriptingCanceledException($"DialogYesNo returned {result}");
            return result == DialogResult.Yes;
        }

        public bool DataFileExists(string path)
        {
            return _settings.ScriptFunctions.DataFileExists(path);
        }

        public Version GetOBMMVersion()
        {
            return _settings.FrameworkSettings.CurrentOBMMVersion;
        }

        public Version GetOBSEVersion()
        {
            return _settings.ScriptFunctions.ScriptExtenderVersion();
        }

        public Version GetOBGEVersion()
        {
            return _settings.ScriptFunctions.GraphicsExtenderVersion();
        }

        public Version GetOblivionVersion()
        {
            return _settings.ScriptFunctions.OblivionVersion();
        }

        public Version GetOBSEPluginVersion(string plugin)
        {
            return _settings.ScriptFunctions.OBSEPluginVersion(plugin);
        }

        public string[] GetPlugins(string path, string pattern, bool recurse)
        {
            if(_omod.PluginsList == null)
                throw new ScriptingNullListException(false);

            return _omod.PluginsList.Select(x => x.Name).FileEnumeration(path, pattern, recurse).ToArray();
        }

        public string[] GetDataFiles(string path, string pattern, bool recurse)
        {
            if(_omod.DataList == null)
                throw new ScriptingNullListException();

            return _omod.DataList.Select(x => x.Name).FileEnumeration(path, pattern, recurse).ToArray();
        }

        public string[] GetPluginFolders(string path, string pattern, bool recurse)
        {
            //this function is kinda stupid as you can't really have folders with plugins
            if (_omod.PluginsList == null)
                throw new ScriptingNullListException();

            return _omod.PluginsList.Select(x => x.Name).DirectoryEnumeration(path, pattern, recurse).ToArray();
        }

        public string[] GetDataFolders(string path, string pattern, bool recurse)
        {
            if (_omod.DataList == null)
                throw new ScriptingNullListException();

            return _omod.DataList.Select(x => x.Name).DirectoryEnumeration(path, pattern, recurse).ToArray();
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

        public string[] Select(IEnumerable<string> items, IEnumerable<string>? previews, IEnumerable<string>? descs, string title, bool many)
        {
            //TODO: extract preview images or provide a Stream/Bitmap object for them
            IEnumerable<string> enumerable = items.ToList();
            var result =
                _settings.ScriptFunctions.Select(enumerable, title, many, previews ?? new string[0], descs ?? new string[0]).ToList();

            return result.Select(x => enumerable.ElementAt(result.IndexOf(x))).ToArray();
        }

        public void Message(string msg)
        {
            _settings.ScriptFunctions.Message(msg);
        }

        public void Message(string msg, string title)
        {
            _settings.ScriptFunctions.Message(msg, title);
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
            _srd.UnCheckedPlugins.Add(plugin);
        }

        public void SetDeactivationWarning(string plugin, DeactiveStatus warning)
        {
            throw new NotImplementedException();
        }

        public void ConflictsWith(string filename)
        {
            ConflictsWith(filename, 0, 0, 0, 0, null, ConflictLevel.MajorConflict, false);
        }

        public void ConflictsWith(string filename, string comment)
        {
            ConflictsWith(filename, 0, 0, 0, 0, comment, ConflictLevel.MajorConflict, false);
        }

        public void ConflictsWith(string filename, string comment, ConflictLevel level)
        {
            ConflictsWith(filename, 0, 0, 0, 0, comment, level, false);
        }

        public void ConflictsWith(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion)
        {
            ConflictsWith(name, minMajorVersion, minMinorVersion, maxMajorVersion, maxMinorVersion, null, ConflictLevel.MajorConflict, false);
        }

        public void ConflictsWith(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion,
            string comment)
        {
            ConflictsWith(name, minMajorVersion, minMinorVersion, maxMajorVersion, maxMinorVersion, comment, ConflictLevel.MajorConflict, false);
        }

        public void ConflictsWith(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion,
            string comment, ConflictLevel level)
        {
            ConflictsWith(name, minMajorVersion, minMinorVersion, maxMajorVersion, maxMinorVersion, comment, level, false);
        }

        public void ConflictsWith(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion,
            int maxMinorVersion,
            string? comment, ConflictLevel level, bool regex)
        {
            throw new NotImplementedException();
        }

        public void DependsOn(string filename)
        {
            DependsOn(filename, 0, 0, 0, 0, null, false);
        }

        public void DependsOn(string filename, string comment)
        {
            DependsOn(filename, 0, 0, 0, 0, comment, false);
        }

        public void DependsOn(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion)
        {
            DependsOn(name, minMajorVersion, minMinorVersion, maxMajorVersion, maxMinorVersion, null, false);
        }

        public void DependsOn(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion,
            string comment)
        {
            DependsOn(name, minMajorVersion, minMinorVersion, maxMajorVersion, maxMinorVersion, comment, false);
        }

        public void DependsOn(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion,
            int maxMinorVersion,
            string? comment, bool regex)
        {
            throw new NotImplementedException();
        }

        public void DontInstallAnyPlugins()
        {
            _srd.PluginFiles.Clear();
        }

        public void DontInstallAnyDataFiles()
        {
            _srd.DataFiles.Clear();
        }

        public void InstallAllPlugins()
        {
            if (_omod.PluginsList == null)
                throw new ScriptingNullListException(false);
            _srd.PluginFiles = _omod.PluginsList.Select(x => new PluginFile(x)).ToList();
        }

        public void InstallAllDataFiles()
        {
            if (_omod.DataList == null)
                throw new ScriptingNullListException();
            _srd.DataFiles = _omod.DataList.Select(x => new DataFile(x)).ToList();
        }

        public void DontInstallPlugin(string name)
        {
            _srd.PluginFiles.Remove(_srd.PluginFiles.First(x => x.OriginalFile.Name == name));
        }

        public void DontInstallDataFile(string name)
        {
            _srd.DataFiles.Remove(_srd.DataFiles.First(x => x.OriginalFile.Name == name));
        }

        public void DontInstallDataFolder(string folder, bool recurse)
        {
            var files = _srd.DataFiles
                .Select(x => x.OriginalFile.Name)
                .FileEnumeration(folder, "*", recurse);

            _srd.DataFiles = new List<DataFile>(_srd.DataFiles.Where(x => !files.Contains(x.OriginalFile.Name)));
        }

        public void InstallPlugin(string name)
        {
            if (_omod.PluginsList == null)
                throw new ScriptingNullListException(false);

            _srd.PluginFiles.Add(new PluginFile(_omod.PluginsList.First(x => x.Name == name)));
        }

        public void InstallDataFile(string name)
        {
            if (_omod.DataList == null)
                throw new ScriptingNullListException();
            _srd.PluginFiles.Add(new PluginFile(_omod.DataList.First(x => x.Name == name)));
        }

        public void InstallDataFolder(string folder, bool recurse)
        {
            if (_omod.DataList == null)
                throw new ScriptingNullListException();

            var files = _omod.DataList
                .Select(x => x.Name)
                .FileEnumeration(folder, "*", recurse);

            _srd.DataFiles.AddRange(_omod.DataList.Where(x => files.Contains(x.Name)).Select(x => new DataFile(x)));
        }

        public void CopyPlugin(string from, string to)
        {
            if (_omod.PluginsList == null)
                throw new ScriptingNullListException(false);

            if (_srd.PluginFiles.Any(x =>
                x.OriginalFile.Name.Equals(from, StringComparison.InvariantCultureIgnoreCase)))
            {
                var first = _srd.PluginFiles.First(x =>
                        x.OriginalFile.Name.Equals(from, StringComparison.InvariantCultureIgnoreCase));
                first.Output = first.Output.Replace(from, to);
            }
            else
            {
                var file = new PluginFile(_omod.PluginsList
                    .First(x => x.Name.Equals(from, StringComparison.InvariantCultureIgnoreCase)));
                file.Output = file.Output.Replace(from, to);
                _srd.PluginFiles.Add(file);
            }
        }

        public void CopyDataFile(string from, string to)
        {
            if (_omod.DataList == null)
                throw new ScriptingNullListException();

            if (_srd.DataFiles.Any(x =>
                x.OriginalFile.Name.Equals(from, StringComparison.InvariantCultureIgnoreCase)))
            {
                var first = _srd.DataFiles.First(x =>
                    x.OriginalFile.Name.Equals(from, StringComparison.InvariantCultureIgnoreCase));
                first.Output = first.Output.Replace(from, to);
            }
            else
            {
                var file = new DataFile(_omod.DataList
                    .First(x => x.Name.Equals(from, StringComparison.InvariantCultureIgnoreCase)));
                file.Output = file.Output.Replace(from, to);
                _srd.DataFiles.Add(file);
            }
        }

        public void CopyDataFolder(string from, string to, bool recurse)
        {
            if (_omod.DataList == null)
                throw new ScriptingNullListException();

            var files = _omod.DataList.Select(x => x.Name)
                .FileEnumeration(from, "*", recurse);

            _srd.DataFiles.Where(x => files.Contains(x.OriginalFile.Name)).Do(f =>
            {
                f.Output = f.OriginalFile.Name.Replace(from, to);
            });

            _srd.DataFiles.AddRange(_omod.DataList
                .Where(x => files.Contains(x.Name))
                .Where(x => _srd.DataFiles.All(y => !y.OriginalFile.Equals(x)))
                .Select(x => new DataFile(x){Output = x.Name.Replace(from, to)}));
        }

        public void PatchPlugin(string from, string to, bool create)
        {
            throw new NotImplementedException();
        }

        public void PatchDataFile(string from, string to, bool create)
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
            throw new ScriptingFatalErrorException();
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
            return _settings.ScriptFunctions.InputString(null, null);
        }

        public string InputString(string title)
        {
            return _settings.ScriptFunctions.InputString(title, null);
        }

        public string InputString(string title, string initial)
        {
            return _settings.ScriptFunctions.InputString(title, initial);
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
            if (_srd.DataFiles.Any(x => x.Output.Equals(file, StringComparison.InvariantCultureIgnoreCase)))
                _srd.DataFiles.Remove(_srd.DataFiles.First(x =>
                    x.Output.Equals(file, StringComparison.InvariantCultureIgnoreCase)));
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
            return false;
        }
    }
}
