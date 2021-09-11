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
using System.Threading.Tasks;
using Avalonia.Threading;
using Newtonsoft.Json.Linq;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.Objects;
using UVtools.Core.Operations;
using UVtools.WPF.Extensions;

namespace UVtools.WPF.Structures
{
    public class AppVersionChecker : BindableBase
    {
        public const string GitHubReleaseApi = "https://api.github.com/repos/sn4k3/UVtools/releases/latest";
        private string _version;
        private string _changelog;

        public string Filename
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return $"{About.Software}_win-x64_v{_version}.msi";
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return $"{About.Software}_linux-x64_v{_version}.zip";
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return $"{About.Software}_osx-x64_v{_version}.zip";
                }

                return $"{About.Software}_universal-x86-x64_v{_version}.zip";
            }
        }

        public string Runtime
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
        }

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

        public string DownloadLink => string.IsNullOrEmpty(Filename) ? null : $"{About.Website}/releases/download/v{_version}/{Filename}";

        public bool HaveNewVersion => !string.IsNullOrEmpty(Version);

        public string DownloadedFile { get; private set; }

        public bool Check()
        {
            try
            {
                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(GitHubReleaseApi),
                };

                request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

                var result= NetworkExtensions.HttpClient.Send(request);

                var json = JObject.Parse(result.Content.ReadAsStringAsync().Result);
                string tag_name = json["tag_name"]?.ToString();
                if (string.IsNullOrEmpty(tag_name)) return false;
                tag_name = tag_name.Trim(' ', 'v', 'V');
                Debug.WriteLine($"Version checker: v{App.VersionStr} <=> v{tag_name}");
                Version checkVersion = new(tag_name);
                Changelog = json["body"]?.ToString();
                //if (string.Compare(tag_name, App.VersionStr, StringComparison.OrdinalIgnoreCase) > 0)
                if (App.Version.CompareTo(checkVersion) < 0)
                {
                    Debug.WriteLine($"New version detected: {DownloadLink}\n" +
                                    $"{_changelog}");
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Version = tag_name;
                    });
                    return true;
                }
                /*string htmlCode = client.DownloadString($"{About.Website}/releases");
                    const string searchFor = "/releases/tag/";
                    var startIndex = htmlCode.IndexOf(searchFor, StringComparison.InvariantCultureIgnoreCase) +
                                     searchFor.Length;
                    var endIndex = htmlCode.IndexOf("\"", startIndex, StringComparison.InvariantCultureIgnoreCase);
                    var version = htmlCode.Substring(startIndex, endIndex - startIndex);
                    if (string.Compare(version, $"v{AppSettings.VersionStr}", StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            Version = version;;
                        });
                        return true;
                    }*/
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }

            return false;
        }

        public async Task<bool> AutoUpgrade(OperationProgress progress)
        {
            if (!HaveNewVersion) return false;
            progress.ItemName = "Megabytes";
            try
            {
                var path = Path.GetTempPath();
                DownloadedFile = Path.Combine(path, Filename);
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
                    App.StartProcess(DownloadedFile);
                    Environment.Exit(0);
                }
                else
                {
                    string upgradeFolder = "UPDATED_VERSION";
                    var targetDir = Path.Combine(App.ApplicationPath, upgradeFolder);
                    await using (var stream = File.Open(DownloadedFile, FileMode.Open))
                    {
                        using ZipArchive zip = new(stream, ZipArchiveMode.Read);
                        zip.ExtractToDirectory(targetDir, true);
                    }

                    File.Delete(DownloadedFile);

                    string upgradeFileName = $"{About.Software}_upgrade.sh";
                    var upgradeFile = Path.Combine(App.ApplicationPath, upgradeFileName);
                    await using (var stream = File.CreateText(upgradeFile))
                    {
                        await stream.WriteLineAsync("#!/bin/bash");
                        await stream.WriteLineAsync($"echo {About.Software} v{App.Version} updater script");
                        await stream.WriteLineAsync($"cd '{App.ApplicationPath}'");
                        await stream.WriteLineAsync($"killall {About.Software}");
                        await stream.WriteLineAsync("sleep 0.5");


                        //stream.WriteLine($"[ -f {About.Software} ] && {App.AppExecutableQuoted} & || dotnet {About.Software}.dll &");
                        if (OperatingSystem.IsMacOS() && App.ApplicationPath.EndsWith(".app/Contents/MacOS"))
                        {
                            await stream.WriteLineAsync($"cp -fR {upgradeFolder}/* ../../../");
                            await stream.WriteLineAsync($"open ../../../{About.Software}.app");
                        }
                        else
                        {
                            await stream.WriteLineAsync($"cp -fR {upgradeFolder}/* .");
                            await stream.WriteLineAsync($"if [ -f '{About.Software}' ]; then");
                            await stream.WriteLineAsync($"  ./{About.Software} &");
                            await stream.WriteLineAsync("else");
                            await stream.WriteLineAsync($"  dotnet {About.Software}.dll &");
                            await stream.WriteLineAsync("fi");
                        }

                        await stream.WriteLineAsync($"rm -fr {upgradeFolder}");
                        await stream.WriteLineAsync("sleep 0.5");
                        await stream.WriteLineAsync($"rm -f {upgradeFileName}");
                        //stream.WriteLine("exit");
                        stream.Close();
                    }

                    App.StartProcess("bash", $"\"{upgradeFile}\"");

                    //App.NewInstance(App.MainWindow.SlicerFile?.FileFullPath);
                    Environment.Exit(0);
                }
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
}
