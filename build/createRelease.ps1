# Master release
# Creates and assembles relases for all supported operative systems
# Run under Windows with WSL installed

if($PSVersionTable.PSVersion.Major -lt 7){
    Write-Error("Powershell version $($PSVersionTable.PSVersion) is not compatible with this build script.`n
You need at least the Powershell 7: https://github.com/PowerShell/PowerShell/releases/latest")
    return;
}

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

<#
function WriteXmlToScreen([xml]$xml)
{
    $StringWriter = New-Object System.IO.StringWriter;
    $XmlWriter = New-Object System.Xml.XmlTextWriter $StringWriter;
    $XmlWriter.Formatting = "indented";
    $xml.WriteTo($XmlWriter);
    $XmlWriter.Flush();
    $StringWriter.Flush();
    Write-Output $StringWriter.ToString();
}
#>

# Script working directory
#Set-Location $PSScriptRoot\..

####################################
###         Configuration        ###
####################################
$enableMSI = $true
#$buildOnly = 'win-x64'
#$buildOnly = 'linux-x64'
#$buildOnly = 'osx-x64'
#$buildOnly = 'osx-arm64'
$zipPackages = $true
#$enableNugetPublish = $true

# Profilling
$stopWatch = New-Object -TypeName System.Diagnostics.Stopwatch 
$deployStopWatch = New-Object -TypeName System.Diagnostics.Stopwatch
$stopWatch.Start()


# Variables
$software       = "UVtools"
$project        = "UVtools.UI"
$buildWith      = "Release"
$netVersion     = "6.0"
$publishFolder  = "publish"

$rootPath           = Split-Path $PSScriptRoot -Parent
$buildPath          = "$rootPath\build"
$platformsPath      = "$buildPath\platforms"
$publishPath        = "$rootPath\$publishFolder"
$releasePath        = "$rootPath\$project\bin\$buildWith\net$netVersion"
$objPath            = "$rootPath\$project\obj\$buildWith\net$netVersion"
$changelogFile      = "$rootPath\CHANGELOG.md"
$releaseNotesFile   = "$rootPath\RELEASE_NOTES.md"

#$version = (Get-Command "$releasePath\UVtools.dll").FileVersionInfo.ProductVersion
$projectXml = [Xml] (Get-Content "$rootPath\$project\$project.csproj")
$version = "$($projectXml.Project.PropertyGroup.Version)".Trim();
if([string]::IsNullOrWhiteSpace($version)){
    Write-Error "Can not detect the UVtools version, does $project\$project.csproj exists?"
    exit
}

# MSI Variables
$installer      = "UVtools.Installer"
$installerPath  = "$rootPath\$installer"
$msiOutputFile  = "$installerPath\bin\x64\Release\UVtools.msi"
$msiProductFile = "$installerPath\Code\Product.wxs"
$msiSourceFiles = "$publishPath\${software}_win-x64_v$version"

<#
$msbuildPaths = "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
                "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
                "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
$msbuild = $null
foreach($path in $msbuildPaths) {
    if (Test-Path -Path $path -PathType Leaf)
    {
        $msbuild = "`"$path`" /t:Build /p:Configuration=`"$buildWith`" /p:ProductVersion=`"$version`" /p:HarvestPath=`"$msiSourceFiles`""
        break;
    }
}
#>

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
#Remove-Item $publishFolder -Recurse -ErrorAction Ignore # Clean

####################################
###    Self-contained runtimes   ###
####################################
$runtimes = 
@{
    "win-x64" = @{
        "extraCmd" = ""
        "exclude" = @("UVtools.sh", "opencv_videoio_ffmpeg470_64.dll")
        "include" = @("cvextern.dll")
    }
    "linux-x64" = @{
        "extraCmd" = ""
        "exclude" = @()
        "include" = @("libcvextern.so")
    }
    #"arch-x64" = @{
    #    "extraCmd" = ""
    #    "exclude" = @()
    #    "include" = @("libcvextern.so")
    #}
    #"rhel-x64" = @{
    #    "extraCmd" = ""
    #    "exclude" = @()
    #    "include" = @("libcvextern.so")
    #}
    #"linux-arm64" = @{
    #    "extraCmd" = ""
    #    "exclude" = @()
    #    "include" = @("libcvextern.so")
    #}
    #"unix-x64" = @{
    #    "extraCmd" = ""
    #    "exclude" = @()
    #}
    "osx-x64" = @{
        "extraCmd" = ""
        "exclude" = @()
        "include" = @("libcvextern.dylib")
    }
    "osx-arm64" = @{
        "extraCmd" = ""
        "exclude" = @()
        "include" = @("libcvextern.dylib")
    }
}

# Sync PrusaSlicer profiles
& "$rootPath\Scripts\ImportPrusaSlicerData.ps1"

# Set release notes on projects
$changelog = Get-Content -Path "$changelogFile"
$foundHashTag = $false
$sb = [System.Text.StringBuilder]::new()
foreach($line in $changelog) {
    $line = $line.TrimEnd()
    if([string]::IsNullOrWhiteSpace($line)) { continue }
    if($line.StartsWith('##')) { 
        if(!$foundHashTag)
        {
            $foundHashTag = $true
            continue
        }
        else { break }
    }
    elseif($foundHashTag){
        $sb.AppendLine($line)
    }
}

Write-Host $sb.ToString()
Set-Content -Path "$releaseNotesFile" -Value $sb.ToString()

<#
if($null -ne $enableNugetPublish -and $enableNugetPublish)
{
    $nugetApiKeyFile = 'build/secret/nuget_api.key'
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
#>

foreach ($obj in $runtimes.GetEnumerator()) {
    if(![string]::IsNullOrWhiteSpace($buildOnly) -and !$buildOnly.Equals($obj.Name)) {continue}
    # Configuration
    $deployStopWatch.Restart()
    $runtime = $obj.Name;       # runtime name
    $extraCmd = $obj.extraCmd;  # extra cmd to run with dotnet
    $publishName="${software}_${runtime}_v$version"
    $publishCurrentPath="$publishPath\$publishName"

    #dotnet build "UVtools.Cmd" -c $buildWith
    #dotnet build $project -c $buildWith

    if($runtime.StartsWith("win-"))
    {
        $targetZipName = "${software}_${runtime}_v$version.zip"  # Target zip filename
        $targetZipPath = "$publishPath/$targetZipName"  # Target zip filename
        Remove-Item "$publishCurrentPath" -Recurse -ErrorAction Ignore
        Remove-Item "$targetZipPath" -ErrorAction Ignore
        
        # Deploy
        Write-Output "################################
    Building: $runtime"
        dotnet publish "$rootPath\UVtools.Cmd" -o "$publishCurrentPath" -c $buildWith -r $runtime -p:PublishReadyToRun=true --self-contained $extraCmd
        dotnet publish "$rootPath\$project" -o "$publishCurrentPath" -c $buildWith -r $runtime -p:PublishReadyToRun=true --self-contained $extraCmd

        New-Item "$publishCurrentPath\runtime_package.dat" -ItemType File -Value $runtime
        
        # Cleanup
        Remove-Item "$rootPath\UVtools.Cmd\bin\$buildWith\net$netVersion\$runtime" -Recurse -ErrorAction Ignore
        Remove-Item "$rootPath\UVtools.Cmd\obj\$buildWith\net$netVersion\$runtime" -Recurse -ErrorAction Ignore

        Remove-Item "$releasePath\$runtime" -Recurse -ErrorAction Ignore
        Remove-Item "$objPath\$runtime" -Recurse -ErrorAction Ignore
        
        Write-Output "$publishCurrentPath"
        
        foreach ($excludeObj in $obj.Value.exclude) {
            Write-Output "Excluding: $excludeObj"
            Remove-Item "$publishCurrentPath\$excludeObj" -Recurse -ErrorAction Ignore
        }

        foreach ($includeObj in $obj.Value.include) {
            Write-Output "Including: $includeObj"
            Copy-Item "$platformsPath\$runtime\$includeObj" -Destination "$publishCurrentPath" -Recurse -ErrorAction Ignore
            Copy-Item "$platformsPath\$runtime\$includeObj" -Destination "$publishCurrentPath" -Recurse -ErrorAction Ignore
        }

        if($null -ne $zipPackages -and $zipPackages)
        {
            Write-Output "Compressing $runtime to: $targetZipName"
            wsl --cd "$publishCurrentPath" pwd `&`& zip -rq "../$targetZipName" .
        }
    }
    else
    {
        $buildArgs = '-b' # Bundle
        if($null -ne $zipPackages -and $zipPackages)
        {
            $buildArgs += ' -z' # Zip
        }
        
        wsl --cd "$buildPath" bash -c "./createRelease.sh $buildArgs $runtime"
        #Start-Job { bash -c "'build/createRelease.sh' $using:buildArgs$buildArgs $using:runtime" }
    }
    
    $deployStopWatch.Stop()
    Write-Output "Took: $($deployStopWatch.Elapsed)"
    Write-Output "################################"
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
[System.IO.Compression.ZipFile]::CreateFromDirectory($releasePath, $targetZip, [System.IO.Compression.CompressionLevel]::Optimal, $false, [FixedEncoder]::new())
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
    $publishName = "${software}_${runtime}_v$version";
    $publishCurrentPath="$publishPath\$publishName"
    
    if ((Test-Path -Path $msiSourceFiles) -and ((Get-ChildItem "$msiSourceFiles" | Measure-Object).Count) -gt 0) {

        $msiTargetFile = "$publishPath\$publishName.msi"
        Write-Output "################################"
        Write-Output "Build MSI setup file"

        Remove-Item "$msiTargetFile" -ErrorAction Ignore

        if(Test-Path "$publishCurrentPath\UVtools.Core.dll" -PathType Leaf){
            Add-Type -Path "$publishCurrentPath\UVtools.Core.dll"
        } else {
            Write-Error "Unable to find UVtools.Core.dll"
            return
        }

        # Add edit with UVtools possible extensions
        $extensions = [UVtools.Core.FileFormats.FileFormat]::AllFileExtensions;
        $extensionList = New-Object Collections.Generic.List[String]
        foreach($ext in $extensions)
        {
            if($ext.Extension.Contains('.')) { continue; } # Virtual extension, ignore

            $extKey = "System.FileName:&quot;*.$($ext.Extension.ToLowerInvariant())&quot;";
            if($extensionList.Contains($extKey)) { continue; } # Already here
            $extensionList.Add($extKey);
        }

        if($extensionList.Count -gt 0)
        {
            $regValue = [String]::Join(' OR ', $extensionList)
            (Get-Content "$msiProductFile") -replace '(?<A><RegistryValue Name="AppliesTo" Value=").+(?<B>" Type=.+)', "`${A}$regValue`${B}" | Out-File "$msiProductFile"
        }

        Write-Output "Building: $runtime MSI Installer"

        # Clean and build MSI
        Remove-Item "$installerPath\obj" -Recurse -ErrorAction Ignore
        Remove-Item "$installerPath\bin" -Recurse -ErrorAction Ignore

        dotnet build "$installerPath" -c $buildWith -p:Platform=x64 -p:ProductVersion="$version" -p:HarvestPath="$msiSourceFiles"

        Write-Output "Copying $runtime MSI to: $msiTargetFile"
        Copy-Item $msiOutputFile $msiTargetFile

        Write-Output "Took: $($deployStopWatch.Elapsed)"
        Write-Output "################################"
        Write-Output ""
    }
}


Write-Output "
####################################
###           Completed          ###
####################################
In: $($stopWatch.Elapsed)"


