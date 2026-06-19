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
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public partial class OperationPixelArithmetic : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Enums

    public enum PixelArithmeticIgnoreAreaOperator
    {
        [Description("Smaller than")]
        SmallerThan,
        [Description("Larger than")]
        LargerThan
    }

    #endregion

    #region Subclasses
    class StringMatrix
    {
        public string Text { get; }
        public Matrix<byte> Pattern { get; set; } = null!;

        public StringMatrix(string text)
        {
            Text = text;
        }
    }
    #endregion

    #region Members
    private float _valueStep;
    private ushort _noisePixelArea = 1;
    private byte _noisePasses = 1;

    #endregion

    #region Enums
    public enum PixelArithmeticOperators : byte
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
        //[Description("Exponential: pixels by a brightness")]
        //Exponential,
        [Description("Minimum: set to a brightness if is lower than the current pixel")]
        Minimum,
        [Description("Maximum: set to a brightness if is higher than the current pixel")]
        Maximum,
        [Description("Bitwise Not: invert pixels")]
        BitwiseNot,
        [Description("Bitwise And: with a brightness")]
        BitwiseAnd,
        [Description("Bitwise Or: with a brightness")]
        BitwiseOr,
        [Description("Bitwise Xor: with a brightness")]
        BitwiseXor,
        [Description("AbsDiff: perform a absolute difference between pixel and brightness")]
        AbsDiff,
        [Description("Corrode: Diffuse pixels using uniform random noise")]
        Corrode,
        [Description("Threshold: between a minimum/maximum brightness")]
        Threshold,
        [Description("Keep Region: in the selected ROI or masks")]
        KeepRegion,
        [Description("Discard Region: in the selected ROI or masks")]
        DiscardRegion,
    }

    public enum PixelArithmeticApplyMethod : byte
    {
        [Description("All: Apply to all pixels within the layer")]
        All,
        [Description("Model: Apply only to model pixels")]
        Model,
        [Description("Model surface: Apply only to model surface/visible pixels")]
        ModelSurface,
        [Description("Model surface & inset: Apply only to model surface/visible pixels and within a inset from walls")]
        ModelSurfaceAndInset,
        [Description("Model inner: Apply only to model pixels within a margin from walls")]
        ModelInner,
        [Description("Model walls: Apply only to model walls with a set thickness")]
        ModelWalls,
        //[Description("Model walls minimum: Apply only to model walls where walls must have at least a minimum set thickness")]
        //ModelWallsMinimum
    }
    #endregion

    #region Overrides
    public override string IconClass => "CircleOpacity";
    public override string Title => "Pixel arithmetic";

    public override string Description =>
        "Perform arithmetic operations over the pixels.";

    public override string ConfirmationText =>
        $"arithmetic {Operator}" +
        (ValueEnabled && !UsePattern ? $"={Value}{(_valueStep != 0 ? $" Step={_valueStep}" : string.Empty)}" : string.Empty) +
        (UsePattern && IsUsePatternVisible ? " with pattern" : string.Empty) +
        (Operator is PixelArithmeticOperators.Threshold ? $"/{ThresholdMaxValue}" : string.Empty)
        + $" layers from {LayerIndexStart} through {LayerIndexEnd}";

    public override string ProgressTitle =>
        $"Arithmetic {Operator}"+
        (ValueEnabled && !UsePattern ? $"={Value}{(_valueStep != 0 ? $" Step={_valueStep}" : string.Empty)}" : string.Empty) +
        (UsePattern && IsUsePatternVisible ? " with pattern" : string.Empty) +
        $" layers from {LayerIndexStart} through {LayerIndexEnd}";

    public override string ProgressAction => "Calculated layers";

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();
        if (Operator == PixelArithmeticOperators.KeepRegion && !HaveROI && !HaveMask)
        {
            sb.AppendLine("The 'Keep' operator requires selected ROI/masks.");
        }
        else if (Operator == PixelArithmeticOperators.DiscardRegion && !HaveROI && !HaveMask)
        {
            sb.AppendLine("The 'Discard' operator requires selected ROI/masks.");
        }
        else if (Operator
                     is PixelArithmeticOperators.Add
                     or PixelArithmeticOperators.Subtract
                     or PixelArithmeticOperators.Maximum
                     or PixelArithmeticOperators.BitwiseOr
                     or PixelArithmeticOperators.BitwiseXor
                     or PixelArithmeticOperators.AbsDiff
                 && (Value + _valueStep) == 0)
            /*||
                 (Operator is PixelArithmeticOperators.Exponential && Value == 1)
                 )*/
        {
            sb.AppendLine($"{Operator} by {Value} will have no effect.");
        }
        else if (Operator == PixelArithmeticOperators.Divide && Value == 0)
        {
            sb.AppendLine("Can't divide by 0.");
        }
        else if (Operator == PixelArithmeticOperators.Corrode && NoiseMinOffset >= NoiseMaxOffset)
        {
            sb.AppendLine("Minimum noise offset must be less than the maximum offset.");
        }

        if (ApplyMethod is PixelArithmeticApplyMethod.ModelWalls //or PixelArithmeticApplyMethod.ModelWallsMinimum
            && (
                (WallChamfer && WallThicknessStart == 0 && WallThicknessEnd == 0) ||
                (!WallChamfer && WallThicknessStart == 0)
            )
           )
        {
            sb.AppendLine("The current wall settings will have no effect.");
        }

        if (UsePattern && IsUsePatternVisible)
        {
            var stringMatrix = new[]
            {
                new StringMatrix(PatternText),
                new StringMatrix(PatternTextAlternate),
            };

            foreach (var item in stringMatrix)
            {
                if (string.IsNullOrWhiteSpace(item.Text)) continue;
                var lines = item.Text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                for (var row = 0; row < lines.Length; row++)
                {
                    var bytes = lines[row].Split(' ');
                    if (row == 0)
                    {
                        item.Pattern = new Matrix<byte>(lines.Length, bytes.Length);
                    }
                    else
                    {
                        if (item.Pattern.Cols != bytes.Length)
                        {
                            sb.AppendLine($"Row {row + 1} have invalid number of pixels, the pattern must have equal pixel count per line, per defined on line 1");
                            return sb.ToString();
                        }
                    }

                    for (int col = 0; col < bytes.Length; col++)
                    {
                        if (byte.TryParse(bytes[col], out var value))
                        {
                            item.Pattern[row, col] = (byte)(PatternInvert ? byte.MaxValue - value : value);
                        }
                        else
                        {
                            sb.AppendLine($"{bytes[col]} is a invalid number, use values from 0 to 255");
                            return sb.ToString();
                        }
                    }
                }
            }

            Pattern = stringMatrix[0].Pattern;
            PatternAlternate = stringMatrix[1].Pattern;

            if (Pattern is null && PatternAlternate is null)
            {
                sb.AppendLine("Either even or odd pattern must contain a valid matrix.");
                return sb.ToString();
            }
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"[{Operator}: {Value} Step: {_valueStep}] [Apply: {ApplyMethod}] " +
                     $"[Pattern: {UsePattern}]"
                     + LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }
    #endregion

    #region Properties

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ValueEnabled))]
    [NotifyPropertyChangedFor(nameof(IsUsePatternVisible))]
    [NotifyPropertyChangedFor(nameof(IsThresholdVisible))]
    [NotifyPropertyChangedFor(nameof(IsApplyMethodEnabled))]
    [NotifyPropertyChangedFor(nameof(IsCorrodeVisible))]
    public partial PixelArithmeticOperators Operator { get; set; } = PixelArithmeticOperators.Set;

    public bool IsApplyMethodEnabled =>
        Operator is not (PixelArithmeticOperators.KeepRegion or PixelArithmeticOperators.DiscardRegion);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWallSettingVisible))]
    public partial PixelArithmeticApplyMethod ApplyMethod { get; set; } = PixelArithmeticApplyMethod.Model;

    public bool IsWallSettingVisible => ApplyMethod
        is PixelArithmeticApplyMethod.ModelSurfaceAndInset
        or PixelArithmeticApplyMethod.ModelInner
        or PixelArithmeticApplyMethod.ModelWalls; //or PixelArithmeticApplyMethod.ModelWallsMinimum;

    public uint WallThickness
    {
        get => WallThicknessStart;
        set
        {
            WallThicknessStart = value;
            WallThicknessEnd = value;
        }
    }

    [ObservableProperty]
    public partial uint WallThicknessStart { get; set; } = 20;

    [ObservableProperty]
    public partial uint WallThicknessEnd { get; set; } = 20;

    [ObservableProperty]
    public partial bool WallChamfer { get; set; }

    [ObservableProperty]
    public partial PixelArithmeticIgnoreAreaOperator IgnoreAreaOperator { get; set; } = PixelArithmeticIgnoreAreaOperator.SmallerThan;

    [ObservableProperty]
    public partial uint IgnoreAreaThreshold { get; set; }


    public bool IsCorrodeVisible => Operator is PixelArithmeticOperators.Corrode;

    [ObservableProperty]
    public partial short NoiseMinOffset { get; set; } = -128;

    [ObservableProperty]
    public partial short NoiseMaxOffset { get; set; } = 128;

    [ObservableProperty]
    public partial byte NoiseThreshold { get; set; }

    public ushort NoisePixelArea
    {
        get => _noisePixelArea;
        set => SetProperty(ref _noisePixelArea, Math.Max((byte)1, value));
    }

    public byte NoisePasses
    {
        get => _noisePasses;
        set => SetProperty(ref _noisePasses, Math.Max((byte)1, value));
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ValuePercent))]
    public partial byte Value { get; set; } = byte.MaxValue;

    // 255  - 100
    //value -  x
    public float ValuePercent => MathF.Round(Value * 100f / byte.MaxValue, 2);

    /// <summary>
    /// Mutates the initial brightness with a step that is added/subtracted to the current value dependent on the processed layer count
    /// </summary>
    public float ValueStep
    {
        get => _valueStep;
        set => SetProperty(ref _valueStep, Math.Clamp(value, -byte.MaxValue, byte.MaxValue));
    }

    public bool ValueEnabled => Operator
        is not PixelArithmeticOperators.BitwiseNot
        and not PixelArithmeticOperators.KeepRegion
        and not PixelArithmeticOperators.DiscardRegion
        and not PixelArithmeticOperators.Corrode
    ;

    public bool IsUsePatternVisible => Operator
        is not PixelArithmeticOperators.Threshold
        and not PixelArithmeticOperators.BitwiseNot
        and not PixelArithmeticOperators.KeepRegion
        and not PixelArithmeticOperators.DiscardRegion
        and not PixelArithmeticOperators.Corrode
    ;

    [ObservableProperty]
    public partial bool UsePattern { get; set; }

    [ObservableProperty]
    public partial ThresholdType ThresholdType { get; set; } = ThresholdType.Binary;

    [ObservableProperty]
    public partial byte ThresholdMaxValue { get; set; } = 255;

    public bool IsThresholdVisible => Operator is PixelArithmeticOperators.Threshold;

    /*public bool AffectBackPixelsEnabled => Operator
        is not PixelArithmeticOperators.Subtract
        and not PixelArithmeticOperators.Multiply
        and not PixelArithmeticOperators.Divide
        and not PixelArithmeticOperators.BitwiseNot
        and not PixelArithmeticOperators.BitwiseAnd
        and not PixelArithmeticOperators.KeepRegion
        and not PixelArithmeticOperators.DiscardRegion
        and not PixelArithmeticOperators.Threshold
        ;*/

    [ObservableProperty]
    public partial ushort PatternAlternatePerLayersNumber { get; set; } = 1;

    [ObservableProperty]
    public partial bool PatternInvert { get; set; }

    [ObservableProperty]
    public partial string PatternText { get; set; } = null!;

    [ObservableProperty]
    public partial string PatternTextAlternate { get; set; } = null!;

    [XmlIgnore]
    [ObservableProperty]
    public partial Matrix<byte> Pattern { get; set; } = null!;

    [XmlIgnore]
    [ObservableProperty]
    public partial Matrix<byte>? PatternAlternate { get; set; }

    [ObservableProperty]
    public partial byte PatternGenMinBrightness { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PatternGenBrightnessPercent))]
    public partial byte PatternGenBrightness { get; set; } = 128;

    public float PatternGenBrightnessPercent => Helpers.BrightnessToPercent(PatternGenBrightness);

    [ObservableProperty]
    public partial byte PatternGenInfillThickness { get; set; } = 10;

    [ObservableProperty]
    public partial byte PatternGenInfillSpacing { get; set; } = 20;

    public KernelConfiguration Kernel { get; set; } = new();

    #endregion

    #region Constructor

    public OperationPixelArithmetic() { }

    public OperationPixelArithmetic(FileFormat slicerFile) : base(slicerFile) { }

    #endregion

    #region Methods

    private Size GetMatSizeCropped(Mat? mat = null)
    {
        return ApplyMethod == PixelArithmeticApplyMethod.All ? GetRoiSizeOrDefault(mat) : GetRoiSizeOrDefault(OriginalBoundingRectangle);
    }

    private Mat GetMatRoiCropped(Mat mat)
    {
        return ApplyMethod == PixelArithmeticApplyMethod.All ? GetRoiOrDefault(mat) : GetRoiOrVolumeBounds(mat);
    }

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        Mat? patternMat = null;
        Mat? patternAlternateMat = null;
        Mat patternMatMask = null!;
        Mat patternAlternateMatMask = null!;

        if (UsePattern && IsUsePatternVisible)
        {
            if (Pattern is null)
            {
                Pattern = new Matrix<byte>(2, 2)
                {
                    [0, 0] = 0,
                    [0, 1] = 127,
                    [1, 0] = 127,
                    [1, 1] = 0,
                };

                PatternAlternate ??= new Matrix<byte>(2, 2)
                {
                    [0, 0] = 127,
                    [0, 1] = 0,
                    [1, 0] = 0,
                    [1, 1] = 127,
                };
            }

            PatternAlternate ??= Pattern;

            var target = new Mat(GetMatSizeCropped(), DepthType.Cv8U, 1);
            if (target.IsEmpty) return false;
            patternMat = target.NewZeros();
            patternAlternateMat = target.NewZeros();

            CvInvoke.Repeat(Pattern, (int)Math.Ceiling((double)target.Rows / Pattern.Rows), (int)Math.Ceiling((double)target.Cols / Pattern.Cols), patternMat);
            CvInvoke.Repeat(PatternAlternate, (int)Math.Ceiling((double)target.Rows / PatternAlternate.Rows), (int)Math.Ceiling((double)target.Cols / PatternAlternate.Cols), patternAlternateMat);

            patternMatMask = patternMat.Roi(target);
            patternAlternateMatMask = patternAlternateMat.Roi(target);

            /*if (PatternInvert)
            {
                CvInvoke.BitwiseNot(patternMatMask, patternMatMask);
                CvInvoke.BitwiseNot(patternAlternateMatMask, patternAlternateMatMask);
            }*/
        }
        else if (IsUsePatternVisible)
        {
            patternMatMask = EmguCvExtensions.InitMat(GetMatSizeCropped(), new MCvScalar(Value));
        }


        Parallel.For(LayerIndexStart, LayerIndexEnd + 1, CoreSettings.GetParallelOptions(progress), layerIndex =>
        {
            progress.PauseIfRequested();

            var layer = SlicerFile[layerIndex];
            using (var mat = layer.LayerMat)
            {
                using var original = mat.Clone();
                using var originalRoi = GetMatRoiCropped(original);
                using var target = GetMatRoiCropped(mat);
                Mat tempMat;

                if (UsePattern && IsUsePatternVisible)
                {
                    tempMat = IsNormalPattern((uint)layerIndex) ? patternMatMask : patternAlternateMatMask;
                }
                else if(_valueStep == 0)
                {
                    tempMat = patternMatMask;
                }
                else
                {
                    var layerStep = layerIndex - LayerIndexStart;
                    var valueStepped = Math.Clamp(MathF.Round(Value + _valueStep * layerStep, MidpointRounding.AwayFromZero), 0, 255);
                    tempMat = EmguCvExtensions.InitMat(GetMatSizeCropped(), new MCvScalar(valueStepped));
                }

                Mat? applyMask;

                int wallThickness = FileFormat.MutateGetIterationChamfer(
                    (uint)layerIndex,
                    LayerIndexStart,
                    LayerIndexEnd,
                    (int)WallThicknessStart,
                    (int)WallThicknessEnd,
                    WallChamfer
                );

                switch (ApplyMethod)
                {
                    case PixelArithmeticApplyMethod.All:
                        applyMask = null;
                        break;
                    case PixelArithmeticApplyMethod.Model:
                        applyMask = target.Clone();
                        break;
                    case PixelArithmeticApplyMethod.ModelSurface:
                    case PixelArithmeticApplyMethod.ModelSurfaceAndInset:
                        if (layerIndex == SlicerFile.LastLayerIndex)
                        {
                            applyMask = target.Clone();
                        }
                        else
                        {
                            applyMask = new Mat();

                            // Difference
                            using var nextMat = SlicerFile[layerIndex + 1].LayerMat;
                            using var nextMatRoi = GetMatRoiCropped(nextMat);
                            CvInvoke.Subtract(target, nextMatRoi, applyMask);

                            // 1px walls
                            using var erode = new Mat();
                            int iterations = 1;
                            var kernel = Kernel.GetKernel(ref iterations);
                            CvInvoke.Erode(target, erode, kernel, EmguCvExtensions.AnchorCenter, iterations, BorderType.Reflect101, default);
                            CvInvoke.Subtract(target, erode, erode);
                            CvInvoke.Add(applyMask, erode, applyMask);


                            // Inset from walls
                            if (ApplyMethod == PixelArithmeticApplyMethod.ModelSurfaceAndInset && (wallThickness-1) > 0)
                            {
                                iterations = wallThickness - 1;
                                kernel = Kernel.GetKernel(ref iterations);
                                CvInvoke.Dilate(applyMask, erode, kernel, EmguCvExtensions.AnchorCenter, iterations, BorderType.Reflect101, default);
                                erode.CopyTo(applyMask, target);
                            }
                        }

                        break;
                    case PixelArithmeticApplyMethod.ModelInner:
                    {
                        if (wallThickness <= 0)
                        {
                            applyMask = target.Clone();
                            break;
                        }

                        applyMask = new Mat();
                        int iterations = wallThickness;
                        var kernel = Kernel.GetKernel(ref iterations);
                        CvInvoke.Erode(target, applyMask, kernel, EmguCvExtensions.AnchorCenter, iterations, BorderType.Reflect101, default);
                        break;
                    }
                    case PixelArithmeticApplyMethod.ModelWalls:
                    {
                        if (wallThickness <= 0) // No effect, skip
                        {
                            progress.LockAndIncrement();
                            return;
                        }

                        using var erode = new Mat();
                        applyMask = target.Clone();
                        int iterations = wallThickness;
                        var kernel = Kernel.GetKernel(ref iterations);
                        CvInvoke.Erode(target, erode, kernel, EmguCvExtensions.AnchorCenter, iterations, BorderType.Reflect101, default);
                        applyMask.SetTo(EmguCvExtensions.BlackColor, erode);
                        break;
                    }
                    /*case PixelArithmeticApplyMethod.ModelWallsMinimum:
                    {
                        if (wallThickness <= 0) // No effect, skip
                        {
                            progress.LockAndIncrement();
                            return;
                        }

                        using var erode = new Mat();
                        using var erodeInv = new Mat();
                        applyMask = target.Clone();
                        target.Save($"D:\\wallmin\\original{layerIndex}.png");
                        CvInvoke.Erode(target, erode, kernel, anchor, wallThickness, BorderType.Reflect101, default);
                        erode.Save($"D:\\wallmin\\erode{layerIndex}.png");
                        CvInvoke.Dilate(erode, erode, kernel, anchor, wallThickness, BorderType.Reflect101, default);
                        erode.Save($"D:\\wallmin\\dilate{layerIndex}.png");
                            //CvInvoke.BitwiseXor(target, erode, applyMask);
                            //applyMask.Save($"D:\\wallmin\\bitwiseXor{layerIndex}.png");
                            CvInvoke.BitwiseNot(erode, erodeInv);
                            erodeInv.Save($"D:\\wallmin\\erodeInv{layerIndex}.png");
                            CvInvoke.BitwiseXor(target, erode, erode, erodeInv);
                            erode.Save($"D:\\wallmin\\BitwiseXor{layerIndex}.png");
                            applyMask.SetTo(EmguCvExtensions.BlackColor, erode);
                            applyMask.Save($"D:\\wallmin\\applymask{layerIndex}.png");
                            break;
                    }*/
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                switch (Operator)
                {
                    case PixelArithmeticOperators.Set:
                        tempMat.CopyTo(target, applyMask);
                        break;
                    case PixelArithmeticOperators.Add:
                        CvInvoke.Add(target, tempMat, target, applyMask);
                        break;
                    case PixelArithmeticOperators.Subtract:
                        if (layer.IsEmpty)
                        {
                            progress.LockAndIncrement();
                            return;
                        }
                        CvInvoke.Subtract(target, tempMat, target, applyMask);
                        break;
                    case PixelArithmeticOperators.Multiply:
                        if (layer.IsEmpty)
                        {
                            progress.LockAndIncrement();
                            return;
                        }
                        CvInvoke.Multiply(target, tempMat, target, EmguCvExtensions.NormalizedByteScale);
                        if (ApplyMethod != PixelArithmeticApplyMethod.All) ApplyMask(originalRoi, target, applyMask);
                        break;
                    case PixelArithmeticOperators.Divide:
                        if (layer.IsEmpty)
                        {
                            progress.LockAndIncrement();
                            return;
                        }
                        CvInvoke.Divide(target, tempMat, target);
                        if (ApplyMethod != PixelArithmeticApplyMethod.All) ApplyMask(originalRoi, target, applyMask);
                        break;
                    /*case PixelArithmeticOperators.Exponential:
                        CvInvoke.Pow(target, Value, tempMat);
                        if(!_affectBackPixels) ApplyMask(original, mat, original);
                        break;*/
                    case PixelArithmeticOperators.Minimum:
                        if (layer.IsEmpty)
                        {
                            progress.LockAndIncrement();
                            return;
                        }
                        CvInvoke.Min(target, tempMat, target);
                        if (ApplyMethod != PixelArithmeticApplyMethod.All) ApplyMask(originalRoi, target, applyMask);
                        break;
                    case PixelArithmeticOperators.Maximum:
                        CvInvoke.Max(target, tempMat, target);
                        if (ApplyMethod != PixelArithmeticApplyMethod.All) ApplyMask(originalRoi, target, applyMask);
                        break;
                    case PixelArithmeticOperators.BitwiseNot:
                        CvInvoke.BitwiseNot(target, target, applyMask);
                        break;
                    case PixelArithmeticOperators.BitwiseAnd:
                        if (layer.IsEmpty)
                        {
                            progress.LockAndIncrement();
                            return;
                        }
                        CvInvoke.BitwiseAnd(target, tempMat, target, applyMask);
                        break;
                    case PixelArithmeticOperators.BitwiseOr:
                        CvInvoke.BitwiseOr(target, tempMat, target, applyMask);
                        break;
                    case PixelArithmeticOperators.BitwiseXor:
                        CvInvoke.BitwiseXor(target, tempMat, target, applyMask);
                        break;
                    case PixelArithmeticOperators.AbsDiff:
                        if (layer.IsEmpty)
                        {
                            progress.LockAndIncrement();
                            return;
                        }
                        CvInvoke.AbsDiff(target, tempMat, target);
                        if (ApplyMethod != PixelArithmeticApplyMethod.All) ApplyMask(originalRoi, target, applyMask);
                        break;
                    case PixelArithmeticOperators.Threshold:
                        var tempThreshold = ThresholdType;
                        if (ThresholdType is ThresholdType.Otsu or ThresholdType.Triangle) tempThreshold = ThresholdType.Binary | tempThreshold;
                        CvInvoke.Threshold(target, target, Value, ThresholdMaxValue, tempThreshold);
                        if (ApplyMethod != PixelArithmeticApplyMethod.All) ApplyMask(originalRoi, target, applyMask);
                        break;
                    case PixelArithmeticOperators.Corrode:
                        if (layer.IsEmpty)
                        {
                            progress.LockAndIncrement();
                            return;
                        }
                        var span = mat.GetSpanOfBytes(0, 0);

                        var bounds = HaveROI ? ROI : layer.BoundingRectangle;

                        for (var y = bounds.Y; y < bounds.Bottom; y += _noisePixelArea)
                        for (var x = bounds.X; x < bounds.Right; x += _noisePixelArea)
                        {
                            byte zoneBrightness = 0;
                            for (var y1 = y; y1 < y + _noisePixelArea && y1 < bounds.Bottom && zoneBrightness < byte.MaxValue; y1++)
                            {
                                var pixelPos = mat.GetPixelPos(x, y1);
                                for (var x1 = x; x1 < x + _noisePixelArea && x1 < bounds.Right && zoneBrightness < byte.MaxValue; x1++)
                                {
                                    zoneBrightness = Math.Max(zoneBrightness, span[pixelPos++]);
                                }
                            }

                            if (zoneBrightness <= NoiseThreshold) continue;
                            byte brightness = zoneBrightness;

                            for (ushort i = 0; i < _noisePasses; i++)
                            {
                                brightness = (byte)Math.Clamp(Random.Shared.Next(NoiseMinOffset, NoiseMaxOffset + 1) + brightness, byte.MinValue, byte.MaxValue);
                            }

                            //byte brightness = (byte)Math.Clamp(RandomNumberGenerator.GetInt32(NoiseMinOffset, NoiseMaxOffset + 1) + zoneBrightness, byte.MinValue, byte.MaxValue);
                            for (var y1 = y; y1 < y + _noisePixelArea && y1 < bounds.Bottom; y1++)
                            {
                                var pixelPos = mat.GetPixelPos(x, y1);
                                for (var x1 = x; x1 < x + _noisePixelArea && x1 < bounds.Right; x1++)
                                {

                                    if (span[pixelPos] <= NoiseThreshold) continue;
                                    span[pixelPos++] = brightness;
                                }
                            }
                        }

                        if (ApplyMethod is not PixelArithmeticApplyMethod.All and not PixelArithmeticApplyMethod.Model) ApplyMask(originalRoi, target, applyMask);


                        // old method
                        /*if (HaveROI)
                        {
                            for (var y = ROI.Y; y < ROI.Bottom; y++)
                            for (var x = ROI.X; x < ROI.Right; x++)
                            {
                                var pos = mat.GetPixelPos(x, y);
                                if (span[pos] <= NoiseThreshold) continue;
                                span[pos] = (byte)Math.Clamp(RandomNumberGenerator.GetInt32(NoiseMinOffset, NoiseMaxOffset + 1) + span[pos], byte.MinValue, byte.MaxValue);
                            }

                            if (ApplyMethod
                                is not PixelArithmeticApplyMethod.All
                                and not PixelArithmeticApplyMethod.Model)
                                ApplyMask(originalRoi, target, applyMask);
                        }
                        else // Whole image
                        {
                            var spanMask = applyMask is null ? span : applyMask.GetDataByteSpan();

                            for (var i = 0; i < span.Length; i++)
                            {
                                //if (span[i] <= NoiseThreshold || spanMask[i] == 0) continue;
                                //span[i] = (byte)Math.Clamp(RandomNumberGenerator.GetInt32(NoiseMinOffset, NoiseMaxOffset + 1) + span[i], byte.MinValue, byte.MaxValue);
                                span[i] = (byte)Math.Clamp(random.Next(NoiseMinOffset, NoiseMaxOffset + 1) + span[i], byte.MinValue, byte.MaxValue);
                            }
                        }*/

                        break;
                    case PixelArithmeticOperators.KeepRegion:
                    {
                        if (layer.IsEmpty)
                        {
                            progress.LockAndIncrement();
                            return;
                        }
                        using var targetClone = target.Clone();
                        original.SetTo(EmguCvExtensions.BlackColor);
                        mat.SetTo(EmguCvExtensions.BlackColor);
                        targetClone.CopyTo(target);
                        break;
                    }
                    case PixelArithmeticOperators.DiscardRegion:
                        if (layer.IsEmpty)
                        {
                            progress.LockAndIncrement();
                            return;
                        }
                        target.SetTo(EmguCvExtensions.BlackColor);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                switch (IgnoreAreaOperator)
                {
                    case PixelArithmeticIgnoreAreaOperator.SmallerThan:
                        originalRoi.CopyAreasSmallerThan(IgnoreAreaThreshold, target);
                        break;
                    case PixelArithmeticIgnoreAreaOperator.LargerThan:
                        originalRoi.CopyAreasLargerThan(IgnoreAreaThreshold, target);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(IgnoreAreaOperator));
                }
                ApplyMask(original, mat);

                SlicerFile[layerIndex].LayerMat = mat;

                if (applyMask is not null && !ReferenceEquals(applyMask, target)) applyMask.Dispose();
            }

            progress.LockAndIncrement();
        });

        patternMat?.Dispose();
        patternAlternateMat?.Dispose();

        return !progress.Token.IsCancellationRequested;
    }

    public bool IsNormalPattern(uint layerIndex) => layerIndex / PatternAlternatePerLayersNumber % 2 == 0;

    public bool IsAlternatePattern(uint layerIndex) => !IsNormalPattern(layerIndex);

    public void PresetElephantFootCompensation()
    {
        SelectBottomLayers();
        Operator = PixelArithmeticOperators.Set;
        ApplyMethod = PixelArithmeticApplyMethod.ModelWalls;
        //Value = 190;
        //WallThickness = 20;
        WallChamfer = false;
        UsePattern = false;
    }

    public void PresetPixelDimming()
    {
        Operator = PixelArithmeticOperators.Subtract;
        ApplyMethod = PixelArithmeticApplyMethod.ModelInner;
        //WallThickness = 20;
        WallChamfer = false;
        UsePattern = true;
    }

    public void PresetPixelLightening()
    {
        PresetPixelDimming();
        Operator = PixelArithmeticOperators.Add;
    }

    public void PresetFuzzySkin()
    {
        Operator = PixelArithmeticOperators.Corrode;
        ApplyMethod = PixelArithmeticApplyMethod.ModelSurfaceAndInset;
        NoiseMinOffset = -200;
        NoiseMaxOffset = 64;
        WallThickness = 4;
        IgnoreAreaOperator = PixelArithmeticIgnoreAreaOperator.SmallerThan;
        IgnoreAreaThreshold = 5000;
    }

    public void PresetStripAntiAliasing()
    {
        Operator = PixelArithmeticOperators.Threshold;
        ApplyMethod = PixelArithmeticApplyMethod.All;
        UsePattern = false;
        Value = 127;
        ThresholdMaxValue = 255;
        ThresholdType = ThresholdType.Binary;
    }

    public void PresetHealAntiAliasing()
    {
        Operator = PixelArithmeticOperators.Threshold;
        ApplyMethod = PixelArithmeticApplyMethod.All;
        UsePattern = false;
        Value = 119;
        //ThresholdMaxValue = 255;
        ThresholdType = ThresholdType.ToZero;
    }

    public void PresetHalfBrightness()
    {
        Value = 128;
    }

    public unsafe void LoadPatternFromImage(Mat mat, bool isAlternatePattern = false)
    {
        var result = new string[mat.Height];
        var span = mat.BytePointer;
        Parallel.For(0, mat.Height, CoreSettings.ParallelOptions, y =>
        {
            result[y] = string.Empty;
            var pixelPos = mat.GetPixelPos(0, y);
            for (int x = 0; x < mat.Width; x++)
            {
                result[y] += $"{span[pixelPos++]} ";
            }

            result[y] = result[y].Trim();
        });

        StringBuilder sb = new();
        foreach (var s in result)
        {
            sb.AppendLine(s);
        }

        if (isAlternatePattern)
        {
            PatternTextAlternate = sb.ToString();
        }
        else
        {
            PatternText = sb.ToString();
        }
    }

    public void LoadPatternFromImage(string filepath, bool isAlternatePattern = false)
    {
        try
        {
            using var mat = CvInvoke.Imread(filepath, ImreadModes.Grayscale);
            LoadPatternFromImage(mat, isAlternatePattern);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }

    }


    public void GeneratePattern(object pattern) => GeneratePattern(pattern.ToString()!);
    public void GeneratePattern(string pattern)
    {
        if (pattern == "Chessboard")
        {
            PatternText = string.Format(
                "{0} {1}{2}" +
                "{1} {0}"
                , PatternGenMinBrightness, PatternGenBrightness, "\n");

            PatternTextAlternate = string.Format(
                "{1} {0}{2}" +
                "{0} {1}"
                , PatternGenMinBrightness, PatternGenBrightness, "\n");

            return;
        }

        if (pattern == "Sparse")
        {
            PatternText = string.Format(
                "{1} {0} {0} {0}{2}" +
                "{0} {0} {1} {0}"
                , PatternGenMinBrightness, PatternGenBrightness, "\n");

            PatternTextAlternate = string.Format(
                "{0} {0} {1} {0}{2}" +
                "{1} {0} {0} {0}"
                , PatternGenMinBrightness, PatternGenBrightness, "\n");
            return;
        }

        if (pattern == "Crosses")
        {
            PatternText = string.Format(
                "{1} {0} {1} {0}{2}" +
                "{0} {1} {0} {0}{2}" +
                "{1} {0} {1} {0}{2}" +
                "{0} {0} {0} {0}"
                , PatternGenMinBrightness, PatternGenBrightness, "\n");

            PatternTextAlternate = string.Format(
                "{0} {0} {0} {0}{2}" +
                "{1} {0} {1} {0}{2}" +
                "{0} {1} {0} {0}{2}" +
                "{1} {0} {1} {0}"
                , PatternGenMinBrightness, PatternGenBrightness, "\n");
            return;
        }

        if (pattern == "Strips")
        {
            PatternText = string.Format(
                "{1}{2}" +
                "{0}"
                , PatternGenMinBrightness, PatternGenBrightness, "\n");

            PatternTextAlternate = string.Format(
                "{0}{2}" +
                "{1}"
                , PatternGenMinBrightness, PatternGenBrightness, "\n");
            return;
        }

        if (pattern == "Pyramid")
        {
            PatternText = string.Format(
                "{0} {0} {1} {0} {0} {0}{2}" +
                "{0} {1} {0} {1} {0} {0}{2}" +
                "{1} {0} {1} {0} {1} {0}{2}" +
                "{0} {0} {0} {0} {0} {0}"
                , PatternGenMinBrightness, PatternGenBrightness, "\n");

            PatternTextAlternate = string.Format(
                "{0} {1} {0} {1} {0} {1}{2}" +
                "{0} {0} {1} {0} {1} {0}{2}" +
                "{0} {0} {0} {1} {0} {0}{2}" +
                "{0} {0} {0} {0} {0} {0}"
                , PatternGenMinBrightness, PatternGenBrightness, "\n");
            return;
        }

        if (pattern == "Rhombus")
        {
            PatternText = string.Format(
                "{0} {1} {0} {0}{2}" +
                "{1} {0} {1} {0}{2}" +
                "{0} {1} {0} {0}{2}" +
                "{0} {0} {0} {0}"
                , PatternGenMinBrightness, PatternGenBrightness, "\n");

            PatternTextAlternate = string.Format(
                "{0} {0} {0} {0}{2}" +
                "{0} {1} {0} {0}{2}" +
                "{1} {0} {1} {0}{2}" +
                "{0} {1} {0} {0}"
                , PatternGenMinBrightness, PatternGenBrightness, "\n");
            return;
        }

        if (pattern == "Hearts")
        {
            PatternText = string.Format(
                "{0} {1} {0} {1} {0} {0}{2}" +
                "{1} {0} {1} {0} {1} {0}{2}" +
                "{1} {0} {0} {0} {1} {0}{2}" +
                "{0} {1} {0} {1} {0} {0}{2}" +
                "{0} {0} {1} {0} {0} {0}{2}" +
                "{0} {0} {0} {0} {0} {0}"
                , PatternGenMinBrightness, PatternGenBrightness, "\n");

            PatternTextAlternate = string.Format(
                "{0} {0} {0} {0} {0} {0}{2}" +
                "{0} {0} {1} {0} {1} {0}{2}" +
                "{0} {1} {0} {1} {0} {1}{2}" +
                "{0} {1} {0} {0} {0} {1}{2}" +
                "{0} {0} {1} {0} {1} {0}{2}" +
                "{0} {0} {0} {1} {0} {0}"
                , PatternGenMinBrightness, PatternGenBrightness, "\n");
            return;
        }

        if (pattern == "Slashes")
        {
            PatternText = string.Format(
                "{1} {0} {0}{2}" +
                "{0} {1} {0}{2}" +
                "{0} {0} {1}"
                , PatternGenMinBrightness, PatternGenBrightness, "\n");

            PatternTextAlternate = string.Format(
                "{0} {0} {1}{2}" +
                "{0} {1} {0}{2}" +
                "{1} {0} {0}"
                , PatternGenMinBrightness, PatternGenBrightness, "\n");
            return;
        }

        if (pattern == "Waves")
        {
            PatternText = string.Format(
                "{1} {0} {0}{2}" +
                "{0} {0} {1}"
                , PatternGenMinBrightness, PatternGenBrightness, "\n");

            PatternTextAlternate = string.Format(
                "{0} {0} {1}{2}" +
                "{1} {0} {0}"
                , PatternGenMinBrightness, PatternGenBrightness, "\n");
            return;
        }

        if (pattern == "Solid")
        {
            PatternText = PatternGenBrightness.ToString();
            PatternTextAlternate = null!;
            return;
        }
    }

    public void GenerateInfill(object pattern) => GenerateInfill(pattern.ToString()!);
    public void GenerateInfill(string pattern)
    {
        if (pattern == "Rectilinear")
        {
            PatternText = ("255\n".Repeat(PatternGenInfillSpacing) + "0\n".Repeat(PatternGenInfillThickness)).Trim('\n', '\r');
            PatternTextAlternate = null!;
            return;
        }

        if (pattern == "Square grid")
        {
            var p1 = "255 ".Repeat(PatternGenInfillSpacing) + "0 ".Repeat(PatternGenInfillThickness);
            p1 = p1.Trim() + "\n";
            p1 += p1.Repeat(PatternGenInfillThickness);


            var p2 = "0 ".Repeat(PatternGenInfillSpacing) + "0 ".Repeat(PatternGenInfillThickness);
            p2 = p2.Trim() + '\n';
            p2 += p2.Repeat(PatternGenInfillThickness);

            p2 = p2.Trim('\n', '\r');

            PatternText = p1 + p2;
            PatternTextAlternate = null!;
            return;
        }

        if (pattern == "Waves")
        {
            var p1 = string.Empty;
            var pos = 0;
            for (sbyte dir = 1; dir >= -1; dir -= 2)
            {
                while (pos >= 0 && pos <= PatternGenInfillSpacing)
                {
                    p1 += "255 ".Repeat(pos);
                    p1 += "0 ".Repeat(PatternGenInfillThickness);
                    p1 += "255 ".Repeat(PatternGenInfillSpacing - pos);
                    p1 = p1.Trim() + '\n';

                    pos += dir;
                }

                pos--;
            }

            PatternText = p1.Trim('\n', '\r');
            PatternTextAlternate = null!;
            return;
        }

        if (pattern == "Lattice")
        {
            var p1 = string.Empty;
            var p2 = string.Empty;

            var zeros = Math.Max(0, PatternGenInfillSpacing - PatternGenInfillThickness * 2);

            // Pillar
            for (int i = 0; i < PatternGenInfillThickness; i++)
            {
                p1 += "0 ".Repeat(PatternGenInfillThickness);
                p1 += "255 ".Repeat(zeros);
                p1 += "0 ".Repeat(PatternGenInfillThickness);
                p1 = p1.Trim() + '\n';
            }

            for (int i = 0; i < zeros; i++)
            {
                p1 += "255 ".Repeat(PatternGenInfillSpacing);
                p1 = p1.Trim() + '\n';
            }

            for (int i = 0; i < PatternGenInfillThickness; i++)
            {
                p1 += "0 ".Repeat(PatternGenInfillThickness);
                p1 += "255 ".Repeat(zeros);
                p1 += "0 ".Repeat(PatternGenInfillThickness);
                p1 = p1.Trim() + '\n';
            }

            // Square
            for (int i = 0; i < PatternGenInfillThickness; i++)
            {
                p2 += "0 ".Repeat(PatternGenInfillSpacing);
                p2 = p2.Trim() + '\n';
            }

            for (int i = 0; i < zeros; i++)
            {
                p2 += "0 ".Repeat(PatternGenInfillThickness);
                p2 += "255 ".Repeat(zeros);
                p2 += "0 ".Repeat(PatternGenInfillThickness);
                p2 = p2.Trim() + '\n';
            }

            for (int i = 0; i < PatternGenInfillThickness; i++)
            {
                p2 += "0 ".Repeat(PatternGenInfillSpacing);
                p2 = p2.Trim() + '\n';
            }



            PatternText = p1.Trim('\n', '\r');
            PatternTextAlternate = p2.Trim('\n', '\r');
            return;
        }
    }

    #endregion

    #region Equality

    protected bool Equals(OperationPixelArithmetic other)
    {
        return Operator == other.Operator && ApplyMethod == other.ApplyMethod && WallThicknessStart == other.WallThicknessStart && WallThicknessEnd == other.WallThicknessEnd && WallChamfer == other.WallChamfer && IgnoreAreaOperator == other.IgnoreAreaOperator && IgnoreAreaThreshold == other.IgnoreAreaThreshold && Value == other.Value && UsePattern == other.UsePattern && ThresholdType == other.ThresholdType && ThresholdMaxValue == other.ThresholdMaxValue && PatternAlternatePerLayersNumber == other.PatternAlternatePerLayersNumber && PatternInvert == other.PatternInvert && PatternText == other.PatternText && PatternTextAlternate == other.PatternTextAlternate && PatternGenMinBrightness == other.PatternGenMinBrightness && PatternGenBrightness == other.PatternGenBrightness && PatternGenInfillThickness == other.PatternGenInfillThickness && PatternGenInfillSpacing == other.PatternGenInfillSpacing && NoiseMinOffset == other.NoiseMinOffset && NoiseMaxOffset == other.NoiseMaxOffset && NoiseThreshold == other.NoiseThreshold && _noisePixelArea == other._noisePixelArea && _noisePasses == other._noisePasses;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((OperationPixelArithmetic) obj);
    }

    #endregion
}