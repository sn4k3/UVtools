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

if ($args.Count -gt 0){
    if($args[0] -eq 'clean'){
        if(Test-Path -Path "$libFolder"){
            Remove-Item -Force -Recurse -Path "$libFolder"
        }
        exit;
    }
}

if (-not (Test-Path -Path "$libFolder/$buildFile" -PathType Leaf)) {
    $confirmation = Read-Host "$libFolder directory does not exists, do you want to download it?
y/yes: Download master branch
4.7.0: Download specific tag
n/no: Cancel
Option"

    if ($confirmation -eq 'y' -or $confirmation -eq 'yes') {
        Write-Output "Clone master"
        git clone --recurse-submodules --depth 1 "https://github.com/emgucv/emgucv" "$libFolder"
    }elseif($confirmation -match '^[0-9]+[.][0-9]+[.][0-9]+$') {
        Write-Output "$confirmation"
        git clone --recurse-submodules --depth 1 --branch "$confirmation" "https://github.com/emgucv/emgucv" "$libFolder"
    }
    else {
        exit;
    }
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
