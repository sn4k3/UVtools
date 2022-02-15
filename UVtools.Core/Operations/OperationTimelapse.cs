/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;

namespace UVtools.Core.Operations
{
    [Serializable]
    public class OperationTimelapse : Operation
    {
        #region Constants
        #endregion

        #region Enums
        public enum TimelapseRaiseMode
        {
            [Description("Lift height: Use the lift sequence to raise to the set postion (Faster)")]
            LiftHeight,

            [Description("Virtual layer: Print a blank layer to simulate and raise to the set position (Slower)")]
            VirtualLayer,
        }
        #endregion

        #region Members
        private decimal _raisePositionZ;
        private bool _outputDummyPixel = true;
        private decimal _raiseEachNthHeight = 1;
        private TimelapseRaiseMode _raiseMode = TimelapseRaiseMode.LiftHeight;
        private decimal _waitTimeAfterLift = 1;
        private decimal _exposureTime = 1;
        private bool _useCustomLift;
        private decimal _slowLiftHeight = 3;
        private decimal _liftSpeed;
        private decimal _liftSpeed2;
        private decimal _slowRetractHeight = 3;
        private decimal _retractSpeed;
        private decimal _retractSpeed2;

        #endregion

        #region Overrides

        public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.Normal;

        public override string Title => "Timelapse";
        public override string Description =>
            "Raise the build platform to a set position every odd-even height to be able to take a photo and create a time-lapse video of the print.\n" +
            "You will require external hardware to take the photos, and create the time-lapse video by your own.\n" +
            "NOTE: Only use this tool once. It will delay the total print time significantly.";

        public override string ConfirmationText =>
            $"raise the platform at every odd-even {_raiseEachNthHeight}mm to Z={_raisePositionZ}mm";

        public override string ProgressTitle => "Raising layers";

        public override string ProgressAction => "Raised layer";

        public override string ValidateSpawn()
        {
            if(!SlicerFile.CanUseLayerPositionZ && !SlicerFile.CanUseLayerLiftHeight)
            {
                return NotSupportedMessage;
            }

            return null;
        }

        public override string ValidateInternally()
        {
            var sb = new StringBuilder();

            if (!ValidateSpawn(out var message))
            {
                sb.AppendLine(message);
            }

            if ((_raiseMode == TimelapseRaiseMode.LiftHeight && !SlicerFile.CanUseLayerLiftHeight) ||
                (_raiseMode == TimelapseRaiseMode.VirtualLayer && !SlicerFile.CanUseLayerPositionZ))
            {
                return $"The raise method {_raiseMode} is not compatible with this printer / file format, please choose other method.";
            }
            
            return sb.ToString();
        }

        public override string ToString()
        {
            var result = $"[Mode: {_raiseMode}] [Z={_raisePositionZ}mm] [Raise each: {_raiseEachNthHeight}mm]";
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }
        #endregion

        #region Properties

        public TimelapseRaiseMode RaiseMode
        {
            get => _raiseMode;
            set
            {
                if(!RaiseAndSetIfChanged(ref _raiseMode, value)) return;
                RaisePropertyChanged(nameof(IsLiftHeightMode));
                RaisePropertyChanged(nameof(IsVirtualLayerMode));
            }
        }

        public bool IsLiftHeightMode => _raiseMode is TimelapseRaiseMode.LiftHeight;
        public bool IsVirtualLayerMode => _raiseMode is TimelapseRaiseMode.VirtualLayer;

        /// <summary>
        /// Sets or gets the Z position to raise to
        /// </summary>
        public decimal RaisePositionZ
        {
            get => _raisePositionZ;
            set => RaiseAndSetIfChanged(ref _raisePositionZ, Layer.RoundHeight(Math.Clamp(value, 10, 10000)));
        }

        /// <summary>
        /// True to output a dummy pixel on bounding rectangle position to avoid empty layer and blank image, otherwise set to false
        /// </summary>
        public bool OutputDummyPixel 
        {
            get => _outputDummyPixel; 
            set => RaiseAndSetIfChanged(ref _outputDummyPixel, value); 
        }

        /// <summary>
        /// Gets or sets the alternating height in millimeters to raise when, it will raise only at each defined millimeters and skip the same next millimeters
        /// </summary>
        public decimal RaiseEachNthHeight
        {
            get => _raiseEachNthHeight;
            set => RaiseAndSetIfChanged(ref _raiseEachNthHeight, Math.Max(0, value));
        }

        public ushort RaiseEachNthLayers
        {
            get
            {
                if (_raiseEachNthHeight == 0) return 1;
                return (ushort)Math.Max(1, _raiseEachNthHeight / (decimal)SlicerFile.LayerHeight);
            }
        }

        public decimal WaitTimeAfterLift
        {
            get => _waitTimeAfterLift;
            set => RaiseAndSetIfChanged(ref _waitTimeAfterLift, Math.Max(0, value));
        }

        public decimal ExposureTime
        {
            get => _exposureTime;
            set => RaiseAndSetIfChanged(ref _exposureTime, Math.Max(0, value));
        }

        public bool UseCustomLift
        {
            get => _useCustomLift;
            set => RaiseAndSetIfChanged(ref _useCustomLift, value);
        }

        public bool CanUseCustomLift => _useCustomLift && SlicerFile.CanUseLayerLiftHeight;

        public decimal SlowLiftHeight
        {
            get => _slowLiftHeight;
            set => RaiseAndSetIfChanged(ref _slowLiftHeight, Math.Max(0, value));
        }

        public decimal LiftSpeed
        {
            get => _liftSpeed;
            set => RaiseAndSetIfChanged(ref _liftSpeed, Math.Max(0, value));
        }

        public decimal LiftSpeed2
        {
            get => _liftSpeed2;
            set => RaiseAndSetIfChanged(ref _liftSpeed2, Math.Max(0, value));
        }

        public decimal SlowRetractHeight
        {
            get => _slowRetractHeight;
            set => RaiseAndSetIfChanged(ref _slowRetractHeight, Math.Max(0, value));
        }

        public decimal RetractSpeed
        {
            get => _retractSpeed;
            set => RaiseAndSetIfChanged(ref _retractSpeed, Math.Max(0, value));
        }

        public decimal RetractSpeed2
        {
            get => _retractSpeed2;
            set => RaiseAndSetIfChanged(ref _retractSpeed2, Math.Max(0, value));
        }

        #endregion

        #region Constructor

        public OperationTimelapse()
        {
        }

        public OperationTimelapse(FileFormat slicerFile) : base(slicerFile) 
        {
            if (_raisePositionZ <= 0) _raisePositionZ = (decimal)SlicerFile.MachineZ;
            if(_exposureTime <= 0) _exposureTime = SlicerFile.SupportsGCode ? 0 : 0.05M;

            if (_liftSpeed <= 0) _liftSpeed = (decimal) SlicerFile.LiftSpeed;
            if (_liftSpeed2 <= 0) _liftSpeed2 = (decimal) SlicerFile.MaximumSpeed;

            if (SlicerFile.CanUseLayerRetractSpeed2) // TSMC
            {
                if (_retractSpeed <= 0) _retractSpeed = (decimal)SlicerFile.MaximumSpeed;
                if (_retractSpeed2 <= 0) _retractSpeed2 = (decimal)SlicerFile.RetractSpeed2;
            }
            else
            {
                if (_retractSpeed <= 0) _retractSpeed = (decimal)SlicerFile.RetractSpeed;
            }
            
        }

        #endregion

        #region Equality

        protected bool Equals(OperationTimelapse other)
        {
            return _raisePositionZ == other._raisePositionZ && _outputDummyPixel == other._outputDummyPixel && _raiseEachNthHeight == other._raiseEachNthHeight && _raiseMode == other._raiseMode && _waitTimeAfterLift == other._waitTimeAfterLift && _exposureTime == other._exposureTime && _useCustomLift == other._useCustomLift && _slowLiftHeight == other._slowLiftHeight && _liftSpeed == other._liftSpeed && _liftSpeed2 == other._liftSpeed2 && _slowRetractHeight == other._slowRetractHeight && _retractSpeed == other._retractSpeed && _retractSpeed2 == other._retractSpeed2;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OperationTimelapse) obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(_raisePositionZ);
            hashCode.Add(_outputDummyPixel);
            hashCode.Add(_raiseEachNthHeight);
            hashCode.Add((int) _raiseMode);
            hashCode.Add(_waitTimeAfterLift);
            hashCode.Add(_exposureTime);
            hashCode.Add(_useCustomLift);
            hashCode.Add(_slowLiftHeight);
            hashCode.Add(_liftSpeed);
            hashCode.Add(_liftSpeed2);
            hashCode.Add(_slowRetractHeight);
            hashCode.Add(_retractSpeed);
            hashCode.Add(_retractSpeed2);
            return hashCode.ToHashCode();
        }

        #endregion

        #region Methods

        public void OptimizeRaisePositionZ()
        {
            RaisePositionZ = (decimal) Math.Min(SlicerFile.MachineZ, SlicerFile.PrintHeight + 5);
        }

        public void MaxRaisePositionZ()
        {
            RaisePositionZ = (decimal) SlicerFile.MachineZ;
        }

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            var virtualLayers = new List<uint>();
            float checkpointHeight = SlicerFile[0].PositionZ;
            for (uint layerIndex = LayerIndexStart; layerIndex <= LayerIndexEnd; layerIndex++)
            {
                progress++;
                var layer = SlicerFile[layerIndex];
                if (_raiseEachNthHeight > 0 && (decimal)Layer.RoundHeight(layer.PositionZ - checkpointHeight) < _raiseEachNthHeight) continue;
                if ((decimal)layer.PositionZ >= _raisePositionZ) break; // pass the target height, do not continue
                checkpointHeight = layer.PositionZ;

                switch (_raiseMode)
                {
                    case TimelapseRaiseMode.LiftHeight:
                        if (CanUseCustomLift)
                        {
                            layer.LiftSpeed = (float)_liftSpeed;
                            layer.RetractSpeed = (float)_retractSpeed;

                            if (SlicerFile.CanUseLayerLiftHeight2)
                            {
                                layer.LiftHeight = (float)_slowLiftHeight;
                            }

                            if (SlicerFile.CanUseLayerLiftSpeed2)
                            {
                                layer.LiftSpeed2 = (float)_liftSpeed2;
                            }

                            if (SlicerFile.CanUseLayerRetractHeight2)
                            {
                                layer.RetractHeight2 = (float)_slowRetractHeight;
                            }

                            if (SlicerFile.CanUseLayerRetractSpeed2)
                            {
                                layer.RetractSpeed2 = (float)_retractSpeed2;
                            }
                        }

                        if (SlicerFile.CanUseLayerLiftHeight2 && (layer.LiftHeight2 > 0 || CanUseCustomLift && _slowLiftHeight > 0)) // TSMC
                        {
                            layer.LiftHeight2 = Math.Max(0, (float)_raisePositionZ - layer.PositionZ - layer.LiftHeight);
                        }
                        else
                        {
                            layer.LiftHeightTotal = Math.Max(layer.LiftHeightTotal, (float)_raisePositionZ - layer.PositionZ);
                        }

                        if (SlicerFile.CanUseLayerWaitTimeAfterLift && _waitTimeAfterLift > 0)
                        {
                            layer.WaitTimeAfterLift = (float) _waitTimeAfterLift;
                        }

                        break;
                    case TimelapseRaiseMode.VirtualLayer:
                        virtualLayers.Add(layerIndex);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(RaiseMode));
                }
            }

            if (virtualLayers.Count > 0 && _raiseMode == TimelapseRaiseMode.VirtualLayer)
            {
                using var mat = _outputDummyPixel
                    ? SlicerFile.CreateMatWithDummyPixel()
                    : SlicerFile.CreateMat();

                var layer = new Layer(SlicerFile.LayerCount, mat, SlicerFile)
                {
                    PositionZ = (float) _raisePositionZ,
                    ExposureTime = (float) _exposureTime,
                    // This layer does not require a lift procedure
                    LiftHeightTotal = SlicerFile.SupportsGCode ? 0 : 0.1f, 
                    LiftSpeed = SlicerFile.MaximumSpeed,
                    LiftSpeed2 = SlicerFile.MaximumSpeed,
                    RetractSpeed = SlicerFile.MaximumSpeed,
                    RetractSpeed2 = SlicerFile.MaximumSpeed,
                    RetractHeight2 = 0
                };

                if (CanUseCustomLift)
                {
                    layer.LiftSpeed = (float)_liftSpeed;
                    layer.RetractSpeed = (float)_retractSpeed;

                    /*if (SlicerFile.CanUseLayerLiftHeight2)
                    {
                        layer.LiftHeight = (float)_slowLiftHeight;
                    }*/

                    if (SlicerFile.CanUseLayerLiftSpeed2)
                    {
                        layer.LiftSpeed2 = (float)_liftSpeed2;
                    }

                    if (SlicerFile.CanUseLayerRetractSpeed2)
                    {
                        layer.RetractSpeed2 = (float)_retractSpeed2;
                    }
                }

                layer.SetNoDelays();

                var layers = SlicerFile.ToList();
                for (int i = 0; i < virtualLayers.Count; i++)
                {
                    int insertIndex = (int)virtualLayers[i];
                    SlicerFile[insertIndex].LiftHeightTotal = SlicerFile.SupportsGCode ? 0 : 0.1f;

                    if (CanUseCustomLift)
                    {
                        SlicerFile[insertIndex].LiftSpeed = (float)_liftSpeed;
                        SlicerFile[insertIndex].RetractSpeed = (float)_retractSpeed;

                        if (SlicerFile.CanUseLayerLiftSpeed2)
                        {
                            SlicerFile[insertIndex].LiftSpeed2 = (float)_retractSpeed2;
                        }

                        if (SlicerFile.CanUseLayerRetractSpeed2)
                        {
                            SlicerFile[insertIndex].RetractSpeed2 = (float)_retractSpeed2;
                        }
                    }

                    layers.Insert(insertIndex + i, layer.Clone());
                }
                SlicerFile.SuppressRebuildPropertiesWork(() => SlicerFile.LayerManager.Layers = layers.ToArray());
            }

            return !progress.Token.IsCancellationRequested;
        }

        #endregion
    }
}
