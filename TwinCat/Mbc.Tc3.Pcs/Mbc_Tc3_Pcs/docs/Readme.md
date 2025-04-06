# Documentation of mbc engineering PCS Library

## Getting Started

### About

This Library will help or customer and the mbc team to interact with the Process Control System (PCS) of mbc engineering. There are some default exchange logic and structures for reuse. This Library will help you on PLC side on some point. Example on handling PCS commands.

For code changes history looking at [changelog](Changelog.md).

### Install from Library file

The library must be installed locally. The first time or on a new computer it must be installed the correct library version. For mor details about library see at [infosys - Using libraries](https://infosys.beckhoff.com/english.php?content=../content/1033/tc3_plc_intro/9007203443879435.html&id=8387830030110329229). 

For Installation go to *PLC -> Library Repository* and then press the *Install* button. Choose the Library file to install. In this case choose the file `Mbc_Tc3_Pcs_v1.0.0.0.library`. TwinCat3 will it install on the default behavior to the `system` repository into the folder `C:\TwinCAT\3.1\Components\Plc\Managed Libraries\mbc engineering \MBC TC3 PCS Library\1.0.0.0`. 

> More infos at [infosys - Library installation](https://infosys.beckhoff.com/english.php?content=../content/1033/tc3_plc_intro/9007203473041419.html&id=3025451336790505210)

### Install from a existing PLC Project

If you move a existing PLC Project that use this Library to another computer, it is possible to install it locally in TwinCat 3. This because TwinCat 3 saves all used Library in the PLC project in the project folder under `..\TwinCAT PLC1\Untitled1\_Libraries`

For local installation go to *TwinCAT PLC1 - Untitled1 -> right click -> Install Project Libraries*

### Add the Library Reference to the PLC Project

When the library is installed, it is possible add the Library reference in the PLC project. go to *TwinCAT PLC1 - Untitled1 - Untitled1 Project - References -> right click -> Add Library*. In the list search for *MBC TC3 PCS Library* following press *OK*. Now the library can be used in the Project.
![AddLibrary.JPG image](res/AddLibrary.JPG)

> We recommend to use placeholder reference with the *always newest version* set.

> More infos at [infosys - Library Manager](https://infosys.beckhoff.com/english.php?content=../content/1033/tc3_plc_intro/9007203444454795.html&id=6922828777390816982)

## In a Nutshell

- [CommandBase](CommandBase.md)

# License
    Copyright (c) 2018 BY mbc engineering, CH-6015 Luzern
    Licensed under the Apache License, Version 2.0

[Read the full license](https://www.apache.org/licenses/LICENSE-2.0)