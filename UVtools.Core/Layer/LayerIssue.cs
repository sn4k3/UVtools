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
using UVtools.Core.Objects;

namespace UVtools.Core
{
    #region LayerIssue Class

    public sealed class IssuesDetectionConfiguration
    {
        public IslandDetectionConfiguration IslandConfig { get; }
        public OverhangDetectionConfiguration OverhangConfig { get; }
        public ResinTrapDetectionConfiguration ResinTrapConfig { get; }
        public TouchingBoundDetectionConfiguration TouchingBoundConfig { get; }
        public PrintHeightDetectionConfiguration PrintHeightConfig { get; }
        public bool EmptyLayerConfig { get; }

        public IssuesDetectionConfiguration(IslandDetectionConfiguration islandConfig,
            OverhangDetectionConfiguration overhangConfig, 
            ResinTrapDetectionConfiguration resinTrapConfig, 
            TouchingBoundDetectionConfiguration touchingBoundConfig,
            PrintHeightDetectionConfiguration printHeightConfig, 
            bool emptyLayerConfig)
        {
            IslandConfig = islandConfig;
            OverhangConfig = overhangConfig;
            ResinTrapConfig = resinTrapConfig;
            TouchingBoundConfig = touchingBoundConfig;
            PrintHeightConfig = printHeightConfig;
            EmptyLayerConfig = emptyLayerConfig;
        }
    }

    public sealed class IslandDetectionConfiguration
    {
        /// <summary>
        /// Gets or sets if the detection is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a list of layers to check for islands, absent layers will not be checked.
        /// Set to null to check every layer
        /// </summary>
        public List<uint> WhiteListLayers { get; set; } = null;

        /// <summary>
        /// Combines the island and overhang detections for a better more realistic detection and to discard false-positives. (Slower)
        /// If enabled, and when a island is found, it will check for overhangs on that same island, if no overhang found then the island will be discarded and considered safe, otherwise it will flag as an island issue.
        /// Note: Overhangs settings will be used to configure the detection.Enabling Overhangs is not required for this procedure to work.
        /// </summary>
        public bool EnhancedDetection { get; set; } = true;

        /// <summary>
        /// Gets the setting for whether or not diagonal bonds are considered when evaluation islands.
        /// If true, all 8 neighbors of a pixel (including diagonals) will be considered when finding
        /// individual components on the layer, if false only 4 neighbors (right, left, above, below)
        /// will be considered..
        /// </summary>
        public bool AllowDiagonalBonds { get; set; } = false;

        /// <summary>
        /// Gets or sets the binary threshold, all pixels below this value will turn in black, otherwise white
        /// Set to 0 to disable this operation 
        /// </summary>
        public byte BinaryThreshold { get; set; } = 1;

        /// <summary>
        /// Gets the required area size (x*y) to consider process a island (0-65535)
        /// </summary>
        public ushort RequiredAreaToProcessCheck { get; set; } = 1;

        /// <summary>
        /// Gets the required brightness for check a pixel under a island (0-255)
        /// </summary>
        public byte RequiredPixelBrightnessToProcessCheck { get; set; } = 10;

        /// <summary>
        /// Gets the required number of pixels to support a island and discard it as a issue (0-255)
        /// </summary>
        public byte RequiredPixelsToSupport { get; set; } = 10;

        /// <summary>
        /// Gets the required multiplier from the island pixels to support same island and discard it as a issue
        /// </summary>
        public decimal RequiredPixelsToSupportMultiplier { get; set; } = 0.25m;

        /// <summary>
        /// Gets the required brightness of supporting pixels to count as a valid support (0-255)
        /// </summary>
        public byte RequiredPixelBrightnessToSupport { get; set; } = 150;

        public IslandDetectionConfiguration(bool enabled = true)
        {
            Enabled = enabled;
        }

        public IslandDetectionConfiguration Clone()
        {
            var clone = MemberwiseClone() as IslandDetectionConfiguration;
            return clone;
        }
    }

    /// <summary>
    /// Overhang configuration
    /// </summary>
    public sealed class OverhangDetectionConfiguration
    {
        /// <summary>
        /// Gets or sets if the detection is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a list of layers to check for overhangs, absent layers will not be checked.
        /// Set to null to check every layer
        /// </summary>
        public List<uint> WhiteListLayers { get; set; } = null;

        /// <summary>
        /// Gets or sets if should take in consideration the islands, if yes a island can't be a overhang at same time, otherwise islands and overhangs can be shared
        /// </summary>
        public bool IndependentFromIslands { get; set; } = true;

        /// <summary>
        /// After compute overhangs, masses with a number of pixels bellow this number will be discarded (Not a overhang)
        /// </summary>
        public byte RequiredPixelsToConsider { get; set; } = 1;
        
        /// <summary>
        /// Previous layer will be subtracted from current layer, after will erode by this value.
        /// The survived pixels are potential overhangs.
        /// </summary>
        public byte ErodeIterations { get; set; } = 40;

        public OverhangDetectionConfiguration(bool enabled = true)
        {
            Enabled = enabled;
        }
    }

    public sealed class ResinTrapDetectionConfiguration
    {
        /// <summary>
        /// Gets or sets if the detection is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the starting layer index for the detection which will also be considered a drain layer.
        /// Use this setting to bypass complicated rafts by selected the model first real layer.
        /// </summary>
        public uint StartLayerIndex { get; set; }

        /// <summary>
        /// Gets or sets the binary threshold, all pixels below this value will turn in black, otherwise white
        /// Set to 0 to disable this operation
        /// </summary>
        public byte BinaryThreshold { get; set; } = 127;

        /// <summary>
        /// Gets the required area size (x*y) to consider process a hollow area (0-255)
        /// </summary>
        public byte RequiredAreaToProcessCheck { get; set; } = 1;

        /// <summary>
        /// Gets the number of black pixels required to consider a drain
        /// </summary>
        public byte RequiredBlackPixelsToDrain { get; set; } = 10;

        /// <summary>
        /// Gets the maximum pixel brightness to be a drain pixel (0-150)
        /// </summary>
        public byte MaximumPixelBrightnessToDrain { get; set; } = 30;

        /// <summary>
        /// Gets if suction cups can also be detected together with resin traps
        /// </summary>
        public bool DetectSuctionCups { get; set; } = true;

        /// <summary>
        /// Required minimum area to be considered a suction cup
        /// </summary>
        public uint RequiredAreaToConsiderSuctionCup { get; set; } = 100;

        public ResinTrapDetectionConfiguration(bool enabled = true)
        {
            Enabled = enabled;
        }
    }


    public sealed class TouchingBoundDetectionConfiguration
    {
        /// <summary>
        /// Gets if the detection is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets the minimum pixel brightness to be a touching bound
        /// </summary>
        public byte MinimumPixelBrightness { get; set; } = 127;

        /// <summary>
        /// Gets or sets the margin in pixels from left edge to check for touching white pixels
        /// </summary>
        public byte MarginLeft { get; set; } = 5;

        /// <summary>
        /// Gets or sets the margin in pixels from top to check for touching white pixels
        /// </summary>
        public byte MarginTop { get; set; } = 5;

        /// <summary>
        /// Gets or sets the margin in pixels from right edge to check for touching white pixels
        /// </summary>
        public byte MarginRight { get; set; } = 5;

        /// <summary>
        /// Gets or sets the margin in pixels from bottom edge to check for touching white pixels
        /// </summary>
        public byte MarginBottom { get; set; } = 5;


        public TouchingBoundDetectionConfiguration(bool enabled = true)
        {
            Enabled = enabled;
        }
    }

    public sealed class PrintHeightDetectionConfiguration
    {
        /// <summary>
        /// Gets if the detection is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Get the offset from top to sum to printer max Z height
        /// </summary>
        public float Offset { get; set; }

        public PrintHeightDetectionConfiguration(bool enabled = true)
        {
            Enabled = enabled;
        }
    }


    public class LayerIssue : IEquatable<LayerIssue>, IEnumerable<Point>
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

        public LayerIssue ParentIssue = null;
        public List<LayerIssue> ChildIssues = null;

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

        public LayerIssue(Layer layer, IssueType type, Point[] pixels = null, Point[][] contours = null, Rectangle boundingRectangle = default)
        {
            Layer = layer;
            Type = type;
            Contours = contours;
            Pixels = pixels;
            BoundingRectangle = boundingRectangle;
            Area = (uint)boundingRectangle.Area();
        }

        public LayerIssue(Layer layer, IssueType type, Point[] pixels = null, Rectangle boundingRectangle = default) :
            this(layer, type, pixels, null, boundingRectangle) { }


        public LayerIssue(Layer layer, IssueType type, Point[][] contours, Rectangle boundingRectangle = default) :
            this(layer, type, null, contours, boundingRectangle) { }

        public LayerIssue(Layer layer, IssueType type) :
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


        public bool Equals(LayerIssue other)
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
            return Equals((LayerIssue) obj);
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
