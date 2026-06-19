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
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public partial class OperationRedrawModel : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Enums
    public enum RedrawModelOperators : byte
    {
        [Description("Set: to a brightness")]
        Set,
        [Description("Add: with a brightness")]
        Add,
        [Description("Subtract: with a brightness")]
        Subtract,
        [Description("Multiply: with a brightness")]
        Multiply,
        [Description("Divide: with a brightness")]
        Divide,
        [Description("Minimum: set to a brightness if is lower than the current pixel")]
        Minimum,
        [Description("Maximum: set to a brightness if is higher than the current pixel")]
        Maximum,
        [Description("AbsDiff: perform a absolute difference between pixel and brightness")]
        AbsDiff,
    }

    #endregion

    #region Members


    #endregion

    #region Overrides

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;
    public override string IconClass => "PuzzleEdit";
    public override string Title => "Redraw model/supports";

    public override string Description =>
        "Redraw the model or supports with a set brightness. This requires an extra sliced file from same object but without any supports and raft, straight to the build plate.\n" +
        "Note: Run this tool prior to any made modification. You must find the optimal exposure/brightness combo, or supports can fail.";

    public override string ConfirmationText => "redraw the "+ (RedrawType == RedrawTypes.Supports ? "supports" : "model") +
                                               " with an"+ (RedrawType == RedrawTypes.Model || !ContactPointsOnly ? $" {Operator}" : string.Empty) + $" brightness of {Brightness}?";

    public override string ProgressTitle => "Redrawing " + (RedrawType == RedrawTypes.Supports ? "supports" : "model");

    public override string ProgressAction => "Redraw layers";

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        if (IsFileValid() is null)
        {
            sb.AppendLine("The selected file is not valid.");
        }

        if (RedrawType == RedrawTypes.Model || ContactPointsOnly)
        {
            switch (Operator)
            {
                case RedrawModelOperators.Add:
                case RedrawModelOperators.Subtract:
                case RedrawModelOperators.Maximum:
                case RedrawModelOperators.AbsDiff:
                    if (Brightness == 0) sb.AppendLine($"{Operator} with a brightness of 0 yield no result, please use a value larger than 0.");
                    break;
                case RedrawModelOperators.Divide:
                    if (Brightness == 0) sb.AppendLine($"{Operator} with a brightness of 0 is not valid, please use a value larger than 0.");
                    else if (Brightness == 1) sb.AppendLine($"{Operator} with a brightness of 0 yield no result, please use a value larger than 0.");
                    break;
            }
        }

        return sb.ToString();
    }


    public override string ToString()
    {
        var result = $"[{RedrawType}] [B: {Brightness}] [OP: {Operator}] [CS: {ContactPointsOnly}] [ICLP: {IgnoreContactLessPixels}]";
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }


    #endregion

    #region Enums
    public enum RedrawTypes : byte
    {
        Supports,
        Model,
    }
    #endregion

    #region Constructor

    public OperationRedrawModel() { }

    public OperationRedrawModel(FileFormat slicerFile) : base(slicerFile) { }

    #endregion

    #region Properties

    [XmlIgnore]
    [ObservableProperty]
    public partial string FilePath { get; set; } = null!;

    [ObservableProperty]
    public partial RedrawTypes RedrawType { get; set; } = RedrawTypes.Supports;

    [ObservableProperty]
    public partial RedrawModelOperators Operator { get; set; } = RedrawModelOperators.Minimum;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BrightnessPercent))]
    public partial byte Brightness { get; set; } = 220;

    public decimal BrightnessPercent => Math.Round(Brightness * 100 / 255M, 2);

    [ObservableProperty]
    public partial bool ContactPointsOnly { get; set; } = true;

    [ObservableProperty]
    public partial bool IgnoreContactLessPixels { get; set; } = true;

    #endregion

    #region Equality

    protected bool Equals(OperationRedrawModel other)
    {
        return Brightness == other.Brightness && ContactPointsOnly == other.ContactPointsOnly && RedrawType == other.RedrawType && IgnoreContactLessPixels == other.IgnoreContactLessPixels;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((OperationRedrawModel) obj);
    }

    #endregion

    #region Methods

    public FileFormat? IsFileValid(bool returnNewInstance = false) =>
        FileFormat.FindByExtensionOrFilePath(FilePath, returnNewInstance);

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        var otherFile = IsFileValid(true);
        if (otherFile is null)
        {
            return false;
        }
        otherFile.Decode(FilePath, progress);

        progress.Reset(ProgressAction, otherFile.LayerCount);

        int startLayerIndex = (int)(SlicerFile.LayerCount - otherFile.LayerCount);
        if (startLayerIndex < 0) return false;
        Parallel.For(0, otherFile.LayerCount, CoreSettings.GetParallelOptions(progress), layerIndex =>
        {
            if (SlicerFile[layerIndex].IsEmpty)
            {
                progress.LockAndIncrement();
                return;
            }
            progress.PauseIfRequested();
            var fullMatLayerIndex = startLayerIndex + layerIndex;
            using var fullMat = SlicerFile[fullMatLayerIndex].LayerMat;
            using var original = fullMat.Clone();
            using var bodyMat = otherFile[layerIndex].LayerMat;
            using var fullMatRoi = GetRoiOrDefault(fullMat);
            using var bodyMatRoi = GetRoiOrDefault(bodyMat);
            using var supportsMat = new Mat();

            bool modified = false;
            if (RedrawType == RedrawTypes.Supports && ContactPointsOnly)
            {
                if (layerIndex + 1 >= otherFile.LayerCount) return;
                CvInvoke.Subtract(fullMatRoi, bodyMatRoi, supportsMat); // Supports
                using var contours = supportsMat.FindContours(RetrType.List);
                if (contours.Size <= 0) return;
                using var nextLayerMat = otherFile[layerIndex + 1].LayerMat;
                using var nextLayerMatRoi = GetRoiOrDefault(nextLayerMat);
                var fullSpan = fullMatRoi.GetSpanOfBytes(0, 0);
                var supportsSpan = supportsMat.GetReadOnlySpanOfBytes();
                var nextSpan = nextLayerMatRoi.GetReadOnlySpanOfBytes();
                for (int i = 0; i < contours.Size; i++)
                {
                    var foundContour = false;
                    var rectangle = CvInvoke.BoundingRectangle(contours[i]);
                    for (int y = rectangle.Y; y < rectangle.Bottom && !foundContour; y++)
                    for (int x = rectangle.X; x < rectangle.Right; x++)
                    {
                        var pos = supportsMat.GetPixelPos(x, y);
                        if (IgnoreContactLessPixels)
                        {
                            if (supportsSpan[pos] <= 10) continue;
                            if (nextSpan[pos] <= 0) continue;
                            modified = true;
                            fullSpan[pos] = Brightness;
                        }
                        else
                        {
                            if (supportsSpan[pos] <= 100) continue;
                            if (nextSpan[pos] <= 150) continue;
                            CvInvoke.DrawContours(fullMatRoi, contours, i, new MCvScalar(Brightness), -1, LineType.AntiAlias);
                            modified = true;
                            foundContour = true;
                            break;
                        }

                    }
                }
            }
            else
            {
                switch (RedrawType)
                {
                    case RedrawTypes.Supports:
                        CvInvoke.Subtract(fullMatRoi, bodyMatRoi, supportsMat); // Supports
                        break;
                    case RedrawTypes.Model:
                        CvInvoke.BitwiseAnd(fullMatRoi, bodyMatRoi, supportsMat); // Model
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(RedrawType), RedrawType, null);
                }

                using var patternMat = fullMatRoi.NewSetTo(new MCvScalar(Brightness), supportsMat);

                switch (Operator)
                {
                    case RedrawModelOperators.Set:
                        patternMat.CopyTo(fullMatRoi, fullMatRoi);
                        break;
                    case RedrawModelOperators.Add:
                        CvInvoke.Add(fullMatRoi, patternMat, fullMatRoi, supportsMat);
                        break;
                    case RedrawModelOperators.Subtract:
                        CvInvoke.Subtract(fullMatRoi, patternMat, fullMatRoi, supportsMat);
                        break;
                    case RedrawModelOperators.Multiply:
                        CvInvoke.Multiply(fullMatRoi, patternMat, fullMatRoi, EmguCvExtensions.NormalizedByteScale);
                        break;
                    case RedrawModelOperators.Divide:
                        CvInvoke.Divide(fullMatRoi, patternMat, fullMatRoi);
                        break;
                    case RedrawModelOperators.Minimum:
                        CvInvoke.Min(fullMatRoi, patternMat, fullMatRoi);
                        break;
                    case RedrawModelOperators.Maximum:
                        CvInvoke.Max(fullMatRoi, patternMat, fullMatRoi);
                        break;
                    case RedrawModelOperators.AbsDiff:
                        CvInvoke.AbsDiff(fullMatRoi, patternMat, fullMatRoi);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Operator), Operator, null);
                }
                modified = true;
            }

            if (modified)
            {
                ApplyMask(original, fullMatRoi);
                SlicerFile[fullMatLayerIndex].LayerMat = fullMat;
            }

            progress.LockAndIncrement();
        });

        return !progress.Token.IsCancellationRequested;
    }

    #endregion
}