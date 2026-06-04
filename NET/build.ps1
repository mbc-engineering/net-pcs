##########################################################################
# Cake bootstrapper (modern, dotnet local tool based).
#
# Restores Cake via the .NET local tool manifest (.config/dotnet-tools.json)
# and executes the build script.
##########################################################################

<#
.SYNOPSIS
    Bootstraps and runs the Cake build.

.PARAMETER Script
    The build script to execute. Defaults to build.cake.

.PARAMETER Target
    The Cake target to run (e.g. Build, Test, NugetPublish).

.PARAMETER Configuration
    Build configuration (Debug/Release).

.PARAMETER Verbosity
    Cake verbosity (Quiet|Minimal|Normal|Verbose|Diagnostic).

.PARAMETER ShowDescription
    Show descriptions of tasks instead of running them.

.PARAMETER DryRun
    Performs a dry run.

.PARAMETER ScriptArgs
    Additional arguments forwarded to Cake (e.g. --x86=false --apikey=xyz).
#>
[CmdletBinding()]
Param(
    [string]$Script = "build.cake",
    [string]$Target,
    [string]$Configuration,
    [ValidateSet("Quiet", "Minimal", "Normal", "Verbose", "Diagnostic")]
    [string]$Verbosity,
    [switch]$ShowDescription,
    [Alias("WhatIf", "Noop")]
    [switch]$DryRun,
    [Parameter(Position = 0, Mandatory = $false, ValueFromRemainingArguments = $true)]
    [string[]]$ScriptArgs
)

$ErrorActionPreference = "Stop"

if (-not $PSScriptRoot) {
    $PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent
}

Push-Location $PSScriptRoot
try {
    # Ensure dotnet is available
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnet) {
        throw "The .NET SDK ('dotnet') is required but was not found in PATH."
    }

    Write-Host "Restoring .NET local tools (Cake)..."
    & dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to restore .NET local tools."
    }

    # Build Cake arguments
    $cakeArgs = @($Script)
    if ($Target)          { $cakeArgs += "--target=$Target" }
    if ($Configuration)   { $cakeArgs += "--configuration=$Configuration" }
    if ($Verbosity)       { $cakeArgs += "--verbosity=$Verbosity" }
    if ($ShowDescription) { $cakeArgs += "--showdescription" }
    if ($DryRun)          { $cakeArgs += "--dryrun" }
    if ($ScriptArgs)      { $cakeArgs += $ScriptArgs }

    Write-Host "Running build script: dotnet cake $($cakeArgs -join ' ')"
    & dotnet cake @cakeArgs
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
