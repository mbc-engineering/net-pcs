#tool "nuget:?package=xunit.runner.console"
using System.Linq;
using System.String;
// Importand: Execute Set-ExecutionPolicy RemoteSigned and Set-ExecutionPolicy RemoteSigned -Scope Process as Administrator in x86 and x64 powershell!

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var testreportfolder = Argument("testreportfolder", "testresult").TrimEnd('/');
var nuspecPath = Argument("nuspec", "");
var nugetOutputDirectory =  $"./{Argument("nugetoutputfolder", "nuget")}";
var nugetapikey =  Argument("apikey", "apikeymissing");

///////////////////////////////////////////////////////////////////////////////
// VARIABLES
///////////////////////////////////////////////////////////////////////////////
var nugetPushServerConfiguration = new NuGetPushSettings() 
{
    Source = "nuget.org",    // Is defined in nuget.config!
    ApiKey = nugetapikey,
};

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////
Task("Build")
  .Does(() =>
{
    Information($"Clean Output Folders");
    var directoriesToClean = GetDirectories("./**/bin");
    CleanDirectories(directoriesToClean);

    var solutions  = GetFiles("./**/*.sln");
    foreach (var solution in solutions)
    {
        Information($"Build Solution: {solution}");
        MSBuild(solution, configurator =>
            configurator
                .SetConfiguration(configuration)                
                .WithRestore()           
                .SetVerbosity(Verbosity.Minimal)                
                .UseToolVersion(MSBuildToolVersion.VS2019)
                .SetPlatformTarget(PlatformTarget.MSIL));   // MSIL = AnyCPU    
    }    
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    var allTestAssemblies = GetFiles($"./**/bin/{configuration}/**/*.test.dll");

    var xunitSettings = new XUnit2Settings {
        UseX86 = false,
        Parallelism = ParallelismOption.None,
        HtmlReport = true,
        JUnitReport = true,
        NoAppDomain = true,
        OutputDirectory = $"./{testreportfolder}",        
    };     
    
    // Run Tests in x64 Process
    XUnit2(allTestAssemblies, xunitSettings); 

    // Run Tests in x86 Process
    xunitSettings.UseX86 = true;
    xunitSettings.OutputDirectory += "x86";
    XUnit2(allTestAssemblies, xunitSettings); 
});

Task("NugetPublish")
    //.IsDependentOn("Build")
    .IsDependentOn("Test")
    .Does(() =>
{
    Information($"publish nuget to {nugetPushServerConfiguration.Source} with api key {nugetPushServerConfiguration.ApiKey}");

    // Collect all nuget files
    // !!! NuGet will publish both packages to nuget.org. MyPackage.nupkg will be published first, followed by MyPackage.snupkg.
    var nugetPackages = GetFiles($"./**/bin/{configuration}/**/*.nupkg");

    foreach (var package in nugetPackages)
    {
        // Push the package
        try
        {
            NuGetPush(package, nugetPushServerConfiguration);
        }
        catch (CakeException cex)
        {
            Information(cex);   // Should be somthing like: Response status code does not indicate success: 409 (Conflict - The feed already contains 'Mbc.Pcs.Net.TwinCat.EventLog 1.0.2'.
            Information($"Nuget package {package} perhaps is already published at {nugetPushServerConfiguration.Source}. It will not be try to publish it in this task!");

        }
    }      
});

// Creates a nuget from nuspec file
Task("NugetCreate")
    .Does(() =>
{   
    Information($"Clean Output Folder {nugetOutputDirectory}");
    CleanDirectory(nugetOutputDirectory);

    Information($"Create nuget from nuspec: {nuspecPath}");
    var packSettings = new NuGetPackSettings()
    {
        OutputDirectory = nugetOutputDirectory,
    };
    NuGetPack(nuspecPath, packSettings);
});

// Create and Push from a nuspec file
Task("NugetPush")
    .IsDependentOn("NugetCreate")
    .Does(() =>
{    
    Information($"publish nuget to {nugetPushServerConfiguration.Source} with api key {nugetPushServerConfiguration.ApiKey}");

    // Collect all nuget files
    var nugetPackages = GetFiles($"{nugetOutputDirectory}/*.nupkg");

    foreach (var package in nugetPackages)
    {
        // Push the package
        try
        {
            NuGetPush(package, nugetPushServerConfiguration);
        }
        catch (CakeException cex)
        {
            Information(cex);   // Should be somthing like: Response status code does not indicate success: 409 (Conflict - The feed already contains 'Mbc.Pcs.Net.TwinCat.EventLog 1.0.2'.
            Information($"Nuget package {package} perhaps is already published at {nugetPushServerConfiguration.Source}. It will not be try to publish it in this task!");
        }
    }      
});


Task("Default")    
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("NugetPublish");

RunTarget(target);