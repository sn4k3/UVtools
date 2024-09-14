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
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public class OperationRedrawModel : Operation
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

    private string _filePath = null!;
    private byte _brightness = 220;
    private bool _contactPointsOnly = true;
    private RedrawTypes _redrawType = RedrawTypes.Supports;
    private bool _ignoreContactLessPixels = true;
    private RedrawModelOperators _operator = RedrawModelOperators.Minimum;

    #endregion
        
    #region Overrides

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;
    public override string IconClass => "mdi-puzzle-edit";
    public override string Title => "Redraw model/supports";

    public override string Description =>
        "Redraw the model or supports with a set brightness. This requires an extra sliced file from same object but without any supports and raft, straight to the build plate.\n" +
        "Note: Run this tool prior to any made modification. You must find the optimal exposure/brightness combo, or supports can fail.";

    public override string ConfirmationText => "redraw the "+ (_redrawType == RedrawTypes.Supports ? "supports" : "model") + 
                                               " with an"+ (_redrawType == RedrawTypes.Model || !_contactPointsOnly ? $" {_operator}" : string.Empty) + $" brightness of {_brightness}?";

    public override string ProgressTitle => "Redrawing " + (_redrawType == RedrawTypes.Supports ? "supports" : "model");

    public override string ProgressAction => "Redraw layers";

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        if (IsFileValid() is null)
        {
            sb.AppendLine("The selected file is not valid.");
        }

        if (_redrawType == RedrawTypes.Model || _contactPointsOnly)
        {
            switch (_operator)
            {
                case RedrawModelOperators.Add:
                case RedrawModelOperators.Subtract:
                case RedrawModelOperators.Maximum:
                case RedrawModelOperators.AbsDiff:
                    if (_brightness == 0) sb.AppendLine($"{_operator} with a brightness of 0 yield no result, please use a value larger than 0.");
                    break;
                case RedrawModelOperators.Divide:
                    if (_brightness == 0) sb.AppendLine($"{_operator} with a brightness of 0 is not valid, please use a value larger than 0.");
                    else if (_brightness == 1) sb.AppendLine($"{_operator} with a brightness of 0 yield no result, please use a value larger than 0.");
                    break;
            }
        }

        return sb.ToString();
    }


    public override string ToString()
    {
        var result = $"[{_redrawType}] [B: {_brightness}] [OP: {_operator}] [CS: {_contactPointsOnly}] [ICLP: {_ignoreContactLessPixels}]";
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
    public string FilePath
    {
        get => _filePath;
        set => RaiseAndSetIfChanged(ref _filePath, value);
    }

    public RedrawTypes RedrawType
    {
        get => _redrawType;
        set => RaiseAndSetIfChanged(ref _redrawType, value);
    }

    public RedrawModelOperators Operator
    {
        get => _operator;
        set => RaiseAndSetIfChanged(ref _operator, value);
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

    public decimal BrightnessPercent => Math.Round(_brightness * 100 / 255M, 2);

    public bool ContactPointsOnly
    {
        get => _contactPointsOnly;
        set => RaiseAndSetIfChanged(ref _contactPointsOnly, value);
    }

    public bool IgnoreContactLessPixels
    {
        get => _ignoreContactLessPixels;
        set => RaiseAndSetIfChanged(ref _ignoreContactLessPixels, value);
    }

    #endregion

    #region Equality

    protected bool Equals(OperationRedrawModel other)
    {
        return _brightness == other._brightness && _contactPointsOnly == other._contactPointsOnly && _redrawType == other._redrawType && _ignoreContactLessPixels == other._ignoreContactLessPixels;
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
        FileFormat.FindByExtensionOrFilePath(_filePath, returnNewInstance);

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        var otherFile = IsFileValid(true);
        if (otherFile is null)
        {
            return false;
        }
        otherFile.Decode(_filePath, progress);

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
            if (_redrawType == RedrawTypes.Supports && _contactPointsOnly)
            {
                if (layerIndex + 1 >= otherFile.LayerCount) return;
                CvInvoke.Subtract(fullMatRoi, bodyMatRoi, supportsMat); // Supports
                using var contours = supportsMat.FindContours(RetrType.List);
                if (contours.Size <= 0) return;
                using var nextLayerMat = otherFile[layerIndex + 1].LayerMat;
                using var nextLayerMatRoi = GetRoiOrDefault(nextLayerMat);
                var fullSpan = fullMatRoi.GetDataByteSpan();
                var supportsSpan = supportsMat.GetDataByteReadOnlySpan();
                var nextSpan = nextLayerMatRoi.GetDataByteReadOnlySpan();
                for (int i = 0; i < contours.Size; i++)
                {
                    var foundContour = false;
                    var rectangle = CvInvoke.BoundingRectangle(contours[i]);
                    for (int y = rectangle.Y; y < rectangle.Bottom && !foundContour; y++)
                    for (int x = rectangle.X; x < rectangle.Right; x++)
                    {
                        var pos = supportsMat.GetPixelPos(x, y);
                        if (_ignoreContactLessPixels)
                        {
                            if (supportsSpan[pos] <= 10) continue;
                            if (nextSpan[pos] <= 0) continue;
                            modified = true;
                            fullSpan[pos] = _brightness;
                        }
                        else
                        {
                            if (supportsSpan[pos] <= 100) continue;
                            if (nextSpan[pos] <= 150) continue;
                            CvInvoke.DrawContours(fullMatRoi, contours, i, new MCvScalar(_brightness), -1, LineType.AntiAlias);
                            modified = true;
                            foundContour = true;
                            break;
                        }
                               
                    }
                }
            }
            else
            {
                switch (_redrawType)
                {
                    case RedrawTypes.Supports:
                        CvInvoke.Subtract(fullMatRoi, bodyMatRoi, supportsMat); // Supports
                        break;
                    case RedrawTypes.Model:
                        CvInvoke.BitwiseAnd(fullMatRoi, bodyMatRoi, supportsMat); // Model
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(RedrawType), _redrawType, null);
                }

                using var patternMat = fullMatRoi.NewSetTo(new MCvScalar(_brightness), supportsMat);

                switch (_operator)
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
                        CvInvoke.Multiply(fullMatRoi, patternMat, fullMatRoi, EmguExtensions.ByteScale);
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
                        throw new ArgumentOutOfRangeException(nameof(Operator), _operator, null);
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