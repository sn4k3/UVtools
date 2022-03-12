/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Collections.Generic;
using System.Drawing;

namespace UVtools.Core.Slicer;

public class SliceLine : List<PointF>
{
    private PointF _normal;

    public PointF Normal
    {
        get => _normal;
        set => _normal = CalculateNormal(value);
    }

    public PointF CalculateNormal(PointF normal = default)
    {
        if (!Validate())
            throw new InvalidOperationException("Can't calculate Normal without points set");

        if (normal == PointF.Empty)
            normal = _normal;
        if (normal == PointF.Empty)
            throw new InvalidOperationException("Can't calculate Normal without a starting point");

        // calculate normal slopes 
        var dX = this[0].Y - this[1].Y;
        var dY = this[0].X - this[1].X;
        // determine normal direction
        var xDir = normal.X >= 0 ? 1 : -1;
        var yDir = normal.Y >= 0 ? 1 : -1;
        // check for delta 0 in either direction
        if (dX == 0 && dY == 0)
            return new PointF(0, 0);
        if (dX == 0)
            return new PointF(0, yDir);
        if (dY == 0)
            return new PointF(xDir, 0);

        // if there aren't any zeroes, calculate the hypotenuse
        var hypotenuse = (float)Math.Sqrt((dX * dX) + (dY * dY));
        // use cross-multiplication and solve for x & y to find normal values where hypotenuse == 1
        dX = Math.Abs(dX / hypotenuse) * xDir;
        dY = Math.Abs(dY / hypotenuse) * yDir;
        return new PointF(dX, dY);
    }

    public bool Validate()
    {
        return Count == 2;
    }
}