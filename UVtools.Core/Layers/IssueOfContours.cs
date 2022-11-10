/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UVtools.Core.Extensions;

namespace UVtools.Core.Layers;

public sealed class IssueOfContours : Issue
{
    /// <summary>
    /// Gets the points contours of the issue
    /// </summary>
    public Point[][] Contours { get; init; }

    public IssueOfContours() { }

    public IssueOfContours(Layer layer, IEnumerable<Point[]> contours, Rectangle boundingRectangle, double area) : base(layer, boundingRectangle, area)
    {
        Contours = contours.ToArray();
        PixelsCount = (uint)area;
        FirstPoint = Contours[0][0];
    }

    public IssueOfContours(Layer layer, IEnumerable<Point[]> contours, Rectangle boundingRectangle = default) : this(layer, contours, boundingRectangle, boundingRectangle.Area())
    { }

    private bool Equals(IssueOfContours other)
    {
        if (!base.Equals(other)) return false;
        if (Contours.Length != other.Contours.Length) return false;
        for (int i = 0; i < Contours.Length; i++)
        {
            if (!Contours[i].SequenceEqual(other.Contours[i])) return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is IssueOfContours other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (Contours != null ? Contours.GetHashCode() : 0);
    }
}