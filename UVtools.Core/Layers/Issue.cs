/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.Text.Json.Serialization;
using UVtools.Core.Extensions;

namespace UVtools.Core.Layers;

//[JsonDerivedType(typeof(Issue))]
//[JsonDerivedType(typeof(IssueOfPoints))]
//[JsonDerivedType(typeof(IssueOfContours))]
public class Issue
{
    /// <summary>
    /// Gets the issue type associated
    /// </summary>
    [JsonIgnore]
    public MainIssue? Parent { get; internal set; }

    public MainIssue.IssueType? Type => Parent?.Type;

    /// <summary>
    /// Gets the layer where this issue is present
    /// </summary>
    [JsonIgnore]
    public Layer Layer { get; init; }

    /// <summary>
    /// Gets the layer index
    /// </summary>
    public uint LayerIndex => Layer.Index;

    /// <summary>
    /// Gets the bounding rectangle of the area
    /// </summary>
    public Rectangle BoundingRectangle { get; init; }

    /// <summary>
    /// Gets the number of pixels 
    /// </summary>
    public uint PixelsCount { get; init; }

    /// <summary>
    /// Gets the area of the issue
    /// </summary>
    public double Area { get; init; }

    public Point FirstPoint { get; init; } = new(-1,-1);

    public Issue(Layer layer, Rectangle boundingRectangle, double area)
    {
        Layer = layer;
        BoundingRectangle = boundingRectangle;
        Area = area;
    }

    public Issue(Layer layer, Rectangle boundingRectangle = default) : this(layer, boundingRectangle, boundingRectangle.Area())
    { }

    public virtual void Sort(){ }

    protected bool Equals(Issue other)
    {
        return Type == other.Type && LayerIndex == other.LayerIndex && BoundingRectangle.Equals(other.BoundingRectangle) && PixelsCount == other.PixelsCount && Area.Equals(other.Area);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Issue)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Layer, BoundingRectangle, PixelsCount, Area);
    }
}