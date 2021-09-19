/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Drawing;
using System.Linq;

namespace UVtools.Core.Layers
{
    public sealed class IssueOfPoints : Issue
    {
        /// <summary>
        /// Gets the points containing the coordinates of the issue
        /// </summary>
        public Point[] Points { get; init; }

        public IssueOfPoints(Layer layer, Point[] points, Rectangle boundingRectangle = default) : base(layer, boundingRectangle, points.Length)
        {
            Points = points;
            PixelsCount = (uint)points.Length;
        }

        private bool Equals(IssueOfPoints other)
        {
            return Points.SequenceEqual(other.Points);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is IssueOfPoints other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Points != null ? Points.GetHashCode() : 0);
        }
    }
}
