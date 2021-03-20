using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OblivionModManager.Scripting;
using OMODFramework.Scripting.Data;

namespace OMODFramework.Scripting.ScriptHandlers
{
    internal class ScriptFunctions : IScriptFunctions
    {
        private readonly OMODScriptSettings _settings;
        private readonly OMOD _omod;
        private readonly ScriptReturnData _srd;
        private IExternalScriptFunctions ExternalScriptFunctions => _settings.ExternalScriptFunctions;
        
        internal ScriptFunctions(OMODScriptSettings settings, OMOD omod, ScriptReturnData srd)
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
            return DialogYesNo(msg, string.Empty);
        }

        public bool DialogYesNo(string msg, string title)
        {
            var result = title == string.Empty
                ? ExternalScriptFunctions.DialogYesNo(msg)
                : ExternalScriptFunctions.DialogYesNo(msg, title);
            if (result == DialogResult.Cancel)
                throw new NotImplementedException();
            return result == DialogResult.Yes;
        }

        public bool DataFileExists(string path)
        {
            return ExternalScriptFunctions.DataFileExists(path);
        }

        public Version GetOBMMVersion()
        {
            return _settings.CurrentOBMMVersion;
        }

        public Version GetOBSEVersion()
        {
            return ExternalScriptFunctions.GetScriptExtenderVersion();
        }

        // ReSharper disable once IdentifierTypo
        public Version GetOBGEVersion()
        {
            return ExternalScriptFunctions.GetGraphicsExtenderVersion();
        }

        public Version GetOblivionVersion()
        {
            return ExternalScriptFunctions.GetOblivionVersion();
        }

        public Version GetOBSEPluginVersion(string plugin)
        {
            return ExternalScriptFunctions.GetOBSEPluginVersion(plugin);
        }

        public string[] GetPlugins(string path, string pattern, bool recurse)
        {
            var plugins = _omod.GetPluginFiles()
                .FileEnumeration(path, pattern, recurse)
                .Select(x => x.Name);
            return plugins.ToArray();
        }

        public string[] GetDataFiles(string path, string pattern, bool recurse)
        {
            var dataFiles = _omod.GetDataFiles()
                .FileEnumeration(path, pattern, recurse)
                .Select(x => x.Name);
            return dataFiles.ToArray();
        }

        public string[] GetPluginFolders(string path, string pattern, bool recurse)
        {
            //this function is completely useless because you can't plugins in folders
            //TODO: missing citation
            return Array.Empty<string>();
        }

        public string[] GetDataFolders(string path, string pattern, bool recurse)
        {
            var dataFolders = _omod.GetDataFiles()
                .FileEnumeration(path, pattern, recurse)
                .Select(x => x.Name)
                .Select(Path.GetDirectoryName)
                .Distinct()
                .NotNull();
            return dataFolders.ToArray();
        }

        public string[] GetActiveEspNames()
        {
            return ExternalScriptFunctions
                .GetPlugins()
                .Where(x => x.Active)
                .Select(x => x.Name)
                .ToArray();
        }

        public string[] GetExistingEspNames()
        {
            return ExternalScriptFunctions
                .GetPlugins()
                .Select(x => x.Name)
                .ToArray();
        }

        public string[] GetActiveOmodNames()
        {
            return ExternalScriptFunctions.GetActiveOMODNames().ToArray();
        }

        public IEnumerable<string> Select(IEnumerable<string> items, IEnumerable<string> previews,
            IEnumerable<string> descriptions, string title, bool many)
        {
            return Select(items.ToArray(), previews.ToArray(), descriptions.ToArray(), title, many);
        }
        
        // ReSharper disable once IdentifierTypo
        public string[] Select(string[] items, string[]? previews, string[]? descs, string title, bool many)
        {
            var bitmapPreviews = new List<string>();
            if (previews != null)
            {
                bitmapPreviews = previews
                    .Select(preview => _omod.GetDataFiles()
                        .First(x => x.Name.Equals(preview, StringComparison.OrdinalIgnoreCase)))
                    .Select(file => file.GetFileInFolder(_srd.DataFolder))
                    //.Select(x => new Bitmap(x))
                    .ToList();
            }

            var result = ExternalScriptFunctions
                .Select(items, title, many, bitmapPreviews, descs ?? Array.Empty<string>())
                .ToList();

            return result
                .Select(x => items[x])
                .Select(x =>
                {
                    //removing the pipe from the item if present
                    if (x[0] == '|')
                        x = x[1..];
                    return x;
                })
                .ToArray();
        }

        public void Message(string msg)
        {
            ExternalScriptFunctions.Message(msg);
        }

        public void Message(string msg, string title)
        {
            ExternalScriptFunctions.Message(msg, title);
        }
        
        public void DisplayImage(string path)
        {
            DisplayImage(path, null);
        }

        public void DisplayImage(string path, string? title)
        {
            var file = _omod.GetDataFiles().First(x => x.Name.Equals(path, StringComparison.OrdinalIgnoreCase));
            var filePath = file.GetFileInFolder(_srd.DataFolder);
            
            if (!File.Exists(filePath))
                throw new NotImplementedException();

            //var bitmap = new Bitmap(filePath);
            ExternalScriptFunctions.DisplayImage(filePath, title);
        }

        public void DisplayText(string path)
        {
            DisplayText(path, null);
        }

        public void DisplayText(string path, string? title)
        {
            var file = _omod.GetDataFiles().First(x => x.Name.Equals(path, StringComparison.OrdinalIgnoreCase));
            var filePath = file.GetFileInFolder(_srd.DataFolder);
            
            if (!File.Exists(filePath))
                throw new NotImplementedException();

            var text = File.ReadAllText(filePath);
            ExternalScriptFunctions.DisplayText(text, title);
        }

        public void LoadEarly(string plugin)
        {
            var pluginFile = _srd.PluginFiles.First(x => x.Output.Equals(plugin, StringComparison.OrdinalIgnoreCase));
            pluginFile.LoadEarly = true;
        }

        public void LoadBefore(string plugin1, string plugin2)
        {
            var pluginFile = _srd.PluginFiles.First(x => x.Output.Equals(plugin1, StringComparison.OrdinalIgnoreCase));
            var otherPlugin = _srd.PluginFiles.First(x => x.Output.Equals(plugin2, StringComparison.OrdinalIgnoreCase));
            
            pluginFile.LoadBefore.Add(otherPlugin);
        }

        public void LoadAfter(string plugin1, string plugin2)
        {
            var pluginFile = _srd.PluginFiles.First(x => x.Output.Equals(plugin1, StringComparison.OrdinalIgnoreCase));
            var otherPlugin = _srd.PluginFiles.First(x => x.Output.Equals(plugin2, StringComparison.OrdinalIgnoreCase));
            
            pluginFile.LoadAfter.Add(otherPlugin);
        }

        public void SetNewLoadOrder(string[] plugins)
        {
            throw new NotImplementedException();
        }

        public void UncheckEsp(string plugin)
        {
            var pluginFile = _srd.PluginFiles.First(x => x.Output.Equals(plugin, StringComparison.OrdinalIgnoreCase));
            pluginFile.IsUnchecked = true;
        }

        public void SetDeactivationWarning(string plugin, DeactiveStatus warning)
        {
            var pluginFile = _srd.PluginFiles.First(x => x.Output.Equals(plugin, StringComparison.OrdinalIgnoreCase));
            pluginFile.Warning = warning;
        }

        #region ConflictsWith

        public void ConflictsWith(string filename)
        {
            ConflictsWith(filename, 0, 0, 0, 0, null, ConflictLevel.MajorConflict, false);
        }

        // ReSharper disable once IdentifierTypo
        public void ConslictsWith(string filename, string comment)
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

        public void ConflictsWith(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion,
            string? comment, ConflictLevel level, bool regex)
        {
            var cd = new ConflictData
            {
                File = name,
                Comment = comment ?? string.Empty,
                MinVersion = new Version(minMajorVersion, minMinorVersion),
                MaxVersion = new Version(maxMajorVersion, maxMinorVersion),
                Partial = regex,
                Level = level,
                Type = ConflictType.Conflicts
            };
            
            _srd.Conflicts.Add(cd);
        }

        #endregion

        #region DependsOn

        public void DependsOn(string filename)
        {
            DependsOn(filename, 0, 0, 0, 0, null, false);
        }

        public void DependsOn(string filename, string comment)
        {
            DependsOn(filename, 0, 0, 0, 0, comment, false);
        }

        public void DependsOn(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion,
            int maxMinorVersion)
        {
            DependsOn(name, minMajorVersion, minMinorVersion, maxMajorVersion, maxMinorVersion, null, false);
        }

        public void DependsOn(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion,
            int maxMinorVersion,
            string comment)
        {
            DependsOn(name, minMajorVersion, minMinorVersion, maxMajorVersion, maxMinorVersion, comment, false);
        }

        public void DependsOn(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion,
            int maxMinorVersion, string? comment, bool regex)
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

        #endregion

        // ReSharper disable once IdentifierTypo
        public void DontInstallAnyPlugins()
        {
            _srd.PluginFiles.Clear();
        }

        // ReSharper disable once IdentifierTypo
        public void DontInstallAnyDataFiles()
        {
            _srd.DataFiles.Clear();
        }

        public void InstallAllPlugins()
        {
            var files = _omod.GetPluginFiles()
                .Select(x => new PluginFile(x))
                .ToHashSet();
            
            _srd.PluginFiles.Clear();
            _srd.PluginFiles.UnionWith(files);
        }

        public void InstallAllDataFiles()
        {
            var files = _omod.GetDataFiles()
                .Select(x => new DataFile(x))
                .ToHashSet();
            
            _srd.DataFiles.Clear();
            _srd.DataFiles.UnionWith(files);
        }

        // ReSharper disable once IdentifierTypo
        public void DontInstallPlugin(string name)
        {
            var plugin = _srd.PluginFiles.First(x => x.Input.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (!_srd.PluginFiles.Remove(plugin))
                throw new NotImplementedException();
        }

        // ReSharper disable once IdentifierTypo
        public void DontInstallDataFile(string name)
        {
            var dataFile = _srd.DataFiles.First(x => x.Input.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (!_srd.DataFiles.Remove(dataFile))
                throw new NotImplementedException();
        }

        // ReSharper disable once IdentifierTypo
        public void DontInstallDataFolder(string folder, bool recurse)
        {
            var files = _srd.DataFiles.FileEnumeration(folder, "*", recurse);
            _srd.DataFiles.ExceptWith(files);
        }
        
        /*
         * The following functions all do a similar thing but for a different type of file. The important thing in these
         * functions is how we either add a new element to the HashSet or modify an existing one. We use
         * HashSet<T>.TryGetValue to get the existing element that is equal to the one we provide. The element is equal
         * when the Outputs are the same. We want every Output to be unique in the collection so we just modify the
         * existing one if both point to the same Output.
         */
        
        public void InstallPlugin(string name)
        {
            var original = _omod.GetPluginFiles()
                .First(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            var pluginFile = new PluginFile(original);

            if (_srd.PluginFiles.TryGetValue(pluginFile, out var actualValue))
            {
                actualValue.Input = pluginFile.Input;
                return;
            }

            _srd.PluginFiles.Add(pluginFile);
        }

        public void InstallDataFile(string name)
        {
            var original = _omod.GetDataFiles()
                .First(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            var dataFile = new DataFile(original);

            if (_srd.DataFiles.TryGetValue(dataFile, out var actualValue))
            {
                actualValue.Input = dataFile.Input;
                return;
            }
            
            _srd.DataFiles.Add(dataFile);
        }

        public void InstallDataFolder(string folder, bool recurse)
        {
            var files = _omod.GetDataFiles()
                .FileEnumeration(folder, "*", recurse)
                .Select(x => new DataFile(x));

            foreach (var dataFile in files)
            {
                if (_srd.DataFiles.TryGetValue(dataFile, out var actualValue))
                {
                    actualValue.Input = dataFile.Input;
                    continue;
                }

                _srd.DataFiles.Add(dataFile);
            }
        }

        public void CopyPlugin(string from, string to)
        {
            var original = _omod.GetPluginFiles()
                .First(x => x.Name.Equals(from, StringComparison.OrdinalIgnoreCase));
            var pluginFile = new PluginFile(original, to);

            if (_srd.PluginFiles.TryGetValue(pluginFile, out var actualValue))
            {
                actualValue.Input = pluginFile.Input;
                return;
            }

            _srd.PluginFiles.Add(pluginFile);
        }

        public void CopyDataFile(string from, string to)
        {
            var original = _omod.GetDataFiles()
                .First(x => x.Name.Equals(from, StringComparison.OrdinalIgnoreCase));
            var dataFile = new DataFile(original, to);

            if (_srd.DataFiles.TryGetValue(dataFile, out var actualValue))
            {
                actualValue.Input = dataFile.Input;
                return;
            }
            
            _srd.DataFiles.Add(dataFile);
        }

        public void CopyDataFolder(string from, string to, bool recurse)
        {
            var files = _omod.GetDataFiles()
                .FileEnumeration(from, "*", recurse)
                .Select(x => new DataFile(x, x.Name.Replace(from, to, StringComparison.OrdinalIgnoreCase)));
            
            foreach (var dataFile in files)
            {
                if (_srd.DataFiles.TryGetValue(dataFile, out var actualValue))
                {
                    actualValue.Input = dataFile.Input;
                    continue;
                }

                _srd.DataFiles.Add(dataFile);
            }
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
            _srd.RegisteredBSAs.Add(path);
        }

        public void UnregisterBSA(string path)
        {
            _srd.UnregisteredBSAs.Add(path);
        }

        public void EditINI(string section, string key, string value)
        {
            _srd.INIEdits.Add(new INIEditInfo(section, key, value));
        }

        public void EditShader(byte package, string name, string path)
        {
            var dataFile = _omod.GetDataFiles().First(x => x.Name.Equals(path, StringComparison.OrdinalIgnoreCase));
            _srd.SDPEdits.Add(new SDPEditInfo(package, name, dataFile));
        }

        public void FatalError()
        {
            throw new NotImplementedException();
        }

        // ReSharper disable once IdentifierTypo
        public void SetGMST(string file, string edid, string value)
        {
            var pluginFile = _omod.GetPluginFiles().First(x => x.Name.Equals(file, StringComparison.OrdinalIgnoreCase));
            _srd.PluginEdits.Add(new PluginEditInfo(value, pluginFile, edid, true));
        }

        // ReSharper disable once IdentifierTypo
        public void SetGlobal(string file, string edid, string value)
        {
            var pluginFile = _omod.GetPluginFiles().First(x => x.Name.Equals(file, StringComparison.OrdinalIgnoreCase));
            _srd.PluginEdits.Add(new PluginEditInfo(value, pluginFile, edid, false));
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
            return ExternalScriptFunctions.InputString(null, null);
        }

        public string InputString(string title)
        {
            return ExternalScriptFunctions.InputString(title, null);
        }

        public string InputString(string title, string initial)
        {
            return ExternalScriptFunctions.InputString(title, initial);
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
            var compressedFile = _omod.GetDataFiles()
                .First(x => x.Name.Equals(file, StringComparison.OrdinalIgnoreCase));
            var path = compressedFile.GetFileInFolder(_srd.DataFolder);

            if (!File.Exists(path))
                throw new NotImplementedException();

            return File.ReadAllBytes(path);
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
            var dataFile = _srd.DataFiles.First(x => x.Output.Equals(file));
            if (!_srd.DataFiles.Remove(dataFile))
                throw new NotImplementedException();
        }

        public void CancelDataFolderCopy(string folder)
        {
            var files = _srd.DataFiles
                .Where(x => x.Output.StartsWith(folder, StringComparison.OrdinalIgnoreCase));
            _srd.DataFiles.ExceptWith(files);
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
