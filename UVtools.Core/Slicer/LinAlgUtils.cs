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
using System.Globalization;
using System.Linq;
using QuantumConcepts.Formats.StereoLithography;

namespace UVtools.Core.Slicer
{
    public static class LinAlgUtils
    {
        #region Find Facets
        public static List<Facet> FindFacetsIntersectingZIndex(STLDocument stl, float z)
        {
            return stl.Facets.Where(f => f.Vertices.Any(v => v.Z <= z) && f.Vertices.Any(v => v.Z >= z) && Math.Abs(f.Normal.Z) != 1).ToList();
        }

        public static List<Facet> FindFlatFacetsAtZIndex(STLDocument stl, float z)
        {
            return stl.Facets.Where(f => f.Vertices.All(v => v.Z == z)).ToList();
        }

        public static List<Facet> FindTopFlatFacetsAtZIndex(STLDocument stl, float z)
        {
            return stl.Facets.Where(f => f.Vertices.Any(v => v.Z == z) && f.Normal.Z == 1).ToList();
        }
        public static List<Facet> FindBottomFlatFacetsAtZIndex(STLDocument stl, float z)
        {
            return stl.Facets.Where(f => f.Vertices.Any(v => v.Z == z) && f.Normal.Z == -1).ToList();
        }
        #endregion

        public static SliceLine CreateLineFromFacetAtZIndex(Facet facet, float z)
        {
            // This won't work for flat horizontal plane vertices, so throw if that's what we've got
            if (Math.Abs(facet.Normal.Z) == 1)
                throw new Exception("Cannot create lines for flat horizontal planes");
            var verts = facet.Vertices;
            var returnLine = new SliceLine();
            // If any vertices are ON the z-index, just return that line
            returnLine.AddRange(verts.Where(v => v.Z == z).Select(v => new PointF(v.X, v.Y)));
            // For uniformity, I'm representing a point as a "line" between two of the same point
            // It's janky, I'll refactor later
            if (returnLine.Count == 1)
            {
                returnLine.Add(returnLine[0]);
            }
            if (returnLine.Count == 2)
            {
                returnLine.Normal = new PointF(facet.Normal.X, facet.Normal.Y);
                return returnLine;
            }
            // If no vertices are ON the z-index, find the two points where they CROSS it
            if ((verts[0].Z - z) / (verts[1].Z - z) < 0)
                returnLine.Add(CalculateZIntercept(verts[0], verts[1], z));
            if ((verts[2].Z - z) / (verts[1].Z - z) < 0)
                returnLine.Add(CalculateZIntercept(verts[2], verts[1], z));

            if (returnLine.Count < 2)
                returnLine.Add(CalculateZIntercept(verts[0], verts[2], z));

            returnLine.Normal = new PointF(facet.Normal.X, facet.Normal.Y);

            if (!returnLine.Validate())
                throw new InvalidOperationException("Invalid Point Data");

            return returnLine;
        }

        public static PointF CalculateZIntercept(Vertex v1, Vertex v2, float z)
        {
            var returnX = CalculateDimensionalValueAtIndex(new PointF(v1.Z, v1.X), new PointF(v2.Z, v2.X), z);
            var returnY = CalculateDimensionalValueAtIndex(new PointF(v1.Z, v1.Y), new PointF(v2.Z, v2.Y), z);
            return new PointF
            {
                X = returnX,
                Y = returnY
            };
        }

        public static float CalculateDimensionalValueAtIndex(PointF p1, PointF p2, float z, string precision = "0000")
        {
            var slope = (p1.Y - p2.Y) / (p1.X - p2.X);
            var intercept = p1.Y - (slope * p1.X);
            var rawVal = slope * z + intercept;
            // using floats we end up with some infinitesimal rounding errors, 
            // so we need to set the precision to something reasonable. Default is 1/100th of a micron
            // I'm sure there's a better way than converting it to a string and then back to a float,
            // but that's what I've got right now, so that's what I'm doing.
            var strVal = rawVal.ToString($"0.{precision}");
            return float.Parse(strVal, CultureInfo.InvariantCulture.NumberFormat);
        }
    }
}
