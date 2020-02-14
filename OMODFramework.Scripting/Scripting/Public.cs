/*
    Copyright (C) 2019-2020  erri120

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
using OMODFramework;

namespace OblivionModManager.Scripting
{
    public class ScriptingException : ApplicationException {
        internal ScriptingException(string msg) : base(msg) { }
        internal ScriptingException(string msg, Exception inner) : base(msg, inner) { }
    }

    public class ExecutionCancelledException : ApplicationException { }

    public interface IScript {
        void Execute(IScriptFunctions sf);
    }

    public interface IScriptFunctions {
        bool GetDisplayWarnings();

        bool DialogYesNo(string msg);
        bool DialogYesNo(string msg, string title);
        bool DataFileExists(string path);
        Version GetOBMMVersion();
        Version GetOBSEVersion();
        Version GetOBGEVersion();
        Version GetOblivionVersion();
        Version GetOBSEPluginVersion(string plugin);

        string[] GetPlugins(string path, string pattern, bool recurse);
        string[] GetDataFiles(string path, string pattern, bool recurse);
        string[] GetPluginFolders(string path, string pattern, bool recurse);
        string[] GetDataFolders(string path, string pattern, bool recurse);

        string[] GetActiveEspNames();
        string[] GetExistingEspNames();
        string[] GetActiveOmodNames();

        string[] Select(string[] items, string[] previews, string[] descs, string title, bool many);

        void Message(string msg);
        void Message(string msg, string title);
        void DisplayImage(string path);
        void DisplayImage(string path, string title);
        void DisplayText(string path);
        void DisplayText(string path, string title);

        void LoadEarly(string plugin);
        void LoadBefore(string plugin1, string plugin2);
        void LoadAfter(string plugin1, string plugin2);
        void SetNewLoadOrder(string[] plugins);

        void UncheckEsp(string plugin);
        void SetDeactivationWarning(string plugin, DeactivationStatus warning);

        void ConflictsWith(string filename);
        void ConslictsWith(string filename, string comment);
        void ConflictsWith(string filename, string comment, ConflictLevel level);
        void ConflictsWith(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion);
        void ConflictsWith(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment);
        void ConflictsWith(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment, ConflictLevel level);
        void ConflictsWith(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment, ConflictLevel level, bool regex);
        void DependsOn(string filename);
        void DependsOn(string filename, string comment);
        void DependsOn(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion);
        void DependsOn(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment);
        void DependsOn(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment, bool regex);

        void DontInstallAnyPlugins();
        void DontInstallAnyDataFiles();
        void InstallAllPlugins();
        void InstallAllDataFiles();

        void DontInstallPlugin(string name);
        void DontInstallDataFile(string name);
        void DontInstallDataFolder(string folder, bool recurse);
        void InstallPlugin(string name);
        void InstallDataFile(string name);
        void InstallDataFolder(string folder, bool recurse);

        void CopyPlugin(string from, string to);
        void CopyDataFile(string from, string to);
        void CopyDataFolder(string from, string to, bool recurse);

        void PatchPlugin(string from, string to, bool create);
        void PatchDataFile(string from, string to, bool create);

        void RegisterBSA(string path);
        void UnregisterBSA(string path);

        void EditINI(string section, string key, string value);
        void EditShader(byte package, string name, string path);

        void FatalError();

        void SetGMST(string file, string edid, string value);
        void SetGlobal(string file, string edid, string value);

        void SetPluginByte(string file, long offset, byte value);
        void SetPluginShort(string file, long offset, short value);
        void SetPluginInt(string file, long offset, int value);
        void SetPluginLong(string file, long offset, long value);
        void SetPluginFloat(string file, long offset, float value);

        string InputString();
        string InputString(string title);
        string InputString(string title, string initial);

        string ReadINI(string section, string value);
        string ReadRendererInfo(string value);

        void EditXMLLine(string file, int line, string value);
        void EditXMLReplace(string file, string find, string replace);

        System.Windows.Forms.Form CreateCustomDialog();
        
        byte[] ReadDataFile(string file);
        byte[] ReadExistingDataFile(string file);
        byte[] GetDataFileFromBSA(string file);
        byte[] GetDataFileFromBSA(string bsa, string file);

        void GenerateNewDataFile(string file, byte[] data);
        void CancelDataFileCopy(string file);
        void CancelDataFolderCopy(string folder);
        void GenerateBSA(string file, string path, string prefix, int cRatio, int cLevel);

        bool IsSimulation();
    }
}
