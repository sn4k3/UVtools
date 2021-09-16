/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using MoreLinq.Extensions;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations
{
    [Serializable]
    public sealed class OperationDynamicLifts : Operation
    {
        #region Enums

        public enum DynamicLiftsSetMethod : byte
        {
            // Reduces maximal lift height with the number of pixels in layer divided by maximal number of pixels in any layer. Increases the minimal speed with the same ratio.
            [Description("Traditional: Reduces maximal lift height with the number of pixels in layer divided by the maximal of all layers")]
            Traditional,
            //Squeezes lift height and lift speed within full range of min/max values. E.g. the layer with the least pixels gets minimal lift height and maximal lift speed. The layer with the most pixels gets maximal lift height and minimal lift speed.
            [Description("Full Range: Squeezes lift height and lift speed within full range of min/max values")]
            FullRange
        }

        public enum DynamicLiftsLightOffDelaySetMode : byte
        {
            [Description("Set the light-off with an extra delay")]
            UpdateWithExtraDelay,

            [Description("Set the light-off without an extra delay")]
            UpdateWithoutExtraDelay,

            [Description("Set the light-off to zero")]
            SetToZero,
        }
        #endregion

        #region Members

        private DynamicLiftsSetMethod _setMethod = DynamicLiftsSetMethod.FullRange;
        private float _minBottomLiftHeight;
        private float _maxBottomLiftHeight;
        private float _minLiftHeight;
        private float _maxLiftHeight;
        private float _minBottomLiftSpeed;
        private float _maxBottomLiftSpeed;
        private float _minLiftSpeed;
        private float _maxLiftSpeed;

        private float _lightOffDelayBottomExtraTime = 3;
        private float _lightOffDelayExtraTime = 2.5f;
        private DynamicLiftsLightOffDelaySetMode _lightOffDelaySetMode = DynamicLiftsLightOffDelaySetMode.UpdateWithExtraDelay;

        #endregion

        #region Overrides

        public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.Normal;

        public override string Title => "Dynamic lifts";

        public override string Description =>
            "Generate dynamic lift height and speeds for each layer given it mass.\n" +
            "Larger masses requires more lift height and less speed while smaller masses can go with shorter lift height and more speed.\n" +
            "If you have a raft, start after it layer number to not influence the calculations.\n" +
            "Note: Only few printers support this. Running this on an unsupported printer will cause no harm.";

        public override string ConfirmationText =>
            $"generate dynamic lifts from layers {LayerIndexStart} through {LayerIndexEnd}?";

        public override string ProgressTitle =>
            $"Generating dynamic lifts from layers {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressAction => "Generated lifts";

        public override string ValidateSpawn()
        {
            if (!SlicerFile.CanUseLayerLiftHeight || !SlicerFile.CanUseLayerLiftSpeed)
            {
                return NotSupportedMessage;
            }

            return null;
        }

        public override string ValidateInternally()
        {
            var sb = new StringBuilder();

            if (_minBottomLiftHeight > _maxBottomLiftHeight)
            {
                sb.AppendLine("Minimum bottom lift height can't be higher than the maximum.");
            }
            if (_minBottomLiftSpeed > _maxBottomLiftSpeed)
            {
                sb.AppendLine("Minimum bottom lift speed can't be higher than the maximum.");
            }

            if (_minLiftHeight > _maxLiftHeight)
            {
                sb.AppendLine("Minimum lift height can't be higher than the maximum.");
            }
            if (_minLiftSpeed > _maxLiftSpeed)
            {
                sb.AppendLine("Minimum lift speed can't be higher than the maximum.");
            }

            if (_minBottomLiftHeight == _maxBottomLiftHeight &&
                _minBottomLiftSpeed == _maxBottomLiftSpeed &&
                _minLiftHeight == _maxLiftHeight &&
                _minLiftSpeed == _maxLiftSpeed)
            {
                sb.AppendLine("The selected min/max settings are all equal and will not produce a change.");
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            var result = 
                 $"[Method: {_setMethod}]" +
                 $" [Bottom height: {_minBottomLiftHeight}/{_maxBottomLiftHeight}mm]" +
                 $" [Bottom speed: {_minBottomLiftSpeed}/{_maxBottomLiftSpeed}mm/min]" +
                 $" [Height: {_minLiftHeight}/{_maxLiftHeight}mm]" +
                 $" [Speed: {_minLiftSpeed}/{_maxLiftSpeed}mm/min]" +
                 $" [Light-off: {_lightOffDelaySetMode} {_lightOffDelayBottomExtraTime}/{_lightOffDelayExtraTime}s]" +
                 LayerRangeString;
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }

        #endregion

        #region Properties

        public DynamicLiftsSetMethod SetMethod
        {
            get => _setMethod;
            set => RaiseAndSetIfChanged(ref _setMethod, value);
        }

        public float MinBottomLiftHeight
        {
            get => _minBottomLiftHeight;
            set => RaiseAndSetIfChanged(ref _minBottomLiftHeight, (float)Math.Round(value, 2));
        }

        public float MaxBottomLiftHeight
        {
            get => _maxBottomLiftHeight;
            set => RaiseAndSetIfChanged(ref _maxBottomLiftHeight, (float)Math.Round(value, 2));
        }

        public float MinLiftHeight
        {
            get => _minLiftHeight;
            set => RaiseAndSetIfChanged(ref _minLiftHeight, (float)Math.Round(value, 2));
        }

        public float MaxLiftHeight
        {
            get => _maxLiftHeight;
            set => RaiseAndSetIfChanged(ref _maxLiftHeight, (float)Math.Round(value, 2));
        }

        public float MinBottomLiftSpeed
        {
            get => _minBottomLiftSpeed;
            set => RaiseAndSetIfChanged(ref _minBottomLiftSpeed, (float)Math.Round(value, 2));
        }

        public float MaxBottomLiftSpeed
        {
            get => _maxBottomLiftSpeed;
            set => RaiseAndSetIfChanged(ref _maxBottomLiftSpeed, (float)Math.Round(value, 2));
        }

        public float MinLiftSpeed
        {
            get => _minLiftSpeed;
            set => RaiseAndSetIfChanged(ref _minLiftSpeed, (float)Math.Round(value, 2));
        }

        public float MaxLiftSpeed
        {
            get => _maxLiftSpeed;
            set => RaiseAndSetIfChanged(ref _maxLiftSpeed, (float)Math.Round(value, 2));
        }

        public DynamicLiftsLightOffDelaySetMode LightOffDelaySetMode
        {
            get => _lightOffDelaySetMode;
            set => RaiseAndSetIfChanged(ref _lightOffDelaySetMode, value);
        }

        public float LightOffDelayBottomExtraTime
        {
            get => _lightOffDelayBottomExtraTime;
            set => RaiseAndSetIfChanged(ref _lightOffDelayBottomExtraTime, (float)Math.Round(value, 2));
        }

        public float LightOffDelayExtraTime
        {
            get => _lightOffDelayExtraTime;
            set => RaiseAndSetIfChanged(ref _lightOffDelayExtraTime, (float)Math.Round(value, 2));
        }

        //public uint MinBottomLayerPixels => SlicerFile.Where(layer => layer.IsBottomLayer && !layer.IsEmpty && layer.Index >= LayerIndexStart && layer.Index <= LayerIndexEnd).Max(layer => layer.NonZeroPixelCount);
        public uint MinBottomLayerPixels => (from layer in SlicerFile
            where layer.IsBottomLayer
            where !layer.IsEmpty
            where layer.Index >= LayerIndexStart
            where layer.Index <= LayerIndexEnd
            select layer.NonZeroPixelCount).Min();

        //public uint MinNormalLayerPixels => SlicerFile.Where(layer => layer.IsNormalLayer && !layer.IsEmpty && layer.Index >= LayerIndexStart && layer.Index <= LayerIndexEnd).Max(layer => layer.NonZeroPixelCount);
        public uint MinNormalLayerPixels => (from layer in SlicerFile
            where layer.IsNormalLayer
            where !layer.IsEmpty
            where layer.Index >= LayerIndexStart
            where layer.Index <= LayerIndexEnd
            select layer.NonZeroPixelCount).Min();

        //public uint MaxBottomLayerPixels => SlicerFile.Where(layer => layer.IsBottomLayer && layer.Index >= LayerIndexStart && layer.Index <= LayerIndexEnd).Max(layer => layer.NonZeroPixelCount);
        public uint MaxBottomLayerPixels => (from layer in SlicerFile
            where layer.IsBottomLayer
            where !layer.IsEmpty
            where layer.Index >= LayerIndexStart
            where layer.Index <= LayerIndexEnd
            select layer.NonZeroPixelCount).Max();
        //public uint MaxNormalLayerPixels => SlicerFile.Where(layer => layer.IsNormalLayer && layer.Index >= LayerIndexStart && layer.Index <= LayerIndexEnd).Max(layer => layer.NonZeroPixelCount);
        public uint MaxNormalLayerPixels => (from layer in SlicerFile
            where layer.IsNormalLayer
            where !layer.IsEmpty
            where layer.Index >= LayerIndexStart
            where layer.Index <= LayerIndexEnd
            select layer.NonZeroPixelCount).Max();

        #endregion

        #region Constructor

        public OperationDynamicLifts()
        { }

        public OperationDynamicLifts(FileFormat slicerFile) : base(slicerFile)
        { }

        public override void InitWithSlicerFile()
        {
            base.InitWithSlicerFile();

            if(_minBottomLiftHeight <= 0) _minBottomLiftHeight = SlicerFile.BottomLiftHeightTotal;
            if (_maxBottomLiftHeight <= 0 || _maxBottomLiftHeight < _minBottomLiftHeight) _maxBottomLiftHeight = _minBottomLiftHeight;

            if (_minLiftHeight <= 0) _minLiftHeight = SlicerFile.LiftHeightTotal;
            if (_maxLiftHeight <= 0 || _maxLiftHeight < _minLiftHeight) _maxLiftHeight = _minLiftHeight;

            if (_minBottomLiftSpeed <= 0) _minBottomLiftSpeed = SlicerFile.BottomLiftSpeed;
            if (_maxBottomLiftSpeed <= 0 || _maxBottomLiftSpeed < _minBottomLiftSpeed) _maxBottomLiftSpeed = _minBottomLiftSpeed;

            if(_minLiftSpeed <= 0) _minLiftSpeed = SlicerFile.LiftSpeed;
            if (_maxLiftSpeed <= 0 || _maxLiftSpeed < _minLiftSpeed) _maxLiftSpeed = _minLiftSpeed;
        }

        #endregion

        #region Methods

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            uint minBottomPixels = 0;
            uint minNormalPixels = 0;
            uint maxBottomPixels = 0;
            uint maxNormalPixels = 0;

            try
            {
                minBottomPixels = MinBottomLayerPixels;
            }
            catch
            {
            }

            try
            {
                minNormalPixels = MinNormalLayerPixels;
            }
            catch
            {
            }

            try
            {
                maxBottomPixels = MaxBottomLayerPixels;
            }
            catch
            {
            }

            try
            {
                maxNormalPixels = MaxNormalLayerPixels;
            }
            catch
            {
            }

            float liftHeight = 0;
            float liftSpeed = 0;

            //uint max = (from layer in SlicerFile where !layer.IsBottomLayer where !layer.IsEmpty where layer.Index >= LayerIndexStart where layer.Index <= LayerIndexEnd select layer).Aggregate<Layer, uint>(0, (current, layer) => Math.Max(layer.NonZeroPixelCount, current));

            for (uint layerIndex = LayerIndexStart; layerIndex <= LayerIndexEnd; layerIndex++)
            {
                progress.Token.ThrowIfCancellationRequested();
                var layer = SlicerFile[layerIndex];
                
                // Height
                // min - largestpixelcount
                //  x  - pixelcount

                // Speed
                // max - minpixelCount
                //  x  - pixelcount

                if (layer.IsBottomLayer)
                {
                    switch (_setMethod)
                    {
                        case DynamicLiftsSetMethod.Traditional:
                            liftHeight = (_maxBottomLiftHeight * layer.NonZeroPixelCount / maxBottomPixels).Clamp(_minBottomLiftHeight, _maxBottomLiftHeight);
                            liftSpeed = (_maxBottomLiftSpeed - (_maxBottomLiftSpeed * layer.NonZeroPixelCount / maxBottomPixels)).Clamp(_minBottomLiftSpeed, _maxBottomLiftSpeed);
                            break;
                        case DynamicLiftsSetMethod.FullRange:
                            var pixelRatio = (layer.NonZeroPixelCount - minBottomPixels) / (float)(maxBottomPixels - minBottomPixels); // pixel_ratio is between 0 and 1
                            liftHeight = (_minBottomLiftHeight + ((_maxBottomLiftHeight - _minBottomLiftHeight) * pixelRatio)).Clamp(_minBottomLiftHeight, _maxBottomLiftHeight);
                            liftSpeed = (_maxBottomLiftSpeed - ((_maxBottomLiftSpeed - _minBottomLiftSpeed) * pixelRatio)).Clamp(_minBottomLiftSpeed, _maxBottomLiftSpeed);
                            break;
                    }
                    
                }
                else
                {
                    switch (_setMethod)
                    {
                        case DynamicLiftsSetMethod.Traditional:
                            liftHeight = (_maxLiftHeight * layer.NonZeroPixelCount / maxNormalPixels).Clamp(_minLiftHeight, _maxLiftHeight);
                            liftSpeed = (_maxLiftSpeed - (_maxLiftSpeed * layer.NonZeroPixelCount / maxNormalPixels)).Clamp(_minLiftSpeed, _maxLiftSpeed);
                            break;
                        case DynamicLiftsSetMethod.FullRange:
                            var pixelRatio = (layer.NonZeroPixelCount - minNormalPixels) / (float)(maxNormalPixels - minNormalPixels); // pixel_ratio is between 0 and 1
                            liftHeight = (_minLiftHeight + ((_maxLiftHeight - _minLiftHeight) * pixelRatio)).Clamp(_minLiftHeight, _maxLiftHeight);
                            liftSpeed = (_maxLiftSpeed - ((_maxLiftSpeed - _minLiftSpeed) * pixelRatio)).Clamp(_minLiftSpeed, _maxLiftSpeed);
                            break;
                    }
                }

                layer.RetractHeight2 = 0;
                layer.LiftHeightTotal = (float) Math.Round(liftHeight, 1);
                layer.LiftSpeed = (float) Math.Round(liftSpeed, 1);

                switch (_lightOffDelaySetMode)
                {
                    case DynamicLiftsLightOffDelaySetMode.UpdateWithExtraDelay:
                        layer.SetLightOffDelay(layer.IsBottomLayer ? _lightOffDelayBottomExtraTime : _lightOffDelayExtraTime);
                        break;
                    case DynamicLiftsLightOffDelaySetMode.UpdateWithoutExtraDelay:
                        layer.SetLightOffDelay();
                        break;
                    case DynamicLiftsLightOffDelaySetMode.SetToZero:
                        layer.LightOffDelay = 0;
                        break;
                    default:
                        throw new NotImplementedException();
                }
                

                progress++;
            }

            return !progress.Token.IsCancellationRequested;
        }

        public Layer GetSmallestLayer(bool isBottom)
        {
            return SlicerFile.Where((layer, index) => !layer.IsEmpty && layer.IsBottomLayer == isBottom && index >= LayerIndexStart && index <= LayerIndexEnd).MinBy(layer => layer.NonZeroPixelCount).FirstOrDefault();
        }

        public Layer GetLargestLayer(bool isBottom)
        {
            return SlicerFile.Where((layer, index) => !layer.IsEmpty && layer.IsBottomLayer == isBottom && index >= LayerIndexStart && index <= LayerIndexEnd).MaxBy(layer => layer.NonZeroPixelCount).FirstOrDefault();
        }

        #endregion

        #region Equality


        private bool Equals(OperationDynamicLifts other)
        {
            return _setMethod == other._setMethod && _minBottomLiftHeight.Equals(other._minBottomLiftHeight) && _maxBottomLiftHeight.Equals(other._maxBottomLiftHeight) && _minLiftHeight.Equals(other._minLiftHeight) && _maxLiftHeight.Equals(other._maxLiftHeight) && _minBottomLiftSpeed.Equals(other._minBottomLiftSpeed) && _maxBottomLiftSpeed.Equals(other._maxBottomLiftSpeed) && _minLiftSpeed.Equals(other._minLiftSpeed) && _maxLiftSpeed.Equals(other._maxLiftSpeed) && _lightOffDelayBottomExtraTime.Equals(other._lightOffDelayBottomExtraTime) && _lightOffDelayExtraTime.Equals(other._lightOffDelayExtraTime) && _lightOffDelaySetMode == other._lightOffDelaySetMode;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is OperationDynamicLifts other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add((int)_setMethod);
            hashCode.Add(_minBottomLiftHeight);
            hashCode.Add(_maxBottomLiftHeight);
            hashCode.Add(_minLiftHeight);
            hashCode.Add(_maxLiftHeight);
            hashCode.Add(_minBottomLiftSpeed);
            hashCode.Add(_maxBottomLiftSpeed);
            hashCode.Add(_minLiftSpeed);
            hashCode.Add(_maxLiftSpeed);
            hashCode.Add(_lightOffDelayBottomExtraTime);
            hashCode.Add(_lightOffDelayExtraTime);
            hashCode.Add((int)_lightOffDelaySetMode);
            return hashCode.ToHashCode();
        }

        #endregion
    }
}
