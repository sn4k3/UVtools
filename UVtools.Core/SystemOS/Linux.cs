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
/// Linux specific methods
/// </summary>
public static class Linux
{
    /// <summary>
    /// Gets if is running under Linux and under AppImage format
    /// </summary>
    public static bool IsRunningAppImage => !string.IsNullOrWhiteSpace(RunningAppImageRootPath);

    /// <summary>
    /// Gets if is running under Linux and under AppImage format and return the full root path for the running AppImage
    /// </summary>
    public static bool IsRunningAppImageGetPath(out string? path)
    {
        path = RunningAppImageRootPath;
        return !string.IsNullOrWhiteSpace(path);
    }

    /// <summary>
    /// <para>Gets the full root path for the running AppImage. Returns null is not Linux and null/empty if not an AppImage</para>
    /// <para>The return path is the source file location and not the execution path location.</para>
    /// </summary>
    public static string? RunningAppImageRootPath => OperatingSystem.IsLinux() ? Environment.GetEnvironmentVariable("APPIMAGE") : null;


    /// <summary>
    /// Gets the name of the running *.app. Returns null or empty if not running an macOS .app. Returns null or empty if not an AppImage
    /// </summary>
    public static string? RunningAppName => Path.GetFileName(RunningAppImageRootPath);
}