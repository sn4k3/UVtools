[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string] $Command = 'uninstall',

    [Alias('h')]
    [switch] $Help
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ApplicationName = 'UVtools'
$ApplicationSlug = 'uvtools'
$WinGetPackageId = ''
$PackageTypes = @(
    'windows-installer'
    'dotnet-single-file'
    'portable'
)
$ScriptName = if ([string]::IsNullOrWhiteSpace($MyInvocation.MyCommand.Name)) {
    'uninstall.ps1'
} else {
    $MyInvocation.MyCommand.Name
}
$script:RemovedAny = $false
$script:FailedAny = $false

function Show-Help {
    Write-Host 'Usage:'
    Write-Host "  .\$ScriptName"
    Write-Host "  .\$ScriptName uninstall"
    Write-Host "  .\$ScriptName -Help"
    Write-Host ''
    Write-Host 'Removes every detected WinGet, registered installer, single-file, and Portable installation.'
}

function Register-Removed([string] $Description) {
    $script:RemovedAny = $true
    Write-Host "Removed $Description."
}

function Register-Failure([string] $Description, [string] $Message) {
    $script:FailedAny = $true
    Write-Warning "Could not remove $Description. $Message"
}

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Start-RemovalProcess(
    [string] $FilePath,
    [string[]] $ArgumentList,
    [string] $Description,
    [int[]] $SuccessExitCodes = @(0)
) {
    $parameters = @{
        FilePath = $FilePath
        ArgumentList = $ArgumentList
        Wait = $true
        PassThru = $true
    }
    if (-not (Test-IsAdministrator)) {
        $parameters.Verb = 'RunAs'
    }

    try {
        $process = Start-Process @parameters
        if ($process.ExitCode -in $SuccessExitCodes) {
            Register-Removed $Description
            return
        }

        Register-Failure $Description "The uninstaller exited with code $($process.ExitCode)."
    } catch {
        Register-Failure $Description $_.Exception.Message
    }
}

function Uninstall-WithWinGet {
    if ([string]::IsNullOrWhiteSpace($WinGetPackageId)) {
        return
    }

    $winGetCommand = Get-Command -Name 'winget.exe' -CommandType Application `
        -ErrorAction SilentlyContinue
    if ($null -eq $winGetCommand) {
        return
    }

    $commonArguments = @(
        '--id'
        $WinGetPackageId
        '--exact'
        '--source'
        'winget'
        '--accept-source-agreements'
        '--disable-interactivity'
    )
    try {
        & $winGetCommand.Source 'list' @commonArguments 2>$null | Out-Null
        $listExitCode = $LASTEXITCODE
    } catch {
        return
    }
    if ($listExitCode -ne 0) {
        return
    }

    try {
        & $winGetCommand.Source 'uninstall' @commonArguments '--silent' | Out-Host
        $uninstallExitCode = $LASTEXITCODE
        if ($uninstallExitCode -eq 0) {
            Register-Removed "WinGet package $WinGetPackageId"
        } else {
            Register-Failure "WinGet package $WinGetPackageId" `
                "WinGet exited with code $uninstallExitCode."
        }
    } catch {
        Register-Failure "WinGet package $WinGetPackageId" $_.Exception.Message
    }
}

function Get-RegisteredUninstallEntries {
    $registryPaths = @(
        'Registry::HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\*'
        'Registry::HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Uninstall\*'
        'Registry::HKEY_LOCAL_MACHINE\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*'
    )

    $entries = foreach ($registryPath in $registryPaths) {
        Get-ItemProperty -Path $registryPath -ErrorAction SilentlyContinue |
            Where-Object {
                $displayNameProperty = $_.PSObject.Properties['DisplayName']
                $null -ne $displayNameProperty -and
                    $displayNameProperty.Value -eq $ApplicationName
            }
    }
    return @($entries | Sort-Object -Property PSChildName -Unique)
}

function Uninstall-RegisteredInstallers {
    foreach ($entry in @(Get-RegisteredUninstallEntries)) {
        $productCode = [string] $entry.PSChildName
        $windowsInstallerProperty = $entry.PSObject.Properties['WindowsInstaller']
        if ($null -ne $windowsInstallerProperty -and
            $windowsInstallerProperty.Value -eq 1 -and
            $productCode -match '^\{[0-9A-Fa-f-]{36}\}$') {
            Start-RemovalProcess `
                -FilePath "$env:SystemRoot\System32\msiexec.exe" `
                -ArgumentList @('/x', $productCode, '/passive', '/norestart') `
                -Description "Windows Installer package $productCode" `
                -SuccessExitCodes @(0, 1605, 1614, 1641, 3010)
            continue
        }

        $quietUninstallProperty = $entry.PSObject.Properties['QuietUninstallString']
        $uninstallProperty = $entry.PSObject.Properties['UninstallString']
        $uninstallCommand = if ($null -ne $quietUninstallProperty -and
            -not [string]::IsNullOrWhiteSpace($quietUninstallProperty.Value)) {
            [string] $quietUninstallProperty.Value
        } elseif ($null -ne $uninstallProperty) {
            [string] $uninstallProperty.Value
        } else {
            [string]::Empty
        }
        if ([string]::IsNullOrWhiteSpace($uninstallCommand)) {
            Register-Failure "registered installer $productCode" `
                'No uninstall command was registered.'
            continue
        }

        Start-RemovalProcess `
            -FilePath $env:ComSpec `
            -ArgumentList @('/d', '/s', '/c', $uninstallCommand) `
            -Description "registered installer $productCode"
    }
}

function Test-PathWithin([string] $Candidate, [string] $Root) {
    try {
        $candidatePath = [IO.Path]::GetFullPath(
            [Environment]::ExpandEnvironmentVariables($Candidate.Trim().Trim('"'))).TrimEnd('\')
        $rootPath = [IO.Path]::GetFullPath($Root).TrimEnd('\')
        return [string]::Equals($candidatePath, $rootPath,
            [StringComparison]::OrdinalIgnoreCase) -or
            $candidatePath.StartsWith("$rootPath\", [StringComparison]::OrdinalIgnoreCase)
    } catch {
        return $false
    }
}

function Remove-UserPathEntries([string] $InstallDirectory) {
    $userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
    if ([string]::IsNullOrWhiteSpace($userPath)) {
        return
    }

    $entries = @($userPath -split ';' |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $retainedEntries = @($entries |
        Where-Object { -not (Test-PathWithin $_ $InstallDirectory) })
    if ($retainedEntries.Count -eq $entries.Count) {
        return
    }

    try {
        [Environment]::SetEnvironmentVariable('Path', ($retainedEntries -join ';'), 'User')
        Register-Removed 'user PATH entries'
    } catch {
        Register-Failure 'user PATH entries' $_.Exception.Message
    }
}

function Remove-LocalInstallation {
    $installDirectory = Join-Path $env:LOCALAPPDATA "Programs\$ApplicationSlug"
    Remove-UserPathEntries $installDirectory
    if (-not (Test-Path -LiteralPath $installDirectory)) {
        return
    }

    try {
        Remove-Item -LiteralPath $installDirectory -Recurse -Force
        Register-Removed 'local Portable or single-file installation'
    } catch {
        Register-Failure 'local Portable or single-file installation' $_.Exception.Message
    }
}

if ($Help -or $Command -in 'help', '-h', '--help', '/help', '/?') {
    Show-Help
    return
}
if ($Command -ne 'uninstall') {
    Write-Host "Error: unknown command '$Command'." -ForegroundColor Red
    Write-Host "Run .\$ScriptName -Help for usage."
    exit 1
}

foreach ($packageType in $PackageTypes) {
    switch ($packageType) {
        'windows-installer' {
            Uninstall-WithWinGet
            Uninstall-RegisteredInstallers
        }
        'dotnet-single-file' { Remove-LocalInstallation }
        'portable' { Remove-LocalInstallation }
    }
}

if (-not $script:RemovedAny) {
    Write-Host "No $ApplicationName installations were found."
} elseif (-not $script:FailedAny) {
    Write-Host "$ApplicationName was uninstalled successfully."
}
if ($script:FailedAny) {
    exit 1
}
