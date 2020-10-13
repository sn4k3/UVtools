/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Diagnostics;
using System.Net;
using Avalonia.Threading;
using UVtools.Core;
using UVtools.Core.Objects;

namespace UVtools.WPF.Structures
{
    public class AppVersionChecker : BindableBase
    {
        private string _version;
        private string _url;

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

        public string VersionAnnouncementText => $"New version {_version} is available!";

        public string Url => $"{About.Website}/releases/tag/{_version}";

        public bool HaveNewVersion => !string.IsNullOrEmpty(Version);

        public bool Check()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    string htmlCode = client.DownloadString($"{About.Website}/releases");
                    const string searchFor = "/releases/tag/";
                    var startIndex = htmlCode.IndexOf(searchFor, StringComparison.InvariantCultureIgnoreCase) +
                                     searchFor.Length;
                    var endIndex = htmlCode.IndexOf("\"", startIndex, StringComparison.InvariantCultureIgnoreCase);
                    var version = htmlCode.Substring(startIndex, endIndex - startIndex);
                    if (string.Compare(version, $"v{AppSettings.AssemblyVersion}", StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            Version = version;;
                        });
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }

            return false;
        }
    }
}
