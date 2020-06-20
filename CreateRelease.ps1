cd $PSScriptRoot
$version = (Get-Command UVtools.GUI\bin\Release\UVtools.Parser.dll).FileVersionInfo.ProductVersion

Add-Type -A System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory("$PSScriptRoot\UVtools.GUI\bin\Release", "$PSScriptRoot\UVtools.GUI\bin\UVtools_v$version.zip")

Copy-Item "$PSScriptRoot\UVtools.Installer\bin\Release\UVtools.msi" -Destination "$PSScriptRoot\UVtools.GUI\bin\UVtools_v$version.msi"