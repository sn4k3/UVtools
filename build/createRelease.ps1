# When using System.IO.Compression.ZipFile.CreateFromDirectory in PowerShell, it still uses backslashes in the zip paths
# despite this https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/mitigation-ziparchiveentry-fullname-path-separator

# Based upon post by Seth Jackson https://sethjackson.github.io/2016/12/17/path-separators/

#
# PowerShell 5 (WMF5) & 6
# Using class Keyword https://msdn.microsoft.com/powershell/reference/5.1/Microsoft.PowerShell.Core/about/about_Classes
#
# https://gist.github.com/lantrix/738ebfa616d5222a8b1db947793bc3fc
#
if($PSVersionTable.PSVersion.Major -lt 7){
    Write-Error("Powershell version $($PSVersionTable.PSVersion) is not compatible with this build script.`n
You need at least the version 7.")
    return;
}

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
function wixCleanUpElement([System.Xml.XmlElement]$element, [string]$rootPath)
{
    $files = Get-ChildItem -Path $rootPath -File -Force -ErrorAction SilentlyContinue
    $folders = Get-ChildItem -Path $rootPath -Directory -Force -ErrorAction SilentlyContinue
    
    # Removing not existant files
    foreach($e in $element.Component)
    {
        $found = $false
        foreach($file in $files)
        {
            if($e.File.Source.EndsWith("\$($file.Name)")){
                $found = $true
                break
            }
        }

        if ($found -eq $false){
            # File not found on this directory, remove element
            Write-Host("WIX: File '$($e.File.Source)' does not exist anymore, removing...")
            $e.ParentNode.RemoveChild($e) | Out-Null
        }
    }

    # Adding not existant files
    foreach($file in $files)
    {
        $found = $false
        foreach($e in $element.Component)
        {
            if($null -ne $e.File.Source -and $e.File.Source.EndsWith("\$($file.Name)")){
                $found = $true
                break
            }
        }

        if ($found -eq $false){
            # File not found on manifest, add element
            $guid = [guid]::NewGuid().ToString().ToUpper()
            $guidNoSeparator = $guid.Replace('-', '')
            $filePath = $file.FullName.Substring($msiSourceFiles.Length)
            while ($filePath.StartsWith('\'))
            {
                $filePath = $filePath.Remove(0, 1)
            }
            Write-Host("WIX: File '$filePath' does not exist on manifest, adding with GUID: $guid")
            $xmlComponent = $element.OwnerDocument.CreateElement("Component", $element.OwnerDocument.DocumentElement.NamespaceURI)
            $xmlComponent.SetAttribute("Id", "owc$guidNoSeparator")
            $xmlComponent.SetAttribute("Guid", $guid)

            $xmlFile = $element.OwnerDocument.CreateElement("File", $element.OwnerDocument.DocumentElement.NamespaceURI)
            $xmlFile.SetAttribute("Id", "owf$guidNoSeparator")
            $xmlFile.SetAttribute("Source", "`$(var.SourceDir)\$filePath")
            $xmlFile.SetAttribute("KeyPath", 'yes')

            $xmlComponent.AppendChild($xmlFile) | Out-Null
            $element.AppendChild($xmlComponent) | Out-Null
        }
    }
    
    # Removing not existant folters
    foreach($e in $element.Directory)
    {
        $found = $false
        foreach($folder in $folders)
        {
            if($e.Name -eq $folder.Name){
                $found = $true
                break
            }
        }

        if ($found -eq $false){
            # Directory not found on this directory, remove element
            Write-Host("WIX: Directory '$($e.Name)' does not exist anymore, removing...")
            $e.ParentNode.RemoveChild($e) | Out-Null
        }
    }
    
    # Adding not existant directories
    foreach($folder in $folders)
    {
        $foundDirectoryElement = $null
        foreach($e in $element.Directory)
        {
            #Write-Host("$($e.Name) == $($folder.Name)")
            if($null -ne $e.Name -and $e.Name -eq $folder.Name){
                $foundDirectoryElement = $e
                break
            }
        }

        if ($null -eq $foundDirectoryElement){
            # Folder not found on manifest, add element
            $guid = [guid]::NewGuid().ToString().ToUpper()
            $guidNoSeparator = $guid.Replace('-', '')

            $directoryRelativePath = $folder.FullName.Substring($msiSourceFiles.Length)
            while ($directoryRelativePath.StartsWith('\'))
            {
                $directoryRelativePath = $directoryRelativePath.Remove(0, 1)
            }

            Write-Host("WIX: Directory '$directoryRelativePath' does not exist on manifest, adding with GUID: $guid")
            $xmlDirectory = $element.OwnerDocument.CreateElement("Directory", $element.OwnerDocument.DocumentElement.NamespaceURI)
            $xmlDirectory.SetAttribute("Id", "owd$guidNoSeparator")
            $xmlDirectory.SetAttribute("Name", $folder.Name)
            $element.AppendChild($xmlDirectory) | Out-Null
            
            wixCleanUpElement $xmlDirectory $folder.FullName
        }else{
            # Folder found on manifest, recursive from this folder
            wixCleanUpElement $foundDirectoryElement $folder.FullName
        }
    }
}
#>

<#
$msiComponentsXml = [Xml] (Get-Content $msiComponentsFile)
foreach($element in $msiComponentsXml.Wix.Module.Directory.Directory)
{
    if($element.Id -eq 'MergeRedirectFolder')
    {
        wixCleanUpElement $element $msiSourceFiles
        #WriteXmlToScreen($msiComponentsXml);
        #$msiComponentsXml.Save("$rootFolder\$publishFolder\test.xml")
        return
        break
    }
}


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
Set-Location $PSScriptRoot\..

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
$software = "UVtools"
$project = "UVtools.WPF"
$buildWith = "Release"
$netVersion = "6.0"
$rootFolder = $(Get-Location)
$buildFolder = "$rootFolder\build"
$releaseFolder = "$project\bin\$buildWith\net$netVersion"
$objFolder = "$project\obj\$buildWith\net$netVersion"
$publishFolder = "publish"
$platformsFolder = "$buildFolder\platforms"
$changelogFile = "$rootFolder\CHANGELOG.md"

#$version = (Get-Command "$releaseFolder\UVtools.dll").FileVersionInfo.ProductVersion
$projectXml = [Xml] (Get-Content "$project\$project.csproj")
$version = "$($projectXml.Project.PropertyGroup.Version)".Trim();
if([string]::IsNullOrWhiteSpace($version)){
    Write-Error "Can not detect the UVtools version, does $project\$project.csproj exists?"
    exit
}

# MSI Variables
$installer = "UVtools.Installer"
$msiOutputFile = "$rootFolder\UVtools.Installer\bin\x64\Release\UVtools.msi"
$msiProductFile = "$rootFolder\UVtools.Installer\Code\Product.wxs"
$msiSourceFiles = "$rootFolder\$publishFolder\${software}_win-x64_v$version"
$msbuild = "`"${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe`" /t:Build /p:Configuration=`"$buildWith`" /p:MSIProductVersion=`"$version`" /p:HarvestPath=`"$msiSourceFiles`""

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
        "exclude" = @("UVtools.sh")
        "include" = @()
    }
    "linux-x64" = @{
        "extraCmd" = ""
        "exclude" = @()
        "include" = @("libcvextern.so")
    }
    "arch-x64" = @{
        "extraCmd" = ""
        "exclude" = @()
        "include" = @("libcvextern.so")
    }
    "rhel-x64" = @{
        "extraCmd" = ""
        "exclude" = @()
        "include" = @("libcvextern.so")
    }
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
        [void]$sb.AppendLine($line)
    }
}
Write-Host $sb.ToString()
Set-Content -Path "$rootFolder/RELEASE_NOTES.md" -Value $sb.ToString()


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

foreach ($obj in $runtimes.GetEnumerator()) {
    if(![string]::IsNullOrWhiteSpace($buildOnly) -and !$buildOnly.Equals($obj.Name)) {continue}
    # Configuration
    $deployStopWatch.Restart()
    $runtime = $obj.Name;       # runtime name
    $extraCmd = $obj.extraCmd;  # extra cmd to run with dotnet
    $publishName="${software}_${runtime}_v$version"

    #dotnet build "UVtools.Cmd" -c $buildWith
    #dotnet build $project -c $buildWith

    if($runtime.StartsWith("win-"))
    {
        $targetZip = "$publishFolder/${software}_${runtime}_v$version.zip"  # Target zip filename
        Remove-Item "$publishFolder/$publishName" -Recurse -ErrorAction Ignore
        Remove-Item "$targetZip" -ErrorAction Ignore
        
        # Deploy
        Write-Output "################################
    Building: $runtime"
        dotnet publish "UVtools.Cmd" -o "$publishFolder/$publishName" -c $buildWith -r $runtime -p:PublishReadyToRun=true --self-contained $extraCmd
        dotnet publish $project -o "$publishFolder/$publishName" -c $buildWith -r $runtime -p:PublishReadyToRun=true --self-contained $extraCmd

        New-Item "$publishFolder/$publishName/runtime_package.dat" -ItemType File -Value $runtime
        
        # Cleanup
        Remove-Item "UVtools.Cmd\bin\$buildWith\net$netVersion\$runtime" -Recurse -ErrorAction Ignore
        Remove-Item "UVtools.Cmd\obj\$buildWith\net$netVersion\$runtime" -Recurse -ErrorAction Ignore

        Remove-Item "$releaseFolder\$runtime" -Recurse -ErrorAction Ignore
        Remove-Item "$objFolder\$runtime" -Recurse -ErrorAction Ignore
        
        Write-Output "$releaseFolder\$runtime"
        
        foreach ($excludeObj in $obj.Value.exclude) {
            Write-Output "Excluding: $excludeObj"
            Remove-Item "$publishFolder\$runtime\$excludeObj" -Recurse -ErrorAction Ignore
        }

        foreach ($includeObj in $obj.Value.include) {
            Write-Output "Including: $includeObj"
            Copy-Item "$platformsFolder\$runtime\$includeObj" -Destination "$publishFolder\$publishName"  -Recurse -ErrorAction Ignore
        }

        if($null -ne $zipPackages -and $zipPackages)
        {
            Write-Output "Compressing $runtime to: $targetZip"
            wsl cd "$publishFolder/$publishName" `&`& pwd `&`& zip -rq "../../$targetZip" .
        }
    }
    else
    {
        $args = '-b' # Bundle
        if($null -ne $zipPackages -and $zipPackages)
        {
            $args += ' -z' # Zip
        }
        bash -c "'build/createRelease.sh' $args $runtime"
        #Start-Job { bash -c "'build/createRelease.sh' $using:args $using:runtime" }
    }
    
    $deployStopWatch.Stop()
    Write-Output "Took: $($deployStopWatch.Elapsed)
################################"
}

<#
$deployStopWatch.Restart()
#Wait for all jobs to finish.
While ($(Get-Job -State Running).count -gt 0){
    Write-Host -NoNewline "."
    Start-Sleep 1
}

Write-Output " Took: $($deployStopWatch.Elapsed)"

#Get information from each job.
foreach($job in Get-Job){
    $job
    #Receive-Job -Id ($job.Id)
}

#Remove all jobs created.
Get-Job | Remove-Job
#>

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
    $publishName = "${software}_${runtime}_v$version";
    
    if ((Test-Path -Path $msiSourceFiles) -and ((Get-ChildItem "$msiSourceFiles" | Measure-Object).Count) -gt 0) {
        $msiTargetFile = "$publishFolder\$publishName.msi"
        Write-Output "################################"
        Write-Output "Clean and build MSI components manifest file"

        Remove-Item "$msiTargetFile" -ErrorAction Ignore

        <#
        (Get-Content "$msiComponentsFile") -replace 'SourceDir="\.\.\\publish\\.+"', "SourceDir=`"..\publish\$publishName`"" | Out-File "$msiComponentsFile"
        
        $msiComponentsXml = [Xml] (Get-Content "$msiComponentsFile")
        foreach($element in $msiComponentsXml.Wix.Module.Directory.Directory)
        {
            if($element.Id -eq 'MergeRedirectFolder')
            {
                wixCleanUpElement $element $msiSourceFiles
                $msiComponentsXml.Save($msiComponentsFile)
                break
            }
        }
        #>

        if(Test-Path "$publishFolder\$publishName\UVtools.Core.dll" -PathType Leaf){
            Add-Type -Path "$publishFolder\$publishName\UVtools.Core.dll"
        } else {
            Write-Error "Unable to find UVtools.Core.dll"
            return
        }

        # Add edit with UVtools possible extensions
        $extensions = [UVtools.Core.FileFormats.FileFormat]::AllFileExtensions;
        $extensionList = New-Object Collections.Generic.List[String]
        foreach($ext in $extensions)
        {
            if($ext.Extension.Contains('.')) { continue; } # Virtual extension

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
        Remove-Item "$installer\obj" -Recurse -ErrorAction Ignore
        Remove-Item "$installer\bin" -Recurse -ErrorAction Ignore
        Invoke-Expression "& $msbuild $installer\$installer.wixproj"

        Write-Output "Copying $runtime MSI to: $msiTargetFile"
        Copy-Item $msiOutputFile $msiTargetFile

        Write-Output "Took: $($deployStopWatch.Elapsed)
        ################################
        "
    }
    #else {
    #    Write-Error "MSI build is enabled but the runtime '$runtime' is not found."
    #}
    
}


Write-Output "
####################################
###           Completed          ###
####################################
In: $($stopWatch.Elapsed)"


