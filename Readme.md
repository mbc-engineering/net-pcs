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

## How to create Nuget packages for MBC.PCS.NET and push to Server

_First:_ In the File `NET\common.props` increment the assembly Version under `VersionPrefix`. The `VersionSuffix` is optional an can be used for preview packages.
```xml
<VersionPrefix>1.2.1</VersionPrefix>
<VersionSuffix>beta2</VersionSuffix>
```

_Second:_ Run the Script `NET\nuget-publish.ps1`. This build all assemblies and push it to the `mbcpublic` feed defined `NET\NuGet.Config`. The feed path is: `https://mbeec.pkgs.visualstudio.com/_packaging/public/nuget/v3/index.json`

## How to create Mbc_Tc3_Pcs  TC3 library
_First:_ In the File `Mbc_Tc3_Pcs` project increment the Project Version number.

_Second:_ In the subnote of `Mbc_Tc3_Pcs` right click on the node `Mbc_Tc3_Pcs Project` and select Save as library and install. Save the generated library unter `TwinCat\Mbc.Tc3.Pcs\Library` with the following name `Mbc_Tc3_Pcs_vx.x.x.x.library` (replace x with the Project Version number).

_Third:_ Commit the created library to git 

# License
    Copyright (c) 2018 BY mbc engineering GmbH, CH-6015 Luzern
    Licensed under the Apache License, Version 2.0

[Read the full license](https://www.apache.org/licenses/LICENSE-2.0)