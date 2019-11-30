# OMODFramework

[![Build Status](https://dev.azure.com/erri120/OMODFramework/_apis/build/status/erri120.OMODFramework?branchName=master)](https://dev.azure.com/erri120/OMODFramework/_build/latest?definitionId=3&branchName=master)
![Nuget](https://img.shields.io/nuget/v/OMODFramework)
![Discord](https://img.shields.io/discord/648941417783361571?logo=discord)

This project is the continuation and overhaul of my previous `OMOD-Framework`. Aside from the fact that I remove the `-` from the name, this project will be more refined than the last one. I've implemented more features from the [Oblivion Mod Manager](https://www.nexusmods.com/oblivion/mods/2097) and finally use continuous integration with Azure DevOps to build, test and release this project.

## Features

- Extraction
- Creation
- Script Execution

## OMOD

`.omod` files are used exclusively by the [Oblivion Mod Manager](https://www.nexusmods.com/oblivion/mods/2097) aka `OBMM`. This was fine 11 years ago. Today the Oblivion modding community still stands strong and continues to mod their favorite game. There are sadly some huge and essential mods still in the OMOD format. [Mod Organizer 2](https://github.com/Modorganizer2/modorganizer) has [recently](https://github.com/ModOrganizer2/modorganizer/releases/tag/v2.2.0) added more support for [running Oblivion OBSE with MO2](https://github.com/ModOrganizer2/modorganizer/wiki/Running-Oblivion-OBSE-with-MO2) and made me wanna mod Oblivion again, only to find out that you still need OBMM for some stuff.

The source code for the original OBMM, written in .NET 2 ... yes _.NET 2_, was made available in 2010 under the _GPLv2_ license.

This Framework uses a lot of the original algorithms for extraction, compression and of course all functions needed for script executing.

## Download

This Framework is available on [NuGet](https://www.nuget.org/packages/OMODFramework/), [GitHub Packages](https://github.com/erri120/OMODFramework/packages/63159) and [GitHub Release](https://github.com/erri120/OMODFramework/releases).

Be sure to check the dependencies for the current version on [NuGet](https://www.nuget.org/packages/OMODFramework/) **befor** installing.

## Usage

### Extraction

```cSharp
var omod = new OMOD(path);

// returns the absolute path to the folder containing the data/plugin files
var data = omod.GetDataFiles();
var plugins = omod.GetPlugins();
```

### Creation

```cSharp
var ops = new OMODCreationOptions
{
    Name = "", //required
    Author = "", //required
    Email = "",
    Website = "",
    Description = "", //required
    Image = "",
    MajorVersion = 0, //required
    MinorVersion = 0, //required
    BuildVersion = 0, //required
    CompressionType = CompressionType.SevenZip, //required
    DataFileCompressionLevel = CompressionLevel.Medium, //required
    OMODCompressionLevel = CompressionLevel.Medium, //required
    ESPs = new List<string>(),
    ESPPaths = new List<string>(),
    DataFiles = new List<string>(), //required
    DataFilePaths = new List<string>(), //required
    Readme = "",
    Script = ""
};

OMOD.CreateOMOD(ops, "test.omod");
```

### Script Execution

```cSharp
var omod = new OMOD(path);

var scriptFunctions = new ScriptFunctions(); //custom class that inherits IScriptFunctions

var srd = omod.RunScript(scriptFunctions);
```

## License

```text
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
```
