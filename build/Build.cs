using System;
using System.Linq;
using System.Text.RegularExpressions;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Utilities.Collections;
using Serilog;
using StageKit.Fallout;
using StageKit.Runtime;
using UVtools.Core.FileFormats;

namespace build;

public sealed partial class Build : StageKitBuild
{
    public Build()
    {
        DependOnTargets =
        [
            ImportPsProfiles
        ];

        PackagingTypes =
        [
            ApplicationPackagingType.Portable,
            ApplicationPackagingType.WindowsInstaller,
            ApplicationPackagingType.LinuxAppImage,
            ApplicationPackagingType.LinuxDeb,
            ApplicationPackagingType.LinuxRpm,
            ApplicationPackagingType.LinuxArchPackage,
            ApplicationPackagingType.MacOSAppBundle
        ];

        WindowsInstallScriptWinGetPackageId = "PTRTECH.UVtools";

        FileAssociations.UnionWith(FileFormat.AllFileExtensions
            .Where(extension =>
                !extension.IsVirtual
                && !extension.Equals("zip")
                && extension.GetFileFormat() is not ImageFile
            )
            .Select(extension => new FileAssociation(extension.Extension, extension.Description)));

        BeforePublishRid = context =>
            Log.Information("Publishing {Rid} to {Path}",
                context.RuntimeIdentifier, context.PublishPath);

        AfterPublishRid = context =>
        {
            Log.Information("Published {Rid} to {Path}",
                context.RuntimeIdentifier, context.PublishPath);
        };
    }

    public override AbsolutePath MediaDirectory => RootDirectory / "UVtools.CAD";

    public Target ImportPsProfiles => _ => _
        .Executes(() =>
        {
            var psFolder =
                AbsolutePath.Create(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)) /
                "PrusaSlicer";
            var psFolderPrinter = psFolder / "printer";
            var psFolderPrint = psFolder / "sla_print";

            var outputFolder = RootDirectory / "PrusaSlicer";

            if (psFolderPrinter.DirectoryExists())
            {
                psFolderPrinter.GlobFiles("*.ini").ForEach(file =>
                {
                    var content = file.ReadAllText();
                    if (SlaPrinterRegex().IsMatch(content))
                    {
                        var destFolder = outputFolder / "printer";
                        file.CopyToDirectory(destFolder, ExistsPolicy.FileOverwriteIfNewer);
                        //Log.Information("Copied {file} to {destFile}", file.Name, destFolder);
                    }
                });
            }
            else
            {
                Log.Warning("Skipping PrusaSlicer printer profile import. Directory not found: {Path}",
                    psFolderPrinter);
            }

            if (psFolderPrint.DirectoryExists())
            {
                psFolderPrint.CopyToDirectory(outputFolder, ExistsPolicy.MergeAndOverwriteIfNewer);
            }
            else
            {
                Log.Warning("Skipping PrusaSlicer SLA print profile import. Directory not found: {Path}",
                    psFolderPrint);
            }
        });

    protected override WindowsInstallerOptions CreateWindowsInstallerOptions()
    {
        var options = base.CreateWindowsInstallerOptions();
        options.SyncContextMenuOpenWithFileAssociations();
        options.InstallerScope = InstallerScope.PerMachine;
        options.PathRegistration = PathRegistration.UserDefaultYes;
        return options;
    }

    /// <inheritdoc />
    protected override LinuxAppBundleOptions CreateLinuxAppBundleOptions()
    {
        var options = base.CreateLinuxAppBundleOptions();
        options.MinimumDisplayLength = 1024;
        options.SnapStagePackages.Add("libfontconfig1");
        options.AppRunScriptBeforeExec = $$"""
                                           function help() {
                                               cat <<'EOF'
                                            _   ___     ___              _     
                                           | | | \ \   / / |_ ___   ___ | |___ 
                                           | | | |\ \ / /| __/ _ \ / _ \| / __|
                                           | |_| | \ V / | || (_) | (_) | \__ \
                                            \___/   \_/   \__\___/ \___/|_|___/

                                           --------------------------------------------------------------------------
                                              All the great {{SoftwareName}} functionality inside an AppImage package.
                                           --------------------------------------------------------------------------
                                           (This package uses the AppImage software packaging technology for Linux
                                            ['One App == One File'] for easy availability of the newest {{SoftwareName}}
                                            releases across all major Linux distributions.)
                                            Usage:  --help, -h
                                            ------     # This message
                                                    <path/to/file1> [path/to/file2] [path/to/file3] [...]
                                                       # Opens and loads specific file(s) with UVtools
                                                    --cmd-help
                                                       # Display UVtoolsCmd help message
                                                    --cmd, -c <argument(s)> [option(s)]
                                                       # Redirect a command to UVtoolsCmd
                                                    --appimage-extract
                                                       # Unpack this AppImage into a local sub-directory [currently named 'squashfs-root']
                                                    --appimage-help
                                                       # Show available AppImage options
                                           EOF
                                           }

                                           if [[ "${1:-}" == "--help" || "${1:-}" == "-h" ]]; then
                                               help
                                               exit 0
                                           fi

                                           if [[ "${1:-}" == "--cmd-help" ]]; then
                                               exec 'UVtoolsCmd' --help
                                               exit $?
                                           fi

                                           if [[ "${1:-}" == "--cmd" || "${1:-}" == "-c" ]]; then
                                               if [ "$#" -lt 2 ]; then
                                                    echo 'UVtoolsCmd requires at least one parameter'
                                            		exec 'UVtoolsCmd' --help
                                                    exit $?
                                               fi

                                               shift
                                           	   exec 'UVtoolsCmd' "$@"
                                               exit $?
                                           fi

                                           """;

        return options;
    }

    public static int Main() => Execute<Build>(x => x.Compile);

    [GeneratedRegex("printer_technology.*=.*(SLA)")]
    private static partial Regex SlaPrinterRegex();
}