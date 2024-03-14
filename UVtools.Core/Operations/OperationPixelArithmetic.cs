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
public class OperationPixelArithmetic : Operation
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
    private PixelArithmeticOperators _operator = PixelArithmeticOperators.Set;
    private PixelArithmeticApplyMethod _applyMethod = PixelArithmeticApplyMethod.Model;
    private uint _wallThicknessStart = 20;
    private uint _wallThicknessEnd = 20;
    private bool _wallChamfer;
    private PixelArithmeticIgnoreAreaOperator _ignoreAreaOperator = PixelArithmeticIgnoreAreaOperator.SmallerThan;
    private uint _ignoreAreaThreshold;
    private byte _value = byte.MaxValue;
    private bool _usePattern;
    private ThresholdType _thresholdType = ThresholdType.Binary;
    private byte _thresholdMaxValue = 255;
    private ushort _patternAlternatePerLayersNumber = 1;
    private bool _patternInvert;
    private string _patternText = null!;
    private string _patternTextAlternate = null!;
    private Matrix<byte> _pattern = null!;
    private Matrix<byte>? _patternAlternate;
    private byte _patternGenMinBrightness;
    private byte _patternGenBrightness = 128;
    private byte _patternGenInfillThickness = 10;
    private byte _patternGenInfillSpacing = 20;
    private short _noiseMinOffset = -128;
    private short _noiseMaxOffset = 128;
    private byte _noiseThreshold;
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
    public override string IconClass => "mdi-circle-opacity";
    public override string Title => "Pixel arithmetic";

    public override string Description =>
        "Perform arithmetic operations over the pixels.";

    public override string ConfirmationText =>
        $"arithmetic {_operator}" +
        (ValueEnabled && !_usePattern ? $"={_value}" : string.Empty) +
        (_usePattern && IsUsePatternVisible ? " with pattern" : string.Empty) +
        (_operator is PixelArithmeticOperators.Threshold ? $"/{_thresholdMaxValue}" : string.Empty)
        + $" layers from {LayerIndexStart} through {LayerIndexEnd}";

    public override string ProgressTitle =>
        $"Arithmetic {_operator}"+
        (ValueEnabled && !_usePattern ? $"={_value}" : string.Empty) +
        (_usePattern && IsUsePatternVisible ? " with pattern" : string.Empty) +
        $" layers from {LayerIndexStart} through {LayerIndexEnd}";

    public override string ProgressAction => "Calculated layers";

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();
        if (_operator == PixelArithmeticOperators.KeepRegion && !HaveROI && !HaveMask)
        {
            sb.AppendLine("The 'Keep' operator requires selected ROI/masks.");
        }
        else if (_operator == PixelArithmeticOperators.DiscardRegion && !HaveROI && !HaveMask)
        {
            sb.AppendLine("The 'Discard' operator requires selected ROI/masks.");
        }
        else if (_operator 
                     is PixelArithmeticOperators.Add 
                     or PixelArithmeticOperators.Subtract
                     or PixelArithmeticOperators.Maximum
                     or PixelArithmeticOperators.BitwiseOr
                     or PixelArithmeticOperators.BitwiseXor
                     or PixelArithmeticOperators.AbsDiff
                 && _value == 0) 
            /*||
                 (_operator is PixelArithmeticOperators.Exponential && _value == 1)
                 )*/
        {
            sb.AppendLine($"{_operator} by {_value} will have no effect.");
        }
        else if (_operator == PixelArithmeticOperators.Divide && _value == 0)
        {
            sb.AppendLine("Can't divide by 0.");
        }
        else if (_operator == PixelArithmeticOperators.Corrode && _noiseMinOffset >= _noiseMaxOffset)
        {
            sb.AppendLine("Minimum noise offset must be less than the maximum offset.");
        }

        if (_applyMethod is PixelArithmeticApplyMethod.ModelWalls //or PixelArithmeticApplyMethod.ModelWallsMinimum
            && (
                (_wallChamfer && _wallThicknessStart == 0 && _wallThicknessEnd == 0) ||
                (!_wallChamfer && _wallThicknessStart == 0)
            )
           )
        {
            sb.AppendLine("The current wall settings will have no effect.");
        }

        if (_usePattern && IsUsePatternVisible)
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
                            item.Pattern[row, col] = (byte)(_patternInvert ? byte.MaxValue - value : value);
                        }
                        else
                        {
                            sb.AppendLine($"{bytes[col]} is a invalid number, use values from 0 to 255");
                            return sb.ToString();
                        }
                    }
                }
            }

            _pattern = stringMatrix[0].Pattern;
            _patternAlternate = stringMatrix[1].Pattern;

            if (_pattern is null && _patternAlternate is null)
            {
                sb.AppendLine("Either even or odd pattern must contain a valid matrix.");
                return sb.ToString();
            }
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"[{_operator}: {_value}] [Apply: {_applyMethod}] " +
                     $"[Pattern: {_usePattern}]"
                     + LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }
    #endregion

    #region Properties

    public PixelArithmeticOperators Operator
    {
        get => _operator;
        set
        {
            if(!RaiseAndSetIfChanged(ref _operator, value)) return;
            RaisePropertyChanged(nameof(ValueEnabled));
            RaisePropertyChanged(nameof(IsUsePatternVisible));
            RaisePropertyChanged(nameof(IsThresholdVisible));
            RaisePropertyChanged(nameof(IsApplyMethodEnabled));
            RaisePropertyChanged(nameof(IsCorrodeVisible));
        }
    }

    public bool IsApplyMethodEnabled =>
        _operator is not (PixelArithmeticOperators.KeepRegion or PixelArithmeticOperators.DiscardRegion);

    public PixelArithmeticApplyMethod ApplyMethod
    {
        get => _applyMethod;
        set
        {
            if(!RaiseAndSetIfChanged(ref _applyMethod, value)) return;
            RaisePropertyChanged(nameof(IsWallSettingVisible));
        }
    }

    public bool IsWallSettingVisible => _applyMethod 
        is PixelArithmeticApplyMethod.ModelSurfaceAndInset
        or PixelArithmeticApplyMethod.ModelInner 
        or PixelArithmeticApplyMethod.ModelWalls; //or PixelArithmeticApplyMethod.ModelWallsMinimum;

    public uint WallThickness
    {
        get => _wallThicknessStart;
        set
        {
            WallThicknessStart = value;
            WallThicknessEnd = value;
        }
    }

    public uint WallThicknessStart
    {
        get => _wallThicknessStart;
        set => RaiseAndSetIfChanged(ref _wallThicknessStart, value);
    }

    public uint WallThicknessEnd
    {
        get => _wallThicknessEnd;
        set => RaiseAndSetIfChanged(ref _wallThicknessEnd, value);
    }

    public bool WallChamfer
    {
        get => _wallChamfer;
        set => RaiseAndSetIfChanged(ref _wallChamfer, value);
    }

    public PixelArithmeticIgnoreAreaOperator IgnoreAreaOperator
    {
        get => _ignoreAreaOperator;
        set => RaiseAndSetIfChanged(ref _ignoreAreaOperator, value);
    }

    public uint IgnoreAreaThreshold
    {
        get => _ignoreAreaThreshold;
        set => RaiseAndSetIfChanged(ref _ignoreAreaThreshold, value);
    }


    public bool IsCorrodeVisible => _operator is PixelArithmeticOperators.Corrode;

    public short NoiseMinOffset
    {
        get => _noiseMinOffset;
        set => RaiseAndSetIfChanged(ref _noiseMinOffset, value);
    }

    public short NoiseMaxOffset
    {
        get => _noiseMaxOffset;
        set => RaiseAndSetIfChanged(ref _noiseMaxOffset, value);
    }

    public byte NoiseThreshold
    {
        get => _noiseThreshold;
        set => RaiseAndSetIfChanged(ref _noiseThreshold, value);
    }

    public ushort NoisePixelArea
    {
        get => _noisePixelArea;
        set => RaiseAndSetIfChanged(ref _noisePixelArea, Math.Max((byte)1, value));
    }

    public byte NoisePasses
    {
        get => _noisePasses;
        set => RaiseAndSetIfChanged(ref _noisePasses, Math.Max((byte)1, value));
    }

    public byte Value
    {
        get => _value;
        set
        {
            if(!RaiseAndSetIfChanged(ref _value, value)) return;
            RaisePropertyChanged(nameof(ValuePercent));
        }
    }

    // 255  - 100
    //value -  x
    public float ValuePercent => (float) Math.Round(_value * 100f / byte.MaxValue, 2);

    public bool ValueEnabled => _operator
        is not PixelArithmeticOperators.BitwiseNot
        and not PixelArithmeticOperators.KeepRegion
        and not PixelArithmeticOperators.DiscardRegion
        and not PixelArithmeticOperators.Corrode
    ;

    public bool IsUsePatternVisible => _operator
        is not PixelArithmeticOperators.Threshold
        and not PixelArithmeticOperators.BitwiseNot
        and not PixelArithmeticOperators.KeepRegion
        and not PixelArithmeticOperators.DiscardRegion
        and not PixelArithmeticOperators.Corrode
    ;

    public bool UsePattern
    {
        get => _usePattern;
        set => RaiseAndSetIfChanged(ref _usePattern, value);
    }

    public ThresholdType ThresholdType
    {
        get => _thresholdType;
        set => RaiseAndSetIfChanged(ref _thresholdType, value);
    }

    public byte ThresholdMaxValue
    {
        get => _thresholdMaxValue;
        set => RaiseAndSetIfChanged(ref _thresholdMaxValue, value);
    }

    public bool IsThresholdVisible => _operator is PixelArithmeticOperators.Threshold;

    /*public bool AffectBackPixelsEnabled => _operator
        is not PixelArithmeticOperators.Subtract
        and not PixelArithmeticOperators.Multiply
        and not PixelArithmeticOperators.Divide
        and not PixelArithmeticOperators.BitwiseNot
        and not PixelArithmeticOperators.BitwiseAnd
        and not PixelArithmeticOperators.KeepRegion
        and not PixelArithmeticOperators.DiscardRegion
        and not PixelArithmeticOperators.Threshold
        ;*/

    public ushort PatternAlternatePerLayersNumber
    {
        get => _patternAlternatePerLayersNumber;
        set => RaiseAndSetIfChanged(ref _patternAlternatePerLayersNumber, value);
    }

    public bool PatternInvert
    {
        get => _patternInvert;
        set => RaiseAndSetIfChanged(ref _patternInvert, value);
    }

    public string PatternText
    {
        get => _patternText;
        set => RaiseAndSetIfChanged(ref _patternText, value);
    }

    public string PatternTextAlternate
    {
        get => _patternTextAlternate;
        set => RaiseAndSetIfChanged(ref _patternTextAlternate, value);
    }

    [XmlIgnore]
    public Matrix<byte> Pattern
    {
        get => _pattern;
        set => RaiseAndSetIfChanged(ref _pattern, value);
    }

    [XmlIgnore]
    public Matrix<byte>? PatternAlternate
    {
        get => _patternAlternate;
        set => RaiseAndSetIfChanged(ref _patternAlternate, value);
    }

    public byte PatternGenMinBrightness
    {
        get => _patternGenMinBrightness;
        set => RaiseAndSetIfChanged(ref _patternGenMinBrightness, value);
    }

    public byte PatternGenBrightness
    {
        get => _patternGenBrightness;
        set
        {
            RaiseAndSetIfChanged(ref _patternGenBrightness, value);
            RaisePropertyChanged(nameof(PatternGenBrightnessPercent));
        }
    }

    public float PatternGenBrightnessPercent => Helpers.BrightnessToPercent(_patternGenBrightness);

    public byte PatternGenInfillThickness
    {
        get => _patternGenInfillThickness;
        set => RaiseAndSetIfChanged(ref _patternGenInfillThickness, value);
    }

    public byte PatternGenInfillSpacing
    {
        get => _patternGenInfillSpacing;
        set => RaiseAndSetIfChanged(ref _patternGenInfillSpacing, value);
    }

    public KernelConfiguration Kernel { get; set; } = new();

    #endregion

    #region Constructor

    public OperationPixelArithmetic() { }

    public OperationPixelArithmetic(FileFormat slicerFile) : base(slicerFile) { }

    #endregion

    #region Methods

    private Size GetMatSizeCropped(Mat? mat = null)
    {
        return _applyMethod == PixelArithmeticApplyMethod.All ? GetRoiSizeOrDefault(mat) : GetRoiSizeOrDefault(OriginalBoundingRectangle);
    }

    private Mat GetMatRoiCropped(Mat mat)
    {
        return _applyMethod == PixelArithmeticApplyMethod.All ? GetRoiOrDefault(mat) : GetRoiOrVolumeBounds(mat);
    }

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        Mat? patternMat = null;
        Mat? patternAlternateMat = null;
        Mat patternMatMask = null!;
        Mat patternAlternateMatMask = null!;

        if (_usePattern && IsUsePatternVisible)
        {
            if (_pattern is null)
            {
                _pattern = new Matrix<byte>(2, 2)
                {
                    [0, 0] = 0,
                    [0, 1] = 127,
                    [1, 0] = 127,
                    [1, 1] = 0,
                };

                _patternAlternate ??= new Matrix<byte>(2, 2)
                {
                    [0, 0] = 127,
                    [0, 1] = 0,
                    [1, 0] = 0,
                    [1, 1] = 127,
                };
            }

            _patternAlternate ??= _pattern;

            var target = new Mat(GetMatSizeCropped(), DepthType.Cv8U, 1);
            patternMat = target.NewZeros();
            patternAlternateMat = target.NewZeros();

            CvInvoke.Repeat(_pattern, (int)Math.Ceiling((double)target.Rows / _pattern.Rows), (int)Math.Ceiling((double)target.Cols / _pattern.Cols), patternMat);
            CvInvoke.Repeat(_patternAlternate, (int)Math.Ceiling((double)target.Rows / _patternAlternate.Rows), (int)Math.Ceiling((double)target.Cols / _patternAlternate.Cols), patternAlternateMat);

            patternMatMask = patternMat.Roi(target);
            patternAlternateMatMask = patternAlternateMat.Roi(target);

            /*if (_patternInvert)
            {
                CvInvoke.BitwiseNot(patternMatMask, patternMatMask);
                CvInvoke.BitwiseNot(patternAlternateMatMask, patternAlternateMatMask);
            }*/
        }
        else if (IsUsePatternVisible)
        {
            patternMatMask = EmguExtensions.InitMat(GetMatSizeCropped(), new MCvScalar(_value));
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

                if (_usePattern && IsUsePatternVisible)
                {
                    tempMat = IsNormalPattern((uint)layerIndex) ? patternMatMask : patternAlternateMatMask;
                }
                else
                {
                    tempMat = patternMatMask;
                }

                Mat? applyMask;
                    
                int wallThickness = FileFormat.MutateGetIterationChamfer(
                    (uint)layerIndex,
                    LayerIndexStart,
                    LayerIndexEnd,
                    (int)_wallThicknessStart,
                    (int)_wallThicknessEnd,
                    _wallChamfer
                );

                switch (_applyMethod)
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
                            CvInvoke.Erode(target, erode, kernel, EmguExtensions.AnchorCenter, iterations, BorderType.Reflect101, default);
                            CvInvoke.Subtract(target, erode, erode);
                            CvInvoke.Add(applyMask, erode, applyMask);
                                

                            // Inset from walls
                            if (_applyMethod == PixelArithmeticApplyMethod.ModelSurfaceAndInset && (wallThickness-1) > 0)
                            {
                                iterations = wallThickness - 1;
                                kernel = Kernel.GetKernel(ref iterations);
                                CvInvoke.Dilate(applyMask, erode, kernel, EmguExtensions.AnchorCenter, iterations, BorderType.Reflect101, default);
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
                        CvInvoke.Erode(target, applyMask, kernel, EmguExtensions.AnchorCenter, iterations, BorderType.Reflect101, default);
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
                        CvInvoke.Erode(target, erode, kernel, EmguExtensions.AnchorCenter, iterations, BorderType.Reflect101, default);
                        applyMask.SetTo(EmguExtensions.BlackColor, erode);
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
                            applyMask.SetTo(EmguExtensions.BlackColor, erode);
                            applyMask.Save($"D:\\wallmin\\applymask{layerIndex}.png");
                            break;
                    }*/
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                switch (_operator)
                {
                    case PixelArithmeticOperators.Set:
                        tempMat.CopyTo(target, applyMask);
                        break;
                    case PixelArithmeticOperators.Add:
                        CvInvoke.Add(target, tempMat, target, applyMask);
                        break;
                    case PixelArithmeticOperators.Subtract:
                        CvInvoke.Subtract(target, tempMat, target, applyMask);
                        break;
                    case PixelArithmeticOperators.Multiply:
                        CvInvoke.Multiply(target, tempMat, target, EmguExtensions.ByteScale);
                        if (_applyMethod != PixelArithmeticApplyMethod.All) ApplyMask(originalRoi, target, applyMask);
                        break;
                    case PixelArithmeticOperators.Divide:
                        CvInvoke.Divide(target, tempMat, target);
                        if (_applyMethod != PixelArithmeticApplyMethod.All) ApplyMask(originalRoi, target, applyMask);
                        break;
                    /*case PixelArithmeticOperators.Exponential:
                        CvInvoke.Pow(target, _value, tempMat);
                        if(!_affectBackPixels) ApplyMask(original, mat, original);
                        break;*/
                    case PixelArithmeticOperators.Minimum:
                        CvInvoke.Min(target, tempMat, target);
                        if (_applyMethod != PixelArithmeticApplyMethod.All) ApplyMask(originalRoi, target, applyMask);
                        break;
                    case PixelArithmeticOperators.Maximum:
                        CvInvoke.Max(target, tempMat, target);
                        if (_applyMethod != PixelArithmeticApplyMethod.All) ApplyMask(originalRoi, target, applyMask);
                        break;
                    case PixelArithmeticOperators.BitwiseNot:
                        CvInvoke.BitwiseNot(target, target, applyMask);
                        break;
                    case PixelArithmeticOperators.BitwiseAnd:
                        CvInvoke.BitwiseAnd(target, tempMat, target, applyMask);
                        break;
                    case PixelArithmeticOperators.BitwiseOr:
                        CvInvoke.BitwiseOr(target, tempMat, target, applyMask);
                        break;
                    case PixelArithmeticOperators.BitwiseXor:
                        CvInvoke.BitwiseXor(target, tempMat, target, applyMask);
                        break;
                    case PixelArithmeticOperators.AbsDiff:
                        CvInvoke.AbsDiff(target, tempMat, target);
                        if (_applyMethod != PixelArithmeticApplyMethod.All) ApplyMask(originalRoi, target, applyMask);
                        break;
                    case PixelArithmeticOperators.Threshold:
                        var tempThreshold = _thresholdType;
                        if (_thresholdType is ThresholdType.Otsu or ThresholdType.Triangle) tempThreshold = ThresholdType.Binary | tempThreshold;
                        CvInvoke.Threshold(target, target, _value, _thresholdMaxValue, tempThreshold);
                        if (_applyMethod != PixelArithmeticApplyMethod.All) ApplyMask(originalRoi, target, applyMask);
                        break;
                    case PixelArithmeticOperators.Corrode:
                        var span = mat.GetDataByteSpan();
                        var random = new Random();

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

                            if (zoneBrightness <= _noiseThreshold) continue;
                            byte brightness = zoneBrightness;

                            for (ushort i = 0; i < _noisePasses; i++)
                            {
                                brightness = (byte)Math.Clamp(random.Next(_noiseMinOffset, _noiseMaxOffset + 1) + brightness, byte.MinValue, byte.MaxValue);
                            }

                            //byte brightness = (byte)Math.Clamp(RandomNumberGenerator.GetInt32(_noiseMinOffset, _noiseMaxOffset + 1) + zoneBrightness, byte.MinValue, byte.MaxValue);
                            for (var y1 = y; y1 < y + _noisePixelArea && y1 < bounds.Bottom; y1++)
                            {
                                var pixelPos = mat.GetPixelPos(x, y1);
                                for (var x1 = x; x1 < x + _noisePixelArea && x1 < bounds.Right; x1++)
                                {
                                        
                                    if (span[pixelPos] <= _noiseThreshold) continue;
                                    span[pixelPos++] = brightness;
                                }
                            }
                        }

                        if (_applyMethod is not PixelArithmeticApplyMethod.All and not PixelArithmeticApplyMethod.Model) ApplyMask(originalRoi, target, applyMask);


                        // old method
                        /*if (HaveROI)
                        {
                            for (var y = ROI.Y; y < ROI.Bottom; y++)
                            for (var x = ROI.X; x < ROI.Right; x++)
                            {
                                var pos = mat.GetPixelPos(x, y);
                                if (span[pos] <= _noiseThreshold) continue;
                                span[pos] = (byte)Math.Clamp(RandomNumberGenerator.GetInt32(_noiseMinOffset, _noiseMaxOffset + 1) + span[pos], byte.MinValue, byte.MaxValue);
                            }

                            if (_applyMethod
                                is not PixelArithmeticApplyMethod.All
                                and not PixelArithmeticApplyMethod.Model)
                                ApplyMask(originalRoi, target, applyMask);
                        }
                        else // Whole image
                        {
                            var spanMask = applyMask is null ? span : applyMask.GetDataByteSpan();

                            for (var i = 0; i < span.Length; i++)
                            {
                                //if (span[i] <= _noiseThreshold || spanMask[i] == 0) continue;
                                //span[i] = (byte)Math.Clamp(RandomNumberGenerator.GetInt32(_noiseMinOffset, _noiseMaxOffset + 1) + span[i], byte.MinValue, byte.MaxValue);
                                span[i] = (byte)Math.Clamp(random.Next(_noiseMinOffset, _noiseMaxOffset + 1) + span[i], byte.MinValue, byte.MaxValue);
                            }
                        }*/

                        break;
                    case PixelArithmeticOperators.KeepRegion:
                    {
                        using var targetClone = target.Clone();
                        original.SetTo(EmguExtensions.BlackColor);
                        mat.SetTo(EmguExtensions.BlackColor);
                        targetClone.CopyTo(target);
                        break;
                    }
                    case PixelArithmeticOperators.DiscardRegion:
                        target.SetTo(EmguExtensions.BlackColor);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                switch (_ignoreAreaOperator)
                {
                    case PixelArithmeticIgnoreAreaOperator.SmallerThan:
                        originalRoi.CopyAreasSmallerThan(_ignoreAreaThreshold, target);
                        break;
                    case PixelArithmeticIgnoreAreaOperator.LargerThan:
                        originalRoi.CopyAreasLargerThan(_ignoreAreaThreshold, target);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(_ignoreAreaOperator));
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

    public bool IsNormalPattern(uint layerIndex) => layerIndex / _patternAlternatePerLayersNumber % 2 == 0;

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
        var span = mat.GetBytePointer();
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
                , _patternGenMinBrightness, _patternGenBrightness, "\n");

            PatternTextAlternate = string.Format(
                "{1} {0}{2}" +
                "{0} {1}"
                , _patternGenMinBrightness, _patternGenBrightness, "\n");

            return;
        }

        if (pattern == "Sparse")
        {
            PatternText = string.Format(
                "{1} {0} {0} {0}{2}" +
                "{0} {0} {1} {0}"
                , _patternGenMinBrightness, _patternGenBrightness, "\n");

            PatternTextAlternate = string.Format(
                "{0} {0} {1} {0}{2}" +
                "{1} {0} {0} {0}"
                , _patternGenMinBrightness, _patternGenBrightness, "\n");
            return;
        }

        if (pattern == "Crosses")
        {
            PatternText = string.Format(
                "{1} {0} {1} {0}{2}" +
                "{0} {1} {0} {0}{2}" +
                "{1} {0} {1} {0}{2}" +
                "{0} {0} {0} {0}"
                , _patternGenMinBrightness, _patternGenBrightness, "\n");

            PatternTextAlternate = string.Format(
                "{0} {0} {0} {0}{2}" +
                "{1} {0} {1} {0}{2}" +
                "{0} {1} {0} {0}{2}" +
                "{1} {0} {1} {0}"
                , _patternGenMinBrightness, _patternGenBrightness, "\n");
            return;
        }

        if (pattern == "Strips")
        {
            PatternText = string.Format(
                "{1}{2}" +
                "{0}"
                , _patternGenMinBrightness, _patternGenBrightness, "\n");

            PatternTextAlternate = string.Format(
                "{0}{2}" +
                "{1}"
                , _patternGenMinBrightness, _patternGenBrightness, "\n");
            return;
        }

        if (pattern == "Pyramid")
        {
            PatternText = string.Format(
                "{0} {0} {1} {0} {0} {0}{2}" +
                "{0} {1} {0} {1} {0} {0}{2}" +
                "{1} {0} {1} {0} {1} {0}{2}" +
                "{0} {0} {0} {0} {0} {0}"
                , _patternGenMinBrightness, _patternGenBrightness, "\n");

            PatternTextAlternate = string.Format(
                "{0} {1} {0} {1} {0} {1}{2}" +
                "{0} {0} {1} {0} {1} {0}{2}" +
                "{0} {0} {0} {1} {0} {0}{2}" +
                "{0} {0} {0} {0} {0} {0}"
                , _patternGenMinBrightness, _patternGenBrightness, "\n");
            return;
        }

        if (pattern == "Rhombus")
        {
            PatternText = string.Format(
                "{0} {1} {0} {0}{2}" +
                "{1} {0} {1} {0}{2}" +
                "{0} {1} {0} {0}{2}" +
                "{0} {0} {0} {0}"
                , _patternGenMinBrightness, _patternGenBrightness, "\n");

            PatternTextAlternate = string.Format(
                "{0} {0} {0} {0}{2}" +
                "{0} {1} {0} {0}{2}" +
                "{1} {0} {1} {0}{2}" +
                "{0} {1} {0} {0}"
                , _patternGenMinBrightness, _patternGenBrightness, "\n");
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
                , _patternGenMinBrightness, _patternGenBrightness, "\n");

            PatternTextAlternate = string.Format(
                "{0} {0} {0} {0} {0} {0}{2}" +
                "{0} {0} {1} {0} {1} {0}{2}" +
                "{0} {1} {0} {1} {0} {1}{2}" +
                "{0} {1} {0} {0} {0} {1}{2}" +
                "{0} {0} {1} {0} {1} {0}{2}" +
                "{0} {0} {0} {1} {0} {0}"
                , _patternGenMinBrightness, _patternGenBrightness, "\n");
            return;
        }

        if (pattern == "Slashes")
        {
            PatternText = string.Format(
                "{1} {0} {0}{2}" +
                "{0} {1} {0}{2}" +
                "{0} {0} {1}"
                , _patternGenMinBrightness, _patternGenBrightness, "\n");

            PatternTextAlternate = string.Format(
                "{0} {0} {1}{2}" +
                "{0} {1} {0}{2}" +
                "{1} {0} {0}"
                , _patternGenMinBrightness, _patternGenBrightness, "\n");
            return;
        }

        if (pattern == "Waves")
        {
            PatternText = string.Format(
                "{1} {0} {0}{2}" +
                "{0} {0} {1}"
                , _patternGenMinBrightness, _patternGenBrightness, "\n");

            PatternTextAlternate = string.Format(
                "{0} {0} {1}{2}" +
                "{1} {0} {0}"
                , _patternGenMinBrightness, _patternGenBrightness, "\n");
            return;
        }

        if (pattern == "Solid")
        {
            PatternText = _patternGenBrightness.ToString();
            PatternTextAlternate = null!;
            return;
        }
    }

    public void GenerateInfill(object pattern) => GenerateInfill(pattern.ToString()!);
    public void GenerateInfill(string pattern)
    {
        if (pattern == "Rectilinear")
        {
            PatternText = ("255\n".Repeat(_patternGenInfillSpacing) + "0\n".Repeat(_patternGenInfillThickness)).Trim('\n', '\r');
            PatternTextAlternate = null!;
            return;
        }

        if (pattern == "Square grid")
        {
            var p1 = "255 ".Repeat(_patternGenInfillSpacing) + "0 ".Repeat(_patternGenInfillThickness);
            p1 = p1.Trim() + "\n";
            p1 += p1.Repeat(_patternGenInfillThickness);


            var p2 = "0 ".Repeat(_patternGenInfillSpacing) + "0 ".Repeat(_patternGenInfillThickness);
            p2 = p2.Trim() + '\n';
            p2 += p2.Repeat(_patternGenInfillThickness);

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
                while (pos >= 0 && pos <= _patternGenInfillSpacing)
                {
                    p1 += "255 ".Repeat(pos);
                    p1 += "0 ".Repeat(_patternGenInfillThickness);
                    p1 += "255 ".Repeat(_patternGenInfillSpacing - pos);
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

            var zeros = Math.Max(0, _patternGenInfillSpacing - _patternGenInfillThickness * 2);

            // Pillar
            for (int i = 0; i < _patternGenInfillThickness; i++)
            {
                p1 += "0 ".Repeat(_patternGenInfillThickness);
                p1 += "255 ".Repeat(zeros);
                p1 += "0 ".Repeat(_patternGenInfillThickness);
                p1 = p1.Trim() + '\n';
            }

            for (int i = 0; i < zeros; i++)
            {
                p1 += "255 ".Repeat(_patternGenInfillSpacing);
                p1 = p1.Trim() + '\n';
            }

            for (int i = 0; i < _patternGenInfillThickness; i++)
            {
                p1 += "0 ".Repeat(_patternGenInfillThickness);
                p1 += "255 ".Repeat(zeros);
                p1 += "0 ".Repeat(_patternGenInfillThickness);
                p1 = p1.Trim() + '\n';
            }

            // Square
            for (int i = 0; i < _patternGenInfillThickness; i++)
            {
                p2 += "0 ".Repeat(_patternGenInfillSpacing);
                p2 = p2.Trim() + '\n';
            }

            for (int i = 0; i < zeros; i++)
            {
                p2 += "0 ".Repeat(_patternGenInfillThickness);
                p2 += "255 ".Repeat(zeros);
                p2 += "0 ".Repeat(_patternGenInfillThickness);
                p2 = p2.Trim() + '\n';
            }

            for (int i = 0; i < _patternGenInfillThickness; i++)
            {
                p2 += "0 ".Repeat(_patternGenInfillSpacing);
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
        return _operator == other._operator && _applyMethod == other._applyMethod && _wallThicknessStart == other._wallThicknessStart && _wallThicknessEnd == other._wallThicknessEnd && _wallChamfer == other._wallChamfer && _ignoreAreaOperator == other._ignoreAreaOperator && _ignoreAreaThreshold == other._ignoreAreaThreshold && _value == other._value && _usePattern == other._usePattern && _thresholdType == other._thresholdType && _thresholdMaxValue == other._thresholdMaxValue && _patternAlternatePerLayersNumber == other._patternAlternatePerLayersNumber && _patternInvert == other._patternInvert && _patternText == other._patternText && _patternTextAlternate == other._patternTextAlternate && _patternGenMinBrightness == other._patternGenMinBrightness && _patternGenBrightness == other._patternGenBrightness && _patternGenInfillThickness == other._patternGenInfillThickness && _patternGenInfillSpacing == other._patternGenInfillSpacing && _noiseMinOffset == other._noiseMinOffset && _noiseMaxOffset == other._noiseMaxOffset && _noiseThreshold == other._noiseThreshold && _noisePixelArea == other._noisePixelArea && _noisePasses == other._noisePasses;
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