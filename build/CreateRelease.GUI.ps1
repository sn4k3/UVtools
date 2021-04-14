cd $PSScriptRoot
cd ..
$version = (Get-Command UVtools.GUI\bin\Release\UVtools.Core.dll).FileVersionInfo.FileVersion

Remove-Item "UVtools.GUI\bin\Release\Logs" -Recurse -ErrorAction Ignore

Add-Type -A System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory("UVtools.GUI\bin\Release", "UVtools.GUI\bin\UVtools_v$version.zip")

Copy-Item "UVtools.Installer\bin\Release\UVtools.msi" -Destination "UVtools.GUI\bin\UVtools_v$version.msi"