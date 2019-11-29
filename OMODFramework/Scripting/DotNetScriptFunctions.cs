using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OblivionModManager.Scripting;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using File = Alphaleonis.Win32.Filesystem.File;
using Path = Alphaleonis.Win32.Filesystem.Path;

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

        private static void CheckPathSafety(string path)
        {
            if(!Utils.IsSafeFileName(path))
                throw new ScriptingException($"Illegal file name: '{path}'");
        }

        private void CheckPluginDataSafety(string path, bool plugin)
        {
            CheckPathSafety(path);
            if(!File.Exists(Path.Combine(plugin ? _plugins : _dataFiles, path)))
                throw new ScriptingException($"File '{path}' does not exist");
        }

        private static void CheckFolderSafety(string path)
        {
            if(!Utils.IsSafeFolderName(path))
                throw new ScriptingException($"Illegal folder name: '{path}'");
        }

        private void CheckPluginDataFolderSafety(string path, bool plugin)
        {
            if (path.EndsWith("\\") || path.EndsWith("/")) path = path.Remove(path.Length - 1);
            CheckFolderSafety(path);
            if(!Directory.Exists(Path.Combine(plugin ? _plugins : _dataFiles, path)))
                throw new ScriptingException($"Folder '{path}' does not exist");
        }

        private void GetFilePaths(string path, string pattern, bool recursive, out IReadOnlyCollection<string> list)
        {
            list = Directory.GetFiles(path, !string.IsNullOrWhiteSpace(pattern) ? pattern : "*",
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }

        private void GetDirectoryPaths(string path, string pattern, bool recursive, out IReadOnlyCollection<string> list)
        {
            list = Directory.GetDirectories(path, !string.IsNullOrWhiteSpace(pattern) ? pattern : "*",
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }

        private void StripPathList(ref IReadOnlyCollection<string> paths, int baseLength, out IList<string> rList)
        {
            var list = (List<string>)paths;
            paths.Where(Path.IsPathRooted).Do(p => list[list.IndexOf(p)] = p.Substring(baseLength));
            rList = list;
        }

        public bool GetDisplayWarnings()
        {
            //TODO: original: Settings.ShowScriptWarnings whether to show warnings or not
            return true;
        }

        public bool DialogYesNo(string msg)
        {
            return DialogYesNo(msg, "Question");
        }

        public bool DialogYesNo(string msg, string title)
        {
            return _handler.ScriptFunctions.DialogYesNo(title, msg) == 1;
        }

        public bool DataFileExists(string path)
        {
            CheckPathSafety(path);
            return _handler.ScriptFunctions.DataFileExists(path);
        }

        public Version GetOBMMVersion()
        {
            return new Version(Framework.MajorVersion, Framework.MinorVersion, Framework.BuildNumber);
        }

        public Version GetOBSEVersion()
        {
            return _handler.ScriptFunctions.ScriptExtenderVersion();
        }

        public Version GetOBGEVersion()
        {
            return _handler.ScriptFunctions.GraphicsExtenderVersion();
        }

        public Version GetOblivionVersion()
        {
            return _handler.ScriptFunctions.OblivionVersion();
        }

        public Version GetOBSEPluginVersion(string plugin)
        {
            plugin = Path.ChangeExtension(plugin, ".dll");
            CheckPathSafety(plugin);
            return _handler.ScriptFunctions.OBSEPluginVersion(plugin);
        }

        public string[] GetPlugins(string path, string pattern, bool recurse)
        {
            CheckPluginDataFolderSafety(path, true);
            GetFilePaths(Path.Combine(_plugins, path), pattern, recurse, out var paths);
            StripPathList(ref paths, _plugins.Length, out var list);
            return list.ToArray();
        }

        public string[] GetDataFiles(string path, string pattern, bool recurse)
        {
            CheckPluginDataFolderSafety(path, false);
            GetFilePaths(Path.Combine(_dataFiles, path), pattern, recurse, out var paths);
            StripPathList(ref paths, _dataFiles.Length, out var list);
            return list.ToArray();
        }

        public string[] GetPluginFolders(string path, string pattern, bool recurse)
        {
            CheckPluginDataFolderSafety(path, true);
            GetDirectoryPaths(Path.Combine(_plugins, path), pattern, recurse, out var paths);
            StripPathList(ref paths, _plugins.Length, out var list);
            return list.ToArray();
        }

        public string[] GetDataFolders(string path, string pattern, bool recurse)
        {
            CheckPluginDataFolderSafety(path, true);
            GetDirectoryPaths(Path.Combine(_dataFiles, path), pattern, recurse, out var paths);
            StripPathList(ref paths, _dataFiles.Length, out var list);
            return list.ToArray();
        }

        public string[] GetActiveEspNames()
        {
            var a = new List<string>();
            _handler.ScriptFunctions.GetESPs().Where(e => e.Active).OrderBy(e => e.Name).Do(e => a.Add(e.Name));
            return a.ToArray();
        }

        public string[] GetExistingEspNames()
        {
            var a = new List<string>();
            _handler.ScriptFunctions.GetESPs().OrderBy(e => e.Name).Do(e => a.Add(e.Name));
            return a.ToArray();
        }

        public string[] GetActiveOmodNames()
        {
            return _handler.ScriptFunctions.GetActiveOMODNames().ToArray();
        }

        public string[] Select(string[] items, string[] previews, string[] descs, string title, bool many)
        {
            throw new NotImplementedException();
        }

        public void Message(string msg)
        {
            _handler.ScriptFunctions.Message(msg);
        }

        public void Message(string msg, string title)
        {
            _handler.ScriptFunctions.Message(msg, title);
        }

        public void DisplayImage(string path)
        {
            DisplayImage(path, null);
        }

        public void DisplayImage(string path, string title)
        {
            CheckPluginDataSafety(path, false);
            _handler.ScriptFunctions.DisplayImage(path, title ?? "");
        }

        public void DisplayText(string path)
        {
            DisplayText(path, null);
        }

        public void DisplayText(string path, string title)
        {
            CheckPluginDataSafety(path, false);
            var s = File.ReadAllText(Path.Combine(_dataFiles, path), Encoding.UTF8);
            _handler.ScriptFunctions.DisplayText(s, title ?? "");
        }

        public void LoadEarly(string plugin)
        {
            CheckPathSafety(plugin);
            if (!_srd.EarlyPlugins.Contains(plugin))
                _srd.EarlyPlugins.Add(plugin);
        }

        public void LoadBefore(string plugin1, string plugin2)
        {
            LoadOrder(plugin1.ToLower(), plugin2.ToLower(), false);
        }

        public void LoadAfter(string plugin1, string plugin2)
        {
            LoadOrder(plugin1.ToLower(), plugin2.ToLower(), true);
        }

        private void LoadOrder(string plugin1, string plugin2, bool loadAfter)
        {
            CheckPathSafety(plugin1);
            var found = _srd.CopyPlugins.Count(s => s.CopyTo == plugin1) >= 1;
            if(!found) CheckPluginDataSafety(plugin1, true);

            CheckPathSafety(plugin2);
            _srd.LoadOrderSet.RemoveWhere(s => s.Plugin == plugin1 && s.Target == plugin2);
            _srd.LoadOrderSet.Add(new PluginLoadInfo(plugin1, plugin2, loadAfter));
        }

        public void SetNewLoadOrder(string[] plugins)
        {
            // Method is kinda useless
            /*  in OBMM the System.IO.FileInfo.LastWriteTime of the plugin inside the
                data folder gets overwritten and than OBMM reads all ESPs again to load
                the changes... idk
             */
            if(plugins.Length != _handler.ScriptFunctions.GetESPs().Count)
                throw new ScriptingException("SetNewLoadOrder was called with an invalid plugin list!");
            
            plugins.Do(p =>
            {
                CheckPathSafety(p);
                if(!_handler.ScriptFunctions.DataFileExists(p))
                    throw new ScriptingException($"Plugin '{p}' does not exist!");
            });
        }

        public void UncheckEsp(string plugin)
        {
            CheckPluginDataSafety(plugin, true);
            if (!_srd.UncheckedPlugins.Contains(plugin.ToLower()))
                _srd.UncheckedPlugins.Add(plugin.ToLower());
        }

        public void SetDeactivationWarning(string plugin, DeactivationStatus warning)
        {
            CheckPluginDataSafety(plugin, true);
            _srd.ESPDeactivation.RemoveWhere(e => e.Plugin == plugin.ToLower());
            _srd.ESPDeactivation.Add(new ScriptESPWarnAgainst(plugin.ToLower(), warning));
        }

        public void ConflictsWith(string filename)
        {
            ConflictsWith(filename, 0, 0, 0, 0, null, ConflictLevel.MajorConflict, false);
        }

        public void ConslictsWith(string filename, string comment)
        {
            ConflictsWith(filename, 0, 0, 0, 0, comment, ConflictLevel.MajorConflict, false);
        }

        public void ConflictsWith(string filename, string comment, ConflictLevel level)
        {
            ConflictsWith(filename, 0, 0, 0, 0, comment, level, false);
        }

        public void ConflictsWith(
            string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion)
        {
            ConflictsWith(name, minMajorVersion, minMinorVersion, maxMajorVersion, maxMinorVersion, null,
                ConflictLevel.MajorConflict, false);
        }

        public void ConflictsWith(
            string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion,
            string comment)
        {
            ConflictsWith(name, minMajorVersion, minMinorVersion, maxMajorVersion, maxMinorVersion, comment,
                ConflictLevel.MajorConflict, false);
        }

        public void ConflictsWith(
            string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion,
            string comment,
            ConflictLevel level)
        {
            ConflictsWith(name, minMajorVersion, minMinorVersion, maxMajorVersion, maxMinorVersion, comment, level,
                false);
        }

        public void ConflictsWith(
            string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion,
            string comment,
            ConflictLevel level, bool regex)
        {
            var cd = new ConflictData
            {
                File = name,
                Comment = comment,
                Level = level,
                MinMajorVersion = minMajorVersion,
                MinMinorVersion = minMinorVersion,
                MaxMajorVersion = maxMajorVersion,
                MaxMinorVersion = maxMinorVersion,
                Partial = regex
            };
            _srd.ConflictsWith.Add(cd);
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

        public void DependsOn(
            string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment)
        {
            DependsOn(name, minMajorVersion, minMinorVersion, maxMajorVersion, maxMinorVersion, comment, false);
        }

        public void DependsOn(
            string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment,
            bool regex)
        {
            var cd = new ConflictData
            {
                File = name,
                Comment = comment,
                MinMajorVersion = minMajorVersion,
                MinMinorVersion = minMinorVersion,
                MaxMajorVersion = maxMajorVersion,
                MaxMinorVersion = maxMinorVersion,
                Partial = regex
            };
            _srd.DependsOn.Add(cd);
        }

        public void DontInstallAnyPlugins()
        {
            _srd.InstallAllPlugins = false;
        }

        public void DontInstallAnyDataFiles()
        {
            _srd.InstallAllData = false;
        }

        public void InstallAllPlugins()
        {
            _srd.InstallAllPlugins = true;
        }

        public void InstallAllDataFiles()
        {
            _srd.InstallAllData = true;
        }

        public void DontInstallPlugin(string name)
        {
            InstallSomething(name, false, true, false, false);
        }

        public void DontInstallDataFile(string name)
        {
            InstallSomething(name, false, false, false, false);
        }

        public void DontInstallDataFolder(string folder, bool recurse)
        {
            InstallSomething(folder, true, false, false, true);
        }

        public void InstallPlugin(string name)
        {
            InstallSomething(name, false, true, true, false);
        }

        public void InstallDataFile(string name)
        {
            InstallSomething(name, false, false, true, false);
        }

        public void InstallDataFolder(string folder, bool recurse)
        {
            InstallSomething(folder, true, false, true, recurse);
        }

        private void InstallSomething(string path, bool folder, bool plugin, bool install, bool recursive)
        {
            if (folder)
            {
                CheckPluginDataFolderSafety(path, plugin);
                var p = plugin ? Path.Combine(_plugins, path) : Path.Combine(_dataFiles, path);
                Directory.GetFiles(p, "*",
                    recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Do(f =>
                {
                    var file = Path.GetFullPath(f).Substring(plugin ? _plugins.Length : _dataFiles.Length);
                    if (install)
                    {
                        /*if (plugin) haven't seen a DontInstallPluginFolder yet
                        {

                        }
                        else
                        {*/
                            _srd.IgnoreData.RemoveWhere(s => s == file);
                            if (!_srd.InstallData.Contains(file))
                                _srd.InstallData.Add(file);
                        //}
                    }
                    else
                    {
                        _srd.InstallData.RemoveWhere(s => s == file);
                        if (!_srd.IgnoreData.Contains(file))
                            _srd.IgnoreData.Add(file);
                    }
                });
            }
            else
            {
                CheckPluginDataSafety(path, plugin);
                if (plugin)
                {
                    if (install)
                    {
                        _srd.IgnorePlugins.RemoveWhere(s => s == path);
                        if (!_srd.InstallPlugins.Contains(path))
                            _srd.InstallPlugins.Add(path);
                    }
                    else
                    {
                        _srd.InstallPlugins.RemoveWhere(s => s == path);
                        if (!_srd.IgnorePlugins.Contains(path))
                            _srd.IgnorePlugins.Add(path);
                    }
                }
                else
                {
                    if (install)
                    {
                        _srd.IgnoreData.RemoveWhere(s => s == path);
                        if (!_srd.InstallData.Contains(path))
                            _srd.InstallData.Add(path);
                    }
                    else
                    {
                        _srd.InstallData.RemoveWhere(s => s == path);
                        if (!_srd.IgnoreData.Contains(path))
                            _srd.IgnoreData.Add(path);
                    }
                }
            }
        }

        public void CopyPlugin(string from, string to)
        {
            CopySomething(from, to, true, false, false);
        }

        public void CopyDataFile(string from, string to)
        {
            CopySomething(from, to, false, false, false);
        }

        public void CopyDataFolder(string from, string to, bool recurse)
        {
            CopySomething(from, to, false, true, recurse);
        }

        private void CopySomething(string from, string to, bool plugin, bool folder, bool recursive)
        {
            if (folder)
            {
                from = Path.GetFullPath(Path.Combine(_dataFiles, from));
                Directory.GetFiles(from, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Do(
                    f =>
                    {
                        var fileFrom = Path.GetFullPath(f).Substring(_dataFiles.Length);
                        var fileTo = Path.GetFullPath(f).Substring(from.Length);
                        if (fileTo.StartsWith($"{Path.DirectorySeparatorChar}") ||
                            fileTo.StartsWith($"{Path.AltDirectorySeparatorChar}"))
                            fileTo = fileTo.Substring(1);

                        fileTo = Path.Combine(to, fileTo);
                        _srd.CopyDataFiles.RemoveWhere(s => s.CopyTo == fileTo.ToLower());
                        _srd.CopyDataFiles.Add(new ScriptCopyDataFile(fileFrom, fileTo));
                    });
            }
            else
            {
                CheckPathSafety(to);
                CheckPluginDataSafety(from, plugin);
                if (plugin && !to.EndsWith(".esp") && !to.EndsWith(".esm")) 
                    throw new ScriptingException("Copied plugins must have a .esp or .esm file extension");
                if (!plugin && (to.EndsWith(".esp") || to.EndsWith(".esm")))
                    throw new ScriptingException("Copied data files cannot have a .esp or .esm file extension");
                
                if (plugin)
                {
                    _srd.CopyPlugins.RemoveWhere(s => s.CopyTo == to.ToLower());
                    _srd.CopyPlugins.Add(new ScriptCopyDataFile(from.ToLower(), to.ToLower()));
                } else {
                    _srd.CopyDataFiles.RemoveWhere(s => s.CopyTo == to.ToLower());
                    _srd.CopyDataFiles.Add(new ScriptCopyDataFile(from.ToLower(), to.ToLower()));
                }
            }
        }

        public void PatchPlugin(string from, string to, bool create)
        {
            CheckPluginDataSafety(from, true);
            CheckPathSafety(to);

            if(!to.EndsWith(".esp") && !to.EndsWith(".esm"))
                throw new ScriptingException("Copied plugins must have a .esp or .esm file extension");
            if(to.Contains("\\") || to.Contains("/"))
                throw new ScriptingException("Cannot copy a plugin to a sub directory of the data folder");

            var args = (IReadOnlyCollection<object>) new List<object>{from, to, true, _dataFiles, _plugins};
            _handler.Registry.GetFunctionByName("PatchPlugin").Execute(ref args);
        }

        public void PatchDataFile(string from, string to, bool create)
        {
            CheckPluginDataSafety(from, false);
            CheckPathSafety(to);

            if(to.EndsWith(".esp") || to.EndsWith(".esm"))
                throw new ScriptingException("Copied data files must not have a .esp or .esm file extension");

            var args = (IReadOnlyCollection<object>) new List<object>{from, to, false, _dataFiles, _plugins};
            _handler.Registry.GetFunctionByName("PatchDataFile").Execute(ref args);
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
            return false;
        }
    }
}
