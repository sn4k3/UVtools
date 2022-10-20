/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Drawing;
using UVtools.Core.Extensions;

namespace UVtools.Core.Printer
{
    /// <summary>
    /// Utility methods over a printer screen
    /// </summary>
    public static class Screen
    {
        /// <summary>
        /// Gets the pixel size in mm
        /// </summary>
        /// <param name="resolution">Resolution in pixels</param>
        /// <param name="displaySize">Display size in mm</param>
        /// <returns>Pixel size in mm</returns>
        public static float GetPixelSize(int resolution, float displaySize) => resolution == 0 ? 0 : displaySize / resolution;

        /// <summary>
        /// Gets the pixel size in microns
        /// </summary>
        /// <param name="resolution">Resolution in pixels</param>
        /// <param name="displaySize">Display size in mm</param>
        /// <returns>Pixel size in microns</returns>
        public static ushort GetPixelSizeMicrons(int resolution, float displaySize) => (ushort)(GetPixelSize(resolution, displaySize) * 1000);

        /// <summary>
        /// Gets the pixel size in mm
        /// </summary>
        /// <param name="resolution">Resolution in pixels</param>
        /// <param name="displaySize">Display size in mm</param>
        /// <returns>Pixel size in mm</returns>
        public static SizeF GetPixelSize(Size resolution, SizeF displaySize) => displaySize.Divide(resolution);

        /// <summary>
        /// Gets the pixel size in microns
        /// </summary>
        /// <param name="resolution">Resolution in pixels</param>
        /// <param name="displaySize">Display size in mm</param>
        /// <returns>Pixel size in microns</returns>
        public static Size GetPixelSizeMicrons(Size resolution, SizeF displaySize)
        {
            var pixel = GetPixelSize(resolution, displaySize);
            return new Size((int)(pixel.Width * 1000), (int)(pixel.Height * 1000));
        }
    }
}
