cd $PSScriptRoot
$version = (Get-Command UVtools.WPF\bin\Release\netcoreapp3.1\UVtools.dll).FileVersionInfo.FileVersion
echo "UVtools v$version"
Remove-Item "$PSScriptRoot\UVtools.WPF\bin\Release\netcoreapp3.1\Assets\usersettings.xml" -Recurse -ErrorAction Ignore

Add-Type -A System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory("$PSScriptRoot\UVtools.WPF\bin\Release\netcoreapp3.1", "$PSScriptRoot\UVtools.WPF\bin\UVtools_v$version.zip")

Copy-Item "$PSScriptRoot\UVtools.Installer\bin\Release\UVtools.msi" -Destination "$PSScriptRoot\UVtools.WPF\bin\UVtools_v$version.msi"