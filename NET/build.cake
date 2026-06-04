///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////
var target              = Argument("target", "Default");
var configuration       = Argument("configuration", "Release");
var testReportFolder    = Argument("testreportfolder", "testresult").TrimEnd('/');
var nuspecPath          = Argument("nuspec", "");
var nugetOutputDirectory = $"./{Argument("nugetoutputfolder", "nuget")}";
var nugetApiKey         = Argument("apikey", "apikeymissing");
var runX86Tests         = Argument("x86", false);   // pass --x86=true to enable x86 test run

///////////////////////////////////////////////////////////////////////////////
// VARIABLES
///////////////////////////////////////////////////////////////////////////////
var solutionFile = "./Mbc.Pcs.Net.slnx";

var nugetPushServerConfiguration = new DotNetNuGetPushSettings
{
    Source = "nuget.org",       // defined in nuget.config
    ApiKey = nugetApiKey,
    SkipDuplicate = true,       // tolerate already published packages
};

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////
Task("Clean")
    .Does(() =>
{
    Information("Clean Output Folders");
    CleanDirectories("./**/bin");
    CleanDirectories("./**/obj");
});

Task("Restore")
    .Does(() =>
{
    Information($"Restore Solution: {solutionFile}");
    DotNetRestore(solutionFile);
});

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() =>
{
    Information($"Build Solution: {solutionFile}");
    DotNetBuild(solutionFile, new DotNetBuildSettings
    {
        Configuration = configuration,
        NoRestore = true,
        Verbosity = DotNetVerbosity.Minimal,
        MSBuildSettings = new DotNetMSBuildSettings()
            .SetContinuousIntegrationBuild(true),
    });
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    // xunit v3 test projects (OutputType=Exe) are executed via `dotnet test`.
    // Multi-target test projects (net8.0;net10.0) are run for every TFM automatically.

    // Default run: host architecture (x64 on Windows runners / dev machines).
    // No --arch / no RID is passed, so the existing RID-less restore/build is reused.
    RunDotNetTest(runtimeIdentifier: null, reportSubFolder: "");

    if (runX86Tests)
    {
        // x86 needs a RID-specific restore + build because dotnet test --arch x86
        // implies a win-x86 RuntimeIdentifier, which requires assets for that RID.
        RunDotNetTest(runtimeIdentifier: "win-x86", reportSubFolder: "x86");
    }
    else
    {
        Information("Skipping x86 test run (pass --x86=true to enable).");
    }
});

void RunDotNetTest(string runtimeIdentifier, string reportSubFolder)
{
    var resultsDir = MakeAbsolute(Directory($"./{testReportFolder}{reportSubFolder}"));
    EnsureDirectoryExists(resultsDir);

    var archLabel = runtimeIdentifier ?? "host (x64)";
    Information($"Run tests ({archLabel}) -> {resultsDir}");

    if (runtimeIdentifier != null)
    {
        // RID-specific restore so project.assets.json contains the targeted RID.
        DotNetRestore(solutionFile, new DotNetRestoreSettings
        {
            Runtime = runtimeIdentifier,
        });
    }

    DotNetTest(solutionFile, new DotNetTestSettings
    {
        Configuration = configuration,
        NoBuild = runtimeIdentifier == null,    // reuse RID-less build for default run
        NoRestore = true,
        Runtime = runtimeIdentifier,            // null => no RID (host arch)
        ResultsDirectory = resultsDir,
        Loggers = new[]
        {
            "trx",
            "console;verbosity=normal",
        },
        ArgumentCustomization = args => args
            .Append("--blame-hang")
            .Append("--blame-hang-timeout 5m"),
    });
}

// Pushes all .nupkg files produced by the regular build (GeneratePackageOnBuild=true).
Task("NugetPublish")
    .IsDependentOn("Test")
    .Does(() =>
{
    Information($"Publish nuget packages to {nugetPushServerConfiguration.Source}");

    var packages = GetFiles($"./**/bin/{configuration}/**/*.nupkg");

    foreach (var package in packages)
    {
        try
        {
            DotNetNuGetPush(package.FullPath, nugetPushServerConfiguration);
        }
        catch (CakeException cex)
        {
            // e.g. 409 Conflict if version already exists on the feed
            Information(cex);
            Information($"Nuget package {package} could not be pushed (possibly already published). Continuing.");
        }
    }
});

// Creates a nuget package from a nuspec file (legacy path for nuspec-based packages,
// e.g. Mbc.Pcs.Net.TwinCat.EventLog.nuspec).
Task("NugetCreate")
    .Does(() =>
{
    if (string.IsNullOrWhiteSpace(nuspecPath))
    {
        throw new CakeException("Argument --nuspec=<path> is required for NugetCreate.");
    }

    Information($"Clean Output Folder {nugetOutputDirectory}");
    CleanDirectory(nugetOutputDirectory);

    Information($"Create nuget from nuspec: {nuspecPath}");
    NuGetPack(nuspecPath, new NuGetPackSettings
    {
        OutputDirectory = nugetOutputDirectory,
    });
});

// Create and push packages from a nuspec file
Task("NugetPush")
    .IsDependentOn("NugetCreate")
    .Does(() =>
{
    Information($"Publish nuget packages to {nugetPushServerConfiguration.Source}");

    var packages = GetFiles($"{nugetOutputDirectory}/*.nupkg");

    foreach (var package in packages)
    {
        try
        {
            DotNetNuGetPush(package.FullPath, nugetPushServerConfiguration);
        }
        catch (CakeException cex)
        {
            Information(cex);
            Information($"Nuget package {package} could not be pushed (possibly already published). Continuing.");
        }
    }
});

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("NugetPublish");

RunTarget(target);
