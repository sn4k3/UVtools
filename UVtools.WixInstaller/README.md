# Installer

`UVtools.WixInstaller.wixproj` was scaffolded by the Fallout `GenerateWindowsWixInstaller` target. The publish
pipeline builds it once per Windows runtime identifier and passes every value it needs:

| Property | Source |
| --- | --- |
| `PublishDirectory` | The staged, published payload for the runtime identifier |
| `ApplicationName` | `SoftwareName` |
| `ApplicationExecutableName` | `SoftwareExecutableFileNameWithoutExtension` |
| `BuildVersion` | `SoftwareVersion` |
| `OutputName` | The release asset name |
| `Platform` | `x64` or `arm64` |
| `Company` | `SoftwareCompany` |
| `Copyright` | `SoftwareCopyright` |
| `Description` | `SoftwareDescription` |
| `Keywords` | `SoftwareKeywords` |
| `RepositoryUrl` | `SoftwareRepositoryUrl` |
| `ApplicationIcon` | `WindowsIconFile` |

## Before the first release

1. Add this project to the solution so the pipeline discovers it.
2. Replace `Resources/License.rtf` with the real license text.
3. Replace the placeholder artwork in `Resources/`.
4. Keep the generated `UpgradeCode` values stable; changing one makes Windows treat future
   installers as a different product instead of an upgrade.

The installer lets users choose `INSTALLFOLDER` and records the selected directory, along with the current MSI
product code, software version, architecture, and executable path, under the installer's registry key. An
upgrade or repair restores the directory as the default unless the wizard or the command line supplies one.

A full uninstall deletes that key outright and leaves nothing behind, so a later fresh install starts from the
default directory rather than the one previously chosen.

## Installation scope

`InstallerScope` defaults to `perMachineOrUser`, which lets the user pick the scope and starts on a non-elevated
per-user installation. Set it to `perUser` or `perMachine` to enforce one scope and hide the selector. Windows
Installer keeps a product in the context it was first installed in, so the selector is disabled while a previous
installation is detected, and a silent installation is per-user unless `ALLUSERS=1` is passed:

```powershell
msiexec /i <package>.msi ALLUSERS=1 /qn
```

The shortcut and PATH choices of the previous installation are restored on silent and basic-UI installs too,
which never run the wizard.

## PATH registration

Set `InstallerPathRegistration` in this project or on the command line to configure PATH registration:

| Value | Behavior |
| --- | --- |
| *(blank)* | Disable PATH registration and hide the option. |
| `Register` | Always append the install directory to PATH and hide the option. |
| `UserDefaultNo` | Show an unchecked option for the user. |
| `UserDefaultYes` | Show a checked option for the user. |

Per-user installations update the current user's PATH; per-machine installations update the system PATH. Which
of the two applies follows the installation context Windows Installer resolved, not the scope picked in the
wizard, because a per-user installation cannot write the system PATH. The selected option is restored on
upgrades, and the install directory owned by this MSI is removed from PATH during uninstall without removing
unrelated PATH entries. The entry is the installation directory as Windows Installer resolves it, so it carries
a trailing separator.

## Context menu ("Open with")

Set `ContextMenuOpenWithFileAssociations` in this project, on the command line, or through Fallout's
`WindowsInstallerOptions.ContextMenuOpenWithFileAssociations` to register the application in the Windows Explorer right-click
context menu for specified file types or extensions:

```xml
<ContextMenuOpenWithFileAssociations>.sl1;.sl1s;*.zip;*.photon</ContextMenuOpenWithFileAssociations>
```

Leave it blank to omit context menu registration. Specify `*` to show the context menu for all files.

## Authenticode signing

Set `AuthenticodeCertificateThumbprint` to the SHA-1 thumbprint of a code-signing certificate in the current
user's certificate store. Every `.exe` and `.dll` in the payload is signed, which for a self-contained publish
replaces the signatures the .NET runtime binaries ship with. Redefine the `InstallerPayloadToSign` item to
narrow that set, for example to `$(PublishDirectory)\$(ApplicationExecutableName).exe` for the entry executable
alone. Each file costs one timestamped `signtool` invocation, so a self-contained payload of a few hundred
assemblies adds several minutes to a signed release build.

## Installer artwork

| Property | Image size | Layout |
| --- | --- | --- |
| `InstallerDialogImage` | 493 × 312 pixels | Welcome and completion background. Place artwork in the leftmost 164 pixels; keep the right side clear for wizard text. |
| `InstallerBannerImage` | 493 × 58 pixels | Header on subsequent pages, including installation options. Place the logo at the right edge and keep the left side clear for titles. |

Use BMP or PNG images. Each property is optional; omitting it retains the corresponding WiX default,
and a configured missing file fails the build.

See the [WiX artwork documentation](https://docs.firegiant.com/wix/tools/wixext/wixui/#replacing-the-default-bitmaps).
