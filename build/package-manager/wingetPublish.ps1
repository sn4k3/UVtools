# Script working directory
Set-Location $PSScriptRoot\..\..

####################################
###         Configuration        ###
####################################
# Variables
$projectAPIUrl = "https://api.github.com/repos/sn4k3/UVtools/releases/latest"
$outputFolder = "manifests"
$msiUrl = $null

Write-Output "
####################################
###    UVtools Winget publish    ###
####################################
"

$wingetTokenKeyFile = 'build/winget_token.key'
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
if($actionInput -eq "y" || $actionInput -eq "yes")
{
    Remove-Item $outputFolder -Recurse -ErrorAction Ignore # Clean
    #wingetcreate.exe update PTRTECH.UVtools --interactive
    wingetcreate.exe update PTRTECH.UVtools --urls "$msiUrl|x64" --version $version --token $wingetTokenKeyFile --submit
    Remove-Item $outputFolder -Recurse -ErrorAction Ignore # Clean
}

Write-Output "
####################################
###           Completed          ###
####################################
"