/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.Objects;
using UVtools.Core.Operations;
using UVtools.Core.SystemOS;
using UVtools.WPF.Extensions;

namespace UVtools.WPF.Structures;

public class AppVersionChecker : BindableBase
{
    public const string GitHubReleaseApi = "https://api.github.com/repos/sn4k3/UVtools/releases/latest";
    public const string RuntimePackageFile = "runtime_package.dat";
    private string _version;
    private string _changelog;

    public string Filename
    {
        get
        {
            var file = Path.Combine(App.ApplicationPath, RuntimePackageFile);
            if (File.Exists(file))
            {
                try
                {
                    var package = File.ReadAllText(file).Trim();
                    if (!string.IsNullOrWhiteSpace(package) && (package.EndsWith("-x64") || package.EndsWith("-arm64")))
                    {
                        if (OperatingSystem.IsWindows()) return $"{About.Software}_{package}_v{_version}.msi";
                        if (Linux.IsRunningAppImage) return $"{About.Software}_{package}_v{_version}.AppImage";
                        return $"{About.Software}_{package}_v{_version}.zip";
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }

            if (OperatingSystem.IsWindows())
            {
                return $"{About.Software}_win-x64_v{_version}.msi";
            }
            if (OperatingSystem.IsLinux())
            {
                return Linux.IsRunningAppImage
                        ? $"{About.Software}_linux-x64_v{_version}.AppImage"
                        : $"{About.Software}_linux-x64_v{_version}.zip";
            }
            if (OperatingSystem.IsMacOS())
            {
                return RuntimeInformation.ProcessArchitecture is Architecture.Arm or Architecture.Arm64 
                    ? $"{About.Software}_osx-arm64_v{_version}.zip" 
                    : $"{About.Software}_osx-x64_v{_version}.zip";
            }

            return $"{About.Software}_universal-x86-x64_v{_version}.zip";
        }
    }

    /*public string Runtime
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                return "win-x64";
            }
            if (OperatingSystem.IsLinux())
            {
                return "linux-x64";
            }
            if (OperatingSystem.IsMacOS())
            {
                return "osx-x64";
            }

            return "universal-x86-x64";
        }
    }*/

    public string Version
    {
        get => _version;
        set
        {
            if(!RaiseAndSetIfChanged(ref _version, value)) return;
            RaisePropertyChanged(nameof(VersionAnnouncementText));
            RaisePropertyChanged(nameof(HaveNewVersion));
        }
    }

    public string Changelog
    {
        get => _changelog;
        set => RaiseAndSetIfChanged(ref _changelog, value);
    }

    public string VersionAnnouncementText => $"New version v{_version} is available!";

    public string UrlLatestRelease = $"{About.Website}/releases/latest";

    public string DownloadLink { get; private set; }

    public bool HaveNewVersion => !string.IsNullOrEmpty(Version);

    public string DownloadedFile { get; private set; }

    /// <summary>
    /// Check for new version
    /// </summary>
    /// <param name="alwaysTrigger">True to always found as new update, otherwise it will compare versions and trigger only if a new version is found</param>
    /// <returns></returns>
    public bool Check(bool alwaysTrigger = false)
    {
        try
        {
            _version = null;
            DownloadLink = null;
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(GitHubReleaseApi),
            };

            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

            var result= NetworkExtensions.HttpClient.Send(request);

            var json = JsonNode.Parse(result.Content.ReadAsStream());
            
            string tag_name = json["tag_name"]?.ToString();
            if (string.IsNullOrEmpty(tag_name)) return false;
            tag_name = tag_name.Trim(' ', 'v', 'V');
            Debug.WriteLine($"Version checker: v{About.VersionStr} <=> v{tag_name}");
            Version checkVersion = new(tag_name);
            Changelog = json["body"]?.ToString();
            if (alwaysTrigger || About.Version.CompareTo(checkVersion) < 0)
            {
                var assets = json["assets"].AsArray();
                
                Version = tag_name;
                var fileName = Filename;
                foreach (var asset in assets)
                {
                    var name = asset["name"]!.ToString();
                    if (OperatingSystem.IsLinux() && name.StartsWith($"{About.Software}_linux-") && name.EndsWith(".AppImage"))
                    {
                        // Force generic Linux first
                        if ((Linux.IsRunningAppImage && name.EndsWith(".AppImage")) || (!Linux.IsRunningAppImage && name.EndsWith(".zip")))
                        {
                            DownloadLink = asset["browser_download_url"]!.ToString();
                        }
                    }
                    if (name != fileName) continue;
                    DownloadLink = asset["browser_download_url"]!.ToString();
                    break;
                }

                Debug.WriteLine($"New version detected: {DownloadLink}\n{_changelog}");

                return true;
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.ToString());
        }

        return false;
    }

    public async Task<bool> AutoUpgrade(OperationProgress progress)
    {
        if (!HaveNewVersion || string.IsNullOrWhiteSpace(DownloadLink)) return false;
        progress.ItemName = "Megabytes";
        try
        {
            var downloadFilename = Filename;
            var path = Path.GetTempPath();
            DownloadedFile = Path.Combine(path, downloadFilename);
            Debug.WriteLine($"Downloading to: {DownloadedFile}");
            progress.ItemName = "Megabytes";


            var iprogress = new Progress<(long total, long bytes)>();
            iprogress.ProgressChanged += (_, tuple) =>
            {
                progress.ItemCount = (uint)(tuple.total / 1000000);
                progress.ProcessedItems = (uint)(tuple.bytes / 1000000);
            };
            using var result = await NetworkExtensions.DownloadAsync(DownloadLink, DownloadedFile, iprogress, progress.Token);

            progress.Reset("Extracting");

            if (OperatingSystem.IsWindows())
            {
                SystemAware.StartProcess(DownloadedFile);
            }
            else if (downloadFilename.EndsWith(".AppImage") && Linux.IsRunningAppImageGetPath(out var appImagePath)) // Linux AppImage
            {
                var directory = Path.GetDirectoryName(appImagePath);
                const string newFilename = $"{About.Software}.AppImage";
                //var oldFileName = Path.GetFileName(appImagePath);
                // Try to keep same filename logic if user renamed the file, like UVtools.AppImage would keep same same
                //var newFilename = Regex.Replace(oldFileName, @"v\d+.\d+.\d+", $"v{_version}");
                var newFullPath = Path.Combine(directory, newFilename);

                if (File.Exists(appImagePath)) File.Delete(appImagePath);
                File.Move(DownloadedFile, newFullPath, true);
                SystemAware.StartProcess("chmod", $"775 \"{newFullPath}\"", true);
                Thread.Sleep(500);
                SystemAware.StartProcess(newFullPath);
            }
            else // MacOS and generic linux (no AppImage -- plain zip)
            {
                var tmpDirectory = PathExtensions.GetTemporaryDirectory("UVtoolsUpdate-", true);
                var extractDirectoryPath = Path.Combine(tmpDirectory, "extracted");

                ZipFile.ExtractToDirectory(DownloadedFile, extractDirectoryPath, true);
                File.Delete(DownloadedFile);

                var upgradeScriptFileName = "upgrade.sh";
                var upgradeScriptFilePath = Path.Combine(tmpDirectory, upgradeScriptFileName);
                var applicationPath = App.ApplicationPath;
                await using (var stream = File.CreateText(upgradeScriptFilePath))
                {
                    stream.NewLine = "\n";
                    await stream.WriteLineAsync("#!/bin/bash");
                    await stream.WriteLineAsync();
                    await stream.WriteLineAsync("testcmd() { command -v \"$1\" &> /dev/null; }");
                    await stream.WriteLineAsync();
                    await stream.WriteLineAsync("cd \"$(dirname \"$0\")\"");
                    await stream.WriteLineAsync($"echo '{About.Software} v{About.VersionStr} -> {_version} updater script'");
                    await stream.WriteLineAsync();
                    await stream.WriteLineAsync($"oldversion='{About.VersionStr}'");
                    await stream.WriteLineAsync($"newversion='{_version}'");
                    await stream.WriteLineAsync();
                    //await stream.WriteLineAsync($"cd '{App.ApplicationPath}'");
                    await stream.WriteLineAsync("echo '- Killing processes'");
                    await stream.WriteLineAsync($"killall {About.Software}");
                    await stream.WriteLineAsync($"ps -ef | grep '.*dotnet.*{About.Software}.dll' | grep -v grep | awk '{{print $2}}' | xargs kill");
                    await stream.WriteLineAsync("sleep 1");
                    await stream.WriteLineAsync();

                    if (OperatingSystem.IsMacOS())
                    {
                        await stream.WriteLineAsync("echo '- Removing com.apple.quarantine flag'");
                        await stream.WriteLineAsync($"find '{extractDirectoryPath}' -print0 | xargs -0 xattr -d com.apple.quarantine &> /dev/null");
                    }

                    await stream.WriteLineAsync("echo '- Sync/copying files over'");
                    if (macOS.IsRunningAppGetPath(out var macOSAppPath) && Directory.Exists(Path.Combine(extractDirectoryPath, "UVtools.app")))
                    {
                        await stream.WriteLineAsync("if testcmd rsync; then");
                        await stream.WriteLineAsync($"  rsync -arctxv --delete --remove-source-files --stats '{Path.Combine(extractDirectoryPath, "UVtools.app")}/' '{macOSAppPath}'");
                        await stream.WriteLineAsync("else");
                        await stream.WriteLineAsync($"  cp -fR '{extractDirectoryPath}/'* '{macOSAppPath}'");
                        await stream.WriteLineAsync("fi");
                    }
                    else // Linux generic and macOS generic
                    {
                        await stream.WriteLineAsync("if testcmd rsync; then");
                        await stream.WriteLineAsync($"  rsync -arctxv --remove-source-files --stats '{extractDirectoryPath}/' '{App.ApplicationPath}'");
                        await stream.WriteLineAsync("else");
                        await stream.WriteLineAsync($"  cp -fR '{extractDirectoryPath}/'* '{App.ApplicationPath}'");
                        await stream.WriteLineAsync("fi");

                        if (About.VersionStr != _version)
                        {
                            var di = new DirectoryInfo(App.ApplicationPath);
                            var newDirectoryName = Regex.Replace(di.Name, $@"({About.Software}.*)(v\d+.\d+.\d+)", $@"$1v{_version}", RegexOptions.IgnoreCase);
                            if (di.Name != newDirectoryName)
                            {
                                await stream.WriteLineAsync("echo '- Directory is able to rename version name'");
                                applicationPath = Path.Combine(di.Parent?.FullName!, newDirectoryName);
                            }
                        }
                    }

                    await stream.WriteLineAsync();
                    await stream.WriteLineAsync("sleep 0.5");

                    if (App.ApplicationPath == applicationPath)
                    {
                        await stream.WriteLineAsync($"cd '{applicationPath}'");
                    }
                    else // Can move
                    {
                        await stream.WriteLineAsync("echo '- Attempt to rename directory to the new version name'");
                        await stream.WriteLineAsync($"if [ -d '{applicationPath}' ]; then");
                        await stream.WriteLineAsync($"  cd '{App.ApplicationPath}'");
                        await stream.WriteLineAsync("else");
                        await stream.WriteLineAsync($"  mv -f '{App.ApplicationPath}' '{applicationPath}'");
                        await stream.WriteLineAsync($"  cd '{applicationPath}'");
                        await stream.WriteLineAsync("fi");
                    }

                    await stream.WriteLineAsync("echo '- UVtools will now launch'");
                    if (App.SlicerFile is not null && App.SlicerFile.FileFullPath != Path.Combine(App.ApplicationPath, About.DemoFile))
                    {
                        // Reload last file
                        await stream.WriteLineAsync($"nohup bash 'UVtools.sh' '{App.SlicerFile.FileFullPath}' &> /dev/null &");
                    }
                    else
                    {
                        await stream.WriteLineAsync("nohup bash 'UVtools.sh' &> /dev/null &");
                    }
                    await stream.WriteLineAsync("disown");
                    await stream.WriteLineAsync();
                    await stream.WriteLineAsync($"echo '- Removing: {tmpDirectory}'");
                    await stream.WriteLineAsync($"rm -fr '{tmpDirectory}'");
                    await stream.WriteLineAsync("echo '- Completed'");
                    //await stream.WriteLineAsync("sleep 0.5");
                    //await stream.WriteLineAsync($"rm -f {upgradeScriptFileName}");
                    //await stream.WriteLine("exit");
                }

                if (Program.IsDebug)
                {
                    Console.WriteLine("In debug mode, will not auto-upgrade, please run:");
                    Console.WriteLine($"bash \"{upgradeScriptFilePath}\"");
                }
                else
                {
                    SystemAware.StartProcess("bash", $"\"{upgradeScriptFilePath}\"");
                }

                //App.NewInstance(App.MainWindow.SlicerFile?.FileFullPath);
            }
            
            Environment.Exit(0);
            return true;
        }
        catch (OperationCanceledException)
        {
            if(File.Exists(DownloadedFile)) File.Delete(DownloadedFile);
        }
        catch (Exception e)
        {
            await App.MainWindow.MessageBoxError(e.ToString(), "Error downloading the file");
            if (File.Exists(DownloadedFile)) File.Delete(DownloadedFile);
            return false;
        }


        return false;
    }
}