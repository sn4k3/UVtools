/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;

namespace UVtools.Core.Operations;

[Serializable]
public class OperationLithophane : Operation
{
    #region Enum

    public enum LithophaneBaseType : byte
    {
        None,
        Square,
        Model
    }

    #endregion

    #region Members
    private decimal _layerHeight;
    private ushort _bottomLayerCount;
    private decimal _bottomExposure;
    private decimal _normalExposure;

    private string? _filePath;
    private RotateDirection _rotate = RotateDirection.None;
    private bool _mirror;
    private bool _invertColor;
    private decimal _resizeFactor = 100;
    private bool _enhanceContrast;
    private sbyte _brightnessGain;
    private byte _gapClosingIterations;
    private byte _removeNoiseIterations;
    private byte _gaussianBlur;
    private byte _startThresholdRange = 1;
    private byte _endThresholdRange = byte.MaxValue;
    private decimal _baseThickness = 2;
    private LithophaneBaseType _baseType = LithophaneBaseType.Square;
    private ushort _baseMargin = 80;
    private decimal _lithophaneHeight = 3;
    private bool _oneLayerPerThreshold;
    private bool _enableAntiAliasing = true;

    #endregion

    #region Overrides

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;
    public override string IconClass => "fas fa-portrait";
    public override string Title => "Lithophane";
    public override string Description =>
        "Generate lithophane from a picture.\n" +
        "Note: The current opened file will be overwritten with this lithophane, use a dummy or a not needed file.";

    public override string ConfirmationText =>
        "generate the lithophane?";

    public override string ProgressTitle =>
        "Generating lithophane";

    public override string ProgressAction => "Threshold levels";

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();
        if (string.IsNullOrWhiteSpace(_filePath))
        {
            sb.AppendLine("The input file is empty");
        }
        else if(!File.Exists(_filePath))
        {
            sb.AppendLine("The input file does not exists");
        }
        else
        {
            using var mat = GetSourceMat();
            if (mat is null)
            {
                sb.AppendLine("Unable to generate the mat from source file, is it a valid image file?");
            }
            else
            {
                if (SlicerFile.ResolutionX < mat.Width * _resizeFactor / 100 || SlicerFile.ResolutionY < mat.Height * _resizeFactor / 100)
                {
                    //int differenceX = (int)SlicerFile.ResolutionX - mat.Width;
                    //int differenceY = (int)SlicerFile.ResolutionY - mat.Height;
                    var scaleX = SlicerFile.ResolutionX * 100f / mat.Width;
                    var scaleY = SlicerFile.ResolutionY * 100f / mat.Height;
                    var maxScale = Math.Min(scaleX, scaleY);

                    sb.AppendLine($"The printer resolution is not enough to accomodate the lithophane image, please scale down to a maximum of {maxScale:F0}%");
                }
            }
        }

        if (_startThresholdRange == _endThresholdRange)
        {
            sb.AppendLine($"Start threshold can't be equal than end threshold ({_endThresholdRange})");
        }
        else if (_startThresholdRange > _endThresholdRange)
        {
            sb.AppendLine("Start threshold can't be higher than end threshold");
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"{(FileExists ? $"{Path.GetFileName(_filePath)} ({Math.Abs(GetHashCode())})" : $"Lithophane {Math.Abs(GetHashCode())}")}";
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }
    #endregion

    #region Constructor

    public OperationLithophane() { }

    public OperationLithophane(FileFormat slicerFile) : base(slicerFile)
    {
        if (_layerHeight <= 0) _layerHeight = (decimal)SlicerFile.LayerHeight;
        if (_bottomExposure <= 0) _bottomExposure = (decimal)SlicerFile.BottomExposureTime;
        if (_normalExposure <= 0) _normalExposure = (decimal)SlicerFile.ExposureTime;
        if (_bottomLayerCount <= 0) _bottomLayerCount = SlicerFile.BottomLayerCount;
        _mirror = SlicerFile.DisplayMirror != FlipDirection.None;
    }

    #endregion

    #region Properties
    public decimal LayerHeight
    {
        get => _layerHeight;
        set => RaiseAndSetIfChanged(ref _layerHeight, Layer.RoundHeight(value));
    }

    public ushort BottomLayerCount
    {
        get => _bottomLayerCount;
        set => RaiseAndSetIfChanged(ref _bottomLayerCount, value);
    }

    public decimal BottomExposure
    {
        get => _bottomExposure;
        set => RaiseAndSetIfChanged(ref _bottomExposure, Math.Round(value, 2));
    }

    public decimal NormalExposure
    {
        get => _normalExposure;
        set => RaiseAndSetIfChanged(ref _normalExposure, Math.Round(value, 2));
    }

    public string? FilePath
    {
        get => _filePath;
        set => RaiseAndSetIfChanged(ref _filePath, value);
    }

    public bool FileExists => !string.IsNullOrWhiteSpace(_filePath) && File.Exists(_filePath);

    public RotateDirection Rotate
    {
        get => _rotate;
        set => RaiseAndSetIfChanged(ref _rotate, value);
    }

    public bool Mirror
    {
        get => _mirror;
        set => RaiseAndSetIfChanged(ref _mirror, value);
    }

    public bool InvertColor
    {
        get => _invertColor;
        set => RaiseAndSetIfChanged(ref _invertColor, value);
    }

    public decimal ResizeFactor
    {
        get => _resizeFactor;
        set => RaiseAndSetIfChanged(ref _resizeFactor, Math.Max(1, value));
    }

    public bool EnhanceContrast
    {
        get => _enhanceContrast;
        set => RaiseAndSetIfChanged(ref _enhanceContrast, value);
    }

    public sbyte BrightnessGain
    {
        get => _brightnessGain;
        set => RaiseAndSetIfChanged(ref _brightnessGain, value);
    }

    public byte GapClosingIterations
    {
        get => _gapClosingIterations;
        set => RaiseAndSetIfChanged(ref _gapClosingIterations, value);
    }

    public byte RemoveNoiseIterations
    {
        get => _removeNoiseIterations;
        set => RaiseAndSetIfChanged(ref _removeNoiseIterations, value);
    }

    public byte GaussianBlur
    {
        get => _gaussianBlur;
        set => RaiseAndSetIfChanged(ref _gaussianBlur, value);
    }

    public byte StartThresholdRange
    {
        get => _startThresholdRange;
        set => RaiseAndSetIfChanged(ref _startThresholdRange, Math.Max((byte)1, value));
    }

    public byte EndThresholdRange
    {
        get => _endThresholdRange;
        set => RaiseAndSetIfChanged(ref _endThresholdRange, Math.Max((byte)1, value));
    }

    public decimal BaseThickness
    {
        get => _baseThickness;
        set => RaiseAndSetIfChanged(ref _baseThickness, Math.Max(0, value));
    }

    public LithophaneBaseType BaseType
    {
        get => _baseType;
        set => RaiseAndSetIfChanged(ref _baseType, value);
    }

    public ushort BaseMargin
    {
        get => _baseMargin;
        set => RaiseAndSetIfChanged(ref _baseMargin, value);
    }

    public decimal LithophaneHeight
    {
        get => _lithophaneHeight;
        set => RaiseAndSetIfChanged(ref _lithophaneHeight, Math.Max(0.01m, value));
    }

    public bool OneLayerPerThreshold
    {
        get => _oneLayerPerThreshold;
        set => RaiseAndSetIfChanged(ref _oneLayerPerThreshold, value);
    }

    public bool EnableAntiAliasing
    {
        get => _enableAntiAliasing;
        set => RaiseAndSetIfChanged(ref _enableAntiAliasing, value);
    }

    #endregion

    #region Equality

    protected bool Equals(OperationLithophane other)
    {
        return _layerHeight == other._layerHeight && _bottomLayerCount == other._bottomLayerCount && _bottomExposure == other._bottomExposure && _normalExposure == other._normalExposure && _filePath == other._filePath && _rotate == other._rotate && _mirror == other._mirror && _invertColor == other._invertColor && _enhanceContrast == other._enhanceContrast && _resizeFactor == other._resizeFactor && _brightnessGain == other._brightnessGain && _gapClosingIterations == other._gapClosingIterations && _removeNoiseIterations == other._removeNoiseIterations && _gaussianBlur == other._gaussianBlur && _startThresholdRange == other._startThresholdRange && _endThresholdRange == other._endThresholdRange && _baseThickness == other._baseThickness && _baseType == other._baseType && _baseMargin == other._baseMargin && _lithophaneHeight == other._lithophaneHeight && _oneLayerPerThreshold == other._oneLayerPerThreshold && _enableAntiAliasing == other._enableAntiAliasing;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((OperationLithophane) obj);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(_layerHeight);
        hashCode.Add(_bottomLayerCount);
        hashCode.Add(_bottomExposure);
        hashCode.Add(_normalExposure);
        hashCode.Add(_filePath);
        hashCode.Add((int) _rotate);
        hashCode.Add(_mirror);
        hashCode.Add(_invertColor);
        hashCode.Add(_enhanceContrast);
        hashCode.Add(_resizeFactor);
        hashCode.Add(_brightnessGain);
        hashCode.Add(_gapClosingIterations);
        hashCode.Add(_removeNoiseIterations);
        hashCode.Add(_gaussianBlur);
        hashCode.Add(_startThresholdRange);
        hashCode.Add(_endThresholdRange);
        hashCode.Add(_baseThickness);
        hashCode.Add((int) _baseType);
        hashCode.Add(_baseMargin);
        hashCode.Add(_lithophaneHeight);
        hashCode.Add(_oneLayerPerThreshold);
        hashCode.Add(_enableAntiAliasing);
        return hashCode.ToHashCode();
    }

    #endregion

    #region Methods

    public Mat? GetSourceMat()
    {
        if (!FileExists) return null;
        try
        {
            var mat = CvInvoke.Imread(_filePath, ImreadModes.Grayscale);
            if (_invertColor) CvInvoke.BitwiseNot(mat, mat);
            mat = mat.CropByBounds();
            return mat.Size == Size.Empty ? null : mat;
        }
        catch
        {
            // ignored
        }

        return null;
    }

    public Mat? GetTargetMat()
    {
        var mat = GetSourceMat();
        if (mat is null) return null;
        
        if (_resizeFactor != 100) mat.Resize((double)_resizeFactor / 100.0);
        
        if (_enhanceContrast) CvInvoke.EqualizeHist(mat, mat);
        if (_brightnessGain != 0)
        {
            using var mask = mat.NewSetTo(new MCvScalar(Math.Abs(_brightnessGain)));
            if(_brightnessGain > 0) CvInvoke.Add(mat, mask, mat, mat);
            else CvInvoke.Subtract(mat, mask, mat, mat);
        }

        if (_gaussianBlur > 0)
        {
            var ksize = 1 + _gaussianBlur * 2;
            CvInvoke.GaussianBlur(mat, mat, new Size(ksize, ksize), 0);
        }

        if (_removeNoiseIterations > 0) CvInvoke.MorphologyEx(mat, mat, MorphOp.Open, EmguExtensions.Kernel3x3Rectangle, new Point(-1, -1), _removeNoiseIterations, BorderType.Reflect101, default);
        if (_gapClosingIterations > 0) CvInvoke.MorphologyEx(mat, mat, MorphOp.Close, EmguExtensions.Kernel3x3Rectangle, new Point(-1, -1), _gapClosingIterations, BorderType.Reflect101, default);
        
        if (_rotate != RotateDirection.None) CvInvoke.Rotate(mat, mat, (RotateFlags) _rotate);
        if (_mirror)
        {
            var flip = SlicerFile.DisplayMirror;
            if (flip == FlipDirection.None) flip = FlipDirection.Horizontally;
            CvInvoke.Flip(mat, mat, (FlipType)flip);
        }

        return mat;
    }

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        using var mat = GetTargetMat();
        if (mat is null) return false;
        
        var layersBag = new ConcurrentDictionary<byte, Layer>();
        progress.Reset("Threshold levels", byte.MaxValue);
        Parallel.For(_startThresholdRange, _endThresholdRange, CoreSettings.GetParallelOptions(progress), threshold =>
        {
            using var thresholdMat = new Mat();
            CvInvoke.Threshold(mat, thresholdMat, threshold, byte.MaxValue, ThresholdType.Binary);
            if (CvInvoke.CountNonZero(thresholdMat) == 0) return;

            if (_enableAntiAliasing)
            {
                CvInvoke.GaussianBlur(thresholdMat, thresholdMat, new Size(3, 3), 0);
            }

            using var layerMat = EmguExtensions.InitMat(SlicerFile.Resolution);
            thresholdMat.CopyToCenter(layerMat);
            layersBag.TryAdd((byte)threshold, new Layer(layerMat, SlicerFile));
            progress.LockAndIncrement();
        });

        if (layersBag.Count == 0)
        {
            throw new InvalidOperationException("Unable to continue due to no threshold layers was generated, either by lack of pixels or by using a short range.");
        }

        var thresholdLayers = layersBag.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToArray();

        if (!_oneLayerPerThreshold)
        {
            var layerIncrementF = thresholdLayers.Length * _layerHeight / Math.Max(_layerHeight, _lithophaneHeight);
            if (layerIncrementF >= 2)
            {
                var layerIncrement = (uint) layerIncrementF;
                var indexes = new int[(int)Math.Ceiling(thresholdLayers.Length / (float)layerIncrement)];
                var newLayers = new Layer[indexes.Length];
                var count = 0;
                for (int index = 0; index < thresholdLayers.Length; index++)
                {
                    if (index % layerIncrement != 0) continue;
                    newLayers[count] = thresholdLayers[index];
                    indexes[count++] = index;
                    
                }

                progress.ResetNameAndProcessed("Packed layers");
                Parallel.ForEach(indexes, CoreSettings.GetParallelOptions(progress), i =>
                {
                    progress.LockAndIncrement();
                    using var mat = thresholdLayers[i].LayerMat;
                    for (int index = i+1; index < i + layerIncrement && index < thresholdLayers.Length; index++)
                    {
                        using var nextMat = thresholdLayers[index].LayerMat;
                        CvInvoke.Max(mat, nextMat, mat);
                        progress.LockAndIncrement();
                    }

                    thresholdLayers[i].LayerMat = mat;
                });


                thresholdLayers = newLayers;
            }
            else if (layerIncrementF < 1)
            {
                var layerIncrement = (uint)Math.Ceiling(1/layerIncrementF);
                if (layerIncrement > 1)
                {
                    progress.Reset("Packed layers");
                    var newLayers = new Layer[thresholdLayers.Length * layerIncrement];
                    for (int i = 0; i < thresholdLayers.Length; i++)
                    {
                        var layer = thresholdLayers[i];
                        var newIndex = i * layerIncrement;
                        newLayers[newIndex] = layer;
                        for (int x = 1; x < layerIncrement; x++)
                        {
                            newLayers[++newIndex] = layer.Clone();
                        }

                    }
                    thresholdLayers = newLayers;
                }
            }
        }

        if (_baseType != LithophaneBaseType.None && _baseThickness > 0)
        {
            int baseLayerCount = (int)(_baseThickness / _layerHeight);
            var newLayers = new Layer[thresholdLayers.Length + baseLayerCount];
            using var baseMat = SlicerFile.CreateMat();
            
            switch (_baseType)
            {
                case LithophaneBaseType.Square:
                {
                    var rectangle = new Rectangle(
                        baseMat.Width / 2 - mat.Width / 2 - _baseMargin / 2,
                        baseMat.Height / 2 - mat.Height / 2 - _baseMargin / 2, 
                        mat.Width + _baseMargin, 
                        mat.Height + _baseMargin);
                    CvInvoke.Rectangle(baseMat, rectangle, EmguExtensions.WhiteColor, -1, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                    break;
                }
                case LithophaneBaseType.Model:
                {
                    using var dilatedMat = new Mat();
                    CvInvoke.Threshold(mat, dilatedMat, 1, byte.MaxValue, ThresholdType.Binary);
                    CvInvoke.Dilate(dilatedMat, dilatedMat, EmguExtensions.Kernel3x3Rectangle, new Point(-1, -1), _baseMargin, BorderType.Reflect101, default);
                    dilatedMat.CopyToCenter(baseMat);
                    break;
                }
            }
            
            var baseLayer = new Layer(baseMat, SlicerFile);
            newLayers[0] = baseLayer;
            for (int i = 1; i < baseLayerCount; i++)
            {
                newLayers[i] = baseLayer.Clone();
            }
            Array.Copy(thresholdLayers, 0, newLayers, baseLayerCount, thresholdLayers.Length);
            thresholdLayers = newLayers;
        }

        SlicerFile.SuppressRebuildPropertiesWork(() =>
        {
            SlicerFile.LayerHeight = (float) _layerHeight;
            SlicerFile.BottomLayerCount = _bottomLayerCount;
            SlicerFile.BottomExposureTime = (float) _bottomExposure;
            SlicerFile.ExposureTime = (float) _normalExposure;

            SlicerFile.Layers = thresholdLayers;
        }, true);
        

        using var bgrMat = new Mat();
        CvInvoke.CvtColor(mat, bgrMat, ColorConversion.Gray2Bgr);
        int baseLine = 0;
        var textSize = CvInvoke.GetTextSize("UVtools Lithophane", FontFace.HersheyDuplex, 2, 3, ref baseLine);
        CvInvoke.PutText(bgrMat, "UVtools Lithophane", new Point(bgrMat.Width / 2 - textSize.Width / 2, 60), FontFace.HersheyDuplex, 2, new MCvScalar(255, 27, 245), 3);
        SlicerFile.SetThumbnails(bgrMat);

        return !progress.Token.IsCancellationRequested;
    }


    #endregion
}