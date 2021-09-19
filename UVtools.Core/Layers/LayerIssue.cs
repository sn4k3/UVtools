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
using UVtools.Core.Layers;
using UVtools.Core.Objects;

namespace UVtools.Core
{
    #region LayerIssue Class

    public class LayerIssueOLD : IEquatable<LayerIssueOLD>, IEnumerable<Point>
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
        /// Gets the parent layer
        /// </summary>
        public Layer Layer { get; init; }

        /// <summary>
        /// Gets the layer index
        /// </summary>
        public uint LayerIndex => Layer.Index;

        /// <summary>
        /// Gets the issue type associated
        /// </summary>
        public IssueType Type { get; init; }

        /// <summary>
        /// Gets the pixels points containing the issue
        /// </summary>
        public Point[] Pixels { get; init; }

        /// <summary>
        /// Gets the contours containing the issue
        /// </summary>
        public Point[][] Contours { get; init; }

        public int PixelsCount
        {
            get
            {
                if (Pixels is not null) return Pixels.Length;
                if (Contours is not null) return (int)Area;
                return 0;
            }
        }

        /// <summary>
        /// Gets the bounding rectangle of the pixel area
        /// </summary>
        public Rectangle BoundingRectangle { get; init; }

        /// <summary>
        /// Gets the area of the issue
        /// </summary>
        public double Area { get; init; }

        /// <summary>
        /// Gets the X coordinate for the first point, -1 if doesn't exists
        /// </summary>
        public int X => FirstPoint.X;

        /// <summary>
        /// Gets the Y coordinate for the first point, -1 if doesn't exists
        /// </summary>
        public int Y => FirstPoint.Y;

        /// <summary>
        /// Gets the XY point for first point
        /// </summary>
        public Point FirstPoint
        {
            get
            {
                if (Pixels?.Length > 0) return Pixels[0];
                if (Contours?.Length > 0) return Contours[0][0];
                return new Point(-1, -1);
            }
        }

        public string FirstPointStr => $"{FirstPoint.X}, {FirstPoint.Y}";

        /// <summary>
        /// Check if this issue have a valid start point to show
        /// </summary>
        public bool HaveValidPoint => Pixels?.Length > 0 || Contours?.Length > 0;

        public LayerIssueOLD(Layer layer, IssueType type, Point[] pixels = null, Point[][] contours = null, Rectangle boundingRectangle = default)
        {
            Layer = layer;
            Type = type;
            Contours = contours;
            Pixels = pixels;
            BoundingRectangle = boundingRectangle;
            Area = (uint)boundingRectangle.Area();
        }

        public LayerIssueOLD(Layer layer, IssueType type, Point[] pixels = null, Rectangle boundingRectangle = default) :
            this(layer, type, pixels, null, boundingRectangle) { }


        public LayerIssueOLD(Layer layer, IssueType type, Point[][] contours, Rectangle boundingRectangle = default) :
            this(layer, type, null, contours, boundingRectangle) { }

        public LayerIssueOLD(Layer layer, IssueType type) :
            this(layer, type, null, null, default)
        { }

        public Point this[uint index] => Pixels[index];

        public Point this[int index] => Pixels[index];

        public IEnumerator<Point> GetEnumerator()
        {
            return ((IEnumerable<Point>)Pixels).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return $"{nameof(Type)}: {Type}, Layer: {Layer.Index}, {nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(PixelsCount)}: {PixelsCount}";
        }


        public bool Equals(LayerIssueOLD other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Layer.Index == other.Layer.Index
                   && Type == other.Type 
                   && PixelsCount == other.PixelsCount 
                   && Pixels is not null && other.Pixels is not null && Pixels.SequenceEqual(other.Pixels)
                   && Contours is not null && other.Contours is not null && Contours.SequenceEqual(other.Contours)
                   //&& BoundingRectangle.Equals(other.BoundingRectangle)
                ;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((LayerIssueOLD) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Layer != null ? Layer.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) Type;
                hashCode = (hashCode * 397) ^ (Pixels != null ? Pixels.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Contours != null ? Contours.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ BoundingRectangle.GetHashCode();
                return hashCode;
            }
        }
    }
    #endregion

    #region LayerHollowArea

    public class LayerHollowArea : IEnumerable<Point[]>
    {
        public enum AreaType : byte
        {
            Unknown = 0,
            Trap,
            Drain
        }
        /// <summary>
        /// Gets contours
        /// </summary>
        public Point[][] Contours { get; }

        public Rectangle BoundingRectangle { get; }

        public double Area { get; set; }

        public AreaType Type { get; set; } = AreaType.Unknown;

        public bool Processed { get; set; }

        #region Indexers
        public Point[] this[uint index]
        {
            get => index < Contours.Length ? Contours[index] : null;
            set => Contours[index] = value;
        }

        public Point[] this[int index]
        {
            get => index < Contours.Length ? Contours[index] : null;
            set => Contours[index] = value;
        }

        #endregion

        public IEnumerator<Point[]> GetEnumerator()
        {
            return ((IEnumerable<Point[]>)Contours).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public LayerHollowArea() { }

        public LayerHollowArea(Point[][] contours, Rectangle boundingRectangle, double area, AreaType type = AreaType.Unknown)
        {
            Contours = contours;
            BoundingRectangle = boundingRectangle;
            Area = area;
            Type = type;
        }
    }
    #endregion

    #region ResinTrapGround

    public sealed class ResinTrapGroup : BindableBase, IList<LayerHollowArea>
    {
        #region List Implementation
        private readonly List<LayerHollowArea> hollowAreaList = new();
        private LayerHollowArea.AreaType _currentAreaType = LayerHollowArea.AreaType.Trap;

        public IEnumerator<LayerHollowArea> GetEnumerator()
        {
            return hollowAreaList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) hollowAreaList).GetEnumerator();
        }

        public void Add(LayerHollowArea item)
        {
            if (item.Type == LayerHollowArea.AreaType.Drain)
            {
                CurrentAreaType = LayerHollowArea.AreaType.Drain;
            }
            else if (_currentAreaType == LayerHollowArea.AreaType.Drain)
            {
                item.Type = LayerHollowArea.AreaType.Drain;
            }
            hollowAreaList.Add(item);
        }

        public void Add(ResinTrapGroup group)
        {
            foreach (var area in group)
            {
                Add(area);
            }
        }

        public void Clear()
        {
            CurrentAreaType = LayerHollowArea.AreaType.Trap;
            hollowAreaList.Clear();
        }

        public bool Contains(LayerHollowArea item)
        {
            return hollowAreaList.Contains(item);
        }

        public void CopyTo(LayerHollowArea[] array, int arrayIndex)
        {
            hollowAreaList.CopyTo(array, arrayIndex);
        }

        public bool Remove(LayerHollowArea item)
        {
            var result = hollowAreaList.Remove(item);
            if (Count == 0) CurrentAreaType = LayerHollowArea.AreaType.Trap;
            return result;
        }

        public int Count => hollowAreaList.Count;

        public bool IsReadOnly => false;

        public int IndexOf(LayerHollowArea item)
        {
            return hollowAreaList.IndexOf(item);
        }

        public void Insert(int index, LayerHollowArea item)
        {
            if (item.Type == LayerHollowArea.AreaType.Drain)
            {
                CurrentAreaType = LayerHollowArea.AreaType.Drain;
            }
            else if (_currentAreaType == LayerHollowArea.AreaType.Drain)
            {
                item.Type = LayerHollowArea.AreaType.Drain;
            }
            hollowAreaList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            hollowAreaList.RemoveAt(index);
            if (Count == 0) CurrentAreaType = LayerHollowArea.AreaType.Trap;
        }

        public LayerHollowArea this[int index]
        {
            get => hollowAreaList[index];
            set => hollowAreaList[index] = value;
        }
        #endregion

        #region Properties
        public LayerHollowArea.AreaType CurrentAreaType
        {
            get => _currentAreaType;
            set
            {
                if(!RaiseAndSetIfChanged(ref _currentAreaType, value)) return;
                foreach (var area in this) // Update previous items
                {
                    area.Type = _currentAreaType;
                }
            }
        }
        #endregion
    }

    public sealed class ResinTrapTree
    {
        public List<ResinTrapGroup> Groups { get; } = new();

        public ResinTrapGroup FindHollowGroup(LayerHollowArea hollowArea)
        {
            var i = FindHollowGroupIndex(hollowArea);
            return i >= 0 ? Groups[i] : null;
        }

        public int FindHollowGroupIndex(LayerHollowArea hollowArea)
        {
            for (var i = 0; i < Groups.Count; i++)
            {
                if (Groups[i].Any(area => ReferenceEquals(area, hollowArea)))
                {
                    return i;
                }
            }

            return -1;
        }

        public ResinTrapGroup AddRoot(LayerHollowArea hollowArea) => AddRoot(hollowArea, out _);
        public ResinTrapGroup AddRoot(LayerHollowArea hollowArea, out int index)
        {
            index = FindHollowGroupIndex(hollowArea);
            if (index < 0) // Not found
            {
                index = Groups.Count;
                Groups.Add(new(){hollowArea});
            }
            else // Exists
            {
                Groups[index].Add(hollowArea);
            }

            return Groups[index];
        }

        public ResinTrapGroup AddChild(ResinTrapGroup group, LayerHollowArea hollowArea)
        {
            // This will find if the area exists in any other group,
            // If yes then the groups are merged, otherwise it will be added to the parent group

            var findGroup = FindHollowGroup(hollowArea);
            if (findGroup is not null && !ReferenceEquals(group, findGroup))
            {
                return MergeGroups(group, findGroup, true);
            }
            if(group.IndexOf(hollowArea) == -1) group.Add(hollowArea);
            return group;
        }

        /// <summary>
        /// Merges two groups
        /// </summary>
        /// <param name="group1"></param>
        /// <param name="group2"></param>
        /// <param name="manageGroups">True to remove old groups and add the new to group list</param>
        /// <returns>A new group instance holding group1 and group2 items</returns>
        public ResinTrapGroup MergeGroups(ResinTrapGroup group1, ResinTrapGroup group2, bool manageGroups = false)
        {
            ResinTrapGroup newGroup = new (){group1, group2};
            if (manageGroups)
            {
                Groups.Remove(group1);
                Groups.Remove(group2);
                Groups.Add(newGroup);
            }
            return newGroup;
        }
    }
    #endregion

}
