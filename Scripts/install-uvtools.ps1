[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string] $Command = 'install',

    [Parameter(Position = 1)]
    [Alias('v')]
    [string] $Version = 'latest',

    [Alias('h')]
    [switch] $Help,

    [Alias('l')]
    [switch] $List,

    [Alias('list-changelog')]
    [switch] $ListChangelog,

    [ValidateRange(1, 2147483647)]
    [int] $ChangelogLimit = 20
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Repository = 'sn4k3/UVtools'
$ApplicationName = 'UVtools'
$ApplicationSlug = 'uvtools'
$ExecutableName = 'UVtools.exe'
$SingleFileName = 'UVtools.exe'
$WinGetPackageId = 'PTRTECH.UVtools'
$PackageTypes = @(
    'windows-installer'
    'portable'
)
$ScriptName = if ([string]::IsNullOrWhiteSpace($MyInvocation.MyCommand.Name)) {
    'install.ps1'
} else {
    $MyInvocation.MyCommand.Name
}

function Show-Header {
    Write-Host ''
    Write-Host '============================================================'
    Write-Host " $ApplicationName installer"
    Write-Host '============================================================'
    Write-Host 'Usage:'
    Write-Host "  .\$ScriptName [install] [latest|VERSION]"
    Write-Host "  .\$ScriptName -Version VERSION"
    Write-Host "  .\$ScriptName -List"
    Write-Host "  .\$ScriptName -ListChangelog [-ChangelogLimit LIMIT]"
    Write-Host "  .\$ScriptName help"
    Write-Host 'Commands:'
    Write-Host '  install [VERSION]   Install or downgrade to the latest or selected version.'
    Write-Host '  list                Show the available published release versions.'
    Write-Host '  list-changelog      Show release changelogs (default: 20 versions).'
    Write-Host '  help                Show detailed help and examples.'
    Write-Host 'Options:'
    Write-Host '  -Version VERSION    Pick a release version, including an older version.'
    Write-Host '  -List               Show the available published release versions.'
    Write-Host '  -ListChangelog      Show release changelogs (default: 20 versions).'
    Write-Host '  -ChangelogLimit N   Limit the number of changelog versions.'
    Write-Host '  -Help                Show detailed help.'
    Write-Host '============================================================'
    Write-Host ''
}

function Show-Help {
    Show-Header
    Write-Host 'The installer selects the best compatible Windows asset published for this system.'
    Write-Host 'Selecting an older release installs or downgrades to that version when the package permits it.'
    Write-Host ''
    Write-Host 'Examples:'
    Write-Host "  .\$ScriptName"
    Write-Host "  .\$ScriptName install v1.2.3"
    Write-Host "  .\$ScriptName -Version 1.2.3"
    Write-Host "  .\$ScriptName -List"
    Write-Host "  .\$ScriptName -ListChangelog"
    Write-Host "  .\$ScriptName -ListChangelog -ChangelogLimit 5"
    Write-Host "  .\$ScriptName help"
}

function Stop-Install([string] $Message) {
    Write-Host "Error: $Message" -ForegroundColor Red
    Write-Host "Run .\$ScriptName help for usage."
    exit 1
}

function Install-WithWinGet {
    if ([string]::IsNullOrWhiteSpace($WinGetPackageId)) {
        return $false
    }

    $winGetCommand = Get-Command -Name 'winget.exe' -CommandType Application `
        -ErrorAction SilentlyContinue
    if ($null -eq $winGetCommand) {
        Write-Host 'WinGet is unavailable; falling back to the GitHub release asset.'
        return $false
    }

    $winGetArguments = @(
        'install'
        '--id'
        $WinGetPackageId
        '--exact'
        '--source'
        'winget'
        '--silent'
        '--accept-package-agreements'
        '--accept-source-agreements'
        '--disable-interactivity'
    )
    if ($Version -ne 'latest') {
        $winGetArguments += @('--version', ($Version -replace '^[vV]', ''), '--force')
    }

    Write-Host "Installing $ApplicationName with WinGet..."
    try {
        & $winGetCommand.Source @winGetArguments | Out-Host
        $winGetExitCode = $LASTEXITCODE
    } catch {
        Write-Host "WinGet could not install $ApplicationName. Falling back to the GitHub release asset."
        return $false
    }

    if ($winGetExitCode -eq 0) {
        Write-Host "$ApplicationName was installed successfully with WinGet."
        return $true
    }

    Write-Host "WinGet exited with code $winGetExitCode. Falling back to the GitHub release asset."
    return $false
}

function Get-Architecture {
    $processorArchitecture = if ([string]::IsNullOrWhiteSpace($env:PROCESSOR_ARCHITEW6432)) {
        $env:PROCESSOR_ARCHITECTURE
    } else {
        $env:PROCESSOR_ARCHITEW6432
    }

    switch ($processorArchitecture) {
        { $_ -in 'AMD64', 'x86_64' } { return 'x64' }
        { $_ -in 'ARM64', 'AARCH64' } { return 'arm64' }
        default { Stop-Install "Unsupported architecture: $processorArchitecture" }
    }
}

function Resolve-Arguments {
    if ($Help -or $Command -in 'help', '-h', '--help') {
        return [pscustomobject]@{
            Action = 'help'
            Version = 'latest'
        }
    }

    if ($List -or $Command -in 'list', '-l', '--list') {
        return [pscustomobject]@{
            Action = 'list'
            Version = 'latest'
        }
    }

    if ($ListChangelog -or $Command -in 'list-changelog', '--list-changelog') {
        return [pscustomobject]@{
            Action = 'list-changelog'
            Version = 'latest'
        }
    }

    if ($Command -eq 'install') {
        return [pscustomobject]@{
            Action = 'install'
            Version = $Version
        }
    }

    if ($Version -eq 'latest') {
        return [pscustomobject]@{
            Action = 'install'
            Version = $Command
        }
    }

    Stop-Install "Unknown command: $Command"
}

function Test-HttpNotFound($ErrorRecord) {
    try {
        if ([int]$ErrorRecord.Exception.Response.StatusCode -eq 404) {
            return $true
        }
    } catch {}

    try {
        return [int]$ErrorRecord.Exception.StatusCode -eq 404
    } catch {}

    return $false
}

function Show-AvailableVersions {
    $headers = @{
        Accept = 'application/vnd.github+json'
        'X-GitHub-Api-Version' = '2022-11-28'
    }
    $versions = @()
    $page = 1
    do {
        $releaseUri = "https://api.github.com/repos/$Repository/releases?per_page=100&page=$page"
        try {
            $releaseResponse = Invoke-RestMethod -Uri $releaseUri -Headers $headers
            $releases = @($releaseResponse)
        } catch {
            Stop-Install "Unable to retrieve available versions from $Repository. $($_.Exception.Message)"
        }

        $versions += @($releases | ForEach-Object { $_.tag_name } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        $page++
    } while ($releases.Count -eq 100)

    if ($versions.Count -eq 0) {
        Stop-Install "No published release versions were found for $Repository."
    }

    Write-Host "Available versions for ${ApplicationName}:"
    foreach ($availableVersion in $versions) {
        Write-Host "  $availableVersion"
    }
}

function Show-ReleaseChangelogs([int] $Limit) {
    $headers = @{
        Accept = 'application/vnd.github+json'
        'X-GitHub-Api-Version' = '2022-11-28'
    }
    $availableReleases = @()
    $page = 1
    $pageSize = [Math]::Min(100, $Limit)
    do {
        $releaseUri = "https://api.github.com/repos/$Repository/releases?per_page=$pageSize&page=$page"
        try {
            $releaseResponse = Invoke-RestMethod -Uri $releaseUri -Headers $headers
            $releases = @($releaseResponse)
        } catch {
            Stop-Install "Unable to retrieve release changelogs from $Repository. $($_.Exception.Message)"
        }

        $pageReleases = @($releases | Where-Object {
            -not [string]::IsNullOrWhiteSpace($_.tag_name)
        })
        $remaining = $Limit - $availableReleases.Count
        $availableReleases += @($pageReleases | Select-Object -First $remaining)
        $page++
    } while ($releases.Count -eq $pageSize -and $availableReleases.Count -lt $Limit)

    if ($availableReleases.Count -eq 0) {
        Stop-Install "No published release changelogs were found for $Repository."
    }

    Write-Host "Published changelog for ${ApplicationName}:"
    foreach ($release in $availableReleases) {
        $availableVersion = $release.tag_name -replace '^[vV]', ''
        Write-Host ''
        Write-Host "# $availableVersion"
        Write-Host ''
        if ([string]::IsNullOrWhiteSpace($release.body)) {
            Write-Host 'No changelog provided.'
        } else {
            Write-Host $release.body
        }
    }
}

function Get-Release {
    $headers = @{
        Accept = 'application/vnd.github+json'
        'X-GitHub-Api-Version' = '2022-11-28'
    }

    if ($Version -eq 'latest') {
        $releaseUri = "https://api.github.com/repos/$Repository/releases/latest"
    } else {
        if ($Version -notmatch '^v?[0-9]+(\.[0-9]+){1,3}([-+][0-9A-Za-z.-]+)?$') {
            Stop-Install "Invalid version '$Version'. Use latest or a release tag such as v1.2.3."
        }

        $escapedVersion = [Uri]::EscapeDataString($Version)
        $releaseUri = "https://api.github.com/repos/$Repository/releases/tags/$escapedVersion"
    }

    try {
        return Invoke-RestMethod -Uri $releaseUri -Headers $headers
    } catch {
        $initialError = $_
        if ($Version -eq 'latest') {
            Stop-Install "Unable to resolve the latest release from $Repository. $($_.Exception.Message)"
        }

        if (-not $Version.StartsWith('v', [StringComparison]::OrdinalIgnoreCase)) {
            $escapedVersion = [Uri]::EscapeDataString("v$Version")
            $releaseUri = "https://api.github.com/repos/$Repository/releases/tags/$escapedVersion"
            try {
                return Invoke-RestMethod -Uri $releaseUri -Headers $headers
            } catch {
                if ((Test-HttpNotFound $initialError) -or (Test-HttpNotFound $_)) {
                    Stop-Install "Release version '$Version' was not found in $Repository."
                }

                Stop-Install "Unable to resolve release '$Version' from $Repository. $($_.Exception.Message)"
            }
        }

        if (Test-HttpNotFound $initialError) {
            Stop-Install "Release version '$Version' was not found in $Repository."
        }

        Stop-Install "Unable to resolve release '$Version' from $Repository. $($initialError.Exception.Message)"
    }
}

function Find-Asset($Release, [string] $PackageType, [string] $Architecture) {
    $runtimeIdentifier = "win-$Architecture"
    $extensions = switch ($PackageType) {
        'windows-installer' { '.msi', '.exe' }
        'dotnet-single-file' { '.exe' }
        'portable' { '.zip' }
        default { @() }
    }

    foreach ($extension in $extensions) {
        $asset = @($Release.assets) |
            Where-Object { $_.name -like "*$runtimeIdentifier*$extension" } |
            Select-Object -First 1
        if ($null -ne $asset) {
            return [pscustomobject]@{
                PackageType = $PackageType
                Asset = $asset
            }
        }
    }

    return $null
}

function Select-Asset($Release, [string] $Architecture) {
    foreach ($packageType in $PackageTypes) {
        $selection = Find-Asset $Release $packageType $Architecture
        if ($null -ne $selection) {
            return $selection
        }
    }

    Stop-Install "No selected package is available for win-$Architecture."
}

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Start-Installer([string] $Path) {
    $extension = [IO.Path]::GetExtension($Path)
    if ($extension -ieq '.msi') {
        $parameters = @{
            FilePath = "$env:SystemRoot\System32\msiexec.exe"
            ArgumentList = @('/i', "`"$Path`"", '/passive', '/norestart')
            Wait = $true
            PassThru = $true
        }
    } else {
        $parameters = @{
            FilePath = $Path
            Wait = $true
            PassThru = $true
        }
    }

    if (-not (Test-IsAdministrator)) {
        $parameters.Verb = 'RunAs'
    }

    $process = Start-Process @parameters
    if ($process.ExitCode -notin 0, 1641, 3010) {
        Stop-Install "The Windows installer exited with code $($process.ExitCode)."
    }
}

function Add-ToUserPath([string] $Directory) {
    $userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
    $entries = @($userPath -split ';' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $alreadyPresent = $entries | Where-Object {
        [string]::Equals($_.TrimEnd('\'), $Directory.TrimEnd('\'),
            [StringComparison]::OrdinalIgnoreCase)
    }
    if ($null -eq $alreadyPresent) {
        $newPath = (@($entries) + $Directory) -join ';'
        [Environment]::SetEnvironmentVariable('Path', $newPath, 'User')
    }

    if ($env:Path -notlike "*$Directory*") {
        $env:Path = "$Directory;$env:Path"
    }
}

function Install-SingleFile([string] $Path) {
    $destination = Join-Path $env:LOCALAPPDATA "Programs\$ApplicationSlug"
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Copy-Item -LiteralPath $Path -Destination (Join-Path $destination $SingleFileName) -Force
    Add-ToUserPath $destination
}

function Install-Portable([string] $Path) {
    $destination = Join-Path $env:LOCALAPPDATA "Programs\$ApplicationSlug"
    $stagingDirectory = "$destination.new"
    Remove-Item -LiteralPath $stagingDirectory -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $stagingDirectory -Force | Out-Null
    Expand-Archive -LiteralPath $Path -DestinationPath $stagingDirectory -Force
    $executable = Get-ChildItem -LiteralPath $stagingDirectory -Filter $ExecutableName -File -Recurse |
        Select-Object -First 1
    if ($null -eq $executable) {
        Stop-Install "The archive does not contain $ExecutableName."
    }

    Remove-Item -LiteralPath $destination -Recurse -Force -ErrorAction SilentlyContinue
    Move-Item -LiteralPath $stagingDirectory -Destination $destination
    $relativeDirectory = $executable.DirectoryName.Substring($stagingDirectory.Length).TrimStart('\')
    $executableDirectory = if ([string]::IsNullOrWhiteSpace($relativeDirectory)) {
        $destination
    } else {
        Join-Path $destination $relativeDirectory
    }
    Add-ToUserPath $executableDirectory
}

$ResolvedArguments = Resolve-Arguments
$Version = $ResolvedArguments.Version
if ($ResolvedArguments.Action -eq 'help') {
    Show-Help
    return
}

Show-Header
if ($ResolvedArguments.Action -eq 'list') {
    Show-AvailableVersions
    return
}
if ($ResolvedArguments.Action -eq 'list-changelog') {
    Show-ReleaseChangelogs -Limit $ChangelogLimit
    return
}

if (Install-WithWinGet) {
    return
}

$Architecture = Get-Architecture
$Release = Get-Release
$Selection = Select-Asset $Release $Architecture
$TemporaryDirectory = Join-Path ([IO.Path]::GetTempPath()) "stagekit-$([Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $TemporaryDirectory | Out-Null

try {
    $AssetFile = Join-Path $TemporaryDirectory $Selection.Asset.name
    Write-Host "Downloading $ApplicationName ($($Selection.PackageType))..."
    Invoke-WebRequest -Uri $Selection.Asset.browser_download_url -OutFile $AssetFile -UseBasicParsing

    switch ($Selection.PackageType) {
        'windows-installer' { Start-Installer $AssetFile }
        'dotnet-single-file' { Install-SingleFile $AssetFile }
        'portable' { Install-Portable $AssetFile }
        default { Stop-Install "Unsupported package type: $($Selection.PackageType)" }
    }

    Write-Host "$ApplicationName was installed successfully."
} finally {
    Remove-Item -LiteralPath $TemporaryDirectory -Recurse -Force -ErrorAction SilentlyContinue
}
