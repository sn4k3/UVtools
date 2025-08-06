# Script to build the cvextern.dll
# Can be run outside UVtools and as standalone script

# Requirements:
## git
## cmake
## Visual Studio with development tools and sdk

# Script working directory
#Set-Location $PSScriptRoot

$libFolder = 'emgucv'
$buildFile = "platforms\windows\Build_Binary_x86.bat"
$buildArgs = '64 mini commercial no-openni no-doc no-package build'
$customBuild = '-DWITH_EIGEN:BOOL=FALSE -DWITH_MSMF:BOOL=FALSE -DWITH_DSHOW:BOOL=FALSE -DWITH_FFMPEG:BOOL=FALSE -DWITH_GSTREAMER:BOOL=FALSE -DWITH_1394:BOOL=FALSE -DVIDEOIO_ENABLE_PLUGINS:BOOL=FALSE -DBUILD_opencv_videoio:BOOL=FALSE -DBUILD_opencv_gapi:BOOL=FALSE -DWITH_PROTOBUF:BOOL=FALSE -DBUILD_PROTOBUF:BOOL=FALSE'

$prompt = $null
$branch = $null
if ($args.Count -gt 0) {
    if (-not ([string]::IsNullOrWhiteSpace($args[0]))) {
        if($args[0] -eq 'clean') {
            if(Test-Path -Path "$libFolder") {
                Remove-Item -Force -Recurse -Path "$libFolder"
            }
            exit;
        } else {
            $prompt = $args[0]
        }
    }
}

$foundCMake = [bool] (Get-Command -ErrorAction Ignore -Type Application cmake)
if (!$foundCMake)
{
    Write-Error "cmake not found, please install via Visual Studio."
	exit;
}

$foundDotnet = [bool] (Get-Command -ErrorAction Ignore -Type Application dotnet)
if (!$foundDotnet)
{
    Write-Error "dotnet not found, please install."
	exit;
}

if (-not $prompt) {
    $prompt = Read-Host "Select a branch or tag to build cvextern.dll
y/yes/master/main: Download master branch
4.9.0: Download specific tag
n/no: Cancel
Option"
}

if ($prompt -eq 'y' -or $prompt -eq 'yes' -or $prompt -eq 'master' -or $prompt -eq 'main') {
    $branch = 'master'
} elseif($prompt -match '^[0-9]+[.][0-9]+[.][0-9]+$') {
    $branch = $prompt
} else {
    exit;
}

if (-not $branch) {
    Write-Error "Invalid branch or tag specified."
    exit;
}

$libFolder = "$libFolder-$branch"

if (-not (Test-Path -Path "$libFolder/$buildFile" -PathType Leaf)) {
    Write-Output "Clonning $branch of EmguCV to $libFolder"
    git clone --recurse-submodules --depth 1 --branch "$branch" "https://github.com/emgucv/emgucv" "$libFolder"
}

Set-Location "$libFolder"

Write-Output "Configuring"
# https://docs.opencv.org/4.x/db/d05/tutorial_config_reference.html
$search = (Get-Content -Path "$buildFile" | Select-String -Pattern "$customBuild").Matches.Success
if(-not $search){
    $searchKeyword = '%CMAKE% %EMGU_CV_CMAKE_CONFIG_FLAGS%'
    (Get-Content -Path "$buildFile") -replace "$searchKeyword", "$searchKeyword $customBuild" | Set-Content -Path "$buildFile"
}

Write-Output "Building"
cmd.exe /c "$buildFile $buildArgs"
Write-Output "Completed - Check for errors but also for libcvextern presence on $libFolder\libs\runtimes"
