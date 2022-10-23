/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.IO;

namespace UVtools.Core.SystemOS;

/// <summary>
/// MacOS specific methods
/// </summary>
public static class macOS
{
    /// <summary>
    /// Gets if is running under MacOS and under .app format
    /// </summary>
    public static bool IsRunningApp => OperatingSystem.IsMacOS() && AppContext.BaseDirectory.EndsWith(Path.Combine(".app", "Contents", $"MacOS{Path.DirectorySeparatorChar}"));

    /// <summary>
    /// Gets if is running under macOS and under .app format and return the full root path for the running .app
    /// </summary>
    public static bool IsRunningAppGetPath(out string? path)
    {
        path = RunningAppRootPath;
        return !string.IsNullOrWhiteSpace(path);
    }

    /// <summary>
    /// Gets the full root path for the running .app. Returns null or empty if not running an macOS .app
    /// </summary>
    public static string? RunningAppRootPath => IsRunningApp ? Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.FullName : null;


    /// <summary>
    /// Gets the name of the running .app. Returns null or empty if not running an macOS .app
    /// </summary>
    public static string? RunningAppName => IsRunningApp ? Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Name : null;
}