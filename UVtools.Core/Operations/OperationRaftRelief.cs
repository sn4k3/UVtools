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
using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV.Util;
using UVtools.Core.EmguCV;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;

[Serializable]
public class OperationRaftRelief : Operation
{
    #region Enums
    public enum RaftReliefTypes : byte
    {
        [Description("Relief: Drill raft to relief pressure and remove some mass")]
        Relief,

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
    private byte _brightness;
    private byte _dilateIterations = 15;// +/- 1.5mm radius
    private byte _wallMargin = 40;      // +/- 2mm
    private byte _holeDiameter = 80;    // +/- 4mm
    private byte _holeSpacing = 40;     // +/- 2mm
    private byte _tabBrightness = byte.MaxValue;
    private ushort _tabTriangleBase = 200;
    private ushort _tabTriangleHeight = 250;

    #endregion

    #region Overrides
    public override string IconClass => "fas fa-bowling-ball";
    public override string Title => "Raft relief";
    public override string Description =>
        "Relief raft by adding holes in between to reduce FEP suction, save resin and easier to remove the prints.";

    public override string ConfirmationText =>
        $"relief the raft";

    public override string ProgressTitle =>
        $"Relieving raft";

    public override string ProgressAction => "Relieved layers";

    public override LayerRangeSelection StartLayerRangeSelection =>
        LayerRangeSelection.None;

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();
        if (_reliefType == RaftReliefTypes.Tabs)
        {
            if(_tabTriangleHeight == 0) sb.AppendLine("The tab height can't be 0");
            if(_tabTriangleBase == 0) sb.AppendLine("The tab base can't be 0");
            if(_tabBrightness == 0) sb.AppendLine("The tab brightness can't be 0");
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"[{_reliefType}] [Mask layer: {_maskLayerIndex}] [Ignore: {_ignoreFirstLayers}] [B: {_brightness}] [Dilate: {_dilateIterations}] [Wall margin: {_wallMargin}] [Hole diameter: {_holeDiameter}] [Hole spacing: {_holeSpacing}]";
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
            RaisePropertyChanged(nameof(IsDimming));
            RaisePropertyChanged(nameof(IsDecimate));
            RaisePropertyChanged(nameof(IsTabs));
            RaisePropertyChanged(nameof(BrightnessPercent));
        }
    }

    public bool IsRelief => _reliefType == RaftReliefTypes.Relief;
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

    public byte Brightness
    {
        get => _brightness;
        set
        {
            if (!RaiseAndSetIfChanged(ref _brightness, value)) return;
            RaisePropertyChanged(nameof(BrightnessPercent));
        }
    }

    public decimal BrightnessPercent => Math.Round((_reliefType == RaftReliefTypes.Tabs ? _tabBrightness : _brightness) * 100 / 255M, 2);

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

    public byte TabBrightness
    {
        get => _tabBrightness;
        set => RaiseAndSetIfChanged(ref _tabBrightness, Math.Max((byte)1, value));
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
        return _reliefType == other._reliefType && _maskLayerIndex == other._maskLayerIndex && _ignoreFirstLayers == other._ignoreFirstLayers && _brightness == other._brightness && _dilateIterations == other._dilateIterations && _wallMargin == other._wallMargin && _holeDiameter == other._holeDiameter && _holeSpacing == other._holeSpacing && _tabTriangleBase == other._tabTriangleBase && _tabTriangleHeight == other._tabTriangleHeight;
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
        hashCode.Add(_brightness);
        hashCode.Add(_dilateIterations);
        hashCode.Add(_wallMargin);
        hashCode.Add(_holeDiameter);
        hashCode.Add(_holeSpacing);
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
        var anchor = new Point(-1, -1);
        var kernel = EmguExtensions.Kernel3x3Rectangle;

        uint firstSupportLayerIndex = _maskLayerIndex;
        if (firstSupportLayerIndex <= 0)
        {
            uint layerCount = Math.Min(SlicerFile.LayerCount, maxLayerCount);
            progress.Reset("Tracing raft", layerCount, firstSupportLayerIndex);
            for (; firstSupportLayerIndex < layerCount; firstSupportLayerIndex++)
            {
                progress.ThrowIfCancellationRequested();
                supportsMat = GetRoiOrDefault(SlicerFile[firstSupportLayerIndex].LayerMat);
                var circles = CvInvoke.HoughCircles(supportsMat, HoughModes.Gradient, 1, 5, 80, 35, 5, 200);
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

        if (DilateIterations > 0)
        {
            CvInvoke.Dilate(supportsMat, supportsMat,
                CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1)),
                new Point(-1, -1), DilateIterations, BorderType.Reflect101, new MCvScalar());
        }

        var color = new MCvScalar(255 - Brightness);

        switch (ReliefType)
        {
            case RaftReliefTypes.Relief:
                patternMat = EmguExtensions.InitMat(supportsMat.Size);
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
            case RaftReliefTypes.Dimming:
                patternMat = EmguExtensions.InitMat(supportsMat.Size, color);
                break;
        }

        progress.Reset(ProgressAction, firstSupportLayerIndex - _ignoreFirstLayers);
        Parallel.For(_ignoreFirstLayers, firstSupportLayerIndex, CoreSettings.GetParallelOptions(progress), layerIndex =>
        {
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

                        CvInvoke.Erode(target, mask, kernel, anchor, WallMargin, BorderType.Reflect101, default);
                        CvInvoke.Subtract(mask, supportsMat, mask);
                        CvInvoke.Subtract(target, patternMat, target, mask);
                    }

                    break;
                case RaftReliefTypes.Decimate:
                    supportsMat.CopyTo(target);
                    break;
                case RaftReliefTypes.Tabs:
                {
                    using var contours = mat.FindContours(RetrType.External);
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

                    var color = new MCvScalar(_tabBrightness);

                    for (var i = 0; i < contours.Size; i++)
                    {
                        using var contour = new EmguContour(contours[i]);
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
                                   && contour.Bounds.Contains(x, y))
                            {
                                var pixel = span[mat.GetPixelPos(x, y)];

                                if (pixel > 0)
                                {
                                    if (!foundFirstWhite)
                                    {
                                        foundFirstWhite = true;
                                        continue;
                                    }

                                    if (CvInvoke.PointPolygonTest(contours[i], new PointF(x, y), false) == 0) // Must be on edge
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

                            CvInvoke.Ellipse(mat, new Point(x, y), new Size(triangleBaseRadius, (int)(triangleBaseRadius / 1.5)), 90 * dir, 0, 180, EmguExtensions.WhiteColor, -1, LineType.AntiAlias);
                            using var vec = new VectorOfPoint(polygon);
                            CvInvoke.FillPoly(mat, vec, color, LineType.AntiAlias);
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