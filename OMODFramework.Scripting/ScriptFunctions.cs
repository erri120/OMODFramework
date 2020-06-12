using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OblivionModManager.Scripting;

namespace OMODFramework.Scripting
{
    internal class ScriptFunctions : OblivionModManager.Scripting.IScriptFunctions
    {
        private readonly IScriptSettings _settings;
        private readonly OMOD _omod;
        private readonly ScriptReturnData _srd;
        private readonly IEqualityComparer<string> _comparer;

        internal ScriptFunctions(IScriptSettings settings, OMOD omod, ScriptReturnData srd)
        {
            _settings = settings;
            _omod = omod;
            _srd = srd;
            _comparer = new PathComparer();
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
            if(_omod.OMODFile.PluginsList == null)
                throw new ScriptingNullListException(false);

            return _omod.OMODFile.PluginsList.Select(x => x.Name).FileEnumeration(path, pattern, recurse).ToArray();
        }

        public string[] GetDataFiles(string path, string pattern, bool recurse)
        {
            if(_omod.OMODFile.DataList == null)
                throw new ScriptingNullListException();

            return _omod.OMODFile.DataList.Select(x => x.Name).FileEnumeration(path, pattern, recurse).ToArray();
        }

        public string[] GetPluginFolders(string path, string pattern, bool recurse)
        {
            //this function is kinda stupid as you can't really have folders with plugins
            if (_omod.OMODFile.PluginsList == null)
                throw new ScriptingNullListException();

            return _omod.OMODFile.PluginsList.Select(x => x.Name).DirectoryEnumeration(path, pattern, recurse).ToArray();
        }

        public string[] GetDataFolders(string path, string pattern, bool recurse)
        {
            if (_omod.OMODFile.DataList == null)
                throw new ScriptingNullListException();

            return _omod.OMODFile.DataList.Select(x => x.Name).DirectoryEnumeration(path, pattern, recurse).ToArray();
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
            var previewList = new List<Bitmap>();
            if (previews != null)
            {
                if(_omod.OMODFile.DataList == null)
                    throw new ScriptingNullListException();

                previewList = previews
                    .Select(x => _omod.OMODFile.DataList!.First(
                            y => y.Name.EqualsPath(x)))
                    .Select(x => _omod.OMODFile.ExtractDecompressedFile(x))
                    .Select(x => new Bitmap(x)).ToList();
            }

            IEnumerable<string> itemsList = items.ToList();
            var result = _settings.ScriptFunctions.Select(itemsList, title, many, previewList, descs ?? new string[0]).ToList();
            return result.Select(x => itemsList.ElementAt(x)).ToArray();
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
            if (_omod.OMODFile.DataList == null)
                throw new ScriptingNullListException();

            var file = _omod.OMODFile.DataList.First(x =>
                x.Name.Equals(path, StringComparison.InvariantCultureIgnoreCase));

            _settings.ScriptFunctions.DisplayImage(new Bitmap(_omod.OMODFile.ExtractDecompressedFile(file)), null);
        }

        public void DisplayImage(string path, string title)
        {
            if (_omod.OMODFile.DataList == null)
                throw new ScriptingNullListException();

            var file = _omod.OMODFile.DataList.First(x =>
                x.Name.Equals(path, StringComparison.InvariantCultureIgnoreCase));

            _settings.ScriptFunctions.DisplayImage(new Bitmap(_omod.OMODFile.ExtractDecompressedFile(file)), title);
        }

        public void DisplayText(string path)
        {
            if (_omod.OMODFile.DataList == null)
                throw new ScriptingNullListException();

            var file = _omod.OMODFile.DataList.First(x =>
                x.Name.Equals(path, StringComparison.InvariantCultureIgnoreCase));

            string text;
            using (var stream = _omod.OMODFile.ExtractDecompressedFile(file))
            using (var br = new BinaryReader(stream))
            {
                text = br.ReadString();
            }

            _settings.ScriptFunctions.DisplayText(text, null);
        }

        public void DisplayText(string path, string title)
        {
            if (_omod.OMODFile.DataList == null)
                throw new ScriptingNullListException();

            var file = _omod.OMODFile.DataList.First(x =>
                x.Name.Equals(path, StringComparison.InvariantCultureIgnoreCase));

            string text;
            using (var stream = _omod.OMODFile.ExtractDecompressedFile(file))
            using (var br = new BinaryReader(stream))
            {
                text = br.ReadString();
            }

            _settings.ScriptFunctions.DisplayText(text, title);
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
            if (_omod.OMODFile.PluginsList == null)
                throw new ScriptingNullListException(false);

            _srd.PluginFiles = _omod.OMODFile.PluginsList.Select(x => new PluginFile(x)).ToList();
        }

        public void InstallAllDataFiles()
        {
            if (_omod.OMODFile.DataList == null)
                throw new ScriptingNullListException();

            _srd.DataFiles = _omod.OMODFile.DataList.Select(x => new DataFile(x)).ToList();
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
            if (_omod.OMODFile.PluginsList == null)
                throw new ScriptingNullListException(false);

            _srd.PluginFiles.Add(new PluginFile(_omod.OMODFile.PluginsList.First(x => x.Name == name)));
        }

        public void InstallDataFile(string name)
        {
            if (_omod.OMODFile.DataList == null)
                throw new ScriptingNullListException();

            _srd.PluginFiles.Add(new PluginFile(_omod.OMODFile.DataList.First(x => x.Name == name)));
        }

        public void InstallDataFolder(string folder, bool recurse)
        {
            if (_omod.OMODFile.DataList == null)
                throw new ScriptingNullListException();

            var files = _omod.OMODFile.DataList
                .Select(x => x.Name)
                .FileEnumeration(folder, "*", recurse);

            var range = _omod.OMODFile.DataList
                .Where(x => files.Contains(x.Name))
                .Select(x => new DataFile(x))
                .ToList();

            _srd.DataFiles.AddRange(range);
        }

        public void CopyPlugin(string from, string to)
        {
            if (_omod.OMODFile.PluginsList == null)
                throw new ScriptingNullListException(false);

            if (_srd.PluginFiles.Any(x =>
                x.OriginalFile.Name.EqualsPath(from)))
            {
                var first = _srd.PluginFiles.First(x =>
                        x.OriginalFile.Name.EqualsPath(from));
                first.Output = first.Output.ReplaceIgnoreCase(from, to);
            }
            else
            {
                var file = new PluginFile(_omod.OMODFile.PluginsList
                    .First(x => x.Name.EqualsPath(from)));
                file.Output = file.Output = to;
                _srd.PluginFiles.Add(file);
            }
        }

        public void CopyDataFile(string from, string to)
        {
            if (_omod.OMODFile.DataList == null)
                throw new ScriptingNullListException();

            if (_srd.DataFiles.Any(x =>
                x.OriginalFile.Name.EqualsPath(from)))
            {
                var first = _srd.DataFiles.First(x =>
                    x.OriginalFile.Name.EqualsPath(from));
                first.Output = first.Output.ReplaceIgnoreCase(from, to);
            }
            else
            {
                var file = new DataFile(_omod.OMODFile.DataList
                    .First(x => x.Name.EqualsPath(from)));
                file.Output = file.Output = to;
                _srd.DataFiles.Add(file);
            }
        }

        public void CopyDataFolder(string from, string to, bool recurse)
        {
            if (_omod.OMODFile.DataList == null)
                throw new ScriptingNullListException();

            var files = _omod.OMODFile.DataList.Select(x => x.Name)
                .FileEnumeration(from, "*", recurse);

            _srd.DataFiles.Where(x => files.Contains(x.OriginalFile.Name, _comparer)).Do(f =>
            {
                f.Output = f.OriginalFile.Name.ReplaceIgnoreCase(from, to);
            });

            var range = _omod.OMODFile.DataList
                .Where(x => files.Contains(x.Name, _comparer))
                .Where(x => _srd.DataFiles.All(y => !y.OriginalFile.Equals(x)))
                .Select(x => new DataFile(x) {Output = x.Name.ReplaceIgnoreCase(from, to)}).ToList();

            _srd.DataFiles.AddRange(range);
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
            if (_omod.OMODFile.DataList == null)
                throw new ScriptingNullListException();

            var first = _omod.OMODFile.DataList.First(x =>
                x.Name.EqualsPath(file));

            using var stream = _omod.OMODFile.ExtractDecompressedFile(first);
            byte[] buffer = new byte[first.Length];
            stream.Read(buffer, 0, (int) first.Length);

            return buffer;
        }

        public byte[] ReadExistingDataFile(string file)
        {
            return _settings.ScriptFunctions.ReadExistingDataFile(file);
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
            if (_srd.DataFiles.Any(x => x.Output.EqualsPath(file)))
                _srd.DataFiles.Remove(_srd.DataFiles.First(x =>
                    x.Output.EqualsPath(file)));
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
