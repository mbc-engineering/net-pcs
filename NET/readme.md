# Documentation of mbc engineering PCS Library

## Prolog

This Library will help to integrate with the Process Control System (PCS) on a .Net platform with TwinCat 3.1 over a ADS router. There are some default exchange logic and structures for reuse. This Library will help you on PLC side on some point. Example on handling PCS commands.

## Packages

### Mbc.Pcs.Net

Main library, connection to TwincCat PLC over the Beckhoff.TwinCat.Ads lib. Also a plc structure reader eg. for a state that should be present on .net as typed class. 

### Mbc.Ads.Mapper

Helps to map a structure in the plc to a typed interface .net

### Mbc.Pcs.Net.Command

Allow to implement a command patter combined with the TwinCat 3 library descriped under [MBC.Tc3.PCS](TwinCat\Mbc.Tc3.Pcs\Mbc_Tc3_Pcs\docs\Readme.md)

### Mbc.Pcs.Net.Alarm.Service

> Required packages: Mbc.Pcs.Net.Alarm

This package helps to receive event messages from the old Beckhoff TwinCat EventLogger (Located under `C:\TwinCAT\3.1\Components\TcEventLogger`). The Mediator is running as own process, because of correct fail handling in a hosted process.

### Mbc.Pcs.Net.Test.Util

### Mbc.Ads.Utils

## License

    Copyright (c) 2018 BY mbc engineering, CH-6015 Luzern
    Licensed under the Apache License, Version 2.0

[Read the full license](https://www.apache.org/licenses/LICENSE-2.0)

