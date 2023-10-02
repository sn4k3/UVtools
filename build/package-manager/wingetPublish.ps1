# Script working directory
Set-Location $PSScriptRoot

####################################
###         Configuration        ###
####################################
# Variables
$projectAPIUrl = "https://api.github.com/repos/sn4k3/UVtools/releases/latest"
$projectPackageUrl = "https://github.com/sn4k3/UVtools/releases/tag/v"
$rootPath      = $PSScriptRoot
$outputFolder  = "$rootPath/manifests"
$localeYaml    = "PTRTECH.UVtools.locale.en-US.yaml"
$installerYaml = "PTRTECH.UVtools.installer.yaml"
$releaseDate   = Get-Date -Format "yyyy-MM-dd"
$msiUrl = $null

Write-Output "
####################################
###    UVtools Winget publish    ###
####################################
"

$wingetTokenKeyFile = '../secret/winget_token.key'
if (Test-Path -Path $wingetTokenKeyFile -PathType Leaf)
{
    $wingetTokenKeyFile = (Get-Content $wingetTokenKeyFile)
}else{
    Write-Error "Winget manifest publish failed!"
    Write-Error "Invalid token!"
    return
}

$headers = @{
    Authorization = 'Basic {0}' -f $wingetTokenKeyFile
};
$lastRelease = Invoke-RestMethod -Headers $headers -Uri $projectAPIUrl

# Parse the version
$version = $lastRelease.tag_name.Replace('v', '')
if($version.Length -lt 5){
    Write-Error "The version $version is too short to be correct, expecting at least 5 chars, please verify."
    return
}

$projectPackageUrl = "$projectPackageUrl$version"

$manifestsPath     = "$outputFolder/p/PTRTECH/UVtools/$version"
$localeYamlFile    = "$manifestsPath/$localeYaml"
$installerYamlFile = "$manifestsPath/$installerYaml"

# Parse the MSI url
foreach ($asset in $lastRelease.assets)
{
    if($asset.browser_download_url.EndsWith('.msi')) {
        $msiUrl = $asset.browser_download_url
        break
    }
}

# Check if MSI url is valid
if ($null -eq $msiUrl){
    Write-Error "MSI release URL not detected, exiting now."
    return
}

# Debug
Write-Host "
Version: $version
Url: $msiUrl
"

$actionInput = Read-Host -Prompt "Do you want to update the manifest with the current release v$($version)? (Y/Yes or N/No)"
if($actionInput -eq "y" -or $actionInput -eq "yes")
{
    # Clean
    Remove-Item $outputFolder -Recurse -ErrorAction Ignore 

    #wingetcreate.exe update PTRTECH.UVtools --interactive
    wingetcreate.exe update PTRTECH.UVtools --urls "$msiUrl|x64" --version $version --token $wingetTokenKeyFile
    
    # Update dynamic content
    #(Get-Content "$localeYamlFile")    -replace 'ReleaseNotesUrl:.*', "ReleaseNotesUrl: $projectPackageUrl" | Out-File "$localeYamlFile"
    #(Get-Content "$installerYamlFile") -replace 'ReleaseDate:.*', "ReleaseDate: $releaseDate" | Out-File "$installerYamlFile"

    # Submit PR
    wingetcreate.exe submit --token $wingetTokenKeyFile $manifestsPath

    # Clean
    Remove-Item $outputFolder -Recurse -ErrorAction Ignore 
}

Write-Output "
####################################
###           Completed          ###
####################################
"