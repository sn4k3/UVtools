/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System.Drawing;

namespace UVtools.Core.Extensions;

public static class RectangleExtensions
{
    public static Point Center(this Rectangle src)
    {
        return new Point(src.Left + src.Width / 2, src.Top + src.Height / 2);
    }

}