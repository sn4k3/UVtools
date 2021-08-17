/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations
{
    [Serializable]
    public class OperationPixelArithmetic : Operation
    {
        #region Subclasses
        class StringMatrix
        {
            public string Text { get; }
            public Matrix<byte> Pattern { get; set; }

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
        private byte _value = byte.MaxValue;
        private bool _usePattern;
        private ThresholdType _thresholdType = ThresholdType.Binary;
        private byte _thresholdMaxValue = 255;
        private ushort _patternAlternatePerLayersNumber = 1;
        private bool _patternInvert;
        private string _patternText;
        private string _patternTextAlternate;
        private Matrix<byte> _pattern;
        private Matrix<byte> _patternAlternate;
        private byte _patternGenMinBrightness;
        private byte _patternGenBrightness = 128;
        private byte _patternGenInfillThickness = 10;
        private byte _patternGenInfillSpacing = 20;
        

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
            [Description("Threshold: between a minimum/maximum brightness")]
            Threshold,
            [Description("Keep Region: in the selected ROI or masks")]
            KeepRegion,
            [Description("Discard Region: in the selected ROI or masks")]
            DiscardRegion
        }

        public enum PixelArithmeticApplyMethod : byte
        {
            [Description("All: Apply to all pixels within the layer")]
            All,
            [Description("Model: Apply only to model pixels")]
            Model,
            [Description("Model inner: Apply only to model pixels within a margin from walls")]
            ModelInner,
            [Description("Model walls: Apply only to model walls with a set thickness")]
            ModelWalls,
            //[Description("Model walls minimum: Apply only to model walls where walls must have at least a minimum set thickness")]
            //ModelWallsMinimum
        }
        #endregion

        #region Overrides
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

        public override string ValidateInternally()
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
                RaisePropertyChanged(nameof(ThresholdEnabled));
                RaisePropertyChanged(nameof(IsApplyMethodEnabled));
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

        public bool IsWallSettingVisible => _applyMethod is PixelArithmeticApplyMethod.ModelInner or PixelArithmeticApplyMethod.ModelWalls; //or PixelArithmeticApplyMethod.ModelWallsMinimum;

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
            ;

        public bool IsUsePatternVisible => _operator
            is not PixelArithmeticOperators.Threshold
            and not PixelArithmeticOperators.BitwiseNot
            and not PixelArithmeticOperators.KeepRegion
            and not PixelArithmeticOperators.DiscardRegion
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

        public bool ThresholdEnabled => _operator is PixelArithmeticOperators.Threshold;

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
        public Matrix<byte> PatternAlternate
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

        #endregion

        #region Constructor

        public OperationPixelArithmetic() { }

        public OperationPixelArithmetic(FileFormat slicerFile) : base(slicerFile) { }

        #endregion

        #region Methods

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            Mat patternMat = null;
            Mat patternAlternateMat = null;
            Mat patternMatMask = null;
            Mat patternAlternateMatMask = null;
            var anchor = new Point(-1, -1);
            var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), anchor);


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

                using var blankMat = new Mat(SlicerFile.Resolution, DepthType.Cv8U, 1);
                patternMat = blankMat.NewBlank();
                patternAlternateMat = blankMat.NewBlank();
                var target = GetRoiOrDefault(blankMat);

                CvInvoke.Repeat(_pattern, target.Rows / _pattern.Rows + 1, target.Cols / _pattern.Cols + 1, patternMat);
                CvInvoke.Repeat(_patternAlternate, target.Rows / _patternAlternate.Rows + 1, target.Cols / _patternAlternate.Cols + 1, patternAlternateMat);

                patternMatMask = new Mat(patternMat, new Rectangle(0, 0, target.Width, target.Height));
                patternAlternateMatMask = new Mat(patternAlternateMat, new Rectangle(0, 0, target.Width, target.Height));

                /*if (_patternInvert)
                {
                    CvInvoke.BitwiseNot(patternMatMask, patternMatMask);
                    CvInvoke.BitwiseNot(patternAlternateMatMask, patternAlternateMatMask);
                }*/
            }
            else if (_operator is not PixelArithmeticOperators.BitwiseNot
                and not PixelArithmeticOperators.KeepRegion
                and not PixelArithmeticOperators.DiscardRegion)
            {
                patternMatMask = EmguExtensions.InitMat(HaveROI ? ROI.Size : SlicerFile.Resolution, new MCvScalar(_value));
            }


            Parallel.For(LayerIndexStart, LayerIndexEnd + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                using (var mat = SlicerFile[layerIndex].LayerMat)
                {
                    //Execute(mat, tempMat);

                    using var original = mat.Clone();
                    var originalRoi = GetRoiOrDefault(original);
                    var target = GetRoiOrDefault(mat);
                    Mat tempMat;

                    if (_usePattern && IsUsePatternVisible)
                    {
                        tempMat = IsNormalPattern((uint)layerIndex) ? patternMatMask : patternAlternateMatMask;
                    }
                    else
                    {
                        tempMat = patternMatMask;
                    }

                    Mat applyMask;
                    
                    int wallThickness = LayerManager.MutateGetIterationChamfer(
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
                            applyMask = target;
                            break;
                        case PixelArithmeticApplyMethod.ModelInner:
                            applyMask = wallThickness <= 0 ? target : new Mat();
                            CvInvoke.Erode(target, applyMask, kernel, anchor, wallThickness, BorderType.Reflect101, default);
                            break;
                        case PixelArithmeticApplyMethod.ModelWalls:
                        {
                            if (wallThickness <= 0) // No effect, skip
                            {
                                progress.LockAndIncrement();
                                return;
                            }

                            using var erode = new Mat();
                            applyMask = target.Clone();
                            CvInvoke.Erode(target, erode, kernel, anchor, wallThickness, BorderType.Reflect101, default);
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
                            CvInvoke.Multiply(target, tempMat, target);
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
                        case PixelArithmeticOperators.Threshold:
                            CvInvoke.Threshold(target, target, _value, _thresholdMaxValue, _thresholdType);
                            if (_applyMethod != PixelArithmeticApplyMethod.All) ApplyMask(originalRoi, target, applyMask);
                            break;
                        case PixelArithmeticOperators.AbsDiff:
                            CvInvoke.AbsDiff(target, tempMat, target);
                            if (_applyMethod != PixelArithmeticApplyMethod.All) ApplyMask(originalRoi, target, applyMask);
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

                    ApplyMask(original, target);

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

        /*public override bool Execute(Mat mat, params object[] arguments)
        {
            using var original = mat.Clone();
            var target = GetRoiOrDefault(mat);

            Mat tempMat;
            bool needDispose = false;
            if (arguments is not null && arguments.Length > 0)
            {
                tempMat = arguments[0] as Mat;
            }
            else
            {
                tempMat = GetTempMat();
                needDispose = true;
            }

            Mat applyMask;
            var anchor = new Point(-1, -1);
            var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), anchor);
            int wallThickness = LayerManager.MutateGetIterationChamfer(
                layerIndex,
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
                case PixelArithmeticApplyMethod.ModelInner:
                    CvInvoke.Erode(target, applyMask, kernel, anchor, wallThickness, BorderType.Reflect101, default);
                    applyMask = target;
                    break;
                case PixelArithmeticApplyMethod.ModelWalls:
                    applyMask = target;
                    break;
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
                    CvInvoke.Multiply(target, tempMat, target);
                    break;
                case PixelArithmeticOperators.Divide:
                    CvInvoke.Divide(target, tempMat, target);
                    break;
                //case PixelArithmeticOperators.Exponential:
                //    CvInvoke.Pow(target, _value, tempMat);
                //    if(!_affectBackPixels) ApplyMask(original, mat, original);
                //    break;
                case PixelArithmeticOperators.Minimum:
                    CvInvoke.Min(target, tempMat, target);
                    if (_applyMethod != PixelArithmeticApplyMethod.All) ApplyMask(original, target, original);
                    break;
                case PixelArithmeticOperators.Maximum:
                    CvInvoke.Max(target, tempMat, target);
                    if (_applyMethod != PixelArithmeticApplyMethod.All) ApplyMask(original, target, original);
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
                case PixelArithmeticOperators.Threshold:
                    CvInvoke.Threshold(target, target, _value, _thresholdMaxValue, _thresholdType);
                    break;
                case PixelArithmeticOperators.AbsDiff:
                    CvInvoke.AbsDiff(target, tempMat, target);
                    if (_applyMethod != PixelArithmeticApplyMethod.All) ApplyMask(original, target, original);
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

            ApplyMask(original, target);

            if (needDispose)
            {
                tempMat?.Dispose();
            }

            return true;
        }*/

        public Mat GetTempMat() => _operator
                is not PixelArithmeticOperators.BitwiseNot
                and not PixelArithmeticOperators.KeepRegion
                and not PixelArithmeticOperators.DiscardRegion ? EmguExtensions.InitMat(HaveROI ? ROI.Size : SlicerFile.Resolution, new MCvScalar(_value)) : null;

        public void PresetPixelDimming()
        {
            Operator = PixelArithmeticOperators.Subtract;
            ApplyMethod = PixelArithmeticApplyMethod.ModelInner;
            WallThickness = 20;
            WallChamfer = false;
            UsePattern = true;
        }

        public void PresetPixelLightening()
        {
            PresetPixelDimming();
            Operator = PixelArithmeticOperators.Add;
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

        public void PresetHalfBrightness()
        {
            Value = 128;
        }

        public unsafe void LoadPatternFromImage(Mat mat, bool isAlternatePattern = false)
        {
            var result = new string[mat.Height];
            var span = mat.GetBytePointer();
            Parallel.For(0, mat.Height, y =>
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
                PatternTextAlternate = null;
                return;
            }
        }

        public void GenerateInfill(string pattern)
        {
            if (pattern == "Rectilinear")
            {
                PatternText = ($"255\n".Repeat(_patternGenInfillSpacing) + $"0\n".Repeat(_patternGenInfillThickness)).Trim('\n', '\r');
                PatternTextAlternate = null;
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
                PatternTextAlternate = null;
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
                PatternTextAlternate = null;
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
                PatternTextAlternate = p2.Trim('\n', '\r'); ;
                return;
            }
        }

        #endregion

        #region Equality

        protected bool Equals(OperationPixelArithmetic other)
        {
            return _operator == other._operator && _applyMethod == other._applyMethod && _wallThicknessStart == other._wallThicknessStart && _wallThicknessEnd == other._wallThicknessEnd && _wallChamfer == other._wallChamfer && _value == other._value && _usePattern == other._usePattern && _thresholdType == other._thresholdType && _thresholdMaxValue == other._thresholdMaxValue && _patternAlternatePerLayersNumber == other._patternAlternatePerLayersNumber && _patternInvert == other._patternInvert && _patternText == other._patternText && _patternTextAlternate == other._patternTextAlternate && _patternGenMinBrightness == other._patternGenMinBrightness && _patternGenBrightness == other._patternGenBrightness && _patternGenInfillThickness == other._patternGenInfillThickness && _patternGenInfillSpacing == other._patternGenInfillSpacing;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OperationPixelArithmetic)obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add((int)_operator);
            hashCode.Add((int)_applyMethod);
            hashCode.Add(_wallThicknessStart);
            hashCode.Add(_wallThicknessEnd);
            hashCode.Add(_wallChamfer);
            hashCode.Add(_value);
            hashCode.Add(_usePattern);
            hashCode.Add((int)_thresholdType);
            hashCode.Add(_thresholdMaxValue);
            hashCode.Add(_patternAlternatePerLayersNumber);
            hashCode.Add(_patternInvert);
            hashCode.Add(_patternText);
            hashCode.Add(_patternTextAlternate);
            hashCode.Add(_patternGenMinBrightness);
            hashCode.Add(_patternGenBrightness);
            hashCode.Add(_patternGenInfillThickness);
            hashCode.Add(_patternGenInfillSpacing);
            return hashCode.ToHashCode();
        }

        #endregion
    }
}
