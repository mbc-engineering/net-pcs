# MBC PCS Library

## Documentation
- For [documentation of MBC.PCS.NET](NET/readme.md)
- For [documentation of MBC.Tc3.PCS](TwinCat\Mbc.Tc3.Pcs\Mbc_Tc3_Pcs\docs\Readme.md)

## Build
### Requirements
- .Net Framework >= 4.7.1
- Visual Studio 2022
- TwinCat 3.1

### Build Steps
Clone repository and build 
- `NET\Mbc.Pcs.Net.sln`
- `TwinCat\Mbc.Tc3.Pcs.sln` with TwinCat 3.1

### deployment

For Deployment of the .Net nuget packages there is a Cake Build script. 

```powershell
> cd NET
# Unit Tests
..\NET>  .\build.ps1 -t Test

# Publish nuget
..\NET>  .\build.ps1 --target=NugetPublish --apikey=[xxxxxxxx]
```



## How to create Nuget packages for a Mbc.Pcs.Net.TwinCat.EventLog and push to Server

_First:_ 
- Copy the new `Interop.TCEVENTLOGGERLib.dll` and `Interop.TcEventLogProxyLib.dll` to the `Libs` folder.
- Update the `Build\Mbc.Pcs.Net.TwinCat.EventLog.nuspec` file with the correct version.

_Second:_ Run the [cake build](https://cakebuild.net/) Script `> .\build.ps1 --target=NugetPush --nuspec="Build\Mbc.Pcs.Net.TwinCat.EventLog.nuspec" --apikey=[xxxxxxxx]`. This build the nuspec configuration to a package and push it to the `mbcpublic` feed defined `NET\NuGet.Config`.

## How to create Mbc_Tc3_Pcs TC3 library
_First:_ In the File `Mbc_Tc3_Pcs` project increment the Project Version number.

_Second:_ In the subnote of `Mbc_Tc3_Pcs` right click on the node `Mbc_Tc3_Pcs Project` and select Save as library and install. Save the generated library unter `TwinCat\Mbc.Tc3.Pcs\Library` with the following name `Mbc_Tc3_Pcs_vx.x.x.x.library` (replace x with the Project Version number).

_Third:_ Commit the created library to git and create a git tag with the name `Mbc_Tc3_Pcs/vx.x.x.x`.

## Contribute

Feel free to contribute! After review it will merged into de main branch.

Please write your changes into the [changelog](changelog.md).

## License
    Copyright (c) 2018 BY mbc engineering, CH-6015 Luzern
    Licensed under the Apache License, Version 2.0

[Read the full license](https://www.apache.org/licenses/LICENSE-2.0)