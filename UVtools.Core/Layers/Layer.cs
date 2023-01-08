/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using K4os.Compression.LZ4;
using UVtools.Core.EmguCV;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;
using UVtools.Core.Operations;

namespace UVtools.Core.Layers;

#region Enums

public enum LayerCompressionCodec : byte
{
    [Description("PNG: Compression=High Speed=Slow (Use with low RAM)")]
    Png,
    [Description("GZip: Compression=Medium Speed=Medium (Optimal)")]
    GZip,
    [Description("Deflate: Compression=Medium Speed=Medium (Optimal)")]
    Deflate,
    [Description("LZ4: Compression=Low Speed=Fast (Use with high RAM)")]
    Lz4,
    //[Description("None: Compression=None Speed=Fastest (Your soul belongs to RAM)")]
    //None
}

#endregion

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
    private LayerCompressionCodec _compressionCodec;
    private byte[]? _compressedBytes;
    private uint _nonZeroPixelCount;
    private Rectangle _boundingRectangle = Rectangle.Empty;
    private bool _isModified;
    private uint _index;
    private uint _resolutionX;
    private uint _resolutionY;
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
    private byte _lightPWM = FileFormat.DefaultLightPWM;
    private float _materialMilliliters;

    private EmguContours? _contours;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the parent SlicerFile
    /// </summary>
    public FileFormat SlicerFile { get; set; }

    /// <summary>
    /// Image resolution X
    /// </summary>
    public uint ResolutionX
    {
        get => _resolutionX;
        set => RaiseAndSetIfChanged(ref _resolutionX, value);
    }

    /// <summary>
    /// Image resolution Y
    /// </summary>
    public uint ResolutionY
    {
        get => _resolutionY;
        set => RaiseAndSetIfChanged(ref _resolutionY, value);
    }

    /// <summary>
    /// Image resolution
    /// </summary>
    public Size Resolution
    {
        get => new((int)ResolutionX, (int)ResolutionY);
        set
        {
            ResolutionX = (uint)value.Width;
            ResolutionY = (uint)value.Height;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets the number of non zero pixels on this layer image
    /// </summary>
    public uint NonZeroPixelCount
    {
        get => _nonZeroPixelCount;
        internal set
        {
            if (!RaiseAndSetIfChanged(ref _nonZeroPixelCount, value)) return;
            RaisePropertyChanged(nameof(NonZeroPixelRatio));
            RaisePropertyChanged(nameof(NonZeroPixelPercentage));
            RaisePropertyChanged(nameof(Area));
            RaisePropertyChanged(nameof(Volume));
            MaterialMilliliters = -1; // Recalculate
        }
    }

    /// <summary>
    /// Gets the ratio between non zero pixels and display number of pixels
    /// </summary>
    public double NonZeroPixelRatio
    {
        get
        {
            var displayPixelCount = SlicerFile.DisplayPixelCount;
            if (displayPixelCount == 0) return double.NaN;
            return (double)_nonZeroPixelCount / displayPixelCount;
        }
    }

    /// <summary>
    /// Gets the percentage of non zero pixels relative to the display number of pixels
    /// </summary>
    public double NonZeroPixelPercentage
    {
        get
        {
            var pixelRatio = NonZeroPixelRatio;
            if (double.IsNaN(pixelRatio)) return double.NaN;
            return pixelRatio * 100.0;
        }
    }

    /// <summary>
    /// Gets if this layer is empty/all black pixels
    /// </summary>
    public bool IsEmpty => _nonZeroPixelCount == 0;

    /// <summary>
    /// Gets the layer area (XY)  in mm^2
    /// Pixel size * number of pixels
    /// </summary>
    public float Area => GetArea(3);

    /// <summary>
    /// Gets the layer volume (XYZ) in mm^3
    /// Pixel size * number of pixels * layer height
    /// </summary>
    public float Volume => GetVolume(3);

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
            var pixelSize = SlicerFile.PixelSize;
            return new RectangleF(
                (float) Math.Round(_boundingRectangle.X * pixelSize.Width, 2),
                (float)Math.Round(_boundingRectangle.Y * pixelSize.Height, 2),
                (float)Math.Round(_boundingRectangle.Width * pixelSize.Width, 2),
                (float)Math.Round(_boundingRectangle.Height * pixelSize.Height, 2));
        }
    }

    /// <summary>
    /// Gets the first pixel index on this layer
    /// </summary>
    public uint FirstPixelIndex => (uint)(BoundingRectangle.Y * ResolutionX + BoundingRectangle.X);

    /// <summary>
    /// Gets the last pixel index on this layer
    /// </summary>
    public uint LastPixelIndex => (uint)(BoundingRectangle.Bottom * ResolutionX + BoundingRectangle.Right);

    /// <summary>
    /// Gets the first pixel <see cref="Point"/> on this layer
    /// </summary>
    public Point FirstPixelPosition => BoundingRectangle.Location;

    /// <summary>
    /// Gets the last pixel <see cref="Point"/> on this layer
    /// </summary>
    public Point LastPixelPosition => new (BoundingRectangle.Right, BoundingRectangle.Bottom);

    /// <summary>
    /// Gets if is the first layer
    /// </summary>
    public bool IsFirstLayer => _index == 0;

    /// <summary>
    /// Gets if layer is between first and last layer, aka, not first nor last layer
    /// </summary>
    public bool IsIntermediateLayer => !IsFirstLayer && !IsLastLayer;

    /// <summary>
    /// Gets if is the last layer
    /// </summary>
    public bool IsLastLayer => _index >= SlicerFile.LastLayerIndex;

    /// <summary>
    /// Gets if is in the bottom layer group
    /// </summary>
    public bool IsBottomLayer
    {
        get
        {
            var bottomLayers = SlicerFile.BottomLayerCount;
            if (_index < bottomLayers) return true;

            // For same positioned layers
            /*uint layerCount = 1;
            bool nullFallback = false;
            for (uint layerIndex = 1; layerIndex < _index && layerCount < bottomLayers; layerIndex++)
            {
                if (SlicerFile[layerIndex] is null)
                {
                    nullFallback = true;
                    break;
                }
                if (SlicerFile[layerIndex].RelativePositionZ != 0) layerCount++;
            }

            if (nullFallback) return PositionZ / SlicerFile.LayerHeight <= bottomLayers;
            return layerCount <= bottomLayers;*/
            return PositionZ / SlicerFile.LayerHeight <= bottomLayers;
        }
    }

    /// <summary>
    /// Gets if is in the normal layer group
    /// </summary>
    public bool IsNormalLayer => !IsBottomLayer;

    /// <summary>
    /// Gets if this layer is also an transition layer
    /// </summary>
    public bool IsTransitionLayer => SlicerFile.TransitionLayerCount <= Number;

    /// <summary>
    /// Gets the previous layer, returns null if no previous layer
    /// </summary>
    public Layer? PreviousLayer
    {
        get
        {
            if (IsFirstLayer || _index > SlicerFile.Count) return null;
            return SlicerFile[_index - 1];
        }
    }

    /// <summary>
    /// Gets the previous layer if available, otherwise return the calling layer itself
    /// </summary>
    public Layer PreviousLayerOrThis
    {
        get
        {
            if (IsFirstLayer || _index > SlicerFile.Count) return this;
            return SlicerFile[_index - 1];
        }
    }

    /// <summary>
    /// Gets the previous layer with a different height from the current, returns null if no previous layer
    /// </summary>
    public Layer? PreviousHeightLayer
    {
        get
        {
            if (IsFirstLayer || _index > SlicerFile.Count) return null;
            for (int i = (int)_index - 1; i >= 0; i--)
            {
                if (SlicerFile[i].PositionZ < _positionZ) return SlicerFile[i];
            }

            return null;
        }
    }

    /// <summary>
    /// Gets the previous layer matching at least <param name="numberOfPixels"/> pixels, returns null if no previous layer
    /// </summary>
    public Layer? GetPreviousLayerWithAtLeastPixelCountOf(uint numberOfPixels)
    {
        if (IsFirstLayer || _index > SlicerFile.Count) return null;
        for (int i = (int)_index - 1; i >= 0; i--)
        {
            if (SlicerFile[i].NonZeroPixelCount >= numberOfPixels) return SlicerFile[i];
        }

        return null;
    }

    /// <summary>
    /// Gets the next layer, returns null if no next layer
    /// </summary>
    public Layer? NextLayer
    {
        get
        {
            if (_index >= SlicerFile.LastLayerIndex) return null;
            return SlicerFile[_index + 1];
        }
    }

    /// <summary>
    /// Gets the next layer if available, otherwise return the calling layer itself
    /// </summary>
    public Layer NextLayerOrThis
    {
        get
        {
            if (_index >= SlicerFile.LastLayerIndex) return this;
            return SlicerFile[_index + 1];
        }
    }

    /// <summary>
    /// Gets the next layer with a different height from the current, returns null if no next layer
    /// </summary>
    public Layer? NextHeightLayer
    {
        get
        {
            if (_index >= SlicerFile.LastLayerIndex) return null;
            for (var i = _index + 1; i < SlicerFile.LayerCount; i++)
            {
                if (SlicerFile[i].PositionZ > _positionZ) return SlicerFile[i];
            }

            return null;
        }
    }

    /// <summary>
    /// Gets the next layer matching at least <param name="numberOfPixels"/> pixels, returns null if no next layer
    /// </summary>
    public Layer? GetNextLayerWithAtLeastPixelCountOf(uint numberOfPixels)
    {
        if (_index >= SlicerFile.LastLayerIndex) return null;
        for (var i = _index + 1; i < SlicerFile.LayerCount; i++)
        {
            if (SlicerFile[i].NonZeroPixelCount >= numberOfPixels) return SlicerFile[i];
        }

        return null;
    }

    /// <summary>
    /// Gets the layer index
    /// </summary>
    public uint Index
    {
        get => _index;
        set
        {
            if(!RaiseAndSetIfChanged(ref _index, value)) return;
            RaisePropertyChanged(nameof(Number));
        }
    }

    /// <summary>
    /// Gets the layer number, 1 started
    /// </summary>
    public uint Number => _index + 1;

    /// <summary>
    /// Gets or sets the absolute layer position on Z in mm
    /// </summary>
    public float PositionZ
    {
        get => _positionZ;
        set
        {
            //if (value < 0) throw new ArgumentOutOfRangeException(nameof(PositionZ), "Value can't be negative");
            if (!RaiseAndSetIfChanged(ref _positionZ, RoundHeight(value))) return;
            RaisePropertyChanged(nameof(RelativePositionZ));
            RaisePropertyChanged(nameof(LayerHeight));
            //MaterialMilliliters = -1; // Recalculate
        }
    }

    /// <summary>
    /// Gets the relative layer position on Z in mm (Relative to the previous layer)
    /// </summary>
    public float RelativePositionZ
    {
        get
        {
            var previousLayer = PreviousLayer;
            return previousLayer is null ? _positionZ : RoundHeight(_positionZ - previousLayer.PositionZ);
        }
        set => PositionZ = _positionZ - RelativePositionZ + value;
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
            value = (float)Math.Round(value, 2);
            if (value < 0) value = SlicerFile.GetBottomOrNormalValue(this, SlicerFile.BottomWaitTimeBeforeCure, SlicerFile.WaitTimeBeforeCure);
            if (!RaiseAndSetIfChanged(ref _waitTimeBeforeCure, value)) return;
            SlicerFile.UpdatePrintTimeQueued();
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
            value = (float)Math.Round(value, 2);
            if (value < 0) value = SlicerFile.GetBottomOrNormalValue(this, SlicerFile.BottomExposureTime, SlicerFile.ExposureTime);
            if(!RaiseAndSetIfChanged(ref _exposureTime, value)) return;
            SlicerFile.UpdatePrintTimeQueued();
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
            value = (float)Math.Round(value, 2);
            if (value < 0) value = SlicerFile.GetBottomOrNormalValue(this, SlicerFile.BottomWaitTimeAfterCure, SlicerFile.WaitTimeAfterCure);
            if (!RaiseAndSetIfChanged(ref _waitTimeAfterCure, value)) return;
            SlicerFile.UpdatePrintTimeQueued();
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
            value = (float)Math.Round(value, 2);
            if (value < 0) value = SlicerFile.GetBottomOrNormalValue(this, SlicerFile.BottomLightOffDelay, SlicerFile.LightOffDelay);
            if(!RaiseAndSetIfChanged(ref _lightOffDelay, value)) return;
            SlicerFile.UpdatePrintTimeQueued();
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
            value = (float)Math.Round(value, 2);
            if (value < 0) value = SlicerFile.GetBottomOrNormalValue(this, SlicerFile.BottomLiftHeight, SlicerFile.LiftHeight);
            if(!RaiseAndSetIfChanged(ref _liftHeight, value)) return;
            RaisePropertyChanged(nameof(LiftHeightTotal));
            RetractHeight2 = _retractHeight2; // Sanitize
            SlicerFile.UpdatePrintTimeQueued();
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
            value = (float)Math.Round(value, 2);
            if (value <= 0) value = SlicerFile.GetBottomOrNormalValue(this, SlicerFile.BottomLiftSpeed, SlicerFile.LiftSpeed);
            if(!RaiseAndSetIfChanged(ref _liftSpeed, value)) return;
            SlicerFile.UpdatePrintTimeQueued();
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
            value = (float)Math.Round(value, 2);
            if (value < 0) value = SlicerFile.GetBottomOrNormalValue(this, SlicerFile.BottomLiftHeight2, SlicerFile.LiftHeight2);
            if (!RaiseAndSetIfChanged(ref _liftHeight2, value)) return;
            RaisePropertyChanged(nameof(LiftHeightTotal));
            RetractHeight2 = _retractHeight2; // Sanitize
            SlicerFile.UpdatePrintTimeQueued();
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
            value = (float)Math.Round(value, 2);
            if (value <= 0) value = SlicerFile.GetBottomOrNormalValue(this, SlicerFile.BottomLiftSpeed2, SlicerFile.LiftSpeed2);
            if (!RaiseAndSetIfChanged(ref _liftSpeed2, value)) return;
            SlicerFile.UpdatePrintTimeQueued();
        }
    }

    public float WaitTimeAfterLift
    {
        get => _waitTimeAfterLift;
        set
        {
            value = (float)Math.Round(value, 2);
            if (value < 0) value = SlicerFile.GetBottomOrNormalValue(this, SlicerFile.BottomWaitTimeAfterLift, SlicerFile.WaitTimeAfterLift);
            if (!RaiseAndSetIfChanged(ref _waitTimeAfterLift, value)) return;
            SlicerFile.UpdatePrintTimeQueued();
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
            value = (float)Math.Round(value, 2);
            if (value <= 0) value = SlicerFile.GetBottomOrNormalValue(this, SlicerFile.BottomRetractSpeed, SlicerFile.RetractSpeed);
            if (!RaiseAndSetIfChanged(ref _retractSpeed, value)) return;
            SlicerFile.UpdatePrintTimeQueued();
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
            SlicerFile.UpdatePrintTimeQueued();
        }
    }

    /// <summary>
    /// Gets the speed in mm/min for the retracts
    /// </summary>
    public virtual float RetractSpeed2
    {
        get => _retractSpeed2;
        set
        {
            value = (float)Math.Round(value, 2);
            if (value <= 0) value = SlicerFile.GetBottomOrNormalValue(this, SlicerFile.BottomRetractSpeed2, SlicerFile.RetractSpeed2);
            if (!RaiseAndSetIfChanged(ref _retractSpeed2, value)) return;
            SlicerFile.UpdatePrintTimeQueued();
        }
    }

    /// <summary>
    /// Gets or sets the pwm value from 0 to 255
    /// </summary>
    public byte LightPWM
    {
        get => _lightPWM;
        set
        {
            //if (value == 0) value = SlicerFile.GetInitialLayerValueOrNormal(Index, SlicerFile.BottomLightPWM, SlicerFile.LightPWM);
            //if (value == 0) value = FileFormat.DefaultLightPWM;
            RaiseAndSetIfChanged(ref _lightPWM, value);
        }
    }

    /// <summary>
    /// Gets the minimum used speed in mm/min
    /// </summary>
    public float MinimumSpeed
    {
        get
        {
            float speed = float.MaxValue;
            if (LiftSpeed > 0) speed = Math.Min(speed, LiftSpeed);
            if (LiftSpeed2 > 0) speed = Math.Min(speed, LiftSpeed2);
            if (RetractSpeed > 0) speed = Math.Min(speed, RetractSpeed);
            if (RetractSpeed2 > 0) speed = Math.Min(speed, RetractSpeed2);
            if (Math.Abs(speed - float.MaxValue) < 0.01) return 0;

            return speed;
        }
    }

    /// <summary>
    /// Gets the maximum used speed in mm/min
    /// </summary>
    public float MaximumSpeed
    {
        get
        {
            float speed = LiftSpeed;
            speed = Math.Max(speed, LiftSpeed2);
            speed = Math.Max(speed, RetractSpeed);
            speed = Math.Max(speed, RetractSpeed2);

            return speed;
        }
    }

    /// <summary>
    /// Gets if this layer can be exposed to UV light
    /// </summary>
    public bool CanExpose => _exposureTime > 0 && _lightPWM > 0;

    /// <summary>
    /// Gets the layer height in millimeters of this layer
    /// </summary>
    public float LayerHeight
    {
        get
        {
            if (IsFirstLayer) return _positionZ;
            var previousLayer = this;

            while ((previousLayer = previousLayer!.PreviousLayer) is not null) // This cycle returns the correct layer height if two or more layers have the same position z
            {
                var layerHeight = RoundHeight(_positionZ - previousLayer.PositionZ);
                //Debug.WriteLine($"Layer {_index}-{previousLayer.Index}: {_positionZ} - {previousLayer.PositionZ}: {layerHeight}");
                if (layerHeight == 0f) continue;
                if (layerHeight < 0f) break;
                return layerHeight;
            }

            return SlicerFile.LayerHeight;
        }
    }

    /// <summary>
    /// Gets the computed material milliliters spent on this layer
    /// </summary>
    public float MaterialMilliliters
    {
        get => _materialMilliliters;
        set
        {
            if (SlicerFile is null) return;
            //var globalMilliliters = SlicerFile.MaterialMilliliters - _materialMilliliters;
            if (value < 0)
            {
                value = (float) Math.Round(GetVolume() / 1000f, 4);
            }

            if(!RaiseAndSetIfChanged(ref _materialMilliliters, value)) return;
            RaisePropertyChanged(nameof(MaterialMillilitersPercent));
            SlicerFile.MaterialMilliliters = -1; // Recalculate global
            //ParentLayerManager.MaterialMillilitersTimer.Stop();
            //if(!ParentLayerManager.MaterialMillilitersTimer.Enabled)
            //    ParentLayerManager.MaterialMillilitersTimer.Start();
        }
    }

    /// <summary>
    /// Gets the computed material milliliters percentage compared to the rest of the model
    /// </summary>
    public float MaterialMillilitersPercent => SlicerFile.MaterialMilliliters > 0 ? _materialMilliliters * 100 / SlicerFile.MaterialMilliliters : float.NaN;

    /// <summary>
    /// Gets or sets the compression method used to cache the image
    /// </summary>
    public LayerCompressionCodec CompressionCodec
    {
        get => _compressionCodec;
        set
        {
            if (!HaveImage)
            {
                RaiseAndSetIfChanged(ref _compressionCodec, value);
                return;
            }

            // Handle conversion
            if (_compressionCodec == value) return;
            using var mat = LayerMat;
            _compressionCodec = value;
            _compressedBytes = CompressMat(mat, value);
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets layer image compressed data
    /// </summary>
    public byte[]? CompressedBytes
    {
        get => _compressedBytes;
        set
        {
            _compressedBytes = value;
            IsModified = true;
            SlicerFile.BoundingRectangle = Rectangle.Empty;
            _contours?.Dispose();
            _contours = null;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HaveImage));
        }
    }

    public byte[]? CompressedPngBytes
    {
        get
        {
            if (_compressedBytes is null) return null;
            if (_compressionCodec == LayerCompressionCodec.Png) return _compressedBytes;

            using var mat = LayerMat;
            return mat.GetPngByes();
        }
    }

    /// <summary>
    /// True if this layer have an valid initialized image, otherwise false
    /// </summary>
    public bool HaveImage => _compressedBytes is not null && _compressedBytes.Length > 0;
        
    /// <summary>
    /// Gets or sets a new image instance
    /// </summary>
    [XmlIgnore]
    [JsonInclude]
    public Mat LayerMat
    {
        get
        {
            if (!HaveImage) return null!;

            Mat mat;
            switch (_compressionCodec)
            {
                case LayerCompressionCodec.Png:
                    mat = new Mat();
                    CvInvoke.Imdecode(_compressedBytes, ImreadModes.Grayscale, mat);
                    break;
                case LayerCompressionCodec.Lz4:
                    mat = new Mat(Resolution, DepthType.Cv8U, 1);
                    LZ4Codec.Decode(_compressedBytes.AsSpan(), mat.GetDataByteSpan());
                    break;
                case LayerCompressionCodec.GZip:
                {
                    mat = new Mat(Resolution, DepthType.Cv8U, 1);
                    unsafe
                    {
                        fixed (byte* pBuffer = _compressedBytes)
                        {
                            using var compressedStream = new UnmanagedMemoryStream(pBuffer, _compressedBytes!.Length);
                            using var matStream = mat.GetUnmanagedMemoryStream(FileAccess.Write);
                            using var gZipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
                            gZipStream.CopyTo(matStream);
                        }
                    }

                    break;
                }
                case LayerCompressionCodec.Deflate:
                {
                    mat = new Mat(Resolution, DepthType.Cv8U, 1);
                    unsafe
                    {
                        fixed (byte* pBuffer = _compressedBytes)
                        {
                            using var compressedStream = new UnmanagedMemoryStream(pBuffer, _compressedBytes!.Length);
                            using var matStream = mat.GetUnmanagedMemoryStream(FileAccess.Write);
                            using var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
                            deflateStream.CopyTo(matStream);
                        }
                    }

                    break;
                }
                /*case LayerCompressionMethod.None:
                    mat = new Mat(Resolution, DepthType.Cv8U, 1);
                    //mat.SetBytes(_compressedBytes!);
                    _compressedBytes.CopyTo(mat.GetDataByteSpan());
                    break;*/
                default:
                    throw new ArgumentOutOfRangeException(nameof(LayerMat));
            }
            
            return mat; 
        }
        set
        {
            if (value is not null)
            {
                CompressedBytes = CompressMat(value, _compressionCodec);
                _resolutionX = (uint)value.Width;
                _resolutionY = (uint)value.Height;
            }
            else
            {
                _resolutionX = 0;
                _resolutionY = 0;
            }
            
            GetBoundingRectangle(value, true);
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Gets the layer mat with roi of it bounding rectangle
    /// </summary>
    public MatRoi LayerMatBoundingRectangle => new(LayerMat, BoundingRectangle);

    /// <summary>
    /// Gets the layer mat with roi of model bounding rectangle
    /// </summary>
    public MatRoi LayerMatModelBoundingRectangle => new(LayerMat, SlicerFile.BoundingRectangle);

    /// <summary>
    /// Gets the layer mat with a specified roi
    /// </summary>
    /// <param name="roi">Region of interest</param>
    /// <returns></returns>
    public MatRoi GetLayerMat(Rectangle roi) => new(LayerMat, roi);

    /// <summary>
    /// Gets the layer mat with bounding rectangle mat
    /// </summary>
    /// <param name="margin">Margin from bounding rectangle</param>
    /// <returns></returns>
    public MatRoi GetLayerMatBoundingRectangle(int margin) => new(LayerMat, GetBoundingRectangle(margin));

    /// <summary>
    /// Gets the layer mat with bounding rectangle mat
    /// </summary>
    /// <param name="marginX">X margin from bounding rectangle</param>
    /// <param name="marginY">Y margin from bounding rectangle</param>
    /// <returns></returns>
    public MatRoi GetLayerMatBoundingRectangle(int marginX, int marginY) => new(LayerMat, GetBoundingRectangle(marginX, marginY));

    /// <summary>
    /// Gets the layer mat with bounding rectangle mat
    /// </summary>
    /// <param name="margin">Margin from bounding rectangle</param>
    /// <returns></returns>
    public MatRoi GetLayerMatBoundingRectangle(Size margin) => new(LayerMat, GetBoundingRectangle(margin));

    /// <summary>
    /// Gets a new Brg image instance
    /// </summary>
    [XmlIgnore]
    [JsonInclude]
    public Mat BrgMat
    {
        get
        {
            var mat = LayerMat;
            if (mat is null) return null!;
            CvInvoke.CvtColor(mat, mat, ColorConversion.Gray2Bgr);
            return mat;
        }
    }

    /// <summary>
    /// Gets a computed layer filename, padding zeros are equal to layer count digits
    /// </summary>
    public string Filename => FormatFileNameWithLayerDigits("layer");

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
    public bool IsUsingGlobalParameters
    {
        get
        {
            if (SlicerFile is null) return false; // Cant verify
            if (IsBottomLayer)
            {
                if (
                    _positionZ != RoundHeight(SlicerFile.LayerHeight * Number) ||
                    _lightOffDelay != SlicerFile.BottomLightOffDelay ||
                    _waitTimeBeforeCure != SlicerFile.BottomWaitTimeBeforeCure ||
                    _exposureTime != SlicerFile.BottomExposureTime ||
                    _waitTimeAfterCure != SlicerFile.BottomWaitTimeAfterCure ||
                    _liftHeight != SlicerFile.BottomLiftHeight ||
                    _liftSpeed != SlicerFile.BottomLiftSpeed ||
                    _liftHeight2 != SlicerFile.BottomLiftHeight2 ||
                    _liftSpeed2 != SlicerFile.BottomLiftSpeed2 ||
                    _waitTimeAfterLift != SlicerFile.BottomWaitTimeAfterLift ||
                    _retractSpeed != SlicerFile.BottomRetractSpeed ||
                    _retractHeight2 != SlicerFile.BottomRetractHeight2 ||
                    _retractSpeed2 != SlicerFile.BottomRetractSpeed2 ||
                    _lightPWM != SlicerFile.BottomLightPWM 
                ) 
                    return false;
            }
            else
            {
                if (
                    _positionZ != RoundHeight(SlicerFile.LayerHeight * Number) ||
                    _lightOffDelay != SlicerFile.LightOffDelay ||
                    _waitTimeBeforeCure != SlicerFile.WaitTimeBeforeCure ||
                    _exposureTime != SlicerFile.ExposureTime ||
                    _waitTimeAfterCure != SlicerFile.WaitTimeAfterCure ||
                    _liftHeight != SlicerFile.LiftHeight ||
                    _liftSpeed != SlicerFile.LiftSpeed ||
                    _liftHeight2 != SlicerFile.LiftHeight2 ||
                    _liftSpeed2 != SlicerFile.LiftSpeed2 ||
                    _waitTimeAfterLift != SlicerFile.WaitTimeAfterLift ||
                    _retractSpeed != SlicerFile.RetractSpeed ||
                    _retractHeight2 != SlicerFile.RetractHeight2 ||
                    _retractSpeed2 != SlicerFile.RetractSpeed2 ||
                    _lightPWM != SlicerFile.LightPWM
                ) 
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// True if this layer is using TSMC values, otherwise false
    /// </summary>
    public bool IsUsingTSMC => LiftHeight2 > 0 || RetractHeight2 > 0;

    /// <summary>
    /// Gets tree contours cache for this layer, however should call <see cref="GetContours"/> instead with <see cref="LayerMat"/> instance
    /// </summary>
    public EmguContours Contours
    {
        get
        {
            if (_contours is null)
            {
                using var mat = LayerMat;
                _contours = new EmguContours(mat, RetrType.Tree);
            }

            return _contours;
        }
    }

    /// <summary>
    /// Gets tree contours cache for this layer
    /// </summary>
    public EmguContours GetContours(IInputOutputArray mat)
    {
        return _contours ??= new EmguContours(mat, RetrType.Tree);
    }

    #endregion

    #region Constructor

    public Layer(uint index, FileFormat slicerFile, LayerCompressionCodec? compressionMethod = null)
    {
        compressionMethod ??= CoreSettings.DefaultLayerCompressionCodec;
        _compressionCodec = compressionMethod.Value;

        SlicerFile = slicerFile;
        _index = index;

        _resolutionX = slicerFile.ResolutionX;
        _resolutionY = slicerFile.ResolutionY;
        
        //if (slicerFile is null) return;
        _positionZ = SlicerFile.GetHeightFromLayer(index);
        ResetParameters();
    }

    public Layer(uint index, byte[] pngBytes, FileFormat slicerFile, LayerCompressionCodec? compressionMethod = null) : this(index, slicerFile, compressionMethod)
    {
        if (_compressionCodec == LayerCompressionCodec.Png)
        {
            CompressedBytes = pngBytes;
        }
        else
        {
            var mat = new Mat();
            CvInvoke.Imdecode(pngBytes, ImreadModes.Grayscale, mat);
            LayerMat = mat;
        }

        _isModified = false;
    }

    public Layer(uint index, Mat layerMat, FileFormat slicerFile, LayerCompressionCodec? compressionMethod = null) : this(index, slicerFile, compressionMethod)
    {
        LayerMat = layerMat;
        _isModified = false;
    }

    public Layer(uint index, Stream stream, FileFormat slicerFile, LayerCompressionCodec? compressionMethod = null) : this(index, stream.ToArray(), slicerFile, compressionMethod) 
    { }

    public Layer(FileFormat slicerFile, LayerCompressionCodec? compressionMethod = null) : this(0, slicerFile, compressionMethod)
    { }

    public Layer(byte[] pngBytes, FileFormat slicerFile, LayerCompressionCodec? compressionMethod = null) : this(0, pngBytes, slicerFile, compressionMethod)
    { }

    public Layer(Mat layerMat, FileFormat slicerFile, LayerCompressionCodec? compressionMethod = null) : this(0, layerMat, slicerFile, compressionMethod)
    { }

    public Layer(Stream stream, FileFormat slicerFile, LayerCompressionCodec? compressionMethod = null) : this(0, stream, slicerFile, compressionMethod) { }

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

    public bool Equals(Layer? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_index != other._index) return false;
        if (_compressedBytes?.Length != other._compressedBytes?.Length) return false;
        return _compressedBytes.AsSpan().SequenceEqual(other._compressedBytes.AsSpan());
        //return Equals(_compressedBytes, other._compressedBytes);
    }

    public override bool Equals(object? obj)
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
        public int Compare(Layer? x, Layer? y)
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
               $"{nameof(IsUsingGlobalParameters)}: {IsUsingGlobalParameters}";
    }
    #endregion

    #region Methods

    /// <summary>
    /// Reset all parameters to the default values from the global parameters
    /// </summary>
    public void ResetParameters()
    {
        _lightOffDelay = SlicerFile.GetBottomOrNormalValue(this, SlicerFile.BottomLightOffDelay, SlicerFile.LightOffDelay);
        _waitTimeBeforeCure = SlicerFile.GetBottomOrNormalValue(this, SlicerFile.BottomWaitTimeBeforeCure, SlicerFile.WaitTimeBeforeCure);
        _exposureTime = SlicerFile.GetBottomOrNormalValue(this, SlicerFile.BottomExposureTime, SlicerFile.ExposureTime);
        _waitTimeAfterCure = SlicerFile.GetBottomOrNormalValue(this, SlicerFile.BottomWaitTimeAfterCure, SlicerFile.WaitTimeAfterCure);
        _liftHeight = SlicerFile.GetBottomOrNormalValue(this, SlicerFile.BottomLiftHeight, SlicerFile.LiftHeight);
        _liftSpeed = SlicerFile.GetBottomOrNormalValue(this, SlicerFile.BottomLiftSpeed, SlicerFile.LiftSpeed);
        _liftHeight2 = SlicerFile.GetBottomOrNormalValue(this, SlicerFile.BottomLiftHeight2, SlicerFile.LiftHeight2);
        _liftSpeed2 = SlicerFile.GetBottomOrNormalValue(this, SlicerFile.BottomLiftSpeed2, SlicerFile.LiftSpeed2);
        _waitTimeAfterLift = SlicerFile.GetBottomOrNormalValue(this, SlicerFile.BottomWaitTimeAfterLift, SlicerFile.WaitTimeAfterLift);
        _retractSpeed = SlicerFile.GetBottomOrNormalValue(this, SlicerFile.BottomRetractSpeed, SlicerFile.RetractSpeed);
        _retractHeight2 = SlicerFile.GetBottomOrNormalValue(this, SlicerFile.BottomRetractHeight2, SlicerFile.RetractHeight2);
        _retractSpeed2 = SlicerFile.GetBottomOrNormalValue(this, SlicerFile.BottomRetractSpeed2, SlicerFile.RetractSpeed2);
        _lightPWM = SlicerFile.GetBottomOrNormalValue(this, SlicerFile.BottomLightPWM, SlicerFile.LightPWM);
    }

    /// <summary>
    /// Gets the layer area (XY) in mm^2
    /// Pixel size * number of pixels
    /// </summary>
    public float GetArea() => SlicerFile.PixelArea * _nonZeroPixelCount;


    /// <summary>
    /// Gets the layer area (XY) in mm^2
    /// Pixel size * number of pixels
    /// </summary>
    public float GetArea(byte roundToDigits) => (float)Math.Round(GetArea(), roundToDigits);

    /// <summary>
    /// Gets the layer volume (XYZ) in mm^3
    /// Pixel size * number of pixels * layer height
    /// </summary>
    public float GetVolume() => GetArea() * LayerHeight;

    /// <summary>
    /// Gets the layer volume (XYZ) in mm^3
    /// Pixel size * number of pixels * layer height
    /// </summary>
    public float GetVolume(byte roundToDigits) => (float)Math.Round(GetArea() * LayerHeight, roundToDigits);

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
    /// Gets the wait time before cure, if not available calculate it from light off delay
    /// </summary>
    /// <returns></returns>
    public float GetWaitTimeBeforeCure()
    {
        if (SlicerFile.CanUseLayerWaitTimeBeforeCure)
        {
            return WaitTimeBeforeCure;
        }

        if (SlicerFile.CanUseLayerLightOffDelay)
        {
            return (float)Math.Max(0, Math.Round(LightOffDelay - CalculateLightOffDelay(), 2));
        }

        return 0;
    }

    /// <summary>
    /// Attempt to set wait time before cure if supported, otherwise fallback to light-off delay
    /// </summary>
    /// <param name="time">The time to set</param>
    /// <param name="zeroLightOffDelayCalculateBase">When true and time is zero, it will calculate light-off delay without extra time, otherwise false to set light-off delay to 0 when time is 0</param>
    public void SetWaitTimeBeforeCureOrLightOffDelay(float time = 0, bool zeroLightOffDelayCalculateBase = false)
    {
        if (SlicerFile.CanUseLayerWaitTimeBeforeCure)
        {
            LightOffDelay = 0;
            WaitTimeBeforeCure = time;
        }
        else if(SlicerFile.CanUseLayerLightOffDelay)
        {
            if (time == 0 && !zeroLightOffDelayCalculateBase)
            {
                LightOffDelay = 0;
                return;
            }
            SetLightOffDelay(time);
        }
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

    public string FormatFileName(string prepend  = "", byte padDigits = 0, IndexStartNumber layerIndexStartNumber = default, string appendExt = ".png")
    {
        var index = Index;
        if (layerIndexStartNumber == IndexStartNumber.One)
        {
            index++;
        }
        return $"{prepend}{index.ToString().PadLeft(padDigits, '0')}{appendExt}";
    }

    public string FormatFileName(byte padDigits, IndexStartNumber layerIndexStartNumber = default, string appendExt = ".png")
        => FormatFileName(string.Empty, padDigits, layerIndexStartNumber, appendExt);
    
    public string FormatFileNameWithLayerDigits(string prepend = "", IndexStartNumber layerIndexStartNumber = default, string appendExt = ".png")
        => FormatFileName(prepend, SlicerFile.LayerDigits, layerIndexStartNumber, appendExt);


    public Rectangle GetBoundingRectangle(Mat? mat = null, bool reCalculate = false)
    {
        if (_nonZeroPixelCount > 0 && !reCalculate)
        {
            return BoundingRectangle;
        }
        bool needDispose = false;
        if (mat is null)
        {
            if (_compressedBytes is null || _compressedBytes.Length == 0)
            {
                NonZeroPixelCount = 0;
                return Rectangle.Empty;
            }
            mat = LayerMat;
            needDispose = true;
        }


        //NonZeroPixelCount = (uint)CvInvoke.CountNonZero(mat);
        //BoundingRectangle = _nonZeroPixelCount > 0 ? CvInvoke.BoundingRectangle(mat) : Rectangle.Empty;
        BoundingRectangle = CvInvoke.BoundingRectangle(mat);
        if (_boundingRectangle.IsEmpty)
        {
            NonZeroPixelCount = 0;
        }
        else
        {
            var roiMat = mat.Roi(_boundingRectangle);
            NonZeroPixelCount = (uint)CvInvoke.CountNonZero(roiMat);
            roiMat.DisposeIfSubMatrix();
        }


        if (needDispose) mat!.Dispose();

        return BoundingRectangle;
    }

    public Rectangle GetBoundingRectangle(int marginX, int marginY)
    {
        var rect = BoundingRectangle;
        if (marginX == 0 && marginY == 0) return rect;
        rect.Inflate(marginX / 2, marginY / 2);
        SlicerFile.SanitizeBoundingRectangle(ref rect);
        return rect;
    }

    public Rectangle GetBoundingRectangle(int margin) => GetBoundingRectangle(margin, margin);
    public Rectangle GetBoundingRectangle(Size margin) => GetBoundingRectangle(margin.Width, margin.Height);

    public bool SetValueFromPrintParameterModifier(FileFormat.PrintParameterModifier modifier, decimal value)
    {
        if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.PositionZ))
        {
            PositionZ = (float)value;
            return true;
        }

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

    public byte SetValuesFromPrintParametersModifiers(FileFormat.PrintParameterModifier[]? modifiers)
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

    /// <summary>
    /// Copy all parameters from this layer to an target layer
    /// </summary>
    /// <param name="layer"></param>
    public void CopyParametersTo(Layer layer)
    {
        CopyWaitTimesTo(layer);
        CopyExposureTo(layer);
        CopyLiftTo(layer);
    }

    /// <summary>
    /// Copy all exposure parameters from this layer to an target layer
    /// </summary>
    /// <param name="layer"></param>
    public void CopyExposureTo(Layer layer)
    {
        layer.ExposureTime = _exposureTime;
        layer.LightPWM = _lightPWM;
    }

    /// <summary>
    /// Copy all wait parameters from this layer to an target layer
    /// </summary>
    /// <param name="layer"></param>
    public void CopyWaitTimesTo(Layer layer)
    {
        layer.LightOffDelay = _lightOffDelay;
        layer.WaitTimeBeforeCure = _waitTimeBeforeCure;
        layer.WaitTimeAfterCure = _waitTimeAfterCure;
        layer.WaitTimeAfterLift = _waitTimeAfterLift;
    }

    /// <summary>
    /// Copy all lift parameters from this layer to an target layer
    /// </summary>
    /// <param name="layer"></param>
    public void CopyLiftTo(Layer layer)
    {
        layer.LiftHeight = _liftHeight;
        layer.LiftHeight2 = _liftHeight2;
        layer.LiftSpeed = _liftSpeed;
        layer.LiftSpeed2 = _liftSpeed2;
        layer.RetractHeight2 = _retractHeight2;
        layer.RetractSpeed =  _retractSpeed;
        layer.RetractSpeed2 = _retractSpeed2;
    }

    /// <summary>
    /// Copy the image and related parameters from this layer to an target layer
    /// </summary>
    /// <param name="layer"></param>
    public void CopyImageTo(Layer layer)
    {
        if (!HaveImage) return;
        layer.ResolutionX = _resolutionX;
        layer.ResolutionY = _resolutionY;
        layer.CompressedBytes = _compressedBytes?.ToArray();
        layer._contours = _contours?.Clone();
        layer.BoundingRectangle = _boundingRectangle;
        layer.NonZeroPixelCount = _nonZeroPixelCount;
    }

    public Layer Clone()
    {
        var layer = (Layer)MemberwiseClone();
        layer._compressedBytes = _compressedBytes?.ToArray();
        layer._contours = _contours?.Clone();
        //Debug.WriteLine(ReferenceEquals(_compressedBytes, layer.CompressedBytes));
        return layer;
        /*return new (_index, CompressedBytes?.ToArray()!, SlicerFile)
        {
            _compressionMethod = _compressionMethod,
            _positionZ = _positionZ,
            _lightOffDelay = _lightOffDelay,
            _waitTimeBeforeCure = _waitTimeBeforeCure,
            _exposureTime = _exposureTime,
            _waitTimeAfterCure = _waitTimeAfterCure,
            _liftHeight = _liftHeight,
            _liftSpeed = _liftSpeed,
            _liftHeight2 = _liftHeight2,
            _liftSpeed2 = _liftSpeed2,
            _waitTimeAfterLift = _waitTimeAfterLift,
            _retractSpeed = _retractSpeed,
            _retractHeight2 = _retractHeight2,
            _retractSpeed2 = _retractSpeed2,
            _lightPWM = _lightPWM,
            _boundingRectangle = _boundingRectangle,
            _nonZeroPixelCount = _nonZeroPixelCount,
            _isModified = _isModified,
            _materialMilliliters = _materialMilliliters,
        };*/
    }

    public Layer[] Clone(uint times)
    {
        var layers = new Layer[times];
        for (int i = 0; i < times; i++)
        {
            layers[i] = Clone();
        }

        return layers;
    }
    #endregion

    #region Static Methods

    /// <summary>
    /// Gets the bounding rectangle that is the union of a collection of layers
    /// </summary>
    /// <param name="layers">Layer collection</param>
    /// <returns></returns>
    public static Rectangle GetBoundingRectangleUnion(params Layer[] layers)
    {
        var rect = Rectangle.Empty;
        foreach (var layer in layers)
        {
            if(layer.BoundingRectangle.IsEmpty) continue;
            rect = rect.IsEmpty ? layer.BoundingRectangle : Rectangle.Union(rect, layer.BoundingRectangle);
        }

        return rect;
    }

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
            clonedLayers[layerIndex] = layers[layerIndex].Clone();
        }
        return clonedLayers;
    }

    public static byte[] CompressMat(Mat mat, LayerCompressionCodec codec)
    {
        switch (codec)
        {
            case LayerCompressionCodec.Png:
                return mat.GetPngByes();
            case LayerCompressionCodec.Lz4:
            {
                var span = mat.GetDataByteSpan();
                var target = new byte[LZ4Codec.MaximumOutputSize(span.Length)];
                var encodedLength = LZ4Codec.Encode(span, target.AsSpan());
                Array.Resize(ref target, encodedLength);
                return target;
            }
            case LayerCompressionCodec.GZip:
            {
                return CompressionExtensions.GZipCompressToBytes(mat.GetBytes(), CompressionLevel.Fastest);
                /*using var compressedStream = new MemoryStream();
                using var matStream = mat.GetUnmanagedMemoryStream(FileAccess.Read);
                using var gzipStream = new GZipStream(compressedStream, CompressionLevel.Fastest);
                matStream.CopyTo(gzipStream);
                return compressedStream.ToArray();*/
            }
            case LayerCompressionCodec.Deflate:
            {
                return CompressionExtensions.DeflateCompressToBytes(mat.GetBytes(), CompressionLevel.Fastest);
                /*using var compressedStream = new MemoryStream();
                using var deflateStream = new DeflateStream(compressedStream, CompressionLevel.Fastest);
                deflateStream.Write(mat.GetDataByteSpan());
                return compressedStream.ToArray();*/
            }
            /*case LayerCompressionMethod.None:
                return mat.GetBytes();*/
            default:
                throw new ArgumentOutOfRangeException(nameof(codec));
        }
    }

    #endregion
}