/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;

namespace UVtools.UI.Managers;

public record AnnouncementItem
{
    /// <summary>
    /// Unique identifier for the announcement (e.g. "v7.0.0-release")
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Title displayed in the header and window title
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Announcement message supporting Markdown formatting
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// URL pointing to the full announcement post or discussion
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// If true, accepting/closing the dialog always opens the URL in the browser.
    /// If false, an explicit "Open announcement in browser" button is provided.
    /// </summary>
    public bool AutoOpenUrl { get; init; }

    /// <summary>
    /// Minimum UVtools version this announcement applies to (inclusive, e.g. "7.0.0")
    /// </summary>
    public string? MinVersion { get; init; }

    /// <summary>
    /// Maximum UVtools version this announcement applies to (inclusive, e.g. "7.99.99")
    /// </summary>
    public string? MaxVersion { get; init; }

    /// <summary>
    /// Expiration date (UTC). If null, the announcement is always valid.
    /// </summary>
    public DateTime? ExpiresAt { get; init; }
}