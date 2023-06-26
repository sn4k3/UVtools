/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using UVtools.Core.EmguCV;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;


public class OperationRaftRelief : Operation
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
    private RaftReliefTypes _reliefType = RaftReliefTypes.Relief;
    private uint _maskLayerIndex;
    private byte _ignoreFirstLayers;
    private byte _lowBrightness;
    private byte _dilateIterations = 15;// +/- 1.5mm radius
    private byte _wallMargin = 40;      // +/- 2mm
    private byte _holeDiameter = 80;    // +/- 4mm
    private byte _holeSpacing = 40;     // +/- 2mm
    private byte _linkedLineThickness = 26;
    private byte _linkedMinimumLinks = 4;
    private bool _linkedExternalSupports = true;
    private byte _highBrightness = byte.MaxValue;
    private ushort _tabTriangleBase = 200;
    private ushort _tabTriangleHeight = 250;

    #endregion

    #region Overrides
    public override string IconClass => "fa-solid fa-bowling-ball";
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
        if (_reliefType == RaftReliefTypes.Tabs)
        {
            if(_tabTriangleHeight == 0) sb.AppendLine("The tab height can't be 0");
            if(_tabTriangleBase == 0) sb.AppendLine("The tab base can't be 0");
            if(_highBrightness == 0) sb.AppendLine("The tab brightness can't be 0");
        }
        else if (_reliefType == RaftReliefTypes.LinkedLines)
        {
            if(_linkedLineThickness < 4) sb.AppendLine("The link thickness can't be less than 4");
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"[{_reliefType}] [Mask layer: {_maskLayerIndex}] [Ignore: {_ignoreFirstLayers}] [B: {_lowBrightness}] [Dilate: {_dilateIterations}] [Wall margin: {_wallMargin}] [Hole diameter: {_holeDiameter}] [Hole spacing: {_holeSpacing}]";
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }
    #endregion

    #region Constructor

    public OperationRaftRelief() { }

    public OperationRaftRelief(FileFormat slicerFile) : base(slicerFile) { }

    #endregion

    #region Properties
    public RaftReliefTypes ReliefType
    {
        get => _reliefType;
        set
        {
            if(!RaiseAndSetIfChanged(ref _reliefType, value)) return;
            RaisePropertyChanged(nameof(IsRelief));
            RaisePropertyChanged(nameof(IsLinkedLines));
            RaisePropertyChanged(nameof(IsDimming));
            RaisePropertyChanged(nameof(IsDecimate));
            RaisePropertyChanged(nameof(IsTabs));
            RaisePropertyChanged(nameof(BrightnessPercent));
        }
    }

    public bool IsRelief => _reliefType == RaftReliefTypes.Relief;
    public bool IsLinkedLines => _reliefType == RaftReliefTypes.LinkedLines;
    public bool IsDimming => _reliefType == RaftReliefTypes.Dimming;
    public bool IsDecimate => _reliefType == RaftReliefTypes.Decimate;
    public bool IsTabs => _reliefType == RaftReliefTypes.Tabs;

    public uint MaskLayerIndex
    {
        get => _maskLayerIndex;
        set => RaiseAndSetIfChanged(ref _maskLayerIndex, value);
    }

    public byte IgnoreFirstLayers
    {
        get => _ignoreFirstLayers;
        set => RaiseAndSetIfChanged(ref _ignoreFirstLayers, value);
    }

    public byte LowBrightness
    {
        get => _lowBrightness;
        set
        {
            if (!RaiseAndSetIfChanged(ref _lowBrightness, value)) return;
            RaisePropertyChanged(nameof(BrightnessPercent));
        }
    }

    public byte HighBrightness
    {
        get => _highBrightness;
        set
        {
            if(!RaiseAndSetIfChanged(ref _highBrightness, Math.Max((byte) 1, value))) return;
            RaisePropertyChanged(nameof(BrightnessPercent));
        }
    }

    public decimal BrightnessPercent => Math.Round((_reliefType is RaftReliefTypes.LinkedLines or RaftReliefTypes.Tabs ? _highBrightness : _lowBrightness) * 100 / 255M, 2);

    public byte DilateIterations
    {
        get => _dilateIterations;
        set => RaiseAndSetIfChanged(ref _dilateIterations, value);
    }

    public byte WallMargin
    {
        get => _wallMargin;
        set => RaiseAndSetIfChanged(ref _wallMargin, value);
    }

    public byte HoleDiameter
    {
        get => _holeDiameter;
        set => RaiseAndSetIfChanged(ref _holeDiameter, value);
    }

    public byte HoleSpacing
    {
        get => _holeSpacing;
        set => RaiseAndSetIfChanged(ref _holeSpacing, value);
    }

    public byte LinkedLineThickness
    {
        get => _linkedLineThickness;
        set => RaiseAndSetIfChanged(ref _linkedLineThickness, Math.Max((byte)4, value));
    }

    public byte LinkedMinimumLinks
    {
        get => _linkedMinimumLinks;
        set => RaiseAndSetIfChanged(ref _linkedMinimumLinks, value);
    }

    public bool LinkedExternalSupports
    {
        get => _linkedExternalSupports;
        set => RaiseAndSetIfChanged(ref _linkedExternalSupports, value);
    }

    public ushort TabTriangleBase
    {
        get => _tabTriangleBase;
        set => RaiseAndSetIfChanged(ref _tabTriangleBase, Math.Max((ushort)5, value));
    }

    public ushort TabTriangleHeight
    {
        get => _tabTriangleHeight;
        set
        {
            if (!RaiseAndSetIfChanged(ref _tabTriangleHeight, Math.Max((ushort)5, value))) return;
            RaisePropertyChanged(nameof(BrightnessPercent));
        } 
    }

    #endregion

    #region Equality

    protected bool Equals(OperationRaftRelief other)
    {
        return _reliefType == other._reliefType && _maskLayerIndex == other._maskLayerIndex && _ignoreFirstLayers == other._ignoreFirstLayers && _lowBrightness == other._lowBrightness && _dilateIterations == other._dilateIterations && _wallMargin == other._wallMargin && _holeDiameter == other._holeDiameter && _holeSpacing == other._holeSpacing && _linkedLineThickness == other._linkedLineThickness && _linkedMinimumLinks == other._linkedMinimumLinks && _linkedExternalSupports == other._linkedExternalSupports && _highBrightness == other._highBrightness && _tabTriangleBase == other._tabTriangleBase && _tabTriangleHeight == other._tabTriangleHeight;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((OperationRaftRelief) obj);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add((int) _reliefType);
        hashCode.Add(_maskLayerIndex);
        hashCode.Add(_ignoreFirstLayers);
        hashCode.Add(_lowBrightness);
        hashCode.Add(_dilateIterations);
        hashCode.Add(_wallMargin);
        hashCode.Add(_holeDiameter);
        hashCode.Add(_holeSpacing);
        hashCode.Add(_linkedLineThickness);
        hashCode.Add(_linkedMinimumLinks);
        hashCode.Add(_linkedExternalSupports);
        hashCode.Add(_highBrightness);
        hashCode.Add(_tabTriangleBase);
        hashCode.Add(_tabTriangleHeight);
        return hashCode.ToHashCode();
    }

    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        progress.ItemCount = 0;
        const uint minSupportsRequired = 5;
        const uint maxLayerCount = 1000;

        Mat? supportsMat = null;

        var kernel = EmguExtensions.Kernel3x3Rectangle;

        uint firstSupportLayerIndex = _maskLayerIndex;
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

        if (supportsMat is null || /*firstSupportLayerIndex == 0 ||*/ _ignoreFirstLayers >= firstSupportLayerIndex) return false;
        Mat? patternMat = null;
        using var supportsMatOriginal = supportsMat.Clone();

        if (_dilateIterations > 0)
        {
            CvInvoke.Dilate(supportsMat, supportsMat, EmguExtensions.Kernel3x3Rectangle,
                EmguExtensions.AnchorCenter, _dilateIterations, BorderType.Reflect101, new MCvScalar());
        }

        var color = new MCvScalar(255 - _lowBrightness);

        switch (ReliefType)
        {
            case RaftReliefTypes.Relief:
                patternMat = supportsMat.NewBlank();
                int shapeSize = HoleDiameter + HoleSpacing;
                using (var shape = EmguExtensions.InitMat(new Size(shapeSize, shapeSize)))
                {

                    int center = HoleDiameter / 2;
                    //int centerTwo = operation.HoleDiameter + operation.HoleSpacing + operation.HoleDiameter / 2;
                    int radius = center;
                    CvInvoke.Circle(shape, new Point(shapeSize / 2, shapeSize / 2), radius, color, -1);
                    CvInvoke.Circle(shape, new Point(0, 0), radius / 2, color, -1);
                    CvInvoke.Circle(shape, new Point(0, shapeSize), radius / 2, color, -1);
                    CvInvoke.Circle(shape, new Point(shapeSize, 0), radius / 2, color, -1);
                    CvInvoke.Circle(shape, new Point(shapeSize, shapeSize), radius / 2, color, -1);

                    CvInvoke.Repeat(shape, supportsMat.Height / shape.Height + 1, supportsMat.Width / shape.Width + 1, patternMat);

                    patternMat = new Mat(patternMat, new Rectangle(0, 0, supportsMat.Width, supportsMat.Height));
                }

                break;
            case RaftReliefTypes.LinkedLines:
            {
                using var contours = new EmguContours(supportsMatOriginal);
                using var supportsRedraw = _linkedExternalSupports ? supportsMatOriginal.Clone() : null;
                using var supportsBrightnessCorrection = _highBrightness < byte.MaxValue ? supportsMat.Clone() : null;

                var centroidDistance = contours.CalculateCentroidDistances(false, true);

                var links = Math.Min(_linkedMinimumLinks, contours.Count-1);
                var linkColor = new MCvScalar(_highBrightness);

                //var listPoints = new List<Point>();

                for (int i = 0; i < contours.Count; i++)
                {
                    if(contours[i].Centroid.IsAnyNegative()) continue;
                    //listPoints.Add(contours[i].Centroid);

                    // Link all centroids to each other to calculate the external contour
                    if (_linkedExternalSupports)
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
                if (_linkedExternalSupports)
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
                CvInvoke.MorphologyEx(supportsMat, supportsMat, MorphOp.Close, EmguExtensions.Kernel3x3Rectangle, EmguExtensions.AnchorCenter, 1, BorderType.Reflect101, default);

                break;
            }
            case RaftReliefTypes.Dimming:
                patternMat = EmguExtensions.InitMat(supportsMat.Size, color);
                break;
        }

        progress.Reset(ProgressAction, firstSupportLayerIndex - _ignoreFirstLayers);
        Parallel.For(_ignoreFirstLayers, firstSupportLayerIndex, CoreSettings.GetParallelOptions(progress), layerIndex =>
        {
            progress.PauseIfRequested();
            using var mat = SlicerFile[layerIndex].LayerMat;
            using var original = mat.Clone();
            var target = GetRoiOrDefault(mat);

            switch (ReliefType)
            {
                case RaftReliefTypes.Relief:
                case RaftReliefTypes.Dimming:
                    using (Mat mask = new())
                    {
                        /*CvInvoke.Subtract(target, supportsMat, mask);
                            CvInvoke.Erode(mask, mask, kernel, anchor, operation.WallMargin, BorderType.Reflect101, new MCvScalar());
                            CvInvoke.Subtract(target, patternMat, target, mask);*/

                        CvInvoke.Erode(target, mask, kernel, EmguExtensions.AnchorCenter, WallMargin, BorderType.Reflect101, default);
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
                    var span = mat.GetDataByteSpan();

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

                                    if (CvInvoke.PointPolygonTest(contour.Contour, new PointF(x, y), false) == 0) // Must be on edge
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

                            CvInvoke.Ellipse(mat, new Point(x, y), new Size(triangleBaseRadius, (int)(triangleBaseRadius / 1.5)), 90 * dir, 0, 180, EmguExtensions.WhiteColor, -1);
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