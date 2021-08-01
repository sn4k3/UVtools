# When using System.IO.Compression.ZipFile.CreateFromDirectory in PowerShell, it still uses backslashes in the zip paths
# despite this https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/mitigation-ziparchiveentry-fullname-path-separator

# Based upon post by Seth Jackson https://sethjackson.github.io/2016/12/17/path-separators/

#
# PowerShell 5 (WMF5) & 6
# Using class Keyword https://msdn.microsoft.com/powershell/reference/5.1/Microsoft.PowerShell.Core/about/about_Classes
#
# https://gist.github.com/lantrix/738ebfa616d5222a8b1db947793bc3fc
#

####################################
###        Fix Zip slash         ###
####################################
Add-Type -AssemblyName System.Text.Encoding
Add-Type -AssemblyName System.IO.Compression.FileSystem

class FixedEncoder : System.Text.UTF8Encoding {
    FixedEncoder() : base($true) { }

    [byte[]] GetBytes([string] $s)
    {
        $s = $s.Replace("\", "/");
        return ([System.Text.UTF8Encoding]$this).GetBytes($s);
    }
}

# Script working directory
Set-Location $PSScriptRoot\..

####################################
###         Configuration        ###
####################################
$enableMSI = $true
#$buildOnly = 'win-x64'
#$buildOnly = 'linux-x64'
$enableNugetPublish = $true
# Profilling
$stopWatch = New-Object -TypeName System.Diagnostics.Stopwatch 
$deployStopWatch = New-Object -TypeName System.Diagnostics.Stopwatch
$stopWatch.Start()


# Variables
$software = "UVtools"
$project = "UVtools.WPF"
$buildWith = "Release"
$netFolder = "net5.0"
$releaseFolder = "$project\bin\$buildWith\$netFolder"
$objFolder = "$project\obj\$buildWith\$netFolder"
$publishFolder = "publish"
$platformsFolder = "UVtools.Platforms"

# Not supported yet! No fuse on WSL
$appImageFile = 'appimagetool-x86_64.AppImage'
$appImageFilePath = "$platformsFolder/AppImage/$appImageFile"
$appImageUrl = "https://github.com/AppImage/AppImageKit/releases/download/continuous/$appImageFile"

$macIcns = "UVtools.CAD/UVtools.icns"

#$version = (Get-Command "$releaseFolder\UVtools.dll").FileVersionInfo.ProductVersion
$projectXml = [Xml] (Get-Content "$project\$project.csproj")
$version = "$($projectXml.Project.PropertyGroup.Version)".Trim();
if([string]::IsNullOrWhiteSpace($version)){
    Write-Error "Can not detect the UVtools version, does $project\$project.csproj exists?"
    exit
}

# MSI Variables
$installers = @("UVtools.InstallerMM", "UVtools.Installer")
$msiSourceFile = "UVtools.Installer\bin\Release\UVtools.msi"
$msbuild = "`"${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe`" /t:Build /p:Configuration=$buildWith /p:MSIProductVersion=$version"

Write-Output "
####################################
###  UVtools builder & deployer  ###
####################################
Version: $version [$buildWith]
"

####################################
###   Clean up previous publish  ###
####################################
# Clean up previous publish
Remove-Item $publishFolder -Recurse -ErrorAction Ignore # Clean

####################################
###    Self-contained runtimes   ###
####################################
$runtimes = 
@{
    "win-x64" = @{
        "extraCmd" = "-p:PublishReadyToRun=true"
        "exclude" = @("UVtools.sh")
        "include" = @()
    }
    "linux-x64" = @{
        "extraCmd" = "-p:PublishReadyToRun=true"
        "exclude" = @()
        "include" = @("libcvextern.so")
    }
    "arch-x64" = @{
        "extraCmd" = "-p:PublishReadyToRun=true"
        "exclude" = @()
        "include" = @("libcvextern.so")
    }
    "rhel-x64" = @{
        "extraCmd" = "-p:PublishReadyToRun=true"
        "exclude" = @()
        "include" = @("libcvextern.so")
    }
    #"linux-arm64" = @{
    #    "extraCmd" = "-p:PublishReadyToRun=true"
    #    "exclude" = @()
    #    "include" = @("libcvextern.so")
    #}
    #"unix-x64" = @{
    #    "extraCmd" = "-p:PublishReadyToRun=true"
    #    "exclude" = @("x86", "x64", "libcvextern.dylib")
    #}
    "osx-x64" = @{
        "extraCmd" = "-p:PublishReadyToRun=true"
        "exclude" = @()
        "include" = @("libcvextern.dylib")
    }
}


# https://github.com/AppImage/AppImageKit/wiki/Bundling-.NET-Core-apps
#Invoke-WebRequest -Uri $appImageUrl -OutFile $appImageFilePath
#wsl chmod a+x $appImageFilePath

if($null -ne $enableNugetPublish -and $enableNugetPublish)
{
    $nugetApiKeyFile = 'build/nuget_api.key'
    if (Test-Path -Path $nugetApiKeyFile -PathType Leaf)
    {
        Write-Output "Creating nuget package"
        dotnet pack UVtools.Core --configuration 'Release'

        $nupkg = "UVtools.Core/bin/Release/UVtools.Core.$version.nupkg"

        if (Test-Path -Path $nupkg -PathType Leaf){
            $nugetApiKeyFile = (Get-Content $nugetApiKeyFile)
            dotnet nuget push $nupkg --api-key $nugetApiKeyFile --source https://api.nuget.org/v3/index.json
            #Remove-Item $nupkg
        }else {
            Write-Error "Nuget package publish failed!"
            Write-Error "File '$nupkg' was not found"
            return
        }
    }
}

foreach ($obj in $runtimes.GetEnumerator()) {
    if(![string]::IsNullOrWhiteSpace($buildOnly) -and !$buildOnly.Equals($obj.Name)) {continue}
    # Configuration
    $deployStopWatch.Restart()
    $runtime = $obj.Name;       # runtime name
    $extraCmd = $obj.extraCmd;  # extra cmd to run with dotnet
    $targetZip = "$publishFolder/${software}_${runtime}_v$version.zip"  # Target zip filename
    
    # Deploy
    Write-Output "################################
Building: $runtime"
    dotnet publish $project -o "$publishFolder/$runtime" -c $buildWith -r $runtime $extraCmd
    if(!$runtime.Equals('win-x64'))
    {
        # Fix permissions
        wsl chmod +x "$publishFolder/$runtime/UVtools" `|`| :
        wsl chmod +x "$publishFolder/$runtime/UVtools.sh" `|`| :
    }
    
    # Cleanup
    Remove-Item "$releaseFolder\$runtime" -Recurse -ErrorAction Ignore
    Remove-Item "$objFolder\$runtime" -Recurse -ErrorAction Ignore
    Write-Output "$releaseFolder\$runtime"
    
    foreach ($excludeObj in $obj.Value.exclude) {
        Write-Output "Excluding: $excludeObj"
        Remove-Item "$publishFolder\$runtime\$excludeObj" -Recurse -ErrorAction Ignore
    }

    foreach ($includeObj in $obj.Value.include) {
        Write-Output "Including: $includeObj"
        Copy-Item "$platformsFolder\$runtime\$includeObj" -Destination "$publishFolder\$runtime"  -Recurse -ErrorAction Ignore
    }

    #if($runtime.Equals('linux-x64')){
    #    $appDirDest = "$publishFolder/AppImage.$runtime/AppDir"
    #    Copy-Item "$platformsFolder/AppImage/AppDir" $appDirDest -Force -Recurse
    #    wsl chmod 755 "$appDirDest/AppRun"
    #    wsl cp -a "$publishFolder/$runtime/." "$appDirDest/usr/bin"
    #    wsl $appImageFilePath $appDirDest
    #}
    if($runtime.Equals('osx-x64')){
        $macAppFolder = "${software}.app"
        $macPublishFolder = "$publishFolder/${macAppFolder}"
        $macInfoplist = "$platformsFolder/$runtime/Info.plist"
        $macOutputInfoplist = "$macPublishFolder/Contents"
        $macTargetZipLegacy = "$publishFolder/${software}_${runtime}-legacy_v$version.zip"

        New-Item -ItemType directory -Path "$macPublishFolder"
        New-Item -ItemType directory -Path "$macPublishFolder/Contents"
        New-Item -ItemType directory -Path "$macPublishFolder/Contents/MacOS"
        New-Item -ItemType directory -Path "$macPublishFolder/Contents/Resources"

        
        Copy-Item "$macIcns" -Destination "$macPublishFolder/Contents/Resources"
        ((Get-Content -Path "$macInfoplist") -replace '#VERSION',"$version") | Set-Content -Path "$macOutputInfoplist/Info.plist"
        wsl cp -a "$publishFolder/$runtime/." "$macPublishFolder/Contents/MacOS"

        wsl cd "$publishFolder/" `&`& pwd `&`& zip -r "../$targetZip" "$macAppFolder/*"
        #wsl cd "$publishFolder/$runtime" `&`& pwd `&`& zip -r "../../$macTargetZipLegacy" .
        
    }
    else {
        Write-Output "Compressing $runtime to: $targetZip"
        Write-Output $targetZip "$publishFolder/$runtime"
        wsl cd "$publishFolder/$runtime" `&`& pwd `&`& zip -r "../../$targetZip" .
    }

    # Zip
    #Write-Output "Compressing $runtime to: $targetZip"
    #Write-Output $targetZip "$publishFolder/$runtime"
    #[System.IO.Compression.ZipFile]::CreateFromDirectory("$publishFolder\$runtime", $targetZip, [System.IO.Compression.CompressionLevel]::Optimal, $false, [FixedEncoder]::new())
	#wsl cd "$publishFolder/$runtime" `&`& pwd `&`& chmod +x -f "./$software" `|`| : `&`& zip -r "../../$targetZip" "."
    $deployStopWatch.Stop()
    Write-Output "Took: $($deployStopWatch.Elapsed)
################################
"
}

# Universal package
<#
$deployStopWatch.Restart()
$runtime = "universal-x86-x64"
$targetZip = "$publishFolder\${software}_${runtime}_v$version.zip"

Write-Output "################################
Building: $runtime"
dotnet build $project -c $buildWith

Write-Output "Compressing $runtime to: $targetZip"
[System.IO.Compression.ZipFile]::CreateFromDirectory($releaseFolder, $targetZip, [System.IO.Compression.CompressionLevel]::Optimal, $false, [FixedEncoder]::new())
Write-Output "Took: $($deployStopWatch.Elapsed)
################################
"
$stopWatch.Stop()
#>

# MSI Installer for Windows
if($null -ne $enableMSI -and $enableMSI)
{
    $deployStopWatch.Restart()
    $runtime = 'win-x64'
    $msiTargetFile = "$publishFolder\${software}_${runtime}_v$version.msi"
    Write-Output "################################
    Building: $runtime MSI Installer"

    foreach($installer in $installers)
    {
        # Clean and build MSI
        Remove-Item "$installer\obj" -Recurse -ErrorAction Ignore
        Remove-Item "$installer\bin" -Recurse -ErrorAction Ignore
        Invoke-Expression "& $msbuild $installer\$installer.wixproj"
    }

    Write-Output "Coping $runtime MSI to: $msiTargetFile"
    Copy-Item $msiSourceFile $msiTargetFile

    Write-Output "Took: $($deployStopWatch.Elapsed)
    ################################
    "
}


Write-Output "
####################################
###           Completed          ###
####################################
In: $($stopWatch.Elapsed)"