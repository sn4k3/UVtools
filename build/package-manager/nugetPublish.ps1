Set-Location $PSScriptRoot\..\..
####################################
###         Configuration        ###
####################################
# Variables
$package = "UVtools.Core"
$nugetApiKeyFile = 'build/secret/nuget_api.key'
$githubApiKeyFile = 'build/secret/github_packages.key'
$outputFolder = "artifacts/package/release"

$projectXml = [Xml] (Get-Content "Directory.Build.props")
$version = "$($projectXml.Project.PropertyGroup.UVtoolsVersion)".Trim();
if([string]::IsNullOrWhiteSpace($version)){
    Write-Error "Can not detect the $package version, does $project\$project.csproj exists?"
    exit
}

if (Test-Path -Path $nugetApiKeyFile -PathType Leaf)
{
    Write-Output "Creating nuget package for $package $version"
    #Remove-Item "$outputFolder/*" -Recurse -Include *.nupkg
    dotnet pack $package --configuration 'Release'

    $nupkg = "$outputFolder/$package.$version.nupkg"

    if (Test-Path -Path $nupkg -PathType Leaf){
        $nugetApiKeyFile = (Get-Content $nugetApiKeyFile)
		$githubApiKeyFile = (Get-Content $githubApiKeyFile)
        dotnet nuget push $nupkg --api-key $nugetApiKeyFile --source https://api.nuget.org/v3/index.json --skip-duplicate
		dotnet nuget push $nupkg --api-key $githubApiKeyFile --source https://nuget.pkg.github.com/sn4k3/index.json --skip-duplicate
        #Remove-Item $nupkg
    }else {
        Write-Error "Nuget package publish failed!"
        Write-Error "File '$nupkg' was not found"
        return
    }
}
else{
    Write-Error "The nuget API key was not found. This is private and can only use used by own developer"
}