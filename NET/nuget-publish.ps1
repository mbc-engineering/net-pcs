Param()

<#
Script to help update the PCS Libraries on the Private Nuget stream https://mbeec.pkgs.visualstudio.com/_packaging/public/nuget/v3/index.json

Require dotnet CLI tools >= 2 or >= VS 2017 15.3

With the following command add credentials to a nuget.config (used the downloaded nuget version with credentials from vsts):
.\Nuget\NuGet.exe sources add -name mbcpublic -source https://mbeec.pkgs.visualstudio.com/_packaging/public/nuget/v3/index.json -username vsts-pat -password MyVstsPat -configfile .\NuGet.Config
#>

function Main{
    Write-Host("require minimal 2.0.0 tooling / VS 2017 15.3+")

    # Cleanup old dll and nupkg files from output
    dotnet clean -c Release
    Remove-Item .\Mbc.Pcs.Net\bin\Release\*.nupkg
    Remove-Item .\Mbc.Pcs.Net.Test.Util\bin\Release\*.nupkg

    # Build libraries
    dotnet pack -c Release --include-source --include-symbols .\Mbc.Pcs.Net\Mbc.Pcs.Net.csproj
    dotnet pack -c Release --include-source --include-symbols .\Mbc.Pcs.Net.Test.Util\Mbc.Pcs.Net.Test.Util.csproj
    
    # Push the Packages    
    dotnet nuget push --source mbcpublic --api-key VSTS .\Mbc.Pcs.Net\bin\Release\*.nupkg    
    dotnet nuget push --source mbcpublic --api-key VSTS .\Mbc.Pcs.Net.Test.Util\bin\Release\*.nupkg
}


function CreateTwinCatAds{
    Write-Host("require minimal 2.0.0 tooling / VS 2017 15.3+")

    dotnet clean -c Release

    # See nuspec refrence: https://docs.microsoft.com/en-us/nuget/reference/nuspec
	# Install nuget.exe with: choco install nuget.commandline or download from vsts server with credentials provider
    # Build TwinCat Library
    NuGet.exe pack .\Mbc.Pcs.Net.TwinCat.Ads.nuspec -Version 4.2.163.1 
    dotnet nuget push --source mbcpublic --api-key VSTS .\Mbc.Pcs.Net.TwinCat.Ads.4.2.163.1.nupkg 
}

# Execute the program
Main
