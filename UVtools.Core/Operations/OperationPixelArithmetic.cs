/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
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
        #region Members
        private PixelArithmeticOperators _operator = PixelArithmeticOperators.Set;
        private byte _value = byte.MaxValue;
        private ThresholdType _thresholdType = ThresholdType.Binary;
        private byte _thresholdMaxValue = 255;
        private bool _affectBackPixels;

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
        #endregion

        #region Overrides
        public override string Title => "Pixel arithmetic";

        public override string Description =>
            "Perform arithmetic operations over the pixels";

        public override string ConfirmationText =>
            $"arithmetic {_operator}" +
            (ValueEnabled ? $"={_value}" : string.Empty) +
            (_operator is PixelArithmeticOperators.Threshold ? $"/{_thresholdMaxValue}" : string.Empty)
            + $" layers from {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressTitle =>
            $"Arithmetic {_operator}"+
            (ValueEnabled ? $"={_value}" : string.Empty)
            +$" layers from {LayerIndexStart} through {LayerIndexEnd}";

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

            return sb.ToString();
        }

        public override string ToString()
        {
            var result = $"[{_operator}: {_value}] [ABP: {_affectBackPixels}]"
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
                RaisePropertyChanged(nameof(ThresholdEnabled));
                RaisePropertyChanged(nameof(AffectBackPixelsEnabled));
            }
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

        public bool AffectBackPixels
        {
            get => _affectBackPixels;
            set => RaiseAndSetIfChanged(ref _affectBackPixels, value);
        }

        public bool AffectBackPixelsEnabled => _operator
            is not PixelArithmeticOperators.Subtract
            and not PixelArithmeticOperators.Multiply
            and not PixelArithmeticOperators.Divide
            and not PixelArithmeticOperators.BitwiseNot
            and not PixelArithmeticOperators.BitwiseAnd
            and not PixelArithmeticOperators.KeepRegion
            and not PixelArithmeticOperators.DiscardRegion
            and not PixelArithmeticOperators.Threshold
            ;

        #endregion

        #region Constructor

        public OperationPixelArithmetic() { }

        public OperationPixelArithmetic(FileFormat slicerFile) : base(slicerFile) { }

        #endregion

        #region Methods

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            var tempMat = GetTempMat();

            Parallel.For(LayerIndexStart, LayerIndexEnd + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                using (var mat = SlicerFile[layerIndex].LayerMat)
                {
                    Execute(mat, tempMat);
                    SlicerFile[layerIndex].LayerMat = mat;
                }

                progress.LockAndIncrement();
            });

            tempMat?.Dispose();

            return !progress.Token.IsCancellationRequested;
        }

        public override bool Execute(Mat mat, params object[] arguments)
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

            switch (_operator)
            {
                case PixelArithmeticOperators.Set:
                    tempMat.CopyTo(target, _affectBackPixels ? null : target);
                    break;
                case PixelArithmeticOperators.Add:
                    CvInvoke.Add(target, tempMat, target, _affectBackPixels ? null : target);
                    break;
                case PixelArithmeticOperators.Subtract:
                    CvInvoke.Subtract(target, tempMat, target, _affectBackPixels ? null : target);
                    break;
                case PixelArithmeticOperators.Multiply:
                    CvInvoke.Multiply(target, tempMat, target);
                    break;
                case PixelArithmeticOperators.Divide:
                    CvInvoke.Divide(target, tempMat, target);
                    break;
                /*case PixelArithmeticOperators.Exponential:
                    CvInvoke.Pow(target, _value, tempMat);
                    if(!_affectBackPixels) ApplyMask(original, mat, original);
                    break;*/
                case PixelArithmeticOperators.Minimum:
                    CvInvoke.Min(target, tempMat, target);
                    if (!_affectBackPixels) ApplyMask(original, target, original);
                    break;
                case PixelArithmeticOperators.Maximum:
                    CvInvoke.Max(target, tempMat, target);
                    if (!_affectBackPixels) ApplyMask(original, target, original);
                    break;
                case PixelArithmeticOperators.BitwiseNot:
                    CvInvoke.BitwiseNot(target, target);
                    break;
                case PixelArithmeticOperators.BitwiseAnd:
                    CvInvoke.BitwiseAnd(target, tempMat, target);
                    break;
                case PixelArithmeticOperators.BitwiseOr:
                    CvInvoke.BitwiseOr(target, tempMat, target, _affectBackPixels ? null : target);
                    break;
                case PixelArithmeticOperators.BitwiseXor:
                    CvInvoke.BitwiseXor(target, tempMat, target, _affectBackPixels ? null : target);
                    break;
                case PixelArithmeticOperators.Threshold:
                    CvInvoke.Threshold(target, target, _value, _thresholdMaxValue, _thresholdType);
                    break;
                case PixelArithmeticOperators.AbsDiff:
                    CvInvoke.AbsDiff(target, tempMat, target);
                    if (!_affectBackPixels) ApplyMask(original, target, original);
                    break;
                case PixelArithmeticOperators.KeepRegion:
                {
                    using var targetClone = target.Clone();
                    original.SetTo(EmguExtensions.BlackByte);
                    mat.SetTo(EmguExtensions.BlackByte);
                    targetClone.CopyTo(target);
                    break;
                }
                case PixelArithmeticOperators.DiscardRegion:
                    target.SetTo(EmguExtensions.BlackByte);
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
        }

        public Mat GetTempMat() => _operator
                is not PixelArithmeticOperators.BitwiseNot
                and not PixelArithmeticOperators.KeepRegion
                and not PixelArithmeticOperators.DiscardRegion ? EmguExtensions.InitMat(HaveROI ? ROI.Size : SlicerFile.Resolution, new MCvScalar(_value)) : null;

        public void PresetStripAntiAliasing()
        {
            Operator = PixelArithmeticOperators.Threshold;
            Value = 127;
            ThresholdMaxValue = 255;
            ThresholdType = ThresholdType.Binary;
        }

        public void PresetHalfBrightness()
        {
            Value = 128;
        }

        #endregion

        #region Equality

        protected bool Equals(OperationPixelArithmetic other)
        {
            return _operator == other._operator && _value == other._value && _thresholdType == other._thresholdType && _thresholdMaxValue == other._thresholdMaxValue && _affectBackPixels == other._affectBackPixels;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OperationPixelArithmetic) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int) _operator, _value, (int) _thresholdType, _thresholdMaxValue, _affectBackPixels);
        }

        #endregion
    }
}
