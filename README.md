# OMODFramework

[![Nuget](https://img.shields.io/nuget/v/OMODFramework)](https://www.nuget.org/packages/OMODFramework/)
[![CI](https://github.com/erri120/OMODFramework/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/erri120/OMODFramework/actions/workflows/ci.yml)

The [Oblivion Mod Manager](https://www.nexusmods.com/oblivion/mods/2097) by Timeslip was a utility tool for managing Oblivion mods and was the most prominent mod manager during it's time. One of the crazy features it has was `.omod` files and the multiple different ways of creating installation scripts for them. 10 years later and you still needed to use parts of the tool in order to install OMODs. This library solves the issue and pain involved when dealing with OMODs and is written in modern C# code targeting .NET Standard 2.1 and .NET 5.

## Features

- File Extraction
- OMOD Creation (please don't create new OMODs, only use this for testing)
- Script Execution (only OBMM and inlined C# scripts)

## Usage

### Extraction

```c#
//OMOD implements IDisposable
using var omod = new OMOD("path-to-file.omod");

/*
 * Use on of the different extraction methods:
 *  - ExtractFiles
 *  - ExtractFilesParallel (only for data files)
*/

omod.ExtractFiles(true, "output\\data");

//not every OMOD has plugin files so make sure to check before extracting
if (omod.HasEntryFile(OMODEntryFileType.PluginsCRC))
    omod.ExtractFiles(false, "output\\plugins");
```

### Script Execution

Script Execution is very complex compared to simple extraction. You need the `OMODFramework.Scripting` library for this and have to use the `OMODScriptRunner` class. The important thing to understand is that there are multiple different types of scripts:

- OBMM (custom scripting language of the Oblivion Mod Manager, see [this](http://timeslip.chorrol.com/obmmm/functionlist.htm) for more infos)
- C#
- Python (using IronPython)
- Visual Basic

The OMODFramework only supports OBMM scripts and inlined C# scripts. OBMM scripts are the most common with probably 95% of all scripts being in this language while the rest are C# scripts. I have yet to find any script in Python and Visual Basic. The OMODFramework only supports running inlined C# scripts, as opposed to the Oblivion Mod Manager which compiled the scripts and then used DI to make it run, because script compilation changed in newer .NET versions and also pose a huge security risk. There are also not many C# scripts out there and I included the biggset scripts:

- [DarkUId DarN 16 OMOD Version](https://www.nexusmods.com/oblivion/mods/11280)
- [DarNified UI 1.3.2](https://www.nexusmods.com/oblivion/mods/10763)
- [Horse Armor Revamped 1.8](https://www.nexusmods.com/oblivion/mods/46657)

If you found another C# script, create a new issue and I will look into inlining it as well. Now onto the actual script execution:

```c#
using var omod = new OMOD("path-to-file.omod");

IExternalScriptFunctions scriptFunctions = new MyExternalScriptFunctions();
var settings = new OMODScriptSettings(scriptFunctions);

var srd = OMODScriptRunner.RunScript(omod, settings);
```

You have to create a new class that implements `IExternalScriptFunctions` before you want to execute any script. This interface provides functions that can be called during script execution which this library can not do alone. You do not have to implement every function as some are never called depending on your settings (make sure to adjust `OMODScriptSettings`) but most of them like `Select`, `Message` or the `Display*` functions are very common in OBMM scripts.

`OMODScriptRunner.RunScript` will run the script and return `ScriptReturnData` which contains everything you need to do after script execution in order to install the mod. This library provides additional utility functions like `ScriptReturnData.CopyAllDataFiles` or `ExecuteEdit` functions for edits that need to be done after script execution.
