# Script to build the cvextern.dll
# Can be run outside UVtools and as standalone script

# Requirements:
## git
## cmake
## Visual Studio with development tools and Windows SDK

param(
    [Parameter(Position = 0)]
    [string] $CommandOrBranch,

    [Parameter(Position = 1)]
    [ValidateSet('x64', 'x86_64', 'arm64', 'all')]
    [string] $Arch = 'all',

    [Alias('h')]
    [switch] $Help
)

$ErrorActionPreference = 'Stop'

$baseLibFolder = 'emgucv'
$buildFile = Join-Path 'platforms' 'windows\Build_Binary.ps1'
$buildArgs = @{ Arch = 'x86_64'; ComponentSet = 'Mini'; Toolchain = 'Commercial'; ExtraModules = 'None'; Build = $true }
$architectures = @(switch ($Arch) {
    'all' {
        'x86_64'; 'arm64'
    }
    'arm64' {
        'arm64'
    }
    default {
        'x86_64'
    }
})
$customBuild = @(
    '-DWITH_EIGEN:BOOL=FALSE',
    '-DWITH_MSMF:BOOL=FALSE',
    '-DWITH_DSHOW:BOOL=FALSE',
    '-DWITH_FFMPEG:BOOL=FALSE',
    '-DWITH_GSTREAMER:BOOL=FALSE',
    '-DWITH_1394:BOOL=FALSE',
    '-DVIDEOIO_ENABLE_PLUGINS:BOOL=FALSE',
    '-DBUILD_opencv_videoio:BOOL=FALSE',
    '-DBUILD_opencv_gapi:BOOL=FALSE',
    '-DWITH_PROTOBUF:BOOL=FALSE',
    '-DBUILD_PROTOBUF:BOOL=FALSE'
)
$scriptRoot = if ($PSScriptRoot) {
    $PSScriptRoot
} else {
    (Get-Location).Path
}

function Write-Usage {
    Write-Host 'Usage:'
    Write-Host '  .\cvextern.ps1 clean [-Arch x64|arm64|all]'
    Write-Host '  .\cvextern.ps1 remove'
    Write-Host '  .\cvextern.ps1 [tag] [-Arch x64|arm64|all]'
    Write-Host '  .\cvextern.ps1 5.0.0 -Arch all'
    Write-Host ''
    Write-Host 'Options:'
    Write-Host '  clean   Removes EmguCV Windows build folders'
    Write-Host '  remove  Removes EmguCV source folders'
    Write-Host '  tag     Tag or branch name to clone, for example 5.0.0'
    Write-Host '  Arch    Target architecture; both x64 and ARM64 by default. ARM64 can be cross-compiled on Windows x64.'
    Write-Host '          Install the Visual Studio ARM64/ARM64EC C++ build tools and Windows SDK.'
    Write-Host '  clean   Cleans both architectures unless -Arch is specified.'
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
        $resolvedPath = [IO.Path]::GetFullPath($Path)
        $sourceRoot = [IO.Path]::GetFullPath($scriptRoot).TrimEnd('\') + '\'
        if (-not $resolvedPath.StartsWith($sourceRoot, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove a directory outside $scriptRoot`: $resolvedPath"
        }
        Write-Host "Removing $Description $Path"
        Remove-Item -LiteralPath $resolvedPath -Force -Recurse
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
5.0.0: Download specific tag
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

    $content = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $marker = '    # UVtools custom native options'
    if ($content.Contains($marker)) {
        return
    }

    $searchKeyword = '    Invoke-Native -FilePath $vsEnv.CMakeExe -ArgumentList ($emguFlags.ToArray() + @(''..''))'
    if (-not $content.Contains($searchKeyword)) {
        throw "Unable to patch $Path. Could not find: $searchKeyword"
    }

    $flags = ($customBuild | ForEach-Object { "        '$_'" }) -join ",`r`n"
    $replacement = $marker + "`r`n" + '    $emguFlags.AddRange([string[]]@(' + "`r`n" +
        $flags + "`r`n" + '    ))' + "`r`n" + $searchKeyword
    $resolvedPath = (Resolve-Path -LiteralPath $Path).Path
    [IO.File]::WriteAllText($resolvedPath, $content.Replace($searchKeyword, $replacement), [Text.UTF8Encoding]::new($false))
}

Push-Location $scriptRoot
try {
    if ($args.Count -gt 0) {
        Write-Usage
        Write-Warning 'Unexpected arguments. Specify one command or tag and use -Arch for the target.'
        exit 1
    }

    if ($Help -or $CommandOrBranch -match '^(-h|--help)$') {
        Write-Usage
        exit 0
    }

    switch ($commandOrBranch) {
        'clean' {
            if (-not $PSBoundParameters.ContainsKey('Arch')) {
                $architectures = @('x86_64', 'arm64')
            }
            $directories = @(Get-EmguCvDirectories)
            if ($directories.Count -eq 0) {
                Write-Warning "No $baseLibFolder source folders found"
                exit 0
            }

            foreach ($directory in $directories) {
                foreach ($buildArch in $architectures) {
                    Remove-ExistingDirectory `
                        -Path (Join-Path $directory.FullName "build_$buildArch") `
                        -Description "$buildArch build directory"
                }
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
    # The upstream script can find CMake bundled with Visual Studio without it being on PATH.
    Test-RequiredCommand -Name 'dotnet' -InstallHint 'the .NET SDK'

    $libFolder = Join-Path $scriptRoot "$baseLibFolder-$branch"
    $buildFilePath = Join-Path $libFolder $buildFile

    if (-not (Test-Path -LiteralPath $libFolder -PathType Container)) {
        Write-Output "Cloning $branch of EmguCV to $libFolder"
        git clone --recurse-submodules --depth 1 --branch $branch 'https://github.com/emgucv/emgucv' $libFolder
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }

    if (-not (Test-Path -LiteralPath $buildFilePath -PathType Leaf)) {
        throw "Emgu CV Windows build script not found: $buildFilePath. Use an Emgu CV 5 source checkout."
    }

    Push-Location $libFolder
    try {
        Write-Output 'Configuring'
        Update-BuildFile -Path $buildFile

        foreach ($buildArch in $architectures) {
            Write-Output "Building $buildArch"
            $buildArgs.Arch = $buildArch
            # Upstream checks native exit codes and throws on failure.
            & $buildFilePath @buildArgs

            $nativeArch = if ($buildArch -eq 'x86_64') {
                'x64'
            } else {
                'arm64'
            }
            $nativeFile = Join-Path $libFolder "libs\runtimes\win-$nativeArch\native\cvextern.dll"
            if (-not (Test-Path -LiteralPath $nativeFile -PathType Leaf)) {
                throw "Build finished without the expected native binary: $nativeFile"
            }
            Write-Output "Completed - Native binary: $nativeFile"
        }
    }
    finally {
        Pop-Location
    }
}
finally {
    Pop-Location
}
