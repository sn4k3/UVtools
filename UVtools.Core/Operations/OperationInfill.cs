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
using EmguExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using ZLinq;

namespace UVtools.Core.Operations;


#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public sealed partial class OperationInfill : Operation, IEquatable<OperationInfill>
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
{
    #region Members

    #endregion

    #region Overrides
    public override string IconClass => "Checkerboard";
    public override string Title => "Infill";

    public override string Description =>
        $"Generate infill patterns in the model.\n\nNOTES:\n1) You must exclude floor and ceil layers from the range.\n2) You must take care of drain holes after the operation.";

    public override string ConfirmationText =>
        $"infill model with {InfillType} from layers {LayerIndexStart} through {LayerIndexEnd}?";

    public override string ProgressTitle =>
        $"Infill model with {InfillType} from layers {LayerIndexStart} through {LayerIndexEnd}";

    public override string ProgressAction => "Infilled layers";

    #endregion

    #region Enums
    public enum InfillAlgorithm
    {
        //Rhombus,
        [Description("Straight pillars (Weak)")]
        Pillars,

        [Description("Concentric (Weak)")]
        Concentric,

        [Description("Waves (Medium)")]
        Waves,

        [Description("Cubic cross: Fixed pilars with crossing sections (Optimal)")]
        CubicCross,

        [Description("Cubic alternating cross: Fixed pilars with crossing sections alternating in directions (Optimal)")]
        CubicCrossAlternating,

        [Description("Cubic star: Fixed pilars with crossing sections in star pattern (Strong)")]
        CubicStar,

        [Description("Honeycomb (Strong)")]
        Honeycomb,

        [Description("Gyroid (Strong)")]
        Gyroid,
    }
    #endregion

    #region Properties
    [ObservableProperty]
    public partial InfillAlgorithm InfillType { get; set; } = InfillAlgorithm.CubicCrossAlternating;

    [ObservableProperty]
    public partial decimal FloorCeilThickness { get; set; } = 3.0m;

    [ObservableProperty]
    public partial ushort WallThickness { get; set; } = 64;

    [ObservableProperty]
    public partial byte InfillBrightness { get; set; } = 255;

    [ObservableProperty]
    public partial ushort InfillThickness { get; set; } = 45;

    [ObservableProperty]
    public partial ushort InfillSpacing { get; set; } = 300;

    [ObservableProperty]
    public partial bool ReinforceInfill { get; set; }

    public override string ToString()
    {
        var result = $"[{InfillType}] [Floor/Ceil: {FloorCeilThickness}mm] [Wall: {WallThickness}px] [B: {InfillBrightness}px] [T: {InfillThickness}px] [S: {InfillSpacing}px] [R: {ReinforceInfill}]" + LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    #endregion

    #region Constructor

    public OperationInfill() { }

    public OperationInfill(FileFormat slicerFile) : base(slicerFile) { }

    #endregion

    #region Equality

    public bool Equals(OperationInfill? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return InfillType == other.InfillType && FloorCeilThickness == other.FloorCeilThickness && WallThickness == other.WallThickness && InfillThickness == other.InfillThickness && InfillSpacing == other.InfillSpacing && InfillBrightness == other.InfillBrightness && ReinforceInfill == other.ReinforceInfill;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationInfill other && Equals(other);
    }

    public static bool operator ==(OperationInfill? left, OperationInfill? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(OperationInfill? left, OperationInfill? right)
    {
        return !Equals(left, right);
    }

    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        Mat? mask = null;
        if (InfillType == InfillAlgorithm.Honeycomb)
        {
            mask = GetHoneycombMask(GetRoiSizeOrVolumeSize());

        }
        else if (InfillType == InfillAlgorithm.Concentric)
        {
            mask = GetConcentricMask(GetRoiSizeOrVolumeSize());
        }

#if DEBUG
        /*
        if (mask is not null)
        {
            using var previewMat = new Mat();
            CvInvoke.Resize(mask, previewMat, new Size(mask.Width / 4, mask.Height / 4));
            CvInvoke.Imshow("Honeycomb", previewMat);
            CvInvoke.WaitKey();
        }*/
#endif

        var clonedLayers = SlicerFile.CloneLayers();

        Parallel.For(LayerIndexStart, LayerIndexEnd + 1, CoreSettings.GetParallelOptions(progress), layerIndex =>
        {
            progress.PauseIfRequested();
            using var mat = SlicerFile[layerIndex].LayerMat;
            Execute(mat, layerIndex, mask!, clonedLayers);
            SlicerFile[layerIndex].LayerMat = mat;

            progress.LockAndIncrement();
        });
        mask?.Dispose();
        return !progress.Token.IsCancellationRequested;
    }

    public override bool Execute(Mat mat, params object[]? arguments)
    {
        if (arguments is null || arguments.Length < 3) return false;

        var kernel = EmguCvExtensions.Kernel3X3Rectangle;
        var infillColor = new MCvScalar(InfillBrightness);
        uint index = Convert.ToUInt32(arguments[0]);
        uint layerIndex = index - LayerIndexStart;
        var clonedLayers = (Layer[])arguments[2];

        Mat? patternMask = null;
        using var erode = new Mat();
        using var diff = new Mat();
        using var target = GetRoiOrVolumeBounds(mat);
        using var mask = GetMask(mat);
        bool disposeTargetMask = true;

        if (InfillType is InfillAlgorithm.Pillars
            or InfillAlgorithm.CubicCross
            or InfillAlgorithm.CubicCrossAlternating
            or InfillAlgorithm.CubicStar)
        {
            using var infillPattern = EmguCvExtensions.InitMat(new Size(InfillSpacing, InfillSpacing));
            using var matPattern = mat.NewZeros();
            bool firstPattern = true;
            uint accumulator = 0;
            bool dynamicCenter = false;
            while (accumulator < layerIndex)
            {
                dynamicCenter = !dynamicCenter;
                firstPattern = true;
                accumulator += InfillSpacing;

                if (accumulator >= layerIndex) break;

                if (ReinforceInfill)
                {
                    firstPattern = false;
                    accumulator += InfillThickness;
                }
            }

            if (firstPattern)
            {
                int thickness = InfillThickness / 2;
                // Top Left
                CvInvoke.Rectangle(infillPattern,
                    new Rectangle(0, 0, thickness, thickness),
                    infillColor, -1);

                // Top Right
                CvInvoke.Rectangle(infillPattern,
                    new Rectangle(infillPattern.Width - thickness, 0, thickness, thickness),
                    infillColor, -1);

                // Bottom Left
                CvInvoke.Rectangle(infillPattern,
                    new Rectangle(0, infillPattern.Height - thickness, thickness, thickness),
                    infillColor, -1);

                // Bottom Right
                CvInvoke.Rectangle(infillPattern,
                    new Rectangle(infillPattern.Width - thickness, infillPattern.Height - thickness,
                        thickness, thickness),
                    infillColor, -1);

                // Center cross
                int margin = (int) (InfillSpacing - accumulator + layerIndex) - thickness;
                int marginInv = (int) (accumulator - layerIndex) - thickness;

                if (InfillType == InfillAlgorithm.CubicCross ||
                    (InfillType == InfillAlgorithm.CubicCrossAlternating &&
                     dynamicCenter) ||
                    InfillType == InfillAlgorithm.CubicStar)
                {

                    CvInvoke.Rectangle(infillPattern,
                        new Rectangle(margin, margin, InfillThickness, InfillThickness),
                        infillColor, -1);

                    CvInvoke.Rectangle(infillPattern,
                        new Rectangle(marginInv, marginInv, InfillThickness,
                            InfillThickness),
                        infillColor, -1);

                    CvInvoke.Rectangle(infillPattern,
                        new Rectangle(margin, marginInv, InfillThickness,
                            InfillThickness),
                        infillColor, -1);

                    CvInvoke.Rectangle(infillPattern,
                        new Rectangle(marginInv, margin, InfillThickness,
                            InfillThickness),
                        infillColor, -1);
                }


                if (InfillType == InfillAlgorithm.CubicStar ||
                    (InfillType == InfillAlgorithm.CubicCrossAlternating &&
                     !dynamicCenter))
                {
                    CvInvoke.Rectangle(infillPattern,
                        new Rectangle(margin, -thickness, InfillThickness,
                            InfillThickness),
                        infillColor, -1);

                    CvInvoke.Rectangle(infillPattern,
                        new Rectangle(marginInv, -thickness, InfillThickness,
                            InfillThickness),
                        infillColor, -1);

                    CvInvoke.Rectangle(infillPattern,
                        new Rectangle(-thickness, margin, InfillThickness,
                            InfillThickness),
                        infillColor, -1);

                    CvInvoke.Rectangle(infillPattern,
                        new Rectangle(-thickness, marginInv, InfillThickness,
                            InfillThickness),
                        infillColor, -1);

                    CvInvoke.Rectangle(infillPattern,
                        new Rectangle(InfillSpacing - thickness, margin,
                            InfillThickness, InfillThickness),
                        infillColor, -1);

                    CvInvoke.Rectangle(infillPattern,
                        new Rectangle(InfillSpacing - thickness, marginInv,
                            InfillThickness, InfillThickness),
                        infillColor, -1);

                    CvInvoke.Rectangle(infillPattern,
                        new Rectangle(margin, InfillSpacing - thickness,
                            InfillThickness, InfillThickness),
                        infillColor, -1);

                    CvInvoke.Rectangle(infillPattern,
                        new Rectangle(marginInv, InfillSpacing - thickness,
                            InfillThickness, InfillThickness),
                        infillColor, -1);
                }
            }
            else
            {
                CvInvoke.Rectangle(infillPattern,
                    new Rectangle(0, 0, InfillSpacing, InfillSpacing),
                    infillColor, InfillThickness);
            }


            CvInvoke.Repeat(infillPattern, target.Rows / infillPattern.Rows + 1, target.Cols / infillPattern.Cols + 1, matPattern);
            patternMask = matPattern.Roi(target);
            disposeTargetMask = true;
        }
        else if (InfillType == InfillAlgorithm.Honeycomb)
        {
            if (arguments.Length >= 2)
            {
                patternMask = (Mat)arguments[1];
                disposeTargetMask = false;
            }
            else
            {
                patternMask = GetHoneycombMask(target.Size);
                disposeTargetMask = true;
            }
        }
        else if (InfillType == InfillAlgorithm.Concentric)
        {
            if (arguments.Length >= 2)
            {
                patternMask = (Mat)arguments[1];
                disposeTargetMask = false;
            }
            else
            {
                patternMask = GetConcentricMask(target.Size);
                disposeTargetMask = true;
            }
        }
        else if (InfillType == InfillAlgorithm.Waves)
        {
            var sineHeight = 100;
            var sineWidth = 100;
            var radius = (ushort)(InfillThickness / 2);

            var points = new List<Point>();

            bool isHorizontal = true;
            float accumulator = 0;
            for (int i = 0; i <= layerIndex; i++)
            {
                accumulator += SlicerFile[index].RelativePositionZ;
                if (accumulator >= 2)
                {
                    isHorizontal = !isHorizontal;
                    accumulator = 0;
                }
            }

            //using var infillPattern = EmguCvExtensions.InitMat(new Size(InfillSpacing, InfillSpacing));
            //using var matPattern = new Mat();
            using var infillPattern = mat.NewZeros();

            int maxY = 0;

            if (isHorizontal)
            {
                for (int x = 0; x < mat.Width; x += radius)
                {
                    int y = (int) (Math.Sin((double) x / sineWidth /*+ sineWidth * layerIndex*/) * sineHeight / 2.0 +
                                   sineHeight / 2.0 + radius);
                    points.Add(new Point(x, y));
                    maxY = Math.Max(maxY, y);
                }
                using var infillPatternRoi = infillPattern.Roi(new Size(infillPattern.Width, maxY + radius + 2 + InfillSpacing));
                CvInvoke.Polylines(infillPatternRoi, points.ToArray(), false, infillColor, InfillThickness);

                CvInvoke.Repeat(infillPatternRoi, target.Rows / infillPatternRoi.Rows + 1, 1, infillPattern);
            }
            else
            {
                for (int y = 0; y < mat.Height; y += radius)
                {
                    int x = (int)(Math.Sin((double)y / sineWidth /*+ sineWidth * layerIndex*/) * sineHeight / 2.0 +
                                  sineHeight / 2.0 + radius);
                    points.Add(new Point(x, y));
                    maxY = Math.Max(maxY, x);
                }
                using var infillPatternRoi = infillPattern.Roi(new Size(maxY + radius + 2 + InfillSpacing, infillPattern.Height));
                CvInvoke.Polylines(infillPatternRoi, points.ToArray(), false, infillColor, InfillThickness);
                CvInvoke.Repeat(infillPatternRoi, 1, target.Cols / infillPatternRoi.Cols + 1, infillPattern);

            }
            points.Clear();

            patternMask = infillPattern.Roi(target);
            disposeTargetMask = true;
        }
        else if (InfillType == InfillAlgorithm.Gyroid)
        {
            patternMask = target.NewZeros();

            var scaleRatio = 0.0012 / (InfillSpacing + InfillThickness / 2);
            //var scaleX = 0.04 * InfillSpacing * Math.PI / target.Width;
            //var scaleY = 0.04 * InfillSpacing * Math.PI / target.Height;
            var scaleX = scaleRatio * mat.Width;
            var scaleY = scaleRatio * mat.Height;

            //const double scaleZ = 2.0 * Math.PI;
            //var dz = 2.0 * scaleZ / LayerRangeCount; // z step
            //var zz = 0.05 * (SlicerFile[index].LayerHeight / 0.05f) * (layerIndex + 1);
            float zz = 0;
            for (var i = LayerIndexStart; i <= index; i++)
            {
                zz += SlicerFile[i].RelativePositionZ;
            }

            for (int y = 0; y < patternMask.Height; y++)
            {
                var span = patternMask.GetRowSpanOfBytes(y);
                var yy = y * scaleY; // y position of pixel
                for (int x = 0; x < patternMask.Width; x++)
                {
                    var xx = x * scaleX; // x position of pixel

                    var d = Math.Sin(xx) * Math.Cos(yy) // compute gyroid equation
                            + Math.Sin(yy) * Math.Cos(zz)
                            + Math.Sin(zz) * Math.Cos(xx);
                    //if (d > 1e-6) continue; // Far from surface
                    if (Math.Abs(d) - 0.006*InfillThickness > 0) continue;
                    //if (d - 0.05 > 1e-6) continue;

                    span[x] = InfillBrightness;
                }
            }


            //using var contours = patternMask.FindContours();
            //CvInvoke.DrawContours(patternMask, contours, -1, infillColor, InfillThickness);

            disposeTargetMask = true;
        }

        using var surfaceMat = target.Clone();

        decimal heightAccumulator = (decimal)SlicerFile[index].LayerHeight;
        if (FloorCeilThickness > heightAccumulator)
        {
            for (int floorLayerIndex = (int)(index - 1); heightAccumulator <= FloorCeilThickness && floorLayerIndex >= 0; floorLayerIndex--)
            {
                using var floorMat = clonedLayers[floorLayerIndex].LayerMat;
                using var floorMatRoi = GetRoiOrVolumeBounds(floorMat);

                CvInvoke.BitwiseAnd(surfaceMat, floorMatRoi, surfaceMat);

                heightAccumulator += (decimal)SlicerFile[floorLayerIndex + 1].PositionZ - (decimal)SlicerFile[floorLayerIndex].PositionZ;
            }

            if (heightAccumulator <= FloorCeilThickness) return true;

            heightAccumulator = (decimal)SlicerFile[index].LayerHeight;
            for (var ceilLayerIndex = index + 1; heightAccumulator <= FloorCeilThickness && ceilLayerIndex <= SlicerFile.LastLayerIndex; ceilLayerIndex++)
            {
                using var ceilMat = clonedLayers[ceilLayerIndex].LayerMat;
                using var ceilMatRoi = GetRoiOrVolumeBounds(ceilMat);

                CvInvoke.BitwiseAnd(surfaceMat, ceilMatRoi, surfaceMat);

                heightAccumulator += (decimal)SlicerFile[ceilLayerIndex].PositionZ - (decimal)SlicerFile[ceilLayerIndex - 1].PositionZ;
            }

            if (heightAccumulator <= FloorCeilThickness) return true;
        }


        //patternMask.Save("D:\\pattern.png");
        CvInvoke.Erode(target, erode, kernel, EmguCvExtensions.AnchorCenter, WallThickness, BorderType.Reflect101, default);

        CvInvoke.BitwiseAnd(erode, surfaceMat, erode, mask);
        patternMask!.CopyTo(target, erode);
        //target.SetTo(EmguCvExtensions.BlackColor, erode);
        //erode.CopyTo(target);
        //CvInvoke.BitwiseOr(target, patternMask, target, erode);


        //CvInvoke.Subtract(target, erode, diff);
        //CvInvoke.BitwiseAnd(erode, patternMask, target, mask);
        //CvInvoke.Add(target, diff, target, mask);

        if (disposeTargetMask)
        {
            patternMask.Dispose();
        }

        return true;
    }

    public Mat GetHoneycombMask(Size targetSize)
    {
        var patternMask = EmguCvExtensions.InitMat(targetSize);

        var halfInfillSpacingD = InfillSpacing / 2.0;
        var halfInfillSpacing = (int)Math.Round(halfInfillSpacingD);
        var halfThickenss = InfillThickness / 2;
        int width = (int)Math.Round(4 * (halfInfillSpacingD / Math.Sqrt(3)));
        var infillColor = new MCvScalar(InfillBrightness);

        int cols = (int)Math.Ceiling(targetSize.Width / (width * 0.75f));
        int rows = (int)Math.Ceiling((float)targetSize.Height / InfillSpacing);

        for (int col = 0; col <= cols; col++)
        {
            for (int row = 0; row <= rows; row++)
            {
                // Move over for the column number.
                int x = (int)Math.Round(halfThickenss + col * (width * 0.75f));

                // Move down the required number of rows.
                int y = halfThickenss + halfInfillSpacing + row * InfillSpacing;

                // If the column is odd, move down half a hex more.
                if (col % 2 == 1) y += halfInfillSpacing;

                var points = new Point[]
                {
                    new(x, y),
                    new((int) Math.Round(x + width * 0.25f), y - halfInfillSpacing),
                    new((int) Math.Round(x + width * 0.75f), y - halfInfillSpacing),
                    new(x + width, y),
                    new((int) Math.Round(x + width * 0.75f), y + halfInfillSpacing),
                    new((int) Math.Round(x + width * 0.25f), y + halfInfillSpacing),
                };

                CvInvoke.Polylines(patternMask, points, true, infillColor, InfillThickness);
            }
        }

        return patternMask;
    }

    public Mat GetConcentricMask(Size targetSize)
    {
        var patternMask = EmguCvExtensions.InitMat(targetSize);

        //var halfInfillSpacing = InfillSpacing / 2;
        //var halfThickenss = InfillThickness / 2;
        int multiplier = 1;
        byte position = 0;

        int x = patternMask.Width / 2;
        int y = patternMask.Height / 2;

        Point[] directions =
        [
            new(0, -InfillSpacing), // top
            new(InfillSpacing, 0), // right
            new(0, InfillSpacing), // bottom
            new(-InfillSpacing, 0) // left
        ];

        bool[] hitLimits =
        [
            false,
            false,
            false,
            false
        ];

        var points = new List<Point> {new(x, y)};

        while (hitLimits.AsValueEnumerable().Any(hitLimit => !hitLimit))
        {
            x += directions[position].X * multiplier;
            y += directions[position].Y * multiplier;
            if (x < 0 || y < 0 || x >= patternMask.Width || y >= patternMask.Height) hitLimits[position] = true;
            points.Add(new Point(x, y));
            position++;
            if (position == 2)
            {
                multiplier++;
            }
            else if (position == 4)
            {
                multiplier++;
                position = 0;
            }
        }


        CvInvoke.Polylines(patternMask, points.ToArray(), false, new MCvScalar(InfillBrightness), InfillThickness);

        return patternMask;
    }

    #endregion
}