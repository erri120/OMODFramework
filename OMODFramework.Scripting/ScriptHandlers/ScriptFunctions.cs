using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Force.Crc32;
using OblivionModManager.Scripting;
using OMODFramework.Compression;
using OMODFramework.Oblivion.BSA;
using OMODFramework.Scripting.Data;
using OMODFramework.Scripting.Exceptions;

namespace OMODFramework.Scripting.ScriptHandlers
{
    internal class ScriptFunctions : IScriptFunctions
    {
        private readonly OMODScriptSettings _settings;
        private readonly OMOD _omod;
        private readonly ScriptReturnData _srd;
        private IExternalScriptFunctions ExternalScriptFunctions => _settings.ExternalScriptFunctions;

        private readonly Dictionary<string, BSAReader> _loadedBSAs = new Dictionary<string, BSAReader>(StringComparer.OrdinalIgnoreCase);
        
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
                throw new OMODScriptFunctionException("User canceled the dialog!");
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
            List<int> result;

            var previewPaths = new List<string>();
            if (previews != null)
            {
                previewPaths = previews
                    .Select(preview => _omod.GetDataFile(preview))
                    .Select(file => file.GetFileInFolder(_srd.DataFolder))
                    .ToList();
            }
            
            if (_settings.UseBitmapOverloads)
            {
                var bitmapPreviews = new List<Bitmap>();

                if (previews != null)
                {
                    bitmapPreviews = previewPaths
                        .Select(x => new Bitmap(x))
                        .ToList();
                }
                
                result = ExternalScriptFunctions
                    .Select(items, title, many, bitmapPreviews, descs ?? Array.Empty<string>())
                    .ToList();

                foreach (var bitmap in bitmapPreviews)
                {
                    bitmap.Dispose();
                }
            }
            else
            {
                result = ExternalScriptFunctions
                    .Select(items, title, many, previewPaths, descs ?? Array.Empty<string>())
                    .ToList();
            }

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
            var file = _omod.GetDataFile(path);
            var filePath = file.GetFileInFolder(_srd.DataFolder);

            if (!File.Exists(filePath))
                throw new OMODScriptFunctionException($"Image for DisplayImage does not exist: {filePath}");

            if (_settings.UseBitmapOverloads)
            {
                using var bitmap = new Bitmap(filePath);
                ExternalScriptFunctions.DisplayImage(bitmap, title);
            }
            else
            {
                ExternalScriptFunctions.DisplayImage(filePath, title);
            }
        }

        public void DisplayText(string path)
        {
            DisplayText(path, null);
        }

        public void DisplayText(string path, string? title)
        {
            var file = _omod.GetDataFile(path);
            var filePath = file.GetFileInFolder(_srd.DataFolder);
            
            if (!File.Exists(filePath))
                throw new OMODScriptFunctionException($"Text for DisplayText does not exist: {filePath}");

            var text = File.ReadAllText(filePath);
            ExternalScriptFunctions.DisplayText(text, title);
        }

        public void LoadEarly(string plugin)
        {
            var pluginFile = _srd.GetPluginFile(plugin, false);
            pluginFile.LoadEarly = true;
        }

        public void LoadBefore(string plugin1, string plugin2)
        {
            var pluginFile = _srd.GetPluginFile(plugin1, false);
            var otherPlugin = _srd.GetPluginFile(plugin2, false);
            
            pluginFile.LoadBefore.Add(otherPlugin);
        }

        public void LoadAfter(string plugin1, string plugin2)
        {
            var pluginFile = _srd.GetPluginFile(plugin1, false);
            var otherPlugin = _srd.GetPluginFile(plugin2, false);
            
            pluginFile.LoadAfter.Add(otherPlugin);
        }

        public void SetNewLoadOrder(string[] plugins)
        {
            /*
             * This function was rather interesting in OBMM as it modified the LastWriteTime of all plugin files in the
             * entire Oblivion folder. OBMM then went ahead and sorted those plugins based on the new LastWriteTime. 
             */
            ExternalScriptFunctions.SetNewLoadOrder(plugins);
        }

        public void UncheckEsp(string plugin)
        {
            var pluginFile = _srd.GetPluginFile(plugin, false);
            pluginFile.IsUnchecked = true;
        }

        public void SetDeactivationWarning(string plugin, DeactiveStatus warning)
        {
            var pluginFile = _srd.GetPluginFile(plugin, false);
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
                .Select(x => new PluginFile(x));
            
            _srd.PluginFiles.Clear();
            _srd.PluginFiles.UnionWith(files);
        }

        public void InstallAllDataFiles()
        {
            var files = _omod.GetDataFiles()
                .Select(x => new DataFile(x));
            
            _srd.DataFiles.Clear();
            _srd.DataFiles.UnionWith(files);
        }

        // ReSharper disable once IdentifierTypo
        public void DontInstallPlugin(string name)
        {
            var plugin = _srd.GetPluginFile(name);
            if (!_srd.PluginFiles.Remove(plugin))
                throw new OMODScriptFunctionException($"Unable to remove Plugin from collection: {plugin}");
        }

        // ReSharper disable once IdentifierTypo
        public void DontInstallDataFile(string name)
        {
            var dataFile = _srd.GetDataFile(name);
            if (!_srd.DataFiles.Remove(dataFile))
                throw new OMODScriptFunctionException($"Unable to remove DataFile from collection: {dataFile}");
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
            var original = _omod.GetPluginFile(name);
            
            var pluginFile = new PluginFile(original);
            _srd.PluginFiles.AddOrChange(pluginFile, actualValue => actualValue.Input = pluginFile.Input);
        }

        public void InstallDataFile(string name)
        {
            var original = _omod.GetDataFile(name);
            
            var dataFile = new DataFile(original);
            _srd.DataFiles.AddOrChange(dataFile, actualValue => actualValue.Input = dataFile.Input);
        }

        public void InstallDataFolder(string folder, bool recurse)
        {
            var files = _omod.GetDataFiles()
                .FileEnumeration(folder, "*", recurse)
                .Select(x => new DataFile(x));

            foreach (var dataFile in files)
            {
                _srd.DataFiles.AddOrChange(dataFile, actualValue => actualValue.Input = dataFile.Input);
            }
        }

        public void CopyPlugin(string from, string to)
        {
            var original = _omod.GetPluginFile(from);
            
            var pluginFile = new PluginFile(original, to);
            _srd.PluginFiles.AddOrChange(pluginFile, actualValue => actualValue.Input = pluginFile.Input);
        }

        public void CopyDataFile(string from, string to)
        {
            var original = _omod.GetDataFile(from);
            
            var dataFile = new DataFile(original, to);
            _srd.DataFiles.AddOrChange(dataFile, actualValue => actualValue.Input = dataFile.Input);
        }

        public void CopyDataFolder(string from, string to, bool recurse)
        {
            var fromPath = from.MakePath();
            var toPath = to.MakePath();
            var files = _omod.GetDataFiles()
                .FileEnumeration(fromPath, "*", recurse)
                .Select(x => new DataFile(x, x.Name.Replace(fromPath, toPath, StringComparison.OrdinalIgnoreCase)));
            
            foreach (var dataFile in files)
            {
                _srd.DataFiles.AddOrChange(dataFile, actualValue => actualValue.Input = dataFile.Input);
            }
        }

        public void PatchPlugin(string from, string to, bool create)
        {
            var pluginFile = _omod.GetPluginFile(from);
            
            var filePatch = new FilePatch(pluginFile, to, create, true);
            _srd.FilePatches.AddOrChange(filePatch, actualValue =>
            {
                actualValue.From = pluginFile;
                actualValue.Create = create;
            });
        }

        public void PatchDataFile(string from, string to, bool create)
        {
            var dataFile = _omod.GetDataFile(from);
            
            var filePatch = new FilePatch(dataFile, to, create, false);
            _srd.FilePatches.AddOrChange(filePatch, actualValue =>
            {
                actualValue.From = dataFile;
                actualValue.Create = create;
            });
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
            var iniEditInfo = new INIEditInfo(section, key, value);
            _srd.INIEdits.AddOrChange(iniEditInfo, actualValue => actualValue.NewValue = iniEditInfo.NewValue);
        }

        public void EditShader(byte package, string name, string path)
        {
            var dataFile = _srd.GetOrAddDataFile(path, _omod);
            var sdpEditInfo = new SDPEditInfo(package, name, dataFile);
            _srd.SDPEdits.AddOrChange(sdpEditInfo, actualValue => actualValue.File = sdpEditInfo.File);
        }

        public void FatalError()
        {
            throw new OMODScriptFunctionException("FatalError called!");
        }

        // ReSharper disable once IdentifierTypo
        public void SetGMST(string file, string edid, string value)
        {
            AddPluginEdit(file, edid, value, false);
        }

        // ReSharper disable once IdentifierTypo
        public void SetGlobal(string file, string edid, string value)
        {
            AddPluginEdit(file, edid, value, false);
        }

        // ReSharper disable once IdentifierTypo
        private void AddPluginEdit(string file, string edid, string value, bool isGMST)
        {
            var pluginFile = _srd.GetOrAddPluginFile(file, _omod);
            var pluginEditInfo = new PluginEditInfo(value, pluginFile, edid, isGMST);
            _srd.PluginEdits.AddOrChange(pluginEditInfo, actualValue => actualValue.NewValue = pluginEditInfo.NewValue);
        }
        
        public void SetPluginByte(string file, long offset, byte value)
        {
            SetPlugin(file, offset, typeof(byte), value);
        }

        public void SetPluginShort(string file, long offset, short value)
        {
            SetPlugin(file, offset, typeof(short), value);
        }

        public void SetPluginInt(string file, long offset, int value)
        {
            SetPlugin(file, offset, typeof(int), value);
        }

        public void SetPluginLong(string file, long offset, long value)
        {
            SetPlugin(file, offset, typeof(long), value);
        }

        public void SetPluginFloat(string file, long offset, float value)
        {
            SetPlugin(file, offset, typeof(float), value);
        }

        private void SetPlugin(string file, long offset, Type type, object value)
        {
            var pluginFile = _srd.GetOrAddPluginFile(file, _omod);
            var setPluginInfo = new SetPluginInfo(offset, pluginFile, type, value);
            
            _srd.SetPluginInfos.AddOrChange(setPluginInfo, actualValue =>
            {
                actualValue.ValueType = type;
                actualValue.Value = value;
            });
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
            return ExternalScriptFunctions.ReadINI(section, value);
        }

        public string ReadRendererInfo(string value)
        {
            return ExternalScriptFunctions.ReadRendererInfo(value);
        }

        public void EditXMLLine(string file, int line, string value)
        {
            var dataFile = _srd.GetOrAddDataFile(file, _omod);
            var editXMLInfo = new EditXMLInfo(dataFile, line, value);
            _srd.EditXMLInfos.AddOrChange(editXMLInfo, actualValue => actualValue.Value = value);
        }

        public void EditXMLReplace(string file, string find, string replace)
        {
            var dataFile = _srd.GetOrAddDataFile(file, _omod);
            var editXMLInfo = new EditXMLInfo(dataFile, find, replace);
            _srd.EditXMLInfos.AddOrChange(editXMLInfo, actualValue => actualValue.Replace = replace);
        }

        public byte[] ReadDataFile(string file)
        {
            var compressedFile = _omod.GetDataFile(file);
            var path = compressedFile.GetFileInFolder(_srd.DataFolder);

            if (!File.Exists(path))
                throw new OMODScriptFunctionException($"File for ReadDataFile does not exist: {path}");

            return File.ReadAllBytes(path);
        }

        public byte[] ReadExistingDataFile(string file)
        {
            return ExternalScriptFunctions.ReadExistingDataFile(file);
        }
        
        public void GenerateNewDataFile(string file, byte[] data)
        {
            var filePath = Path.Combine(_srd.DataFolder, file);
            if (File.Exists(filePath))
                throw new OMODScriptFunctionException($"Can not generate new data file because the file already exists: {filePath}");

            /*
             * Very cheesy implementation. Since ScriptReturnData.DataFiles is a HashSet<DataFile> and DataFile inherits
             * from ScriptReturnFile which expects an OMODCompressedFile, we need to create a new fake OMODCompressedFile
             * with offset -1 and pass it to the constructor. Don't know if there is a better solution for this but this
             * works for now.
             */
            
            File.WriteAllBytes(filePath, data);
            var crc = Crc32Algorithm.Compute(data, 0, data.Length);
            var compressedFile = new OMODCompressedFile(file, crc, data.Length, -1);

            _srd.DataFiles.Add(new DataFile(compressedFile));
        }

        public void CancelDataFileCopy(string file)
        {
            var dataFile = _srd.GetDataFile(file, false);
            if (!_srd.DataFiles.Remove(dataFile))
                throw new OMODScriptFunctionException($"Unable to remove DataFile from collection: {dataFile}");
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

        public byte[] GetDataFileFromBSA(string file)
        {
            if (!_settings.UseInternalBSAFunctions)
                return ExternalScriptFunctions.GetDataFileFromBSA(file);
            
            /*
             * The problem with this function is that OBMM loaded all BSAs in the Oblivion data folder on startup and was
             * thus able to search a Dictionary for this file. We can't really do this here so this relies on an external
             * implementation for now.
             */
            
            throw new NotImplementedException();
        }

        public byte[] GetDataFileFromBSA(string bsa, string file)
        {
            /*
             * Similar to GetDataFileFromBSA(string file) but very different as we now have a concrete bsa we need to
             * read where the file will be in. You can still use your own implementation but now we actually have one
             * that checks our bsa cache and finds the file.
             */
            
            var filePath = file.MakePath();
            
            if (!_settings.UseInternalBSAFunctions)
                return ExternalScriptFunctions.GetDataFileFromBSA(bsa, file);

            if (!_loadedBSAs.TryGetValue(bsa, out var reader))
            {
                var bsaPath = ExternalScriptFunctions.GetExistingBSAPath(bsa).MakePath();
                reader = new BSAReader(bsaPath);
                _loadedBSAs.Add(bsa, reader);
            }

            var archiveFile = reader.Files.First(x => x.Path.Equals(filePath, StringComparison.OrdinalIgnoreCase));

            var buffer = new byte[archiveFile.Size];
            using var ms = new MemoryStream(buffer);
            archiveFile.CopyDataTo(ms);

            return buffer;
        }
        
        public bool IsSimulation()
        {
            return false;
        }

        public Form CreateCustomDialog()
        {
            var from = new Form
            {
                Name = "OMODFramework"
            };
            
            return from;
        }
    }
}
