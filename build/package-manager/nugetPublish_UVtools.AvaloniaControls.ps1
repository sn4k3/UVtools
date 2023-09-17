Set-Location $PSScriptRoot\..\..
####################################
###         Configuration        ###
####################################
# Variables
$package = "UVtools.AvaloniaControls"
$nugetApiKeyFile = 'build/secret/nuget_api.key'
$outputFolder = "$package/bin/Release"

$projectXml = [Xml] (Get-Content "$package\$package.csproj")
$version = "$($projectXml.Project.PropertyGroup.Version)".Trim();
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
        dotnet nuget push $nupkg --api-key $nugetApiKeyFile --source https://api.nuget.org/v3/index.json --skip-duplicate
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