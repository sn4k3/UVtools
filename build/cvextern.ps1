# Script to build the cvextern.dll
# Can be run outside UVtools and as standalone script

# Requirements:
## git
## cmake
## Visual Studio with development tools and Windows SDK

$ErrorActionPreference = 'Stop'

$baseLibFolder = 'emgucv'
$buildFile = Join-Path 'platforms' 'windows\Build_Binary_x86.bat'
$buildDirectory = Join-Path 'platforms' 'windows\build'
$buildArgs = '64 mini commercial no-openni no-doc no-package build'
$customBuild = '-DWITH_EIGEN:BOOL=FALSE -DWITH_MSMF:BOOL=FALSE -DWITH_DSHOW:BOOL=FALSE -DWITH_FFMPEG:BOOL=FALSE -DWITH_GSTREAMER:BOOL=FALSE -DWITH_1394:BOOL=FALSE -DVIDEOIO_ENABLE_PLUGINS:BOOL=FALSE -DBUILD_opencv_videoio:BOOL=FALSE -DBUILD_opencv_gapi:BOOL=FALSE -DWITH_PROTOBUF:BOOL=FALSE -DBUILD_PROTOBUF:BOOL=FALSE'
$scriptRoot = if ($PSScriptRoot) { $PSScriptRoot } else { (Get-Location).Path }

function Write-Usage {
    Write-Host 'Usage:'
    Write-Host '  .\cvextern.ps1 clean'
    Write-Host '  .\cvextern.ps1 remove'
    Write-Host '  .\cvextern.ps1 [tag]'
    Write-Host ''
    Write-Host 'Options:'
    Write-Host '  clean   Removes EmguCV Windows build folders'
    Write-Host '  remove  Removes EmguCV source folders'
    Write-Host '  tag     Tag or branch name to clone, for example 4.9.0'
}

function Get-EmguCvDirectories {
    Get-ChildItem -LiteralPath $scriptRoot -Directory |
        Where-Object { $_.Name -eq $baseLibFolder -or $_.Name.StartsWith("$baseLibFolder-") }
}

function Remove-ExistingDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,

        [Parameter(Mandatory = $true)]
        [string] $Description
    )

    if (Test-Path -LiteralPath $Path) {
        Write-Host "Removing $Description $Path"
        Remove-Item -LiteralPath $Path -Force -Recurse
        return
    }

    Write-Warning "$Description $Path does not exist"
}

function Test-RequiredCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name,

        [Parameter(Mandatory = $true)]
        [string] $InstallHint
    )

    if (-not (Get-Command -ErrorAction Ignore -Type Application $Name)) {
        throw "$Name not found, please install $InstallHint."
    }
}

function Resolve-Branch {
    param([string] $Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        $Value = Read-Host "Select a branch or tag to build cvextern.dll
y/yes/master/main: Download master branch
4.9.0: Download specific tag
n/no: Cancel
Option"
    }

    switch -Regex ($Value) {
        '^(y|yes|master|main)$' {
            return 'master'
        }
        '^[0-9]+[.][0-9]+[.][0-9]+$' {
            return $Value
        }
        default {
            return $null
        }
    }
}

function Update-BuildFile {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.Contains($customBuild)) {
        return
    }

    $searchKeyword = '%CMAKE% %EMGU_CV_CMAKE_CONFIG_FLAGS%'
    if (-not $content.Contains($searchKeyword)) {
        throw "Unable to patch $Path. Could not find: $searchKeyword"
    }

    $content.Replace($searchKeyword, "$searchKeyword $customBuild") |
        Set-Content -LiteralPath $Path -NoNewline
}

Push-Location $scriptRoot
try {
    if ($args.Count -gt 1) {
        Write-Usage
        Write-Warning 'Only one command, tag, or branch can be specified.'
        exit 1
    }

    $commandOrBranch = if ($args.Count -eq 1) { $args[0] } else { $null }

    if ($commandOrBranch -match '^(-h|--help)$') {
        Write-Usage
        exit 0
    }

    switch ($commandOrBranch) {
        'clean' {
            $directories = @(Get-EmguCvDirectories)
            if ($directories.Count -eq 0) {
                Write-Warning "No $baseLibFolder source folders found"
                exit 0
            }

            foreach ($directory in $directories) {
                Remove-ExistingDirectory `
                    -Path (Join-Path $directory.FullName $buildDirectory) `
                    -Description 'build directory'
            }

            exit 0
        }
        'remove' {
            $directories = @(Get-EmguCvDirectories)
            if ($directories.Count -eq 0) {
                Write-Warning "No $baseLibFolder source folders found"
                exit 0
            }

            foreach ($directory in $directories) {
                Remove-ExistingDirectory -Path $directory.FullName -Description 'library folder'
            }

            exit 0
        }
    }

    $branch = Resolve-Branch $commandOrBranch
    if (-not $branch) {
        Write-Warning 'No valid branch or tag specified.'
        exit 1
    }

    Test-RequiredCommand -Name 'git' -InstallHint 'Git'
    Test-RequiredCommand -Name 'cmake' -InstallHint 'via Visual Studio'
    Test-RequiredCommand -Name 'dotnet' -InstallHint 'the .NET SDK'

    $libFolder = "$baseLibFolder-$branch"
    $buildFilePath = Join-Path $libFolder $buildFile

    if (-not (Test-Path -LiteralPath $buildFilePath -PathType Leaf)) {
        Write-Output "Cloning $branch of EmguCV to $libFolder"
        git clone --recurse-submodules --depth 1 --branch $branch 'https://github.com/emgucv/emgucv' $libFolder
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }

    Push-Location $libFolder
    try {
        Write-Output 'Configuring'
        # https://docs.opencv.org/4.x/db/d05/tutorial_config_reference.html
        Update-BuildFile -Path $buildFile

        Write-Output 'Building'
        cmd.exe /c "$buildFile $buildArgs"
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }

        Write-Output "Completed - Check for errors but also for libcvextern presence on $libFolder\libs\runtimes"
    }
    finally {
        Pop-Location
    }
}
finally {
    Pop-Location
}
