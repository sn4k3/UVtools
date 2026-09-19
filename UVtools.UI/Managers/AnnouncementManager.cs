/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Material.Icons;
using StageKit.Primitives.System;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.UI.Windows;

namespace UVtools.UI.Managers;

public static class AnnouncementManager
{
    public const string RemoteAnnouncementsUrl =
        "https://raw.githubusercontent.com/sn4k3/UVtools/master/announcements.json";

    public static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(30);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task CheckAndShowAnnouncementsAsync(Window owner)
    {
        if (!UserSettings.Instance.General.CheckAnnouncementsOnStartup) return;

        var general = UserSettings.Instance.General;
        general.DismissedAnnouncementIds ??= [];

        var candidates = new List<AnnouncementItem>();

        // Check if 30 minutes have elapsed since the last remote check
        var shouldFetchRemote = DateTime.UtcNow - general.LastAnnouncementCheckTime >= CheckInterval;

        if (shouldFetchRemote)
        {
            try
            {
                general.LastAnnouncementCheckTime = DateTime.UtcNow;
                UserSettings.Save();

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var response = await NetworkExtensions.HttpClient.GetAsync(RemoteAnnouncementsUrl, cts.Token)
                    .ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
                    var remoteItems = JsonSerializer.Deserialize<List<AnnouncementItem>>(json, JsonOptions);
                    if (remoteItems is { Count: > 0 })
                    {
                        candidates.AddRange(remoteItems);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to fetch remote announcements: {ex.Message}");
            }
        }

        // Add embedded fallback announcements if not already present in candidates
        foreach (var fallback in GetEmbeddedAnnouncements())
        {
            if (candidates.All(c => c.Id != fallback.Id))
            {
                candidates.Add(fallback);
            }
        }

        var currentVersion = About.Version;
        var announcement = candidates.FirstOrDefault(a =>
            !string.IsNullOrWhiteSpace(a.Id) &&
            !general.DismissedAnnouncementIds.Contains(a.Id) &&
            IsVersionCompatible(currentVersion, a.MinVersion, a.MaxVersion) &&
            (a.ExpiresAt is null || DateTime.UtcNow <= a.ExpiresAt.Value));

        if (announcement is null) return;

        // Mark announcement as dismissed and persist
        general.DismissedAnnouncementIds.Add(announcement.Id);
        UserSettings.Save();

        await ShowAnnouncementDialogAsync(owner, announcement);
    }

    /// <summary>
    /// Forces showing the latest announcement dialog, regardless of whether it has already been dismissed.
    /// Used for debugging or manual display from the menu.
    /// </summary>
    public static async Task ShowLastAnnouncementAsync(Window owner)
    {
        var candidates = new List<AnnouncementItem>();

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var response = await NetworkExtensions.HttpClient.GetAsync(RemoteAnnouncementsUrl, cts.Token)
                .ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
                var remoteItems = JsonSerializer.Deserialize<List<AnnouncementItem>>(json, JsonOptions);
                if (remoteItems is { Count: > 0 })
                {
                    candidates.AddRange(remoteItems);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to fetch remote announcements: {ex.Message}");
        }

        foreach (var fallback in GetEmbeddedAnnouncements())
        {
            if (candidates.All(c => c.Id != fallback.Id))
            {
                candidates.Add(fallback);
            }
        }

        var currentVersion = About.Version;
        var announcement =
            candidates.FirstOrDefault(a => IsVersionCompatible(currentVersion, a.MinVersion, a.MaxVersion))
            ?? candidates.FirstOrDefault();

        if (announcement is not null)
        {
            await ShowAnnouncementDialogAsync(owner, announcement);
        }
    }

    /// <summary>
    /// Displays the announcement modal dialog.
    /// </summary>
    public static async Task ShowAnnouncementDialogAsync(Window owner, AnnouncementItem announcement)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            Button[] buttons;
            if (announcement.AutoOpenUrl)
            {
                buttons = [MessageWindow.CreateOkButton()];
            }
            else
            {
                var list = new List<Button>();
                if (!string.IsNullOrWhiteSpace(announcement.Url))
                {
                    list.Add(MessageWindow.CreateLinkButton("Open announcement in browser", MaterialIconKind.OpenInNew,
                        announcement.Url));
                }

                list.Add(MessageWindow.CreateCloseButton());
                buttons = list.ToArray();
            }

            var messageWindow = new MessageWindow(
                announcement.Title,
                MaterialIconKind.Bullhorn,
                announcement.Title,
                announcement.Message,
                TextWrapping.Wrap,
                buttons,
                true
            )
            {
            };

            await messageWindow.ShowDialog(owner);

            if (announcement.AutoOpenUrl && !string.IsNullOrWhiteSpace(announcement.Url))
            {
                HostSystem.OpenUrl(announcement.Url);
            }
        });
    }

    private static bool IsVersionCompatible(Version current, string? minVersionStr, string? maxVersionStr)
    {
        if (!string.IsNullOrWhiteSpace(minVersionStr) && Version.TryParse(minVersionStr, out var min))
        {
            if (CompareVersions(current, min) < 0) return false;
        }

        if (!string.IsNullOrWhiteSpace(maxVersionStr) && Version.TryParse(maxVersionStr, out var max))
        {
            if (CompareVersions(current, max) > 0) return false;
        }

        return true;
    }

    private static int CompareVersions(Version a, Version b)
    {
        var major = a.Major.CompareTo(b.Major);
        if (major != 0) return major;
        var minor = Math.Max(0, a.Minor).CompareTo(Math.Max(0, b.Minor));
        if (minor != 0) return minor;
        var build = Math.Max(0, a.Build).CompareTo(Math.Max(0, b.Build));
        if (build != 0) return build;
        return Math.Max(0, a.Revision).CompareTo(Math.Max(0, b.Revision));
    }

    public static IEnumerable<AnnouncementItem> GetEmbeddedAnnouncements()
    {
        return [];
    }
}