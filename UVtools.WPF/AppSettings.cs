/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
namespace UVtools.WPF;

public static class AppSettings
{
    // Supported ZoomLevels for Layer Preview.
    // These settings eliminate very small zoom factors from the ImageBox default values,
    // while ensuring that 4K/5K build plates can still easily fit on screen.  
    public static readonly int[] ZoomLevels =
        {20, 25, 30, 50, 75, 100, 150, 200, 300, 400, 500, 600, 700, 800, 1200, 1600, 3200};

    // Count of the bottom portion of the full zoom range which will be skipped for
    // assignable actions such as auto-zoom level, and crosshair fade level.  If values
    // are added/removed from ZoomLevels above, this value may also need to be adjusted.
    public const byte ZoomLevelSkipCount = 7; // Start at 2x which is index 7.

    /// <summary>
    /// Returns the zoom level at which the crosshairs will fade and no longer be displayed
    /// </summary>
    public static int CrosshairFadeLevel => ZoomLevels[UserSettings.Instance.LayerPreview.CrosshairFadeLevelIndex + ZoomLevelSkipCount];

    /// <summary>
    /// Returns the zoom level that will be used for autozoom actions
    /// </summary>
    public static int LockedZoomLevel => ZoomLevels[UserSettings.Instance.LayerPreview.ZoomLockLevelIndex + ZoomLevelSkipCount];


    /// <summary>
    /// Minimum Zoom level to which autozoom can be locked. 
    /// </summary>
    public const byte MinLockedZoomLevel = 200;

        
}