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
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Threading;
using Newtonsoft.Json;
using UVtools.Core;
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
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return "win-x64";
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return "linux-x64";
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
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
                using WebClient client = new()
                {
                    Headers = new WebHeaderCollection
                    {
                        {HttpRequestHeader.Accept, "application/json"},
                        {HttpRequestHeader.UserAgent, "Request"}
                    }
                };
                var response = client.DownloadString(GitHubReleaseApi);
                dynamic json = JsonConvert.DeserializeObject(response);
                string tag_name = json.tag_name;
                if (string.IsNullOrEmpty(tag_name)) return false;
                tag_name = tag_name.Trim(' ', 'v', 'V');
                Debug.WriteLine($"Version checker: v{App.VersionStr} <=> v{tag_name}");
                Version checkVersion = new(tag_name);
                Changelog = json.body;
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

        public bool AutoUpgrade(OperationProgress progress)
        {
            if (!HaveNewVersion) return false;
            progress.ItemName = "Megabytes";
            try
            {
                using var client = new WebClient();
                var path = Path.GetTempPath();
                DownloadedFile = Path.Combine(path, Filename);
                Debug.WriteLine($"Downloading to: {DownloadedFile}");


                client.DownloadProgressChanged += (sender, e) =>
                {
                    progress.Reset("Megabytes", (uint)e.TotalBytesToReceive / 1048576, (uint)e.BytesReceived / 1048576);
                };
                client.DownloadFileCompleted += (sender, e) =>
                {
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
                        using (var stream = File.Open(DownloadedFile, FileMode.Open))
                        {
                            using ZipArchive zip = new(stream, ZipArchiveMode.Read);
                            zip.ExtractToDirectory(targetDir, true);
                        }

                        File.Delete(DownloadedFile);

                        string upgradeFileName = $"{About.Software}_upgrade.sh";
                        var upgradeFile = Path.Combine(App.ApplicationPath, upgradeFileName);
                        using (var stream = File.CreateText(upgradeFile))
                        {
                            stream.WriteLine("#!/bin/bash");
                            stream.WriteLine($"echo {About.Software} v{App.Version} updater script");
                            stream.WriteLine($"cd '{App.ApplicationPath}'");
                            stream.WriteLine($"killall {About.Software}");
                            stream.WriteLine("sleep 0.5");
                            stream.WriteLine($"cp -fR {upgradeFolder}/* .");
                            stream.WriteLine($"rm -fr {upgradeFolder}");
                            stream.WriteLine("sleep 0.5");
                            //stream.WriteLine($"[ -f {About.Software} ] && {App.AppExecutableQuoted} & || dotnet {About.Software}.dll &");
                            stream.WriteLine($"if [ -f '{About.Software}' ]; then");
                            stream.WriteLine($"  ./{About.Software} &");
                            stream.WriteLine("else");
                            stream.WriteLine($"  dotnet {About.Software}.dll &");
                            stream.WriteLine("fi");
                            stream.WriteLine($"rm -f {upgradeFileName}");
                            //stream.WriteLine("exit");
                            stream.Close();
                        }

                        App.StartProcess("bash", $"\"{upgradeFile}\"");
                            
                        //App.NewInstance(App.MainWindow.SlicerFile?.FileFullPath);
                        Environment.Exit(0);
                    }

                    lock (e.UserState)
                    {
                        //releases blocked thread
                        Monitor.Pulse(e.UserState);
                    }
                };


                var syncObject = new object();

                lock (syncObject)
                {
                    client.DownloadFileAsync(new Uri(DownloadLink), DownloadedFile, syncObject);
                    //This would block the thread until download completes
                    Monitor.Wait(syncObject);
                }
            }
            catch (Exception e)
            {
                Dispatcher.UIThread.InvokeAsync(async () =>
                    await App.MainWindow.MessageBoxError(e.ToString(), "Error downloading the file"));
                return false;
            }
            

            return false;
        }
    }
}
