# When using System.IO.Compression.ZipFile.CreateFromDirectory in PowerShell, it still uses backslashes in the zip paths
# despite this https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/mitigation-ziparchiveentry-fullname-path-separator

# Based upon post by Seth Jackson https://sethjackson.github.io/2016/12/17/path-separators/

#
# PowerShell 5 (WMF5) & 6
# Using class Keyword https://msdn.microsoft.com/powershell/reference/5.1/Microsoft.PowerShell.Core/about/about_Classes
#
# https://gist.github.com/lantrix/738ebfa616d5222a8b1db947793bc3fc
#

Add-Type -AssemblyName System.Text.Encoding
Add-Type -AssemblyName System.IO.Compression.FileSystem

class FixedEncoder : System.Text.UTF8Encoding {
    FixedEncoder() : base($true) { }

    [byte[]] GetBytes([string] $s)
    {
        $s = $s.Replace("\", "/");
        return ([System.Text.UTF8Encoding]$this).GetBytes($s);
    }
}


cd $PSScriptRoot
$version = (Get-Command UVtools.WPF\bin\Release\netcoreapp3.1\UVtools.dll).FileVersionInfo.ProductVersion
echo "UVtools v$version"

#[IO.Compression.ZipFile]::CreateFromDirectory("$PSScriptRoot\UVtools.WPF\bin\Release\netcoreapp3.1", "$PSScriptRoot\UVtools.WPF\bin\UVtools_v$version.zip")
[System.IO.Compression.ZipFile]::CreateFromDirectory("$PSScriptRoot\UVtools.WPF\bin\Release\netcoreapp3.1", "$PSScriptRoot\UVtools.WPF\bin\UVtools_v$version.zip", [System.IO.Compression.CompressionLevel]::Optimal, $false, [FixedEncoder]::new())
Copy-Item "$PSScriptRoot\UVtools.Installer\bin\Release\UVtools.msi" -Destination "$PSScriptRoot\UVtools.WPF\bin\UVtools_v$version.msi"