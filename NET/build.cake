#tool "nuget:?package=xunit.runner.console"
// Importand: Execute Set-ExecutionPolicy RemoteSigned and Set-ExecutionPolicy RemoteSigned -Scope Process as Administrator in x86 and x64 powershell!

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var testreportfolder = Argument("testreportfolder", "testresult");
var nuspecPath = Argument("nuspec", "");
var nugetOutputDirectory =  $"./{Argument("nugetoutputfolder", "nuget")}";

///////////////////////////////////////////////////////////////////////////////
// VARIABLES
///////////////////////////////////////////////////////////////////////////////
var nugetPushServerConfiguration = new NuGetPushSettings() 
{
    Source = "mbcpublic",    // Is defined in nuget.config!
    ApiKey = "VSTS"
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
                .UseToolVersion(MSBuildToolVersion.VS2017)
                .SetPlatformTarget(PlatformTarget.MSIL));   // MSIL = AnyCPU    
    }    
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    var testAssemblies = GetFiles($"./**/bin/{configuration}/**/*.test.dll");
    var xunitSettings = new XUnit2Settings {
        UseX86 = true,
        Parallelism = ParallelismOption.Assemblies,
        HtmlReport = true,
        JUnitReport = true,
        NoAppDomain = true,
        OutputDirectory = $"./{testreportfolder}",        
    };     
    
    XUnit2(testAssemblies, xunitSettings); 
});

Task("NugetPublish")
    .IsDependentOn("Test")
    .Does(() =>
{
    // Collect all nuget files
    var nugetPackages = GetFiles($"./**/bin/{configuration}/**/*.symbols.nupkg");

    foreach (var package in nugetPackages)
    {
        // Push the package
        try
        {
            // ToDo: check for already published!

            NuGetPush(package, nugetPushServerConfiguration);
        }
        catch (CakeException cex)
        {
            Information(cex);   // Should be somthing like: Response status code does not indicate success: 409 (Conflict - The feed already contains 'Mbc.Common.Interface 0.1.0'. (DevOps Activity ID: EC51694F-2AFF-4B2F-A98F-58FBC3C974FB)).
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
    // Collect all nuget files
    var nugetPackages = GetFiles($"{nugetOutputDirectory}/*.nupkg");

    foreach (var package in nugetPackages)
    {
        // Push the package
        try
        {
            // ToDo: check for already published!

            NuGetPush(package, nugetPushServerConfiguration);
        }
        catch (CakeException cex)
        {
            Information(cex);   // Should be somthing like: Response status code does not indicate success: 409 (Conflict - The feed already contains 'Mbc.Common.Interface 0.1.0'. (DevOps Activity ID: EC51694F-2AFF-4B2F-A98F-58FBC3C974FB)).
            Information($"Nuget package {package} perhaps is already published at {nugetPushServerConfiguration.Source}. It will not be try to publish it in this task!");
        }
    }      
});


Task("Default")
  .IsDependentOn("Build");

RunTarget(target);