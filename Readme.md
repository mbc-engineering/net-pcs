# MBC PCS Library

## Documentation
- For [documentation of MBC.PCS.NET](NET/docs/index.md)
- For [documentation of MBC.Tc3.PCS](TwinCat\Mbc.Tc3.Pcs\Mbc_Tc3_Pcs\docs\Readme.md)

## How to build and run the project
### Requirements
- .Net Framework >= 4.7.1
- TwinCat 3

### Build Steps
Clone repository and build 
- `NET\Mbc.Pcs.Net.sln` with Visual Studio 2017 >= 15.7
- `TwinCat\Mbc.Tc3.Pcs.sln` with TwinCat 3.1

## How to create Nuget packages for a MBC library and push to Server

_First:_ In the Project to publish edit the CSPROJ file. Right click to the project edit xyz.csproj. Increment the assembly Version under `VersionPrefix`. The `VersionSuffix` is optional an can be used for preview packages. See [documentation](https://docs.microsoft.com/en-us/nuget/reference/package-versioning) for mor infos.
```xml
<VersionPrefix>1.2.1</VersionPrefix>
<VersionSuffix>beta2</VersionSuffix>
```

_Second:_ Run the [cake build](https://cakebuild.net/) Script `> .\build.ps1 -target NugetPublish`. This build all assemblies in `Release` configuration and push it to the `mbcpublic` feed defined `NET\NuGet.Config`. The feed path is: `https://mbeec.pkgs.visualstudio.com/_packaging/public/nuget/v3/index.json`

_Third:_ Create a git tag with the name `Mbc.Pcs.Net/vx.x.x.x`.

## How to create Nuget packages for a Mbc.Pcs.Net.TwinCat.Ads and push to Server

_First:_ 
- Copy the new `TwinCAT.Ads.dll` and `TwinCAT.Ads.xml` from `C:\TwinCAT\AdsApi\.NET\v4.0.30319` to the `Libs` folder.
- Update the `Build\Mbc.Pcs.Net.TwinCat.Ads.nuspec` file with the File Version of `TwinCAT.Ads.dll`

_Second:_ Run the [cake build](https://cakebuild.net/) Script `> .\build.ps1 -target NugetPush --nuspec="Build\Mbc.Pcs.Net.TwinCat.Ads.nuspec"`. This build the nuspec configuration to a package and push it to the `mbcpublic` feed defined `NET\NuGet.Config`.

## How to create Nuget packages for a Mbc.Pcs.Net.TwinCat.Ads and push to Server

_First:_ 
- Copy the new `Interop.TCEVENTLOGGERLib.dll` and `Interop.TcEventLogProxyLib.dll` to the `Libs` folder.
- Update the `Build\Mbc.Pcs.Net.TwinCat.EventLog.nuspec` file with the correct version.

_Second:_ Run the [cake build](https://cakebuild.net/) Script `> .\build.ps1 -target NugetPush --nuspec="Build\Mbc.Pcs.Net.TwinCat.EventLog.nuspec"`. This build the nuspec configuration to a package and push it to the `mbcpublic` feed defined `NET\NuGet.Config`.

## How to create Mbc_Tc3_Pcs TC3 library
_First:_ In the File `Mbc_Tc3_Pcs` project increment the Project Version number.

_Second:_ In the subnote of `Mbc_Tc3_Pcs` right click on the node `Mbc_Tc3_Pcs Project` and select Save as library and install. Save the generated library unter `TwinCat\Mbc.Tc3.Pcs\Library` with the following name `Mbc_Tc3_Pcs_vx.x.x.x.library` (replace x with the Project Version number).

_Third:_ Commit the created library to git and create a git tag with the name `Mbc_Tc3_Pcs/vx.x.x.x`.

# License
    Copyright (c) 2018 BY mbc engineering GmbH, CH-6015 Luzern
    Licensed under the Apache License, Version 2.0

[Read the full license](https://www.apache.org/licenses/LICENSE-2.0)