using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OblivionModManager.Scripting;
using OMODFramework.Exceptions;

namespace OMODFramework.Scripting
{
    internal class ScriptFunctions : OblivionModManager.Scripting.IScriptFunctions
    {
        private readonly ScriptSettings _settings;
        private readonly OMOD _omod;
        private readonly ScriptReturnData _srd;
        private readonly IEqualityComparer<string> _comparer;

        internal ScriptFunctions(ScriptSettings settings, OMOD omod, ScriptReturnData srd)
        {
            _settings = settings;
            if(settings.FrameworkSettings.ScriptExecutionSettings == null)
                throw new BadSettingsException("ScriptExecutionSettings must not be null!", nameof(settings.FrameworkSettings.ScriptExecutionSettings));
            if (!settings.FrameworkSettings.ScriptExecutionSettings.VerifySettings())
                throw new BadSettingsException("Bad ScriptExecutionSettings! Check previous exceptions!");
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
            return _omod.OMODFile.Plugins.Select(x => x.Name).FileEnumeration(path, pattern, recurse).ToArray();
        }

        public string[] GetDataFiles(string path, string pattern, bool recurse)
        {
            return _omod.OMODFile.DataFiles.Select(x => x.Name).FileEnumeration(path, pattern, recurse).ToArray();
        }

        public string[] GetPluginFolders(string path, string pattern, bool recurse)
        {
            //this function is kinda stupid as you can't really have folders with plugins
            return _omod.OMODFile.Plugins.Select(x => x.Name).DirectoryEnumeration(path, pattern, recurse).ToArray();
        }

        public string[] GetDataFolders(string path, string pattern, bool recurse)
        {
            return _omod.OMODFile.DataFiles.Select(x => x.Name).DirectoryEnumeration(path, pattern, recurse).ToArray();
        }

        public string[] GetActiveEspNames()
        {
            return _settings.ScriptFunctions.GetESPs().Where(x => x.Active).Select(x => x.Name).ToArray();
        }

        public string[] GetExistingEspNames()
        {
            return _settings.ScriptFunctions.GetESPs().Select(x => x.Name).ToArray();
        }

        public string[] GetActiveOmodNames()
        {
            return _settings.ScriptFunctions.GetActiveOMODNames().ToArray();
        }

        public string[] Select(IEnumerable<string> items, IEnumerable<string>? previews, IEnumerable<string>? descs, string title, bool many)
        {
            var previewList = new List<Bitmap>();
            if (previews != null)
            {
                previewList = previews
                    .Select(x => _omod.OMODFile.DataFiles!.First(
                            y => y.Name.EqualsPath(x)))
                    .Select(x => _omod.OMODFile.ExtractDecompressedFile(x))
                    .Select(x => new Bitmap(x)).ToList();
            }

            IEnumerable<string> itemsList = items.ToList();
            var result = _settings.ScriptFunctions.Select(itemsList, title, many, previewList, descs ?? new string[0]).ToList();
            previewList.Do(x => x.Dispose());
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
            var file = _omod.OMODFile.DataFiles.First(x =>
                x.Name.Equals(path, StringComparison.InvariantCultureIgnoreCase));
            
            var bitmap = new Bitmap(_omod.OMODFile.ExtractDecompressedFile(file));
            _settings.ScriptFunctions.DisplayImage(bitmap, null);
        }

        public void DisplayImage(string path, string title)
        {
            var file = _omod.OMODFile.DataFiles.First(x =>
                x.Name.Equals(path, StringComparison.InvariantCultureIgnoreCase));

            var bitmap = new Bitmap(_omod.OMODFile.ExtractDecompressedFile(file));
            _settings.ScriptFunctions.DisplayImage(bitmap, title);
        }

        public void DisplayText(string path)
        {
            var file = _omod.OMODFile.DataFiles.First(x =>
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
            var file = _omod.OMODFile.DataFiles.First(x =>
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
            _srd.PluginFiles.First(x => x.Output.Equals(plugin, StringComparison.InvariantCultureIgnoreCase))
                .LoadEarly = true;
        }

        public void LoadBefore(string plugin1, string plugin2)
        {
            var plugin =
                _srd.PluginFiles.First(x => x.Output.Equals(plugin1, StringComparison.InvariantCultureIgnoreCase));
            var otherPlugin = _srd.PluginFiles.First(x => x.Output.Equals(plugin2, StringComparison.InvariantCultureIgnoreCase));

            plugin.LoadBefore.Add(otherPlugin);
        }

        public void LoadAfter(string plugin1, string plugin2)
        {
            var plugin =
                _srd.PluginFiles.First(x => x.Output.Equals(plugin1, StringComparison.InvariantCultureIgnoreCase));
            var otherPlugin = _srd.PluginFiles.First(x => x.Output.Equals(plugin2, StringComparison.InvariantCultureIgnoreCase));

            plugin.LoadAfter.Add(otherPlugin);
        }

        public void SetNewLoadOrder(string[] plugins)
        {
            //the OBMM had an interesting implementation for this:
            //it loops through all the plugins and changes the LastWriteTime
            //based on it's position in the array
            //it's basically modifying the files so it can sort using
            //LastWriteTime...
            throw new NotImplementedException();
        }

        public void UncheckEsp(string plugin)
        {
            _srd.PluginFiles.First(x => x.Output.Equals(plugin, StringComparison.InvariantCultureIgnoreCase))
                .IsUnchecked = true;
        }

        public void SetDeactivationWarning(string plugin, DeactiveStatus warning)
        {
            _srd.PluginFiles.First(x => x.Output.Equals(plugin, StringComparison.InvariantCultureIgnoreCase))
                .Warning = warning;
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
            var cd = new ConflictData
            {
                File = name,
                Comment = comment,
                MinVersion = new Version(minMinorVersion, minMinorVersion),
                MaxVersion = new Version(maxMajorVersion, maxMinorVersion),
                Partial = regex,
                Level = level,
                Type = ConflictType.Conflicts
            };

            _srd.Conflicts.Add(cd);
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
            var cd = new ConflictData
            {
                File = name,
                Comment = comment,
                MinVersion = new Version(minMinorVersion, minMinorVersion),
                MaxVersion = new Version(maxMajorVersion, maxMinorVersion),
                Partial = regex,
                Type = ConflictType.Depends
            };

            _srd.Conflicts.Add(cd);
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
            _srd.PluginFiles = _omod.OMODFile.Plugins.Select(x => new PluginFile(x)).ToHashSet();
        }

        public void InstallAllDataFiles()
        {
            _srd.DataFiles = _omod.OMODFile.DataFiles.Select(x => new DataFile(x)).ToHashSet();
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

            _srd.DataFiles.ExceptWith(_srd.DataFiles.Where(x => files.Contains(x.OriginalFile.Name)).ToList());
        }

        public void InstallPlugin(string name)
        {
            _srd.PluginFiles.Add(new PluginFile(_omod.OMODFile.Plugins.First(x => x.Name == name)));
        }

        public void InstallDataFile(string name)
        {
            _srd.PluginFiles.Add(new PluginFile(_omod.OMODFile.DataFiles.First(x => x.Name == name)));
        }

        public void InstallDataFolder(string folder, bool recurse)
        {
            var files = _omod.OMODFile.DataFiles
                .Select(x => x.Name)
                .FileEnumeration(folder, "*", recurse);

            var range = _omod.OMODFile.DataFiles
                .Where(x => files.Contains(x.Name))
                .Select(x => new DataFile(x))
                .ToList();

            _srd.DataFiles.UnionWith(range);
        }

        public void CopyPlugin(string from, string to)
        {
            var file = new PluginFile(_omod.OMODFile.Plugins
                .First(x => x.Name.EqualsPath(from)), to);
            _srd.PluginFiles.Add(file);
        }

        public void CopyDataFile(string from, string to)
        {
            var file = new DataFile(_omod.OMODFile.DataFiles
                .First(x => x.Name.EqualsPath(from)), to);
            _srd.DataFiles.Add(file);
        }

        public void CopyDataFolder(string from, string to, bool recurse)
        {
            var files = _omod.OMODFile.DataFiles.Select(x => x.Name)
                .FileEnumeration(from, "*", recurse);

            var range = _omod.OMODFile.DataFiles
                .Where(x => files.Contains(x.Name, _comparer))
                .Where(x => _srd.DataFiles.All(y => !y.OriginalFile.Equals(x)))
                .Select(x => new DataFile(x, x.Name.ReplaceIgnoreCase(from, to))).ToList();

            _srd.DataFiles.UnionWith(range);
        }

        public void PatchPlugin(string from, string to, bool create)
        {
            var extension = Path.GetExtension(to);
            if(extension == null || extension != ".esp" || extension != ".esm")
                throw new ScriptException($"Extension of {to} is not allowed!");

            //in OBMM this function replaces a plugin file inside the game folder
            //with one from the current OMOD

            PatchFile(from, to, create, false);
        }

        public void PatchDataFile(string from, string to, bool create)
        {
            var extension = Path.GetExtension(to);
            if (extension == null || extension == ".esp" || extension == ".esm")
                throw new ScriptException($"Extension of {to} is not allowed!");

            //in OBMM this function replaces a data file inside the game folder
            //with one from the current OMOD

            PatchFile(from, to, create, true);
        }

        private void PatchFile(string from, string to, bool create, bool data)
        {
            var file = data 
                ? _omod.OMODFile.DataFiles.First(x => x.Name.EqualsPath(from)) 
                : _omod.OMODFile.Plugins.First(x => x.Name.EqualsPath(from));

            _srd.Patches.Add(new PatchInfo(file, to, create, data));
        }

        public void RegisterBSA(string path)
        {
            _srd.RegisteredBSAs.Add(path);
        }

        public void UnregisterBSA(string path)
        {
            _srd.RegisteredBSAs.Remove(path);
        }

        public void EditINI(string section, string key, string value)
        {
            _srd.INIEdits.Add(new INIEditInfo(section, key, value));
        }

        public void EditShader(byte package, string name, string path)
        {
            var file = _omod.OMODFile.DataFiles.First(x => x.Name.EqualsPath(path));

            _srd.SDPEditInfos.Add(new SDPEditInfo(package, name, file.Name));
        }

        public void FatalError()
        {
            throw new ScriptingFatalErrorException();
        }

        public void SetGMST(string file, string edid, string value)
        {
            _srd.ESPEdits.Add(new ESPEditInfo(value, file, edid, true));
        }

        public void SetGlobal(string file, string edid, string value)
        {
            _srd.ESPEdits.Add(new ESPEditInfo(value, file, edid, false));
        }

        public void SetPluginByte(string file, long offset, byte value)
        {
            var entry = _omod.OMODFile.Plugins.First(x => x.Name.EqualsPath(file));
            _srd.SetPluginList.Add(new SetPluginInfo(SetPluginInfoType.Byte, value, offset, entry));
        }

        public void SetPluginShort(string file, long offset, short value)
        {
            var entry = _omod.OMODFile.Plugins.First(x => x.Name.EqualsPath(file));
            _srd.SetPluginList.Add(new SetPluginInfo(SetPluginInfoType.Short, value, offset, entry));
        }

        public void SetPluginInt(string file, long offset, int value)
        {
            var entry = _omod.OMODFile.Plugins.First(x => x.Name.EqualsPath(file));
            _srd.SetPluginList.Add(new SetPluginInfo(SetPluginInfoType.Int, value, offset, entry));
        }

        public void SetPluginLong(string file, long offset, long value)
        {
            var entry = _omod.OMODFile.Plugins.First(x => x.Name.EqualsPath(file));
            _srd.SetPluginList.Add(new SetPluginInfo(SetPluginInfoType.Long, value, offset, entry));
        }

        public void SetPluginFloat(string file, long offset, float value)
        {
            var entry = _omod.OMODFile.Plugins.First(x => x.Name.EqualsPath(file));
            _srd.SetPluginList.Add(new SetPluginInfo(SetPluginInfoType.Float, value, offset, entry));
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
            return _settings.FrameworkSettings.ScriptExecutionSettings!.ReadINIWithInterface 
                ? _settings.ScriptFunctions.ReadOblivionINI(section, value) 
                : OblivionINI.GetINIValue(_settings.FrameworkSettings.ScriptExecutionSettings!.OblivionINIPath!, section, value);
        }

        public string ReadRendererInfo(string value)
        {
            return _settings.FrameworkSettings.ScriptExecutionSettings!.ReadRendererInfoWithInterface
                ? _settings.ScriptFunctions.ReadRenderInfo(value)
                : OblivionRenderInfo.GetInfo(_settings.FrameworkSettings.ScriptExecutionSettings!.OblivionRendererInfoPath!, value);
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
            var first = _omod.OMODFile.DataFiles.First(x =>
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
            var res = _srd.DataFiles.Remove(_srd.DataFiles.First(x =>
                x.Output.EqualsPath(file)));

            if (!res)
            {
                OMODFramework.Utils.Debug($"Unable to Cancel Data File Copy of file {file} because it was not found in DataFiles!");
            }
        }

        public void CancelDataFolderCopy(string folder)
        {
            var other = _srd.DataFiles
                .Where(x => x.Output.StartsWith(folder, StringComparison.InvariantCultureIgnoreCase))
                .ToList();
             
            _srd.DataFiles.ExceptWith(other);
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
