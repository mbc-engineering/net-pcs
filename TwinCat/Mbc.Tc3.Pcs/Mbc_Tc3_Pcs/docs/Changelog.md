# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

The following types of changes exist:
- **Added** for new features.
- **Changed** for changes in existing functionality.
- **Deprecated** for soon-to-be removed features.
- **Removed** for now removed features.
- **Fixed** for any bug fixes.
- **Security** in case of vulnerabilities.

## [1.2.1.1] - 28.05.2021
### Added
- It is now possible to enable or disable commands with the `stHandshake.bEnabled`

## [1.2.0] - 01.06.2018
### Changed
- The `CommandBase` Public Method `Abort` is renamed to Protected Method `Cancelled`
- The `CommandBase` Method `Done` is renamed to `Finish`, to clarify the transient behavior of this method.
- The new `CommandBase` Method `Done` is called at the command state `E_CommandResultCode.Done`, to clarify the state behavior of this method.
- The `CommandBase` Method `CalculateProgress` is new executed every cycle in all states!
- PCS input and output data should not anymore defined in the `VAR_INPUT` and `VAR_OUTPUT` section of the function block. New use the variable attributes `{attribute 'PlcCommandInput'}` and `{attribute 'PlcCommandOutput'}` in the `VAR` section!

### Added
- Now it is possible to calculate custom States with `E_CommandResultCode` because it is not anymore strict. Eg.: `E_CommandResultCode.StartCustom + 1`
- more `CommandBase` documentation

## [1.1.0] - 25.04.2018
### Added
- New Constant in `E_CommandResultCode` which indicates the start of the user result codes
- Properties to set the progress and sub-task frok the Task-Method

## [1.0.0] - 18.04.2018
### Added
- Created PCS Library
- Added CommandBase class for default PCS communication with .NET over ADS
- Readme and this changelog documentation
