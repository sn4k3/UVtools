using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Solutions;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.DotNet;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;
using Serilog;
using StageKit.Fallout;
using StageKit.Runtime;
using UVtools.Core.FileFormats;
using static Fallout.Common.EnvironmentInfo;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

namespace build;

public partial class Build : StageKitBuild
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
        
        BeforePublishRid = context =>
            Log.Information("Publishing {Rid} to {Path}",
                context.RuntimeIdentifier, context.PublishPath);

        AfterPublishRid = context =>
        {
            Log.Information("Published {Rid} to {Path}",
                context.RuntimeIdentifier, context.PublishPath);
        };
    }
    
    /// <inheritdoc />
    protected override LinuxAppBundleOptions CreateLinuxAppBundleOptions()
    {
        var options = base.CreateLinuxAppBundleOptions();
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
                                            "
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

    /*public Target Publish => _ => _
        //.OnlyWhenStatic(() => Configuration == Configuration.Release)
        .DependsOn(Restore, ImportPsProfiles)
        .Executes(() =>
        {
            var publishPaths = new Dictionary<string, AbsolutePath>();
            // Clean previous publishes
            foreach (var rid in RIds)
            {
                var path = PublishDirectory / $"{SoftwareName}_{rid}_v{SoftwareVersion}";
                publishPaths.Add(rid, path);
                path.DeleteDirectory();
            }

            if (ChangelogFile.Exists())
            {
                StringBuilder sb = new();
                var foundHashTag = false;
                TextReader tr = new StreamReader(ChangelogFile);
                while (tr.ReadLine() is { } line)
                {
                    line = line.TrimEnd();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    if (line.StartsWith("##"))
                    {
                        if (!foundHashTag)
                        {
                            foundHashTag = true;
                            continue;
                        }

                        break;
                    }

                    if (foundHashTag)
                    {
                        sb.AppendLine(line);
                    }
                }

                ReleaseNotesFile.WriteAllText(sb.ToString().TrimEnd());
            }

            DotNetPublish(_ => _
                .SetProject(MainProject)
                .SetConfiguration(Configuration)
                .CombineWith(RIds, (settings, rid) => settings
                    .SetSelfContained(true)
                    .SetPublishReadyToRun(true)
                    .SetRuntime(rid)
                    .SetOutput(publishPaths[rid])
                )
            );


            // Set executable permissions
            if (IsUnix)
            {
                foreach (var rid in RIds)
                {
                    if (rid.StartsWith("win")) continue;
                    (publishPaths[rid] / SoftwareName).SetExecutable();
                }
            }

            // Bundle previous publishes
            if (PublishCreateBundles)
            {
                foreach (var rid in RIds)
                {
                    var publishPath = publishPaths[rid];
                    var zipPath = publishPath + ".zip";
                    zipPath.DeleteFile();

                    var arch = "x64";
                    var archAlt = "x86_64";
                    var executableArch = arch;
                    var executableArchAlt = archAlt;

                    if (rid.EndsWith("-arm64"))
                    {
                        arch = "arm64";
                        archAlt = "aarch64";
                    }

                    if (RuntimeInformation.OSArchitecture is Architecture.Arm or Architecture.Arm64
                        or Architecture.Armv6)
                    {
                        executableArch = "arm64";
                        executableArchAlt = "aarch64";
                    }

                    var runtimeCacheFile = publishPath / BuildRuntimeCacheFileName;

                    var runtimeBuild = new BuildRuntime(rid, SoftwareVersion, true);
                    runtimeCacheFile.WriteJson(runtimeBuild);


                    if (!rid.StartsWith("osx"))
                    {
                        Log.Information("Compressing: {fileName}", zipPath.Name);
                        publishPath.ZipTo(zipPath, null, CompressionLevel.SmallestSize, FileMode.Create);
                    }

                    if (rid.StartsWith("win"))
                    {
                        if (IsWin)
                        {
                            runtimeBuild = runtimeBuild with { BundleType = BuildRuntime.BundleTypes.Installer };
                            runtimeCacheFile.WriteJson(runtimeBuild);
                            var fileExtensions = FileFormat.AllFileExtensions;
                            HashSet<string> extensionList = [];
                            foreach (var ext in fileExtensions)
                            {
                                if (ext.Extension.Contains('.')) continue; // Virtual extension, ignore
                                extensionList.Add($"System.FileName:&quot;*.{ext.Extension.ToLowerInvariant()}&quot;");
                            }

                            var msiProductFile = Solution.UVtools_Installer.Directory / "Code" / "Product.wxs";
                            var originalMsiProductFile = msiProductFile.ReadAllText();

                            try
                            {
                                if (extensionList.Count > 0)
                                {
                                    var regValue = string.Join(" OR ", extensionList);
                                    msiProductFile.WriteAllText(MsiAppliesToRegex().Replace(
                                        originalMsiProductFile,
                                        match => $"{match.Groups["A"].Value}{regValue}{match.Groups["B"].Value}"));
                                }

                                DotNetBuild(options => options
                                    .SetProjectFile(Solution.UVtools_Installer)
                                    .SetPlatform(arch)
                                    .SetConfiguration(Configuration)
                                    .SetOutputDirectory(PublishDirectory)
                                );
                            }
                            finally
                            {
                                if (extensionList.Count > 0)
                                {
                                    msiProductFile.WriteAllText(originalMsiProductFile);
                                }
                            }

                            (publishPath + ".wixpdb").DeleteFile();
                        }
                        else
                        {
                            Log.Warning(
                                "Skipping Windows MSI build on non-Windows platform. wix.exe not compatible with Linux yet.");
                        }
                    }
                    else if (rid.StartsWith("osx"))
                    {
                        if (PublishBundleWithMultipleArch)
                        {
                            if (IsUnix)
                            {
                                Log.Information("Bundling macOS multi-arch for {rid} app.", rid);
                                var macOSRootAppPath = PublishDirectory /
                                                       $"{SoftwareName}_osx-multiarch_v{SoftwareVersion}.app";
                                var macOSAppPath = macOSRootAppPath / $"{SoftwareName}.app";
                                var macOSAppContentsPath = macOSAppPath / "Contents";
                                var macOSMacOSBinPath = macOSAppContentsPath / "MacOS";
                                var macOSAppResourcesPath = macOSAppContentsPath / "Resources";
                                var macOSAppBinPath = macOSMacOSBinPath / rid;
                                var macOSAppEntryScriptPath = macOSMacOSBinPath / SoftwareName;
                                var macOSAppInfoPListFile = macOSAppContentsPath / "Info.plist";
                                var macOSAppEntitlementsFile = macOSAppContentsPath / $"{SoftwareName}.entitlements";
                                var icnsLogoFilePath = MediaDirectory / $"{SoftwareName}.icns";
                                macOSAppBinPath.CreateOrCleanDirectory();
                                macOSAppResourcesPath.CreateOrCleanDirectory();
                                icnsLogoFilePath.CopyToDirectory(macOSAppResourcesPath);

                                runtimeBuild = runtimeBuild with
                                {
                                    Runtime = "osx-multiarch", BundleType = BuildRuntime.BundleTypes.App
                                };
                                runtimeCacheFile.WriteJson(runtimeBuild);
                                publishPath.Copy(macOSAppBinPath, ExistsPolicy.MergeAndOverwrite);

                                macOSAppInfoPListFile.WriteAllText(MacAppBundle
                                    .GetInfoPList(SoftwareName, SoftwareRDNS, SoftwareVersion, SoftwareCopyright)
                                    .ReplaceLineEndings("\n"));
                                macOSAppEntitlementsFile.WriteAllText(
                                    MacAppBundle.Entitlements.ReplaceLineEndings("\n"));
                                macOSAppEntryScriptPath.WriteAllText(MacAppBundle.GetMultiArchEntryScript(SoftwareName)
                                    .ReplaceLineEndings("\n"));
                                macOSAppEntryScriptPath.SetExecutable();
                            }
                            else
                            {
                                Log.Warning("Skipping multi-arch bundle, non unix OS.");
                            }
                        }
                        else
                        {
                            Log.Information("Bundling macOS {rid} app.", rid);
                            var macOSRootAppPath = publishPath + ".app";
                            var macOSAppPath = macOSRootAppPath / $"{SoftwareName}.app";
                            var macOSAppContentsPath = macOSAppPath / "Contents";
                            var macOSAppBinPath = macOSAppContentsPath / "MacOS";
                            var macOSAppResourcesPath = macOSAppContentsPath / "Resources";
                            var macOSAppInfoPListFile = macOSAppContentsPath / "Info.plist";
                            var macOSAppEntitlementsFile = macOSAppContentsPath / $"{SoftwareName}.entitlements";
                            var icnsLogoFilePath = MediaDirectory / $"{SoftwareName}.icns";
                            macOSRootAppPath.CreateOrCleanDirectory();
                            macOSAppResourcesPath.CreateOrCleanDirectory();
                            icnsLogoFilePath.CopyToDirectory(macOSAppResourcesPath);

                            runtimeBuild = runtimeBuild with { BundleType = BuildRuntime.BundleTypes.App };
                            runtimeCacheFile.WriteJson(runtimeBuild);
                            publishPath.Copy(macOSAppBinPath);

                            macOSAppInfoPListFile.WriteAllText(MacAppBundle
                                .GetInfoPList(SoftwareName, SoftwareRDNS, SoftwareVersion, SoftwareCopyright)
                                .ReplaceLineEndings("\n"));
                            macOSAppEntitlementsFile.WriteAllText(MacAppBundle.Entitlements.ReplaceLineEndings("\n"));

                            if (IsOsx)
                            {
                                Log.Information("Codesign {name}", macOSAppPath.Name);
                                ProcessTasks.StartProcess("codesign", $"--force --deep --sign - \"{macOSAppPath}\"")
                                    .AssertWaitForExit();
                            }


                            Log.Information("Compressing: {fileName}", zipPath.Name);
                            macOSRootAppPath.ZipTo(zipPath, null, CompressionLevel.SmallestSize, FileMode.Create);

                            macOSRootAppPath.DeleteDirectory();
                        }
                    }
                    else if (rid.StartsWith("linux") || rid.StartsWith("unix"))
                    {
                        if (IsLinux)
                        {
                            runtimeBuild = runtimeBuild with { BundleType = BuildRuntime.BundleTypes.AppImage };
                            runtimeCacheFile.WriteJson(runtimeBuild);

                            var appImagePublishPath = publishPath + ".AppImage";
                            appImagePublishPath.DeleteFile();

                            var appImageToolExtractedFolderName = $"appimagetool-{executableArchAlt}";
                            var appImageToolFileName = appImageToolExtractedFolderName + ".AppImage";


                            var tempBuildPath = AbsolutePath.Create(Path.GetTempPath()) /
                                                $"{SoftwareName}_appimage_build";
                            tempBuildPath.CreateDirectory();

                            var appImageToolPath = tempBuildPath / appImageToolFileName;
                            var appImageToolExtractedPath = tempBuildPath / appImageToolExtractedFolderName;
                            var appImageAppRunBinary = appImageToolExtractedPath / "AppRun";
                            if (!appImageToolPath.FileExists())
                            {
                                // Download file here
                                var url =
                                    $"{LinuxAppBundle.AppImageGitHubUrl}/releases/download/continuous/{appImageToolFileName}";
                                Log.Information("Downloading {url} to {AppImageToolPath}", url, appImageToolPath);
                                HttpTasks.HttpDownloadFile(url, appImageToolPath);
                                if (!appImageToolPath.FileExists())
                                {
                                    Log.Error($"Failed to download {url}");
                                    continue;
                                }

                                appImageToolPath.SetExecutable();
                            }

                            if (!appImageToolExtractedPath.DirectoryExists())
                            {
                                var fuseAvailable = LinuxAppBundle.IsFuseAvailable();
                                if (!fuseAvailable)
                                {
                                    Log.Warning(
                                        "FUSE not detected (libfuse.so.2 missing). AppImage extraction may fail.");
                                }

                                // Extract AppImage so it can be run in Docker containers and on machines that don't have FUSE installed
                                // Note: Extracting requires libglib2.0-0 to be installed
                                ProcessTasks
                                    .StartShell($"\"{appImageToolPath}\" --appimage-extract",
                                        tempBuildPath).AssertWaitForExit();
                                //ProcessTasks.StartShell($"ls -la {tempBuildPath}").AssertWaitForExit();
                                var tempExtractedFolder = tempBuildPath / "squashfs-root";
                                Log.Information("{TempExtractedFolder}: {DirectoryExists}", tempExtractedFolder,
                                    tempExtractedFolder.DirectoryExists());
                                if (!tempExtractedFolder.DirectoryExists())
                                {
                                    Log.Error("Failed to extract {AppImageToolFileName} to {TempBuildPath}",
                                        appImageToolFileName, tempBuildPath);
                                    throw new InvalidOperationException(
                                        $"Failed to extract {appImageToolFileName} to {tempBuildPath}");
                                }

                                tempExtractedFolder.Rename(appImageToolExtractedFolderName);

                                if (appImageAppRunBinary.FileExists())
                                {
                                    appImageAppRunBinary.SetExecutable();
                                }
                                else
                                {
                                    Log.Error("Expected AppRun binary not found at {Path}", appImageAppRunBinary);
                                    throw new InvalidOperationException("AppRun binary missing after extraction");
                                }
                            }

                            // Create AppImage structure
                            var appImageDirPath = publishPath + "_AppImage";
                            appImageDirPath.CreateOrCleanDirectory();

                            // Copy Logo
                            var svgLogoFilePath = MediaDirectory / $"{SoftwareName}.svg";
                            var appImageHiIconDirPath = appImageDirPath / "usr" / "share" / "icons" / "hicolor" /
                                                        "scalable" / "apps";
                            svgLogoFilePath.CopyToDirectory(appImageHiIconDirPath);
                            svgLogoFilePath.CopyToDirectory(appImageDirPath);

                            // Create entry files
                            var appImageAppRunFilePath = appImageDirPath / "AppRun";
                            appImageAppRunFilePath.WriteAllText(LinuxAppBundle.GetAppImageAppRunFile(SoftwareName)
                                .ReplaceLineEndings("\n"));
                            appImageAppRunFilePath.SetExecutable();

                            var appImageDesktopFilePath = appImageDirPath / $"{SoftwareRDNS}.desktop";
                            appImageDesktopFilePath.WriteAllText(LinuxAppBundle.GetAppImageDesktopFile(this)
                                .ReplaceLineEndings("\n"));

                            var appImageApplicationsDirPath = appImageDirPath / "usr" / "share" / "applications";
                            appImageDesktopFilePath.CopyToDirectory(appImageApplicationsDirPath);

                            var appImageAppDataXmlFilePath = appImageDirPath / "usr" / "share" / "metainfo" /
                                                             $"{SoftwareRDNS}.appdata.xml";
                            appImageAppDataXmlFilePath.WriteAllText(LinuxAppBundle.GetAppImageAppDataXmlFile(this)
                                .ReplaceLineEndings("\n"));

                            // Copy application files
                            var appImageBinDirPath = appImageDirPath / "usr" / "bin";
                            publishPath.Copy(appImageBinDirPath);

                            // Create AppImage
                            ProcessTasks
                                .StartShell(
                                    $"ARCH={archAlt} \"{appImageAppRunBinary}\" \"{appImageDirPath}\" \"{appImagePublishPath}\"")
                                .AssertWaitForExit();
                            appImagePublishPath.SetExecutable();

                            appImageDirPath.DeleteDirectory();
                        }
                        else
                        {
                            Log.Warning("Skipping Linux AppImage build on non-Linux platform.");
                        }
                    }

                    if (PublishDiscardNonBundles)
                    {
                        publishPath.DeleteDirectory();
                    }
                }

                if (PublishBundleWithMultipleArch)
                {
                    if (IsUnix)
                    {
                        if (Enumerable.Contains(RIds, "osx-x64") && Enumerable.Contains(RIds, "osx-arm64"))
                        {
                            Log.Information("Bundling macOS multi-arch app.");
                            var macOSRootAppPath =
                                PublishDirectory / $"{SoftwareName}_osx-multiarch_v{SoftwareVersion}.app";
                            var macOSAppPath = macOSRootAppPath / $"{SoftwareName}.app";
                            var macOSAppContentsPath = macOSAppPath / "Contents";
                            var macOSAppResourcesPath = macOSAppContentsPath / "Resources";
                            var macOSAppX64BinPath = macOSAppContentsPath / "MacOS" / "osx-x64";
                            var macOSAppArm64BinPath = macOSAppContentsPath / "MacOS" / "osx-arm64";
                            var macOSAppSharedBinPath = macOSAppContentsPath / "MacOS" / "shared";
                            macOSAppSharedBinPath.CreateOrCleanDirectory();
                            macOSAppResourcesPath.CreateOrCleanDirectory();

                            var files = macOSAppX64BinPath.GlobFiles("**");
                            Parallel.ForEach(files, x64File =>
                            {
                                AbsolutePath arm64File =
                                    x64File.ToString().Replace(macOSAppX64BinPath, macOSAppArm64BinPath);

                                if (!arm64File.Exists()) return;

                                var x64Hash = x64File.GetFileHash();
                                var arm64Hash = arm64File.GetFileHash();
                                if (!x64Hash.Equals(arm64Hash, StringComparison.Ordinal)) return;
                                //Log.Information($"Same hash for: {x64File}");

                                AbsolutePath sharedFile =
                                    x64File.ToString().Replace(macOSAppX64BinPath, macOSAppSharedBinPath);
                                x64File.Move(sharedFile);
                                arm64File.DeleteFile();

                                sharedFile.AddUnixSymlink(x64File);
                                sharedFile.AddUnixSymlink(arm64File);
                            });

                            if (IsOsx)
                            {
                                Log.Information("Codesign {name}", macOSAppPath.Name);
                                ProcessTasks.StartProcess("codesign", $"--force --deep --sign - \"{macOSAppPath}\"")
                                    .AssertWaitForExit();
                            }

                            var zipPath = PublishDirectory / $"{SoftwareName}_osx-multiarch_v{SoftwareVersion}.zip";
                            zipPath.DeleteFile();
                            Log.Information("Compressing: {fileName}", zipPath.Name);
                            macOSRootAppPath.ZipTo(zipPath, null, CompressionLevel.SmallestSize, FileMode.Create);

                            macOSRootAppPath.DeleteDirectory();
                        }
                    }
                    else
                    {
                        Log.Warning("Skipping multi-arch bundle, non unix OS.");
                    }
                }
            }

            */

    public static int Main() => Execute<Build>(x => x.Compile);

    [GeneratedRegex("printer_technology.*=.*(SLA)")]
    private static partial Regex SlaPrinterRegex();

    [GeneratedRegex("(?<A><RegistryValue\\s+Name=\"AppliesTo\"\\s+Value=\")[^\"]*(?<B>\"\\s+Type=.+?\\s*/>)",
        RegexOptions.Singleline)]
    private static partial Regex MsiAppliesToRegex();
}