/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;
using UVtools.Core.Operations;
using Stream = System.IO.Stream;

namespace UVtools.Core
{
    /// <summary>
    /// Represent a Layer
    /// </summary>
    public class Layer : BindableBase, IEquatable<Layer>, IEquatable<uint>
    {
        #region Constants
        public const byte HeightPrecision = 3;
        public const decimal HeightPrecisionIncrement = 0.001M;
        public const decimal MinimumHeight = 0.01M;
        public const decimal MaximumHeight = 0.2M;
        #endregion

        #region Members

        public object Mutex = new();
        private byte[] _compressedBytes;
        private uint _nonZeroPixelCount;
        private Rectangle _boundingRectangle = Rectangle.Empty;
        private bool _isModified;
        private uint _index;
        private float _positionZ;
        private float _lightOffDelay;
        private float _waitTimeBeforeCure;
        private float _exposureTime;
        private float _waitTimeAfterCure;
        private float _liftHeight = FileFormat.DefaultLiftHeight;
        private float _liftSpeed = FileFormat.DefaultLiftSpeed;
        private float _liftHeight2 = FileFormat.DefaultLiftHeight2;
        private float _liftSpeed2 = FileFormat.DefaultLiftSpeed2;
        private float _waitTimeAfterLift;
        private float _retractSpeed = FileFormat.DefaultRetractSpeed;
        private float _retractHeight2 = FileFormat.DefaultRetractHeight2;
        private float _retractSpeed2 = FileFormat.DefaultRetractSpeed2;
        private byte _lightPwm = FileFormat.DefaultLightPWM;
        private float _materialMilliliters;
        #endregion

        #region Properties

        /// <summary>
        /// Gets the parent layer manager
        /// </summary>
        public LayerManager ParentLayerManager { get; set; }

        public FileFormat SlicerFile => ParentLayerManager?.SlicerFile;

        /// <summary>
        /// Gets the number of non zero pixels on this layer image
        /// </summary>
        public uint NonZeroPixelCount
        {
            get => _nonZeroPixelCount;
            internal set
            {
                if(!RaiseAndSetIfChanged(ref _nonZeroPixelCount, value)) return;
                RaisePropertyChanged(nameof(ExposureMillimeters));
                MaterialMilliliters = 0; // Recalculate
            }
        }

        /// <summary>
        /// Gets if this layer is empty/all black pixels
        /// </summary>
        public bool IsEmpty => _nonZeroPixelCount == 0;

        public float ExposureMillimeters
        {
            get
            {
                if (SlicerFile is null) return 0;
                return (float) Math.Round(SlicerFile.PixelSizeMax * _nonZeroPixelCount, 2);
            }
        }

        /// <summary>
        /// Gets the bounding rectangle for the image area
        /// </summary>
        public Rectangle BoundingRectangle
        {
            get => _boundingRectangle;
            internal set
            {
                RaiseAndSetIfChanged(ref _boundingRectangle, value);
                RaisePropertyChanged(nameof(BoundingRectangleMillimeters));
            }
        }

        /// <summary>
        /// Gets the bounding rectangle for the image area in millimeters
        /// </summary>
        public RectangleF BoundingRectangleMillimeters
        {
            get
            {
                if (SlicerFile is null) return RectangleF.Empty;
                var pixelSize = SlicerFile.PixelSize;
                return new RectangleF(
                    (float) Math.Round(_boundingRectangle.X * pixelSize.Width, 2),
                    (float)Math.Round(_boundingRectangle.Y * pixelSize.Height, 2),
                    (float)Math.Round(_boundingRectangle.Width * pixelSize.Width, 2),
                    (float)Math.Round(_boundingRectangle.Height * pixelSize.Height, 2));
            }
        }

        public bool IsBottomLayer => (uint)(PositionZ / SlicerFile.LayerHeight) <= ParentLayerManager.SlicerFile.BottomLayerCount;
        public bool IsNormalLayer => !IsBottomLayer;

        /// <summary>
        /// Gets the layer index
        /// </summary>
        public uint Index
        {
            get => _index;
            set => RaiseAndSetIfChanged(ref _index, value);
        }

        /// <summary>
        /// Gets or sets the layer position on Z in mm
        /// </summary>
        public float PositionZ
        {
            get => _positionZ;
            set
            {
                if (!RaiseAndSetIfChanged(ref _positionZ, RoundHeight(value))) return;
                RaisePropertyChanged(nameof(LayerHeight));
            }
        }

        /// <summary>
        /// Gets or sets the wait time in seconds before cure the layer
        /// AKA: Light-off delay
        /// Chitubox: Rest time after retract
        /// Lychee: Wait before print
        /// </summary>
        public float WaitTimeBeforeCure
        {
            get => _waitTimeBeforeCure;
            set
            {
                if (!RaiseAndSetIfChanged(ref _waitTimeBeforeCure, (float)Math.Round(value, 2))) return;
                SlicerFile?.UpdatePrintTimeQueued();
            }
        }

        /// <summary>
        /// Gets or sets the exposure time in seconds
        /// </summary>
        public float ExposureTime
        {
            get => _exposureTime;
            set
            {
                if (value <= 0) value = SlicerFile.GetInitialLayerValueOrNormal(Index, SlicerFile.BottomExposureTime, SlicerFile.ExposureTime);
                if(!RaiseAndSetIfChanged(ref _exposureTime, (float)Math.Round(value, 2))) return;
                SlicerFile?.UpdatePrintTimeQueued();
            }
        }

        /// <summary>
        /// Gets or sets the wait time in seconds after cure the layer
        /// Chitubox: Rest time before lift
        /// Lychee: Wait after print
        /// </summary>
        public float WaitTimeAfterCure
        {
            get => _waitTimeAfterCure;
            set
            {
                if (!RaiseAndSetIfChanged(ref _waitTimeAfterCure, (float)Math.Round(value, 2))) return;
                SlicerFile?.UpdatePrintTimeQueued();
            }
        }

        /// <summary>
        /// Gets or sets the layer off time in seconds
        /// </summary>
        public float LightOffDelay
        {
            get => _lightOffDelay;
            set
            {
                if (value < 0) value = SlicerFile.GetInitialLayerValueOrNormal(Index, SlicerFile.BottomLightOffDelay, SlicerFile.LightOffDelay);
                if(!RaiseAndSetIfChanged(ref _lightOffDelay, (float)Math.Round(value, 2))) return;
                SlicerFile?.UpdatePrintTimeQueued();
            }
        }

        /// <summary>
        /// Gets: Total lift height (lift1 + lift2)
        /// Sets: Lift1 with value and lift2 with 0
        /// </summary>
        public float LiftHeightTotal
        {
            get => (float)Math.Round(_liftHeight + _liftHeight2, 2);
            set
            {
                LiftHeight = (float)Math.Round(value, 2);
                LiftHeight2 = 0;
            }
        }

        /// <summary>
        /// Gets or sets the lift height in mm
        /// </summary>
        public float LiftHeight
        {
            get => _liftHeight;
            set
            {
                if (value < 0) value = SlicerFile.GetInitialLayerValueOrNormal(Index, SlicerFile.BottomLiftHeight, SlicerFile.LiftHeight);
                if(!RaiseAndSetIfChanged(ref _liftHeight, (float)Math.Round(value, 2))) return;
                RaisePropertyChanged(nameof(LiftHeightTotal));
                RetractHeight2 = _retractHeight2; // Sanitize
                SlicerFile?.UpdatePrintTimeQueued();
            }
        }

        /// <summary>
        /// Gets or sets the speed in mm/min
        /// </summary>
        public float LiftSpeed
        {
            get => _liftSpeed;
            set
            {
                if (value <= 0) value = SlicerFile.GetInitialLayerValueOrNormal(Index, SlicerFile.BottomLiftSpeed, SlicerFile.LiftSpeed);
                if(!RaiseAndSetIfChanged(ref _liftSpeed, (float)Math.Round(value, 2))) return;
                SlicerFile?.UpdatePrintTimeQueued();
            }
        }

        /// <summary>
        /// Gets or sets the lift height in mm
        /// </summary>
        public float LiftHeight2
        {
            get => _liftHeight2;
            set
            {
                if (value < 0) value = SlicerFile.GetInitialLayerValueOrNormal(Index, SlicerFile.BottomLiftHeight2, SlicerFile.LiftHeight2);
                if (!RaiseAndSetIfChanged(ref _liftHeight2, (float)Math.Round(value, 2))) return;
                RaisePropertyChanged(nameof(LiftHeightTotal));
                RetractHeight2 = _retractHeight2; // Sanitize
                SlicerFile?.UpdatePrintTimeQueued();
            }
        }

        /// <summary>
        /// Gets or sets the speed in mm/min
        /// </summary>
        public float LiftSpeed2
        {
            get => _liftSpeed2;
            set
            {
                if (value <= 0) value = SlicerFile.GetInitialLayerValueOrNormal(Index, SlicerFile.BottomLiftSpeed2, SlicerFile.LiftSpeed2);
                if (!RaiseAndSetIfChanged(ref _liftSpeed2, (float)Math.Round(value, 2))) return;
                SlicerFile?.UpdatePrintTimeQueued();
            }
        }

        public float WaitTimeAfterLift
        {
            get => _waitTimeAfterLift;
            set
            {
                if (!RaiseAndSetIfChanged(ref _waitTimeAfterLift, (float)Math.Round(value, 2))) return;
                SlicerFile?.UpdatePrintTimeQueued();
            }
        }

        /// <summary>
        /// Gets: Total retract height (retract1 + retract2) alias of <see cref="LiftHeightTotal"/>
        /// </summary>
        public float RetractHeightTotal => LiftHeightTotal;

        /// <summary>
        /// Gets the retract height in mm
        /// </summary>
        public float RetractHeight => (float)Math.Round(LiftHeightTotal - _retractHeight2, 2);

        /// <summary>
        /// Gets the speed in mm/min for the retracts
        /// </summary>
        public float RetractSpeed
        {
            get => _retractSpeed;
            set
            {
                if (value <= 0) value = SlicerFile.RetractSpeed;
                if(!RaiseAndSetIfChanged(ref _retractSpeed, (float)Math.Round(value, 2))) return;
                SlicerFile?.UpdatePrintTimeQueued();
            }
        }

        /// <summary>
        /// Gets or sets the second retract height in mm
        /// </summary>
        public virtual float RetractHeight2
        {
            get => _retractHeight2;
            set
            {
                value = Math.Clamp((float)Math.Round(value, 2), 0, RetractHeightTotal);
                RaiseAndSetIfChanged(ref _retractHeight2, value);
                RaisePropertyChanged(nameof(RetractHeight));
                RaisePropertyChanged(nameof(RetractHeightTotal));
            }
        }

        /// <summary>
        /// Gets the speed in mm/min for the retracts
        /// </summary>
        public virtual float RetractSpeed2
        {
            get => _retractSpeed2;
            set => RaiseAndSetIfChanged(ref _retractSpeed2, (float)Math.Round(value, 2));
        }

        /// <summary>
        /// Gets or sets the pwm value from 0 to 255
        /// </summary>
        public byte LightPWM
        {
            get => _lightPwm;
            set
            {
                //if (value == 0) value = SlicerFile.GetInitialLayerValueOrNormal(Index, SlicerFile.BottomLightPWM, SlicerFile.LightPWM);
                //if (value == 0) value = FileFormat.DefaultLightPWM;
                RaiseAndSetIfChanged(ref _lightPwm, value);
            }
        }

        /// <summary>
        /// Gets if this layer can be exposed to UV light
        /// </summary>
        public bool CanExpose => _exposureTime > 0 && _lightPwm > 0;

        /// <summary>
        /// Gets the layer height in millimeters of this layer
        /// </summary>
        public float LayerHeight
        {
            get
            {
                if (_index == 0) return _positionZ;
                Layer previousLayer = this;

                while ((previousLayer = previousLayer.PreviousLayer()) is not null) // This cycle returns the correct layer height if two or more layers have the same position z
                {
                    var layerHeight = RoundHeight(_positionZ - previousLayer.PositionZ);
                    //Debug.WriteLine($"Layer {_index}-{previousLayer.Index}: {_positionZ} - {previousLayer.PositionZ}: {layerHeight}");
                    if (layerHeight == 0f) continue;
                    if (layerHeight < 0f) break;
                    return layerHeight;
                }

                return ParentLayerManager.SlicerFile.LayerHeight;
            }
        }

        /// <summary>
        /// Gets the computed material milliliters spent on this layer
        /// </summary>
        public float MaterialMilliliters
        {
            get => _materialMilliliters;
            private set
            {
                if (ParentLayerManager?.SlicerFile is null) return;
                //var globalMilliliters = SlicerFile.MaterialMilliliters - _materialMilliliters;
                if (value <= 0)
                {
                    value = (float) Math.Round(ParentLayerManager.SlicerFile.PixelArea * ParentLayerManager.SlicerFile.LayerHeight * NonZeroPixelCount / 1000f, 4);
                }

                if(!RaiseAndSetIfChanged(ref _materialMilliliters, value)) return;
                SlicerFile.MaterialMilliliters = 0; // Recalculate global
                RaisePropertyChanged(nameof(MaterialMillilitersPercent));
                //ParentLayerManager.MaterialMillilitersTimer.Stop();
                //if(!ParentLayerManager.MaterialMillilitersTimer.Enabled)
                //    ParentLayerManager.MaterialMillilitersTimer.Start();
            }
        }

        /// <summary>
        /// Gets the computed material milliliters percentage compared to the rest of the model
        /// </summary>
        public float MaterialMillilitersPercent => _materialMilliliters * 100 / SlicerFile.MaterialMilliliters;

        /// <summary>
        /// Gets or sets layer image compressed data
        /// </summary>
        public byte[] CompressedBytes
        {
            get => _compressedBytes;
            set
            {
                _compressedBytes = value;
                IsModified = true;
                if (ParentLayerManager is not null)
                    ParentLayerManager.BoundingRectangle = Rectangle.Empty;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HaveImage));
            }
        }

        /// <summary>
        /// True if this layer have an valid initialized image, otherwise false
        /// </summary>
        public bool HaveImage => _compressedBytes is not null && _compressedBytes.Length > 0;
        
        /// <summary>
        /// Gets or sets a new image instance
        /// </summary>
        public Mat LayerMat
        {
            get
            {
                Mat mat = new();
                CvInvoke.Imdecode(_compressedBytes, ImreadModes.Grayscale, mat);
                return mat;
            }
            set
            {
                CompressedBytes = value.GetPngByes();
                GetBoundingRectangle(value, true);
                RaisePropertyChanged();
            }
        }

        public Mat LayerMatBoundingRectangle => new(LayerMat, BoundingRectangle);

        /// <summary>
        /// Gets a new Brg image instance
        /// </summary>
        public Mat BrgMat
        {
            get
            {
                var mat = LayerMat;
                CvInvoke.CvtColor(mat, mat, ColorConversion.Gray2Bgr);
                return mat;
            }
        }

        /// <summary>
        /// Gets a computed layer filename, padding zeros are equal to layer count digits
        /// </summary>
        public string Filename => FormatFileName("layer");

        /// <summary>
        /// Gets if layer image has been modified
        /// </summary>
        public bool IsModified
        {
            get => _isModified;
            set => RaiseAndSetIfChanged(ref _isModified, value);
        }

        /// <summary>
        /// Gets if this layer have same value parameters as global settings
        /// </summary>
        public bool HaveGlobalParameters
        {
            get
            {
                if (SlicerFile is null) return false; // Cant verify
                if (IsBottomLayer)
                {
                    if (
                        LightOffDelay != SlicerFile.BottomLightOffDelay ||
                        WaitTimeBeforeCure != SlicerFile.BottomWaitTimeBeforeCure ||
                        ExposureTime != SlicerFile.BottomExposureTime ||
                        WaitTimeBeforeCure != SlicerFile.BottomWaitTimeBeforeCure ||
                        LiftHeight != SlicerFile.BottomLiftHeight ||
                        LiftSpeed != SlicerFile.BottomLiftSpeed ||
                        LiftHeight2 != SlicerFile.BottomLiftHeight2 ||
                        LiftSpeed2 != SlicerFile.BottomLiftSpeed2 ||
                        WaitTimeAfterLift != SlicerFile.BottomWaitTimeAfterLift ||
                        RetractSpeed != SlicerFile.BottomRetractSpeed ||
                        RetractHeight2 != SlicerFile.BottomRetractHeight2 ||
                        RetractSpeed2 != SlicerFile.BottomRetractSpeed2 ||
                        LightPWM != SlicerFile.BottomLightPWM 
                        ) return false;
                }
                else
                {
                    if (
                        LightOffDelay != SlicerFile.LightOffDelay ||
                        WaitTimeBeforeCure != SlicerFile.WaitTimeBeforeCure ||
                        ExposureTime != SlicerFile.ExposureTime ||
                        WaitTimeAfterCure != SlicerFile.WaitTimeAfterCure ||
                        LiftHeight != SlicerFile.LiftHeight ||
                        LiftSpeed != SlicerFile.LiftSpeed ||
                        LiftHeight2 != SlicerFile.LiftHeight2 ||
                        LiftSpeed2 != SlicerFile.LiftSpeed2 ||
                        WaitTimeAfterLift != SlicerFile.WaitTimeAfterLift ||
                        RetractSpeed != SlicerFile.RetractSpeed ||
                        RetractHeight2 != SlicerFile.RetractHeight2 ||
                        RetractSpeed2 != SlicerFile.RetractSpeed2 ||
                        LightPWM != SlicerFile.LightPWM
                    ) return false;
                }

                return true;
            }
        }

        #endregion

        #region Constructor

        public Layer(uint index, LayerManager parentLayerManager)
        {
            ParentLayerManager = parentLayerManager;
            _index = index;

            if (parentLayerManager is null) return;
            _positionZ = SlicerFile.GetHeightFromLayer(index);
            _lightOffDelay = SlicerFile.GetInitialLayerValueOrNormal(index, SlicerFile.BottomLightOffDelay, SlicerFile.LightOffDelay);
            _waitTimeBeforeCure = SlicerFile.GetInitialLayerValueOrNormal(index, SlicerFile.BottomWaitTimeBeforeCure, SlicerFile.WaitTimeBeforeCure);
            _exposureTime = SlicerFile.GetInitialLayerValueOrNormal(index, SlicerFile.BottomExposureTime, SlicerFile.ExposureTime);
            _waitTimeAfterCure = SlicerFile.GetInitialLayerValueOrNormal(index, SlicerFile.BottomWaitTimeAfterCure, SlicerFile.WaitTimeAfterCure);
            _liftHeight = SlicerFile.GetInitialLayerValueOrNormal(index, SlicerFile.BottomLiftHeight, SlicerFile.LiftHeight);
            _liftSpeed = SlicerFile.GetInitialLayerValueOrNormal(index, SlicerFile.BottomLiftSpeed, SlicerFile.LiftSpeed);
            _liftHeight2 = SlicerFile.GetInitialLayerValueOrNormal(index, SlicerFile.BottomLiftHeight2, SlicerFile.LiftHeight2);
            _liftSpeed2 = SlicerFile.GetInitialLayerValueOrNormal(index, SlicerFile.BottomLiftSpeed2, SlicerFile.LiftSpeed2);
            _waitTimeAfterLift = SlicerFile.GetInitialLayerValueOrNormal(index, SlicerFile.BottomWaitTimeAfterLift, SlicerFile.WaitTimeAfterLift);
            _retractSpeed = SlicerFile.GetInitialLayerValueOrNormal(index, SlicerFile.BottomRetractSpeed, SlicerFile.RetractSpeed);
            _retractHeight2 = SlicerFile.GetInitialLayerValueOrNormal(index, SlicerFile.BottomRetractHeight2, SlicerFile.RetractHeight2);
            _retractSpeed2 = SlicerFile.GetInitialLayerValueOrNormal(index, SlicerFile.BottomRetractSpeed2, SlicerFile.RetractSpeed2);
            _lightPwm = SlicerFile.GetInitialLayerValueOrNormal(index, SlicerFile.BottomLightPWM, SlicerFile.LightPWM);
        }

        public Layer(uint index, byte[] compressedBytes, LayerManager parentLayerManager) : this(index, parentLayerManager)
        {
            CompressedBytes = compressedBytes;
            _isModified = false;
            /*if (compressedBytes.Length > 0)
            {
                GetBoundingRectangle();
            }*/
        }

        public Layer(uint index, byte[] compressedBytes, FileFormat slicerFile) : this(index, compressedBytes, slicerFile.LayerManager)
        {}

        public Layer(uint index, Mat layerMat, LayerManager parentLayerManager) : this(index, parentLayerManager)
        {
            LayerMat = layerMat;
            _isModified = false;
        }

        public Layer(uint index, Mat layerMat, FileFormat slicerFile) : this(index, layerMat, slicerFile.LayerManager) { }

        public Layer(uint index, Stream stream, LayerManager parentLayerManager) : this(index, stream.ToArray(), parentLayerManager) { }
        public Layer(uint index, Stream stream, FileFormat slicerFile) : this(index, stream.ToArray(), slicerFile.LayerManager) { }
        #endregion

        #region Equatables

        public static bool operator ==(Layer obj1, Layer obj2)
        {
            return obj1.Equals(obj2);
        }

        public static bool operator !=(Layer obj1, Layer obj2)
        {
            return !obj1.Equals(obj2);
        }

        public static bool operator >(Layer obj1, Layer obj2)
        {
            return obj1.Index > obj2.Index;
        }

        public static bool operator <(Layer obj1, Layer obj2)
        {
            return obj1.Index < obj2.Index;
        }

        public static bool operator >=(Layer obj1, Layer obj2)
        {
            return obj1.Index >= obj2.Index;
        }

        public static bool operator <=(Layer obj1, Layer obj2)
        {
            return obj1.Index <= obj2.Index;
        }

        public bool Equals(uint other)
        {
            return Index == other;
        }

        public bool Equals(Layer other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (_index != other._index) return false;
            if (_compressedBytes.Length != other._compressedBytes.Length) return false;
            return _compressedBytes.AsSpan().SequenceEqual(other._compressedBytes.AsSpan());
            //return Equals(_compressedBytes, other._compressedBytes);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Layer)obj);
        }

        public override int GetHashCode()
        {
            return (_compressedBytes != null ? _compressedBytes.GetHashCode() : 0);
        }

        private sealed class IndexRelationalComparer : IComparer<Layer>
        {
            public int Compare(Layer x, Layer y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (ReferenceEquals(null, y)) return 1;
                if (ReferenceEquals(null, x)) return -1;
                return x.Index.CompareTo(y.Index);
            }
        }

        public static IComparer<Layer> IndexComparer { get; } = new IndexRelationalComparer();
        #endregion

        #region Formaters

        public override string ToString()
        {
            return $"{nameof(Index)}: {Index}, " +
                   $"{nameof(Filename)}: {Filename}, " +
                   $"{nameof(NonZeroPixelCount)}: {NonZeroPixelCount}, " +
                   $"{nameof(BoundingRectangle)}: {BoundingRectangle}, " +
                   $"{nameof(IsBottomLayer)}: {IsBottomLayer}, " +
                   $"{nameof(IsNormalLayer)}: {IsNormalLayer}, " +
                   $"{nameof(LayerHeight)}: {LayerHeight}mm, " +
                   $"{nameof(PositionZ)}: {PositionZ}mm, " +
                   $"{nameof(LightOffDelay)}: {LightOffDelay}s, " +
                   $"{nameof(WaitTimeBeforeCure)}: {WaitTimeBeforeCure}s, " +
                   $"{nameof(ExposureTime)}: {ExposureTime}s, " +
                   $"{nameof(WaitTimeAfterCure)}: {WaitTimeAfterCure}s, " +
                   $"{nameof(LiftHeight)}: {LiftHeight}mm, " +
                   $"{nameof(LiftSpeed)}: {LiftSpeed}mm/mim, " +
                   $"{nameof(LiftHeight2)}: {LiftHeight2}mm, " +
                   $"{nameof(LiftSpeed2)}: {LiftSpeed2}mm/mim, " +
                   $"{nameof(WaitTimeAfterLift)}: {WaitTimeAfterLift}s, " +
                   $"{nameof(RetractHeight)}: {RetractHeight}mm, " +
                   $"{nameof(RetractSpeed)}: {RetractSpeed}mm/mim, " +
                   $"{nameof(RetractHeight2)}: {RetractHeight2}mm, " +
                   $"{nameof(RetractSpeed2)}: {RetractSpeed2}mm/mim, " +
                   $"{nameof(LightPWM)}: {LightPWM}, " +
                   $"{nameof(IsModified)}: {IsModified}, " +
                   $"{nameof(HaveGlobalParameters)}: {HaveGlobalParameters}";
        }
        #endregion

        #region Methods

        public float CalculateMotorMovementTime(float extraTime = 0)
        {
            return OperationCalculator.LightOffDelayC.CalculateSeconds(this, extraTime);
        }

        public float CalculateLightOffDelay(float extraTime = 0)
        {
            if (SlicerFile is null) return OperationCalculator.LightOffDelayC.CalculateSeconds(this, extraTime);
            return SlicerFile.SupportsGCode ? extraTime : OperationCalculator.LightOffDelayC.CalculateSeconds(this, extraTime);
        }

        public void SetLightOffDelay(float extraTime = 0)
        {
            LightOffDelay = CalculateLightOffDelay(extraTime);
        }

        /// <summary>
        /// Zero all 'wait times / delays' for this layer
        /// </summary>
        public void SetNoDelays()
        {
            LightOffDelay = 0;
            WaitTimeBeforeCure = 0;
            WaitTimeAfterCure = 0;
            WaitTimeAfterLift = 0;
        }

        public string FormatFileName(string prepend, byte padDigits, bool layerIndexZeroStarted = true, string appendExt = ".png")
        {
            var index = Index;
            if (!layerIndexZeroStarted)
            {
                index++;
            }
            return $"{prepend}{index.ToString().PadLeft(padDigits, '0')}{appendExt}";
        }

        public string FormatFileName(string prepend = "", bool layerIndexZeroStarted = true, string appendExt = ".png")
            => FormatFileName(prepend, ParentLayerManager.LayerDigits, layerIndexZeroStarted, appendExt);

        public string FormatFileName(byte padDigits, bool layerIndexZeroStarted = true, string appendExt = ".png")
            => FormatFileName(string.Empty, padDigits, layerIndexZeroStarted, appendExt);


        public Rectangle GetBoundingRectangle(Mat mat = null, bool reCalculate = false)
        {
            if (_nonZeroPixelCount > 0 && !reCalculate)
            {
                return BoundingRectangle;
            }
            bool needDispose = false;
            if (mat is null)
            {
                if (_compressedBytes is null || _compressedBytes.Length == 0) return Rectangle.Empty;
                mat = LayerMat;
                needDispose = true;
            }

            using var nonZeroMat = new Mat();
            CvInvoke.FindNonZero(mat, nonZeroMat);
            NonZeroPixelCount = (uint)nonZeroMat.Height;
            BoundingRectangle = NonZeroPixelCount > 0 ? CvInvoke.BoundingRectangle(nonZeroMat) : Rectangle.Empty;


            if (needDispose) mat.Dispose();

            return BoundingRectangle;
        }

        public Layer PreviousLayer()
        {
            if (ParentLayerManager is null || _index == 0)
                return null;

            return ParentLayerManager[_index - 1];
        }

        public Layer NextLayer()
        {
            if (ParentLayerManager is null || _index >= ParentLayerManager.LayerCount - 1)
                return null;

            return ParentLayerManager[_index + 1];
        }

        public bool SetValueFromPrintParameterModifier(FileFormat.PrintParameterModifier modifier, decimal value)
        {
            if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.LightOffDelay))
            {
                LightOffDelay = (float)value;
                return true;
            }

            if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.WaitTimeBeforeCure))
            {
                WaitTimeBeforeCure = (float)value;
                return true;
            }

            if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.ExposureTime))
            {
                ExposureTime = (float)value;
                return true;
            }

            if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.WaitTimeAfterCure))
            {
                WaitTimeAfterCure = (float)value;
                return true;
            }

            if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.LiftHeight))
            {
                LiftHeight = (float)value;
                return true;
            }

            if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.LiftSpeed))
            {
                LiftSpeed = (float)value;
                return true;
            }

            if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.LiftHeight2))
            {
                LiftHeight2 = (float)value;
                return true;
            }

            if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.LiftSpeed2))
            {
                LiftSpeed2 = (float)value;
                return true;
            }

            if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.WaitTimeAfterLift))
            {
                WaitTimeAfterLift = (float)value;
                return true;
            }

            if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.RetractSpeed))
            {
                RetractSpeed = (float)value;
                return true;
            }

            if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.RetractHeight2))
            {
                RetractHeight2 = (float)value;
                return true;
            }

            if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.RetractSpeed2))
            {
                RetractSpeed2 = (float)value;
                return true;
            }

            if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.LightPWM))
            {
                LightPWM = (byte)value;
                return true;
            }

            return false;
        }

        public byte SetValuesFromPrintParametersModifiers(FileFormat.PrintParameterModifier[] modifiers)
        {
            if (modifiers is null) return 0;
            byte changed = 0;
            foreach (var modifier in modifiers)
            {
                if (!modifier.HasChanged) continue;
                SetValueFromPrintParameterModifier(modifier, modifier.NewValue);
                changed++;
            }

            return changed;
        }

        /*
        /// <summary>
        /// Gets all islands start pixel location for this layer
        /// https://www.geeksforgeeks.org/find-number-of-islands/
        /// </summary>
        /// <returns><see cref="List{T}"/> holding all islands coordinates</returns>
        public List<LayerIssue> GetIssues(uint requiredPixelsToSupportIsland = 5)
        {
            if (requiredPixelsToSupportIsland == 0)
                requiredPixelsToSupportIsland = 1;

            // These arrays are used to 
            // get row and column numbers 
            // of 8 neighbors of a given cell 
            List<LayerIssue> result = new();
            List<Point> pixels = new();



            var mat = LayerMat;
            var bytes = mat.GetDataSpan<byte>();



            var previousLayerImage = PreviousLayer()?.LayerMat;
            var previousBytes = previousLayerImage?.GetBytes();

            // Make a bool array to
            // mark visited cells. 
            // Initially all cells 
            // are unvisited 
            bool[,] visited = new bool[mat.Width, mat.Height];

            // Initialize count as 0 and 
            // traverse through the all 
            // cells of given matrix 
            //uint count = 0;

            // Island checker
            sbyte[] rowNbr = { -1, -1, -1, 0, 0, 1, 1, 1 };
            sbyte[] colNbr = { -1, 0, 1, -1, 1, -1, 0, 1 };
            const uint minPixel = 10;
            const uint minPixelForSupportIsland = 200;
            int pixelIndex;
            uint islandSupportingPixels;
            if (Index > 0)
            {
                for (int y = 0; y < mat.Height; y++)
                {
                    for (int x = 0; x < mat.Width; x++)
                    {
                        pixelIndex = y * mat.Width + x;

                        if (bytes[pixelIndex] > minPixel && !visited[x, y])
                        {
                            // If a cell with value 1 is not 
                            // visited yet, then new island 
                            // found, Visit all cells in this 
                            // island and increment island count 
                            pixels.Clear();
                            pixels.Add(new Point(x, y));
                            islandSupportingPixels = previousBytes[pixelIndex] >= minPixelForSupportIsland ? 1u : 0;

                            int minX = x;
                            int maxX = x;
                            int minY = y;
                            int maxY = y;

                            int x2;
                            int y2;


                            Queue<Point> queue = new();
                            queue.Enqueue(new Point(x, y));
                            // Mark this cell as visited 
                            visited[x, y] = true;

                            while (queue.Count > 0)
                            {
                                var point = queue.Dequeue();
                                y2 = point.Y;
                                x2 = point.X;
                                for (byte k = 0; k < 8; k++)
                                {
                                    //if (isSafe(y2 + rowNbr[k], x2 + colNbr[k]))
                                    var tempy2 = y2 + rowNbr[k];
                                    var tempx2 = x2 + colNbr[k];
                                    pixelIndex = tempy2 * mat.Width + tempx2;
                                    if (tempy2 >= 0 &&
                                        tempy2 < mat.Height &&
                                        tempx2 >= 0 && tempx2 < mat.Width &&
                                        bytes[pixelIndex] >= minPixel &&
                                        !visited[tempx2, tempy2])
                                    {
                                        visited[tempx2, tempy2] = true;
                                        point = new Point(tempx2, tempy2);
                                        pixels.Add(point);
                                        queue.Enqueue(point);

                                        minX = Math.Min(minX, tempx2);
                                        maxX = Math.Max(maxX, tempx2);
                                        minY = Math.Min(minY, tempy2);
                                        maxY = Math.Max(maxY, tempy2);

                                        islandSupportingPixels += previousBytes[pixelIndex] >= minPixelForSupportIsland ? 1u : 0;
                                    }
                                }
                            }
                            //count++;

                            if (islandSupportingPixels >= requiredPixelsToSupportIsland)
                                continue; // Not a island, bounding is strong
                            if (islandSupportingPixels > 0 && pixels.Count < requiredPixelsToSupportIsland &&
                                islandSupportingPixels >= Math.Max(1, pixels.Count / 2)) continue; // Not a island
                            result.Add(new LayerIssue(this, LayerIssue.IssueType.Island, pixels.ToArray(), new Rectangle(minX, minY, maxX - minX, maxY - minY)));
                        }
                    }
                }
            }

            pixels.Clear();

            // TouchingBounds Checker
            for (int x = 0; x < mat.Width; x++) // Check Top and Bottom bounds
            {
                if (bytes[x] >= 200) // Top
                {
                    pixels.Add(new Point(x, 0));
                }

                if (bytes[mat.Width * mat.Height - mat.Width + x] >= 200) // Bottom
                {
                    pixels.Add(new Point(x, mat.Height - 1));
                }
            }

            for (int y = 0; y < mat.Height; y++) // Check Left and Right bounds
            {
                if (bytes[y * mat.Width] >= 200) // Left
                {
                    pixels.Add(new Point(0, y));
                }

                if (bytes[y * mat.Width + mat.Width - 1] >= 200) // Right
                {
                    pixels.Add(new Point(mat.Width - 1, y));
                }
            }

            if (pixels.Count > 0)
            {
                result.Add(new LayerIssue(this, LayerIssue.IssueType.TouchingBound, pixels.ToArray()));
            }

            pixels.Clear();

            return result;
        }*/

        public void CopyImageTo(Layer layer)
        {
            if (!HaveImage) return;
            layer.CompressedBytes = _compressedBytes.ToArray();
            layer.BoundingRectangle = _boundingRectangle;
            layer.NonZeroPixelCount = _nonZeroPixelCount;

        }

        public Layer Clone()
        {
            //var layer = (Layer)MemberwiseClone();
            //layer.CompressedBytes = _compressedBytes.ToArray();
            //Debug.WriteLine(ReferenceEquals(_compressedBytes, layer.CompressedBytes));
            //return layer;
            return new (_index, CompressedBytes.ToArray(), ParentLayerManager)
            {
                PositionZ = _positionZ,
                LightOffDelay = _lightOffDelay,
                WaitTimeBeforeCure = _waitTimeBeforeCure,
                ExposureTime = _exposureTime,
                WaitTimeAfterCure = _waitTimeAfterCure,
                LiftHeight = _liftHeight,
                LiftSpeed = _liftSpeed,
                LiftHeight2 = _liftHeight2,
                LiftSpeed2 = _liftSpeed2,
                WaitTimeAfterLift = _waitTimeAfterLift,
                RetractSpeed = _retractSpeed,
                RetractHeight2 = _retractHeight2,
                RetractSpeed2 = _retractSpeed2,
                LightPWM = _lightPwm,
                BoundingRectangle = _boundingRectangle,
                NonZeroPixelCount = _nonZeroPixelCount,
                IsModified = _isModified,
                MaterialMilliliters = _materialMilliliters
            };
        }
        #endregion

        #region Static Methods

        public static float RoundHeight(float height) => (float) Math.Round(height, HeightPrecision);
        public static double RoundHeight(double height) => Math.Round(height, HeightPrecision);
        public static decimal RoundHeight(decimal height) => Math.Round(height, HeightPrecision);

        public static string ShowHeight(float height) => string.Format($"{{0:F{HeightPrecision}}}", height);
        public static string ShowHeight(double height) => string.Format($"{{0:F{HeightPrecision}}}", height);
        public static string ShowHeight(decimal height) => string.Format($"{{0:F{HeightPrecision}}}", height);

        public static Layer[] CloneLayers(Layer[] layers)
        {
            var clonedLayers = new Layer[layers.Length];
            for (uint layerIndex = 0; layerIndex < layers.Length; layerIndex++)
            {
                clonedLayers[layerIndex] = layers[layerIndex]?.Clone();
            }
            return clonedLayers;
        }

        #endregion
    }
}
