/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using CommunityToolkit.Mvvm.ComponentModel;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using EmguExtensions;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public partial class OperationRaftRelief : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Enums
    public enum RaftReliefTypes : byte
    {
        [Description("Relief: Drill raft to relief pressure and remove some mass")]
        Relief,

        [Description("Linked lines: Remove the raft, keep supports and link them with lines")]
        LinkedLines,

        [Description("Dimming: Darkens the raft to cure it less")]
        Dimming,

        [Description("Decimate: Remove the raft and keep supports only on the plate")]
        Decimate,

        [Description("Tabs: Creates tabs around the raft to easily insert a tool under it and detach the raft from build plate")]
        Tabs
    }
    #endregion

    #region Members
    private byte _linkedLineThickness = 26;
    private byte _highBrightness = byte.MaxValue;
    private ushort _tabTriangleBase = 200;
    private ushort _tabTriangleHeight = 250;

    #endregion

    #region Overrides
    public override string IconClass => "Bowling";
    public override string Title => "Raft relief";
    public override string Description =>
        "Relief raft with a strategy to remove mass, reduce FEP suction, spare resin and easier to remove the prints.";

    public override string ConfirmationText =>
        $"relief the raft";

    public override string ProgressTitle =>
        $"Relieving raft";

    public override string ProgressAction => "Relieved layers";

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();
        if (ReliefType == RaftReliefTypes.Tabs)
        {
            if(_tabTriangleHeight == 0) sb.AppendLine("The tab height can't be 0");
            if(_tabTriangleBase == 0) sb.AppendLine("The tab base can't be 0");
            if(_highBrightness == 0) sb.AppendLine("The tab brightness can't be 0");
        }
        else if (ReliefType == RaftReliefTypes.LinkedLines)
        {
            if(_linkedLineThickness < 4) sb.AppendLine("The link thickness can't be less than 4");
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"[{ReliefType}] [Mask layer: {MaskLayerIndex}] [Ignore: {IgnoreFirstLayers}] [B: {LowBrightness}] [Dilate: {DilateIterations}] [Wall margin: {WallMargin}] [Hole diameter: {HoleDiameter}] [Hole spacing: {HoleSpacing}]";
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }
    #endregion

    #region Constructor

    public OperationRaftRelief() { }

    public OperationRaftRelief(FileFormat slicerFile) : base(slicerFile) { }

    #endregion

    #region Properties
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRelief))]
    [NotifyPropertyChangedFor(nameof(IsLinkedLines))]
    [NotifyPropertyChangedFor(nameof(IsDimming))]
    [NotifyPropertyChangedFor(nameof(IsDecimate))]
    [NotifyPropertyChangedFor(nameof(IsTabs))]
    [NotifyPropertyChangedFor(nameof(BrightnessPercent))]
    public partial RaftReliefTypes ReliefType { get; set; } = RaftReliefTypes.Relief;

    public bool IsRelief => ReliefType == RaftReliefTypes.Relief;
    public bool IsLinkedLines => ReliefType == RaftReliefTypes.LinkedLines;
    public bool IsDimming => ReliefType == RaftReliefTypes.Dimming;
    public bool IsDecimate => ReliefType == RaftReliefTypes.Decimate;
    public bool IsTabs => ReliefType == RaftReliefTypes.Tabs;

    [ObservableProperty]
    public partial uint MaskLayerIndex { get; set; }

    [ObservableProperty]
    public partial byte IgnoreFirstLayers { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BrightnessPercent))]
    public partial byte LowBrightness { get; set; }

    public byte HighBrightness
    {
        get => _highBrightness;
        set
        {
            if(!SetProperty(ref _highBrightness, Math.Max((byte) 1, value))) return;
            OnPropertyChanged(nameof(BrightnessPercent));
        }
    }

    public decimal BrightnessPercent => Math.Round((ReliefType is RaftReliefTypes.LinkedLines or RaftReliefTypes.Tabs ? _highBrightness : LowBrightness) * 100 / 255M, 2);

    [ObservableProperty]
    public partial byte DilateIterations { get; set; } = 15;

    [ObservableProperty]
    public partial byte WallMargin { get; set; } = 40;

    [ObservableProperty]
    public partial byte HoleDiameter { get; set; } = 80;

    [ObservableProperty]
    public partial byte HoleSpacing { get; set; } = 40;

    public byte LinkedLineThickness
    {
        get => _linkedLineThickness;
        set => SetProperty(ref _linkedLineThickness, Math.Max((byte)4, value));
    }

    [ObservableProperty]
    public partial byte LinkedMinimumLinks { get; set; } = 4;

    [ObservableProperty]
    public partial bool LinkedExternalSupports { get; set; } = true;

    public ushort TabTriangleBase
    {
        get => _tabTriangleBase;
        set => SetProperty(ref _tabTriangleBase, Math.Max((ushort)5, value));
    }

    public ushort TabTriangleHeight
    {
        get => _tabTriangleHeight;
        set
        {
            if (!SetProperty(ref _tabTriangleHeight, Math.Max((ushort)5, value))) return;
            OnPropertyChanged(nameof(BrightnessPercent));
        }
    }

    #endregion

    #region Equality

    protected bool Equals(OperationRaftRelief other)
    {
        return ReliefType == other.ReliefType && MaskLayerIndex == other.MaskLayerIndex && IgnoreFirstLayers == other.IgnoreFirstLayers && LowBrightness == other.LowBrightness && DilateIterations == other.DilateIterations && WallMargin == other.WallMargin && HoleDiameter == other.HoleDiameter && HoleSpacing == other.HoleSpacing && _linkedLineThickness == other._linkedLineThickness && LinkedMinimumLinks == other.LinkedMinimumLinks && LinkedExternalSupports == other.LinkedExternalSupports && _highBrightness == other._highBrightness && _tabTriangleBase == other._tabTriangleBase && _tabTriangleHeight == other._tabTriangleHeight;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((OperationRaftRelief) obj);
    }

    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        progress.ItemCount = 0;
        const uint minSupportsRequired = 5;
        const uint maxLayerCount = 1000;

        Mat? supportsMat = null;

        var kernel = EmguCvExtensions.Kernel3X3Rectangle;

        uint firstSupportLayerIndex = MaskLayerIndex;
        if (firstSupportLayerIndex <= 0)
        {
            uint layerCount = Math.Min(SlicerFile.LayerCount, maxLayerCount);
            progress.Reset("Tracing raft", layerCount, firstSupportLayerIndex);
            for (; firstSupportLayerIndex < layerCount; firstSupportLayerIndex++)
            {
                progress.PauseOrCancelIfRequested();
                supportsMat = GetRoiOrDefault(SlicerFile[firstSupportLayerIndex].LayerMat);
                //var circles = CvInvoke.HoughCircles(supportsMat, HoughModes.Gradient, 1, 5, 80, 35, 5, 255); // OLD
                var circles = CvInvoke.HoughCircles(supportsMat, HoughModes.GradientAlt, 1.5, 25, 300, 0.80, 5, 255);
                if (circles.Length >= minSupportsRequired) break;

                supportsMat.Dispose();
                supportsMat = null;
                progress++;
            }
        }
        else
        {
            supportsMat = GetRoiOrDefault(SlicerFile[firstSupportLayerIndex].LayerMat);
        }

        if (supportsMat is null || /*firstSupportLayerIndex == 0 ||*/ IgnoreFirstLayers >= firstSupportLayerIndex) return false;
        Mat? patternMat = null;
        using var supportsMatOriginal = supportsMat.Clone();

        if (DilateIterations > 0)
        {
            CvInvoke.Dilate(supportsMat, supportsMat, EmguCvExtensions.Kernel3X3Rectangle,
                EmguCvExtensions.AnchorCenter, DilateIterations, BorderType.Reflect101, new MCvScalar());
        }

        var color = new MCvScalar(255 - LowBrightness);

        switch (ReliefType)
        {
            case RaftReliefTypes.Relief:
                patternMat = supportsMat.NewZeros();
                int shapeSize = HoleDiameter + HoleSpacing;
                using (var shape = EmguCvExtensions.InitMat(new Size(shapeSize, shapeSize)))
                {

                    int center = HoleDiameter / 2;
                    //int centerTwo = operation.HoleDiameter + operation.HoleSpacing + operation.HoleDiameter / 2;
                    var radius = SlicerFile.PixelsToNormalizedPitch(center);
                    shape.DrawCircle(new Point(shapeSize / 2, shapeSize / 2), radius, color, -1);
                    shape.DrawCircle(new Point(0, 0), radius / 2, color, -1);
                    shape.DrawCircle(new Point(0, shapeSize), radius / 2, color, -1);
                    shape.DrawCircle(new Point(shapeSize, 0), radius / 2, color, -1);
                    shape.DrawCircle(new Point(shapeSize, shapeSize), radius / 2, color, -1);

                    CvInvoke.Repeat(shape, supportsMat.Height / shape.Height + 1, supportsMat.Width / shape.Width + 1, patternMat);

                    patternMat = new Mat(patternMat, new Rectangle(0, 0, supportsMat.Width, supportsMat.Height));
                }

                break;
            case RaftReliefTypes.LinkedLines:
            {
                using var contours = new EmguContours(supportsMatOriginal, RetrType.List);
                using var supportsRedraw = LinkedExternalSupports ? supportsMatOriginal.Clone() : null;
                using var supportsBrightnessCorrection = _highBrightness < byte.MaxValue ? supportsMat.Clone() : null;

                var centroidDistance = contours.CalculateCentroidDistances(false, true);

                var links = Math.Min(LinkedMinimumLinks, contours.Count-1);
                var linkColor = new MCvScalar(_highBrightness);

                //var listPoints = new List<Point>();

                for (int i = 0; i < contours.Count; i++)
                {
                    if(contours[i].Centroid.IsAnyNegative()) continue;
                    //listPoints.Add(contours[i].Centroid);

                    // Link all centroids to each other to calculate the external contour
                    if (LinkedExternalSupports)
                    {
                        for (int x = 0; x < contours.Count; x++)
                        {
                            if (x == i) continue;
                            if (contours[x].Centroid.IsAnyNegative()) continue;
                            CvInvoke.Line(supportsRedraw, contours[i].Centroid, contours[x].Centroid, linkColor, 4);
                        }
                    }

                    for (int link = 0; link < links; link++)
                    {
                        CvInvoke.Line(supportsMat, contours[i].Centroid, centroidDistance[i][link].Contour.Centroid, linkColor, _linkedLineThickness);
                    }
                }


                //CvInvoke.Polylines(supportsMat, listPoints.ToArray(), false, linkColor, _linkedLineThickness);
                // Link external centroids
                if (LinkedExternalSupports)
                {
                    using var externalContours = supportsRedraw!.FindContours(RetrType.External);
                    CvInvoke.DrawContours(supportsMat, externalContours, -1, linkColor, _linkedLineThickness);
                }

                // Fix original supports brightness
                if (_highBrightness < byte.MaxValue)
                {
                    supportsBrightnessCorrection!.CopyTo(supportsMat, supportsBrightnessCorrection);
                }

                // Close minor holes, round imperfections, stronger joints
                CvInvoke.MorphologyEx(supportsMat, supportsMat, MorphOp.Close, EmguCvExtensions.Kernel3X3Rectangle, EmguCvExtensions.AnchorCenter, 1, BorderType.Reflect101, default);

                break;
            }
            case RaftReliefTypes.Dimming:
                patternMat = EmguCvExtensions.InitMat(supportsMat.Size, color);
                break;
        }

        progress.Reset(ProgressAction, firstSupportLayerIndex - IgnoreFirstLayers);
        Parallel.For(IgnoreFirstLayers, firstSupportLayerIndex, CoreSettings.GetParallelOptions(progress), layerIndex =>
        {
            progress.PauseIfRequested();
            using var mat = SlicerFile[layerIndex].LayerMat;
            using var original = mat.Clone();
            using var target = GetRoiOrDefault(mat);

            switch (ReliefType)
            {
                case RaftReliefTypes.Relief:
                case RaftReliefTypes.Dimming:
                    using (Mat mask = new())
                    {
                        /*CvInvoke.Subtract(target, supportsMat, mask);
                            CvInvoke.Erode(mask, mask, kernel, anchor, operation.WallMargin, BorderType.Reflect101, new MCvScalar());
                            CvInvoke.Subtract(target, patternMat, target, mask);*/

                        CvInvoke.Erode(target, mask, kernel, EmguCvExtensions.AnchorCenter, WallMargin, BorderType.Reflect101, default);
                        CvInvoke.Subtract(mask, supportsMat, mask);
                        CvInvoke.Subtract(target, patternMat, target, mask);
                    }

                    break;
                case RaftReliefTypes.LinkedLines:
                case RaftReliefTypes.Decimate:
                    supportsMat.CopyTo(target);
                    break;
                case RaftReliefTypes.Tabs:
                {
                    using var contours = new EmguContours(mat, RetrType.External);
                    var span = mat.GetReadOnlySpanOfBytes();

                    var minX = 10;
                    var maxX = mat.Size.Width - 10;
                    var minY = 10;
                    var maxY = mat.Size.Height - 10;

                    var triangleBaseRadius = _tabTriangleBase / 2;
                    var triangleHeightRadius = _tabTriangleHeight / 2;

                    var directions = new[]
                    {
                        new Point(0, -1),  // Up
                        new Point(1, 0),   // Right
                        new Point(0, 1),   // Down
                        new Point(-1, 0),  // Left
                    };

                    var color = new MCvScalar(_highBrightness);
                    foreach(var contour in contours)
                    {
                        if(contour.Centroid.IsAnyNegative() || contour.Area < 10000) continue;

                        for (var dir = 0; dir < directions.Length; dir++)
                        {
                            var direction = directions[dir];

                            var foundFirstWhite = false;
                            var foundPoint = false;

                            var x = contour.Centroid.X;
                            var y = contour.Centroid.Y;

                            while (!foundPoint
                                   && x >= minX && x <= maxX && y >= minY && y <= maxY
                                   && contour.BoundingRectangle.Contains(x, y))
                            {
                                var pixel = span[mat.GetPixelPos(x, y)];

                                if (pixel > 0)
                                {
                                    if (!foundFirstWhite)
                                    {
                                        foundFirstWhite = true;
                                        continue;
                                    }

                                    if (CvInvoke.PointPolygonTest(contour.Vector, new PointF(x, y), false) == 0) // Must be on edge
                                    {
                                        foundPoint = true;
                                        break;
                                    }
                                }


                                x += direction.X;
                                y += direction.Y;
                            }

                            if(!foundPoint) continue;

                            var polygon = new Point[3];

                            switch (dir)
                            {
                                case 0: // Up
                                    polygon[0] = new Point(Math.Max(10, x - triangleBaseRadius), y);
                                    polygon[1] = new Point(Math.Min(mat.Width - 10, x + triangleBaseRadius), y);
                                    polygon[2] = new Point(x, Math.Max(10, y - triangleHeightRadius));
                                    break;
                                case 1: // Right
                                    polygon[0] = new Point(x, Math.Max(10, y - triangleBaseRadius));
                                    polygon[1] = new Point(x, Math.Min(mat.Width - 10, y + triangleBaseRadius));
                                    polygon[2] = new Point(Math.Min(mat.Width - 10, x + triangleHeightRadius), y);
                                    break;
                                case 2: // Down
                                    polygon[0] = new Point(Math.Max(10, x - triangleBaseRadius), y);
                                    polygon[1] = new Point(Math.Min(mat.Width - 10, x + triangleBaseRadius), y);
                                    polygon[2] = new Point(x, Math.Min(mat.Height - 10, y + triangleHeightRadius));
                                    break;
                                case 3: // Left
                                    polygon[0] = new Point(x, Math.Max(10, y - triangleBaseRadius));
                                    polygon[1] = new Point(x, Math.Min(mat.Width - 10, y + triangleBaseRadius));
                                    polygon[2] = new Point(Math.Max(10, x - triangleHeightRadius), y);
                                    break;
                            }

                            CvInvoke.Ellipse(mat, new Point(x, y), new Size(triangleBaseRadius, (int)(triangleBaseRadius / 1.5)), 90 * dir, 0, 180, EmguCvExtensions.WhiteColor, -1);
                            using var vec = new VectorOfPoint(polygon);
                            CvInvoke.FillPoly(mat, vec, color);
                        }

                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(ReliefType));
            }

            ApplyMask(original, target);
            SlicerFile[layerIndex].LayerMat = mat;

            progress.LockAndIncrement();
        });


        supportsMat.Dispose();
        patternMat?.Dispose();

        return !progress.Token.IsCancellationRequested;
    }

    #endregion
}