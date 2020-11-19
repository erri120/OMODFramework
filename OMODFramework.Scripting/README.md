# OMODFramework.Scripting

## Packaging

`OMODFramework.Scripting` is an old-style `.csproj`, so the package description is split between `Properties/AssemblyInfo.cs` and `OMODFramework.Scripting.nuspec`.

To build a Nuget package, run
```powershell
nuget pack -Properties Configuration=Release -Symbols -SymbolPackageFormat snupkg
```
It is safe to ignore warning `NU5128` as it is a false-positive caused by https://github.com/NuGet/Home/issues/8713.
