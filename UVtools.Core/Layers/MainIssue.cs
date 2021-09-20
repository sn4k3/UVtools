/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UVtools.Core.Extensions;

namespace UVtools.Core.Layers
{
    public class MainIssue : IReadOnlyList<Issue>
    {
        public enum IssueType : byte
        {
            Island,
            Overhang,
            ResinTrap,
            SuctionCup,
            TouchingBound,
            PrintHeight,
            EmptyLayer,
            Debug
            //HoleSandwich,
        }

        /// <summary>
        /// Gets the issue type associated
        /// </summary>
        public IssueType Type { get; init; }

        /// <summary>
        /// Gets the layer where issue is present and starts
        /// </summary>
        public Layer StartLayer => Childs[0].Layer;

        /// <summary>
        /// Gets the layer where issue ends
        /// </summary>
        public Layer EndLayer => Childs[^1].Layer;

        /// <summary>
        /// Gets the layer index
        /// </summary>
        public uint StartLayerIndex => StartLayer.Index;

        /// <summary>
        /// Gets the layer index
        /// </summary>
        public uint EndLayerIndex => EndLayer.Index;

        /// <summary>
        /// Gets the number of layers in this range
        /// </summary>
        public uint LayerRangeCount => 1 + EndLayerIndex - StartLayerIndex;

        public string LayerInfoStr => StartLayerIndex == EndLayerIndex
            ? $"{StartLayerIndex}"
            : $"{StartLayerIndex}-{EndLayerIndex}  ({LayerRangeCount})";


        /// <summary>
        /// Gets the bounding rectangle of the area
        /// </summary>
        public Rectangle BoundingRectangle { get; init; }

        /// <summary>
        /// Gets the area of the issue
        /// </summary>
        public uint PixelCount { get; init; }

        /// <summary>
        /// Gets the area of the issue
        /// </summary>
        public double Area { get; init; }

        /// <summary>
        /// Gets all issues inside this main issue
        /// </summary>
        public Issue[] Childs { get; init; }

        public MainIssue(IssueType type, Rectangle boundingRectangle = default)
        {
            Type = type;
            BoundingRectangle = boundingRectangle;
            Area = boundingRectangle.Area();
        }

        public MainIssue(IssueType type, Issue issue) : this(type, issue.BoundingRectangle)
        {
            Childs = new[] { issue };
            issue.Parent = this;
            PixelCount = issue.PixelsCount;
        }

        public MainIssue(IssueType type, IEnumerable<Issue> issues) : this(type)
        {
            var boundingRectangle = Rectangle.Empty;
            double area = 0;
            foreach (var issue in issues)
            {
                issue.Parent = this;
                area += issue.Area / issue.Layer.LayerHeight;
                PixelCount += issue.PixelsCount;
                if (issue.BoundingRectangle.IsEmpty) continue;
                if (boundingRectangle.IsEmpty)
                {
                    boundingRectangle = issue.BoundingRectangle;
                    continue;
                }

                boundingRectangle.Intersect(issue.BoundingRectangle);
            }

            BoundingRectangle = boundingRectangle;
            Area = area;
            Childs = issues.ToArray();
            Sort();
        }

        private void Sort()
        {
            Array.Sort(Childs, (issue, issue1) => issue.LayerIndex.CompareTo(issue1.LayerIndex));
        }

        public bool IsIssueInBetween(int layerIndex) => layerIndex >= StartLayerIndex && layerIndex <= EndLayerIndex;
        public bool IsIssueInBetween(uint layerIndex) => layerIndex >= StartLayerIndex && layerIndex <= EndLayerIndex;
        public bool IsIssueInBetween(Layer layer) => IsIssueInBetween(layer.Index);


        public IEnumerator<Issue> GetEnumerator()
        {
            return ((IEnumerable<Issue>)Childs).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Childs.GetEnumerator();
        }

        public int Count => Childs.Length;

        public Issue this[int index] => Childs[index];

        protected bool Equals(MainIssue other)
        {
            return Type == other.Type && BoundingRectangle.Equals(other.BoundingRectangle) && PixelCount == other.PixelCount && Area.Equals(other.Area) && Childs.SequenceEqual(other.Childs);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MainIssue)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)Type, BoundingRectangle, PixelCount, Area, Childs);
        }
    }
}
