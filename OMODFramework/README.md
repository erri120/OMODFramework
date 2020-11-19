# OMODFramework

## Packaging

`OMODFramework` is an SDK-style `.csproj`, so the package description is entirely within `OMODFramework.csproj`.

To build a Nuget package, run
```powershell
msbuild .\OMODFramework.csproj /t:pack /p:Configuration:Release
```
