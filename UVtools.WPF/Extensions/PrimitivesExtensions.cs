/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Avalonia;

namespace UVtools.WPF.Extensions
{
    public static class PrimitivesExtensions
    {
        public static System.Drawing.Point ToDotNet(this Point point)
        {
            return new System.Drawing.Point((int) point.X, (int) point.Y);
        }

        public static Point ToAvalonia(this System.Drawing.Point point)
        {
            return new Point(point.X, point.Y);
        }

        public static bool IsEmpty(this Point point)
        {
            return point.X == 0 && point.Y == 0;
        }
    }
}
