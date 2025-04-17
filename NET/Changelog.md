# Changelog
All notable changes to the .NET part of this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

The following types of changes exist:
- **Added** for new features.
- **Changed** for changes in existing functionality.
- **Deprecated** for soon-to-be removed features.
- **Removed** for now removed features.
- **Fixed** for any bug fixes.
- **Security** in case of vulnerabilities.  

## 2025 Spring, Version 5.0.0
- See PR #8
- All packages are now on Version 5.0.0

### Changed
- The interface `IServiceStartable` is now in the library `Mbc.Pcs.Net`.
- Removed NuGet dependency: `Mbc.Common`, `Mbc.Common.Interface`, `Mbc.AsyncUtils`. The required classes are now part of the library.
- Updated `Beckhoff.TwinCAT.Ads` to 6.1.332.
- Removed NLog dependency in some places and used ILogger.
- Removed EnsureThat in all libraries except `Mbc.Ads.Mapper`.
- Separated HDF5 `Mbc.Pcs.Net.DataRecorder` into its own library `Mbc.Pcs.Net.DataRecorder` to reduce dependency.
- Refactored PlcAdsStateReader.
- Set connection to null after notifications.
- Added logging to AdsClient. Provided logging in case of missing samples.
- Rewrote heartbeat for better diagnosis and fewer false alarms.
- MaxDelay factor for notifications for PlcAdsStateReader.
- PlcAdsConnectionProvider checks the target device state on StateChange explicitly and does not use only quality to the router.

### Security
- Updated dependency and removed vulnerability.

### Added
- Support for .NET 8.0.
- Added support for TwinCAT DataTypeCategory Array and Alias of Primitive types in the CommandInputBuilder and CommandOutputBuilder and extended the default used PrimitiveCommandArgumentHandler in PlcCommand.
- Support for conversion of `TwinCAT.PlcOpen.TIME` and `TwinCAT.PlcOpen.DATE` to corresponding .NET types in PlcCommand and AdsMapper.

#### Fixed
- Fixed some bugs 🐞.
- Fixed some unit tests 🦔.

## 2021 Fall
### Changed
- Some refactorings and optimizations in the library.

## 2018-2021
### Changed
- A living library.

## 18.04.2018
### Added
- Created PCS library.
- Added CommandBase class for default PCS communication with .NET over ADS.
- Readme and this changelog documentation.
