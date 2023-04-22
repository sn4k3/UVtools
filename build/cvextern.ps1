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
    }elseif($confirmation -match '^\d+\.\d+\.\d+$') {
        Write-Output "$confirmation"
        git clone --recurse-submodules --depth 1 --branch "$confirmation" "https://github.com/emgucv/emgucv" "$libFolder"
    }
    else {
        exit;
    }
}

Set-Location "$libFolder"

Write-Output "Configuring"
(Get-Content -Path "$buildFile") -replace '-DBUILD_opencv_video:BOOL=FALSE', '-DBUILD_opencv_video:BOOL=FALSE -DWITH_TIFF:BOOL=FALSE -DEMGU_CV_WITH_TIFF:BOOL=FALSE' | Set-Content -Path "$buildFile"

Write-Output "Building"
cmd.exe /c "$buildFile $buildArgs"
Write-Output "Completed - Check for errors but also for libcvextern presence on $libFolder\libs\runtimes"
