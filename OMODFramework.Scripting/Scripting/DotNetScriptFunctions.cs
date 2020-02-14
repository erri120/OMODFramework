/*
    Copyright (C) 2019  erri120

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

/*
 * This file contains parts of the Oblivion Mod Manager licensed under GPLv2
 * and has been modified for use in this OMODFramework
 * Original source: https://www.nexusmods.com/oblivion/mods/2097
 * GPLv2: https://opensource.org/licenses/gpl-2.0.php
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using OblivionModManager.Scripting;
using OMODFramework.Classes;

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

        private static void GetFilePaths(string path, string pattern, bool recursive, out IReadOnlyCollection<string> list)
        {
            list = Directory.GetFiles(path, !string.IsNullOrWhiteSpace(pattern) ? pattern : "*",
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }

        private static void GetDirectoryPaths(string path, string pattern, bool recursive, out IReadOnlyCollection<string> list)
        {
            list = Directory.GetDirectories(path, !string.IsNullOrWhiteSpace(pattern) ? pattern : "*",
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }

        private static void StripPathList(ref IReadOnlyCollection<string> paths, int baseLength, out IList<string> rList)
        {
            var list = paths.ToList();
            paths.Where(Path.IsPathRooted).Do(p => list[list.IndexOf(p)] = p.Substring(baseLength + 1));
            rList = list;
        }

        public bool GetDisplayWarnings()
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            return Framework.Settings.ScriptExecutionSettings.EnableWarnings;
        }

        public bool DialogYesNo(string msg)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            return DialogYesNo(msg, "Question");
        }

        public bool DialogYesNo(string msg, string title)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            return _handler.ScriptFunctions.DialogYesNo(title, msg) == 1;
        }

        public bool DataFileExists(string path)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            CheckPathSafety(path);
            return _handler.ScriptFunctions.DataFileExists(path);
        }

        public Version GetOBMMVersion()
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            return new Version(Framework.Settings.MajorVersion, Framework.Settings.MinorVersion, Framework.Settings.BuildNumber, 0);
        }

        public Version GetOBSEVersion()
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            return _handler.ScriptFunctions.ScriptExtenderVersion();
        }

        public Version GetOBGEVersion()
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            return _handler.ScriptFunctions.GraphicsExtenderVersion();
        }

        public Version GetOblivionVersion()
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            return _handler.ScriptFunctions.OblivionVersion();
        }

        public Version GetOBSEPluginVersion(string plugin)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            plugin = Path.ChangeExtension(plugin, ".dll");
            CheckPathSafety(plugin);
            return _handler.ScriptFunctions.OBSEPluginVersion(plugin);
        }

        public string[] GetPlugins(string path, string pattern, bool recurse)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            CheckPluginDataFolderSafety(path, true);
            GetFilePaths(Path.Combine(_plugins, path), pattern, recurse, out var paths);
            StripPathList(ref paths, _plugins.Length, out var list);
            return list.ToArray();
        }

        public string[] GetDataFiles(string path, string pattern, bool recurse)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            CheckPluginDataFolderSafety(path, false);
            GetFilePaths(Path.Combine(_dataFiles, path), pattern, recurse, out var paths);
            StripPathList(ref paths, _dataFiles.Length, out var list);
            return list.ToArray();
        }

        public string[] GetPluginFolders(string path, string pattern, bool recurse)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            CheckPluginDataFolderSafety(path, true);
            GetDirectoryPaths(Path.Combine(_plugins, path), pattern, recurse, out var paths);
            StripPathList(ref paths, _plugins.Length, out var list);
            return list.ToArray();
        }

        public string[] GetDataFolders(string path, string pattern, bool recurse)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            CheckPluginDataFolderSafety(path, true);
            GetDirectoryPaths(Path.Combine(_dataFiles, path), pattern, recurse, out var paths);
            StripPathList(ref paths, _dataFiles.Length, out var list);
            return list.ToArray();
        }

        public string[] GetActiveEspNames()
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            var a = new List<string>();
            _handler.ScriptFunctions.GetESPs().Where(e => e.Active).OrderBy(e => e.Name).Do(e => a.Add(e.Name));
            return a.ToArray();
        }

        public string[] GetExistingEspNames()
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            var a = new List<string>();
            _handler.ScriptFunctions.GetESPs().OrderBy(e => e.Name).Do(e => a.Add(e.Name));
            return a.ToArray();
        }

        public string[] GetActiveOmodNames()
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            return _handler.ScriptFunctions.GetActiveOMODNames().ToArray();
        }

        public string[] Select(string[] items, string[] previews, string[] descs, string title, bool many)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");

            if (previews != null)
            {
                for (var i = 0; i < previews.Length; i++)
                {
                    if(previews[i] == null)
                        continue;

                    CheckPluginDataSafety(previews[i], false);
                    previews[i] = Path.Combine(_dataFiles, previews[i]);
                }
            }

            var selectedIndex = _handler.ScriptFunctions.Select(items.ToList(), title, many, previews.ToList(), descs.ToList());
            if (selectedIndex == null || selectedIndex.Count == 0)
            {
                _srd.CancelInstall = true;
                return new string[0];
            }

            var result = new string[selectedIndex.Count];
            for (int i = 0; i < selectedIndex.Count; i++)
            {
                result[i] = items[selectedIndex[i]];
            }

            return result;
        }

        public void Message(string msg)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            _handler.ScriptFunctions.Message(msg);
        }

        public void Message(string msg, string title)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            _handler.ScriptFunctions.Message(msg, title);
        }

        public void DisplayImage(string path)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            DisplayImage(path, null);
        }

        public void DisplayImage(string path, string title)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            CheckPluginDataSafety(path, false);
            _handler.ScriptFunctions.DisplayImage(path, title ?? "");
        }

        public void DisplayText(string path)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            DisplayText(path, null);
        }

        public void DisplayText(string path, string title)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            CheckPluginDataSafety(path, false);
            var s = File.ReadAllText(Path.Combine(_dataFiles, path), Encoding.UTF8);
            _handler.ScriptFunctions.DisplayText(s, title ?? "");
        }

        public void LoadEarly(string plugin)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            CheckPathSafety(plugin);
            if (!_srd.EarlyPlugins.Contains(plugin))
                _srd.EarlyPlugins.Add(plugin);
        }

        public void LoadBefore(string plugin1, string plugin2)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            LoadOrder(plugin1.ToLower(), plugin2.ToLower(), false);
        }

        public void LoadAfter(string plugin1, string plugin2)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
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
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            // Method is kinda useless
            /*  in OBMM the System.IO.FileInfo.LastWriteTime of the plugin inside the
                data folder gets overwritten and than OBMM reads all ESPs again to load
                the changes... idk
             */
            if(plugins.Length != _handler.ScriptFunctions.GetESPs().Count())
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
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            CheckPluginDataSafety(plugin, true);
            if (!_srd.UncheckedPlugins.Contains(plugin.ToLower()))
                _srd.UncheckedPlugins.Add(plugin.ToLower());
        }

        public void SetDeactivationWarning(string plugin, DeactivationStatus warning)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            CheckPluginDataSafety(plugin, true);
            _srd.ESPDeactivation.RemoveWhere(e => e.Plugin == plugin.ToLower());
            _srd.ESPDeactivation.Add(new ScriptESPWarnAgainst(plugin.ToLower(), warning));
        }

        public void ConflictsWith(string filename)
        {
            ConflictsWith(filename, 0, 0, 0, 0, null, ConflictLevel.MajorConflict, false);
        }

        // original typo from the oblivion mod manager, gg

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
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
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
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
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
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            _srd.InstallAllPlugins = false;
        }

        public void DontInstallAnyDataFiles()
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            _srd.InstallAllData = false;
        }

        public void InstallAllPlugins()
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            _srd.InstallAllPlugins = true;
        }

        public void InstallAllDataFiles()
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            _srd.InstallAllData = true;
        }

        public void DontInstallPlugin(string name)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            InstallSomething(name, false, true, false, false);
        }

        public void DontInstallDataFile(string name)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            InstallSomething(name, false, false, false, false);
        }

        public void DontInstallDataFolder(string folder, bool recurse)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            InstallSomething(folder, true, false, false, true);
        }

        public void InstallPlugin(string name)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            InstallSomething(name, false, true, true, false);
        }

        public void InstallDataFile(string name)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            InstallSomething(name, false, false, true, false);
        }

        public void InstallDataFolder(string folder, bool recurse)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
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
                    if (file.StartsWith("\\")) file =  file.Substring(1);
                    if (install)
                    {
                        _srd.IgnoreData.RemoveWhere(s => s == file);
                            if (!_srd.InstallData.Contains(file))
                                _srd.InstallData.Add(file);
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
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            CopySomething(from, to, true, false, false);
        }

        public void CopyDataFile(string from, string to)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            CopySomething(from, to, false, false, false);
        }

        public void CopyDataFolder(string from, string to, bool recurse)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
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
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
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
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            CheckPluginDataSafety(from, false);
            CheckPathSafety(to);

            if(to.EndsWith(".esp") || to.EndsWith(".esm"))
                throw new ScriptingException("Copied data files must not have a .esp or .esm file extension");

            var args = (IReadOnlyCollection<object>) new List<object>{from, to, false, _dataFiles, _plugins};
            _handler.Registry.GetFunctionByName("PatchDataFile").Execute(ref args);
        }

        public void RegisterBSA(string path)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            CheckPluginDataSafety(path, false);
            if(path.Contains(";") || path.Contains(",") || path.Contains("="))
                throw new ScriptingException("BSA path cannot contain the characters ',', '=' or ';'");
            if (!_srd.RegisterBSASet.Contains(path.ToLower()))
                _srd.RegisterBSASet.Add(path.ToLower());
        }

        public void UnregisterBSA(string path)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            CheckPluginDataSafety(path, false);
            if(path.Contains(";") || path.Contains(",") || path.Contains("="))
                throw new ScriptingException("BSA path cannot contain the characters ',', '=' or ';'");
            _srd.RegisterBSASet.RemoveWhere(s => s == path.ToLower());
        }

        public void EditINI(string section, string key, string value)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            _srd.INIEdits.Add(new INIEditInfo(section, key, value));
        }

        public void EditShader(byte package, string name, string path)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            CheckPluginDataSafety(path, false);
            _srd.SDPEdits.Add(new SDPEditInfo(package, name, Path.Combine(_dataFiles, path)));
        }

        public void FatalError()
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            Utils.Error("Script called FatalError!");
            _srd.CancelInstall = true;
        }

        public void SetGMST(string file, string edid, string value)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            CheckPluginDataSafety(file, true);
            _srd.ESPEdits.Add(new ScriptESPEdit(true, file.ToLower(), edid, value));
        }

        public void SetGlobal(string file, string edid, string value)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            CheckPluginDataSafety(file, true);
            _srd.ESPEdits.Add(new ScriptESPEdit(false, file.ToLower(), edid, value));
        }

        public void SetPluginByte(string file, long offset, byte value)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            SetPluginData(file, offset, value);
        }

        public void SetPluginShort(string file, long offset, short value)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            SetPluginData(file, offset, value);
        }

        public void SetPluginInt(string file, long offset, int value)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            SetPluginData(file, offset, value);
        }

        public void SetPluginLong(string file, long offset, long value)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            SetPluginData(file, offset, value);
        }

        public void SetPluginFloat(string file, long offset, float value)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            SetPluginData(file, offset, value);
        }

        private void SetPluginData(string file, long offset, object value)
        {
            CheckPluginDataSafety(file, true);
            using (var fs = File.OpenWrite(Path.Combine(_plugins, file)))
            {
                fs.Position = offset;
                switch (value) {
                    case byte b:
                        fs.WriteByte(b);
                        break;
                    case short s: {
                        var data = BitConverter.GetBytes(s);
                        fs.Write(data, 0, 2);
                        break;
                    }
                    case int i: {
                        var data = BitConverter.GetBytes(i);
                        fs.Write(data, 0, 4);
                        break;
                    }
                    case long l: {
                        var data = BitConverter.GetBytes(l);
                        fs.Write(data, 0, 8);
                        break;
                    }
                    case float f: {
                        var data = BitConverter.GetBytes(f);
                        fs.Write(data, 0, 4);
                        break;
                    }
                }
            }
        }

        public string InputString()
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            return InputString("", "");
        }

        public string InputString(string title)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            return InputString(title, "");
        }

        public string InputString(string title, string initial)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            var result = _handler.ScriptFunctions.InputString(title, initial);
            if(result == null)
                FatalError();

            return result ?? "";
        }

        public string ReadINI(string section, string value)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            if (!Framework.Settings.ScriptExecutionSettings.ReadINIWithInterface)
                return OblivionINI.GetINIValue(section, value);

            var s = _handler.ScriptFunctions.ReadOblivionINI(section, value);
            return s ?? throw new OMODFrameworkException(
                       "Could not read the oblivion.ini file using the function IScriptFunctions.ReadOblivionINI");
        }

        public string ReadRendererInfo(string value)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            if (!Framework.Settings.ScriptExecutionSettings.ReadRendererInfoWithInterface)
                return OblivionRenderInfo.GetInfo(value);

            var s = _handler.ScriptFunctions.ReadRendererInfo(value);
            return s ?? throw new OMODFrameworkException(
                       "Could not read the RenderInfo.txt file using the function IScriptFunctions.ReadRendererInfo");
        }

        public void EditXMLLine(string file, int line, string value)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            EditXML(file, line, value, false, null, null);
        }

        public void EditXMLReplace(string file, string find, string replace)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            EditXML(file, 0, null, true, find, replace);
        }

        private void EditXML(string file, int line, string value, bool doReplace, string find, string replace)
        {
            CheckPluginDataSafety(file, false);
            var ext = Path.GetExtension(file).ToLower();
            if(ext != ".txt" && ext != ".xml" && ext != ".bat" && ext != ".ini")
                throw new ScriptingException("Can only edit files with a .xml, .ini, .bat or .txt extension");

            var path = Path.Combine(_dataFiles, file);
            if (doReplace)
            {
                var text = File.ReadAllText(path);
                text = text.Replace(find, replace);

                try
                {
                    File.WriteAllText(path, text);
                }
                catch (Exception e)
                {
                    throw new OMODFrameworkException($"Exception while trying to write all text to file at '{path}'\n{e}");
                }
            }
            else
            {
                var lines = File.ReadAllLines(path);
                if(line < 0 || line >= lines.Length)
                    throw new ScriptingException("Invalid line number!");
                lines[line] = value;
                try
                {
                    File.WriteAllLines(path, lines);
                }
                catch (Exception e)
                {
                    throw new OMODFrameworkException($"Exception while trying to write all lines to file at '{path}'\n{e}");
                }
            }
        }

        public Form CreateCustomDialog()
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            return new Form();
        }

        public byte[] ReadDataFile(string file)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            CheckPluginDataSafety(file, false);
            return File.ReadAllBytes(Path.Combine(_dataFiles, file));
        }

        public byte[] ReadExistingDataFile(string file)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            CheckPathSafety(file);
            return _handler.ScriptFunctions.ReadExistingDataFile(file);
        }

        public byte[] GetDataFileFromBSA(string file)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            return GetFromBSA(null, file);
        }

        public byte[] GetDataFileFromBSA(string bsa, string file)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            return GetFromBSA(bsa, file);
        }

        private byte[] GetFromBSA(string bsa, string file)
        {
            if(!Framework.Settings.ScriptExecutionSettings.HandleBSAsWithInterface)
                return bsa == null ? BSAArchive.GetFileFromBSA(file) : BSAArchive.GetFileFromBSA(bsa, file);

            return bsa == null
                ? _handler.ScriptFunctions.GetDataFileFromBSA(file)
                : _handler.ScriptFunctions.GetDataFileFromBSA(bsa, file);
        }

        public void GenerateNewDataFile(string file, byte[] data)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            CheckPathSafety(file);
            var path = Path.Combine(_dataFiles, file);
            if (!File.Exists(path))
            {
                var ext = Path.GetExtension(path.ToLower());
                if(ext == ".esm" || ext == ".esp")
                    throw new ScriptingException("Copied data files cannot have a .esp or .esm file extension");
                _srd.CopyDataFiles.RemoveWhere(s => s.CopyTo == file.ToLower());
                _srd.CopyDataFiles.Add(new ScriptCopyDataFile(file, file));
            }

            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            try
            {
                File.WriteAllBytes(path, data);
            }
            catch (Exception e)
            {
                throw new OMODFrameworkException($"Could not write all bytes to file at '{path}'\n{e}");
            }
        }

        public void CancelDataFileCopy(string file)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            CheckPathSafety(file);
            _srd.CopyDataFiles.RemoveWhere(s => s.CopyTo == file.ToLower());
            //TODO: OBMM deleted the file at Path.Combine(_dataFiles, file); for some reason
        }

        public void CancelDataFolderCopy(string folder)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            CheckPathSafety(folder);
            _srd.CopyDataFiles.RemoveWhere(s => s.CopyTo.StartsWith(folder.ToLower()));
            //TODO: OBMM deleted the file at Path.Combine(_dataFiles, file); for some reason
        }

        public void GenerateBSA(string file, string path, string prefix, int cRatio, int cLevel)
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            throw new NotImplementedException();
        }

        public bool IsSimulation()
        {
            Utils.Script($"{MethodBase.GetCurrentMethod().Name} got called");
            return false;
        }
    }
}
