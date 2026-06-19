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
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public sealed partial class OperationCalibrateXYZAccuracy : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Members
    private decimal _layerHeight;
    private decimal _bottomExposure;
    private decimal _normalExposure;
    private decimal _displayWidth;
    private decimal _displayHeight;
    private decimal _xSize = 15;
    private decimal _ySize = 15;
    private decimal _zSize = 15;
    private decimal _wallThickness = 3.0M;
    private decimal _observedXSize;
    private decimal _observedYSize;
    private decimal _observedZSize;

    #endregion

    #region Overrides

    public override bool CanROI => false;

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;
    public override string IconClass => "TapeMeasure";
    public override string Title => "XYZ Accuracy";
    public override string Description =>
        "Generates test models with various strategies and increments to verify the XYZ accuracy.\n" +
        "XYZ are accurate when the printed model match the expected size.\n" +
        "You must repeat this test when change any of the following: printer, LEDs, resin and exposure times.\n" +
        "Note: The current opened file will be overwritten with this test, use a dummy or a not needed file.";

    public override string ConfirmationText =>
        $"generate the XYZ accuracy test?";

    public override string ProgressTitle =>
        $"Generating the XYZ accuracy test";

    public override string ProgressAction => "Generated";

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        if (DisplayWidth <= 0)
        {
            sb.AppendLine("Display width must be a positive value.");
        }

        if (DisplayHeight <= 0)
        {
            sb.AppendLine("Display height must be a positive value.");
        }

        if (OutputObjects <= 0)
        {
            sb.AppendLine("No objects to output.");
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"[Layer Height: {_layerHeight}] " +
                     $"[Bottom layers: {BottomLayers}] " +
                     $"[Exposure: {_bottomExposure}/{_normalExposure}] " +
                     $"[X: {_xSize} Y:{_ySize} Z:{_zSize}] " +
                     $"[TB:{TopBottomMargin} LR:{LeftRightMargin}] " +
                     $"[Model: {OutputTLObject.ToByte()}{OutputTCObject.ToByte()}{OutputTRObject.ToByte()}" +
                     $"|{OutputMLObject.ToByte()}{OutputMCObject.ToByte()}{OutputMRObject.ToByte()}" +
                     $"|{OutputBLObject.ToByte()}{OutputBCObject.ToByte()}{OutputBRObject.ToByte()}] " +
                     $"[Hollow: {HollowModel} @ {_wallThickness}mm] [Relief: {CenterHoleRelief}] [Mirror: {MirrorOutput}]";
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    #endregion

    #region Properties

    public decimal DisplayWidth
    {
        get => _displayWidth;
        set
        {
            if(!SetProperty(ref _displayWidth, FileFormat.RoundDisplaySize(value))) return;
            OnPropertyChanged(nameof(Xppmm));
        }
    }

    public decimal DisplayHeight
    {
        get => _displayHeight;
        set
        {
            if(!SetProperty(ref _displayHeight, FileFormat.RoundDisplaySize(value))) return;
            OnPropertyChanged(nameof(Yppmm));
        }
    }

    public decimal Xppmm => DisplayWidth > 0 ? Math.Round(SlicerFile.Resolution.Width / DisplayWidth, 2) : 0;
    public decimal Yppmm => DisplayHeight > 0 ? Math.Round(SlicerFile.Resolution.Height / DisplayHeight, 2) : 0;

    public decimal LayerHeight
    {
        get => _layerHeight;
        set
        {
            if(!SetProperty(ref _layerHeight, Layer.RoundHeight(value))) return;
            OnPropertyChanged(nameof(BottomLayersMM));
            OnPropertyChanged(nameof(LayerCount));
            OnPropertyChanged(nameof(RealZSize));
            OnPropertyChanged(nameof(ObservedZSize));
        }
    }

    public ushort Microns => (ushort)(LayerHeight * 1000);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BottomLayersMM))]
    public partial ushort BottomLayers { get; set; }

    public decimal BottomLayersMM => Layer.RoundHeight(LayerHeight * BottomLayers);

    public decimal BottomExposure
    {
        get => _bottomExposure;
        set => SetProperty(ref _bottomExposure, Math.Round(value, 2));
    }

    public decimal NormalExposure
    {
        get => _normalExposure;
        set => SetProperty(ref _normalExposure, Math.Round(value, 2));
    }

    [ObservableProperty]
    public partial ushort TopBottomMargin { get; set; } = 100;

    [ObservableProperty]
    public partial ushort LeftRightMargin { get; set; } = 100;

    public decimal XSize
    {
        get => _xSize;
        set
        {
            if(!SetProperty(ref _xSize, Math.Round(value, 2))) return;
            OnPropertyChanged(nameof(RealXSize));
            OnPropertyChanged(nameof(ObservedXSize));
        }
    }

    public decimal YSize
    {
        get => _ySize;
        set
        {
            if(!SetProperty(ref _ySize, Math.Round(value, 2))) return;
            OnPropertyChanged(nameof(RealYSize));
            OnPropertyChanged(nameof(ObservedYSize));
        }
    }

    public decimal ZSize
    {
        get => _zSize;
        set
        {
            if(!SetProperty(ref _zSize, Math.Round(value, 2))) return;
            OnPropertyChanged(nameof(LayerCount));
            OnPropertyChanged(nameof(RealZSize));
            OnPropertyChanged(nameof(ObservedZSize));
        }
    }

    public decimal RealXSize
    {
        get
        {
            decimal pixels = _xSize * Xppmm;
            if (pixels <= 0) return 0;
            return Math.Round(_xSize - (pixels - Math.Truncate(pixels)) / Xppmm, 2);
        }

    }

    public decimal RealYSize
    {
        get
        {
            decimal pixels = _ySize * Yppmm;
            if (pixels <= 0) return 0;
            return Math.Round(_ySize - (pixels - Math.Truncate(pixels)) / Yppmm, 2);
        }
    }

    public decimal RealZSize => LayerCount * _layerHeight;

    public uint XPixels => (uint)(XSize * Xppmm);
    public uint YPixels => (uint)(YSize * Yppmm);

    public uint LayerCount => (uint)(ZSize / LayerHeight);

    [ObservableProperty]
    public partial decimal DrainHoleArea { get; set; } = 3;

    [ObservableProperty]
    public partial bool CenterHoleRelief { get; set; } = true;

    [ObservableProperty]
    public partial bool HollowModel { get; set; } = true;

    [ObservableProperty]
    public partial bool MirrorOutput { get; set; }

    public decimal WallThickness
    {
        get => _wallThickness;
        set
        {
            if(!SetProperty(ref _wallThickness, Math.Round(value, 2))) return;
            OnPropertyChanged(nameof(WallThicknessRealXSize));
            OnPropertyChanged(nameof(WallThicknessRealYSize));
        }
    }

    public decimal WallThicknessRealXSize
    {
        get
        {
            decimal pixels = _wallThickness * Xppmm;
            if (pixels <= 0) return 0;
            return Math.Round(_wallThickness - (pixels - Math.Truncate(pixels)) / Xppmm, 2);
        }
    }

    public decimal WallThicknessRealYSize
    {
        get
        {
            decimal pixels = _wallThickness * Yppmm;
            if (pixels <= 0) return 0;
            return Math.Round(_wallThickness - (pixels - Math.Truncate(pixels)) / Yppmm, 2);
        }
    }

    public uint WallThicknessXPixels => (uint)(WallThickness * Xppmm);
    public uint WallThicknessYPixels => (uint)(WallThickness * Yppmm);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutputObjects))]
    public partial bool OutputTLObject { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutputObjects))]
    public partial bool OutputTCObject { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutputObjects))]
    public partial bool OutputTRObject { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutputObjects))]
    public partial bool OutputMLObject { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutputObjects))]
    public partial bool OutputMCObject { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutputObjects))]
    public partial bool OutputMRObject { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutputObjects))]
    public partial bool OutputBLObject { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutputObjects))]
    public partial bool OutputBCObject { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutputObjects))]
    public partial bool OutputBRObject { get; set; }

    public byte OutputObjects => (byte) (OutputTLObject.ToByte() +
                                         OutputTCObject.ToByte() +
                                         OutputTRObject.ToByte() +
                                         OutputMLObject.ToByte() +
                                         OutputMCObject.ToByte() +
                                         OutputMRObject.ToByte() +
                                         OutputBLObject.ToByte() +
                                         OutputBCObject.ToByte() +
                                         OutputBRObject.ToByte());

    public decimal ObservedXSize
    {
        get => _observedXSize;
        set
        {
            if(!SetProperty(ref _observedXSize, Math.Round(value, 2))) return;
            OnPropertyChanged(nameof(ScaleXFactor));
        }
    }

    public decimal ObservedYSize
    {
        get => _observedYSize;
        set
        {
            if(!SetProperty(ref _observedYSize, Math.Round(value, 2))) return;
            OnPropertyChanged(nameof(ScaleYFactor));
        }
    }

    public decimal ObservedZSize
    {
        get => _observedZSize;
        set
        {
            if(!SetProperty(ref _observedZSize, Math.Round(value, 2))) return;
            OnPropertyChanged(nameof(ScaleZFactor));
        }
    }

    // 15 - x
    // 14 - 100
    public decimal ScaleXFactor => ObservedXSize > 0 && RealXSize > 0 ? Math.Round(RealXSize * 100 / ObservedXSize, 2) : 100;
    public decimal ScaleYFactor => ObservedYSize > 0 && RealYSize > 0 ? Math.Round(RealYSize * 100 / ObservedYSize, 2) : 100;
    public decimal ScaleZFactor => ObservedZSize > 0 && RealZSize > 0 ? Math.Round(RealZSize * 100 / ObservedZSize, 2) : 100;

    #endregion

    #region Constructor

    public OperationCalibrateXYZAccuracy() { }

    public OperationCalibrateXYZAccuracy(FileFormat slicerFile) : base(slicerFile)
    { }

    public override void InitWithSlicerFile()
    {
        base.InitWithSlicerFile();
        if (_layerHeight <= 0) _layerHeight = (decimal)SlicerFile.LayerHeight;
        if (BottomLayers <= 0) BottomLayers = SlicerFile.BottomLayerCount;
        if (_bottomExposure <= 0) _bottomExposure = (decimal)SlicerFile.BottomExposureTime;
        if (_normalExposure <= 0) _normalExposure = (decimal)SlicerFile.ExposureTime;
        MirrorOutput = SlicerFile.DisplayMirror != FlipDirection.None;

        if (SlicerFile.DisplayWidth > 0)
            DisplayWidth = (decimal)SlicerFile.DisplayWidth;
        if (SlicerFile.DisplayHeight > 0)
            DisplayHeight = (decimal)SlicerFile.DisplayHeight;
    }

    #endregion

    #region Enums

    #endregion

    #region Properties



    /*public override string ToString()
    {
        var result = $"[{_blurOperation}] [Size: {_size}]" + LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }*/

    #endregion

    #region Equality

    private bool Equals(OperationCalibrateXYZAccuracy other)
    {
        return _layerHeight == other._layerHeight && BottomLayers == other.BottomLayers && _bottomExposure == other._bottomExposure && _normalExposure == other._normalExposure && TopBottomMargin == other.TopBottomMargin && LeftRightMargin == other.LeftRightMargin && _xSize == other._xSize && _ySize == other._ySize && _zSize == other._zSize && CenterHoleRelief == other.CenterHoleRelief && HollowModel == other.HollowModel && _wallThickness == other._wallThickness && _observedXSize == other._observedXSize && _observedYSize == other._observedYSize && _observedZSize == other._observedZSize && OutputTLObject == other.OutputTLObject && OutputTCObject == other.OutputTCObject && OutputTRObject == other.OutputTRObject && OutputMLObject == other.OutputMLObject && OutputMCObject == other.OutputMCObject && OutputMRObject == other.OutputMRObject && OutputBLObject == other.OutputBLObject && OutputBCObject == other.OutputBCObject && OutputBRObject == other.OutputBRObject && MirrorOutput == other.MirrorOutput;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationCalibrateXYZAccuracy other && Equals(other);
    }

    #endregion

    #region Methods

    public void SelectNoneObjects()
    {
        OutputTLObject = false;
        OutputTCObject = false;
        OutputTRObject = false;
        OutputMLObject = false;
        OutputMCObject = false;
        OutputMRObject = false;
        OutputBLObject = false;
        OutputBCObject = false;
        OutputBRObject = false;
    }

    public void SelectAllObjects()
    {
        OutputTLObject = true;
        OutputTCObject = true;
        OutputTRObject = true;
        OutputMLObject = true;
        OutputMCObject = true;
        OutputMRObject = true;
        OutputBLObject = true;
        OutputBCObject = true;
        OutputBRObject = true;
    }

    public void SelectCrossedObjects()
    {
        OutputTLObject = false;
        OutputTCObject = true;
        OutputTRObject = false;
        OutputMLObject = true;
        OutputMCObject = true;
        OutputMRObject = true;
        OutputBLObject = false;
        OutputBCObject = true;
        OutputBRObject = false;
    }

    public void SelectCenterObject()
    {
        OutputTLObject = false;
        OutputTCObject = false;
        OutputTRObject = false;
        OutputMLObject = false;
        OutputMCObject = true;
        OutputMRObject = false;
        OutputBLObject = false;
        OutputBCObject = false;
        OutputBRObject = false;
    }

    public void Sanitize()
    {
        for (ushort i = 0; i < 10000 && (_xSize * Xppmm).DecimalDigits() >= 1; i++)
        {
            XSize += 0.01M;
        }

        for (ushort i = 0; i < 10000 && (_ySize * Yppmm).DecimalDigits() >= 1; i++)
        {
            YSize += 0.01M;
        }

        for (ushort i = 0; i < 10000 && (_zSize / LayerHeight).DecimalDigits() >= 1; i++)
        {
            ZSize += 0.01M;
        }
    }

    public Mat[] GetLayers()
    {
        var layers = new Mat[3];
        for (byte i = 0; i < layers.Length; i++)
        {
            layers[i] = EmguCvExtensions.InitMat(SlicerFile.Resolution);
        }


        int currentX = 0;
        int currentY = 0;
        string positionYString = string.Empty;
        string positionString = string.Empty;

        const FontFace fontFace = FontFace.HersheyDuplex;
        const byte fontStartX = 30;
        const byte fontStartY = 50;
        const double fontScale = 1.3;
        const byte fontThickness = 3;

        var xPixels = XPixels;
        var yPixels = YPixels;

        for (int y = 0; y < 3; y++)
        {
            switch (y)
            {
                case 0:
                    currentY = TopBottomMargin;
                    positionYString = "T";
                    break;
                case 1:
                    currentY = (int)(SlicerFile.Resolution.Height / 2 - yPixels / 2);
                    positionYString = "M";
                    break;
                case 2:
                    currentY = (int)(SlicerFile.Resolution.Height - yPixels - TopBottomMargin);
                    positionYString = "B";
                    break;
            }
            for (int x = 0; x < 3; x++)
            {
                switch (x)
                {
                    case 0:
                        currentX = LeftRightMargin;
                        positionString = $"{positionYString}L";
                        break;
                    case 1:
                        currentX = (int)(SlicerFile.Resolution.Width / 2 - xPixels / 2);
                        positionString = $"{positionYString}C";
                        break;
                    case 2:
                        currentX = (int)(SlicerFile.Resolution.Width - xPixels - LeftRightMargin);
                        positionString = $"{positionYString}R";
                        break;
                }


                for (var i = 0; i < layers.Length; i++)
                {
                    if(y == 0 && x == 0 && !OutputTLObject) continue;
                    if(y == 0 && x == 1 && !OutputTCObject) continue;
                    if(y == 0 && x == 2 && !OutputTRObject) continue;
                    if(y == 1 && x == 0 && !OutputMLObject) continue;
                    if(y == 1 && x == 1 && !OutputMCObject) continue;
                    if(y == 1 && x == 2 && !OutputMRObject) continue;
                    if(y == 2 && x == 0 && !OutputBLObject) continue;
                    if(y == 2 && x == 1 && !OutputBCObject) continue;
                    if(y == 2 && x == 2 && !OutputBRObject) continue;
                    var layer = layers[i];
                    CvInvoke.Rectangle(layer,
                        new Rectangle(currentX, currentY, (int)xPixels, (int) yPixels),
                        EmguCvExtensions.WhiteColor, -1);

                    CvInvoke.PutText(layer, positionString,
                        new Point(currentX + fontStartX, currentY + fontStartY), fontFace, fontScale,
                        EmguCvExtensions.BlackColor, fontThickness);

                    CvInvoke.PutText(layer, $"{XSize},{YSize},{ZSize}",
                        new Point(currentX + fontStartX, (int) (currentY + yPixels - fontStartY + 25)), fontFace, fontScale,
                        EmguCvExtensions.BlackColor, fontThickness);

                    if (CenterHoleRelief)
                    {
                        layer.DrawCircle(new Point((int) (currentX + xPixels / 2), (int) (currentY + yPixels / 2)),
                            SlicerFile.PixelsToNormalizedPitch((int) (Math.Min(xPixels, yPixels) / 4)),
                            EmguCvExtensions.BlackColor, -1);
                    }

                    if (HollowModel && i > 0 && _wallThickness > 0)
                    {
                        Size rectSize = new((int) (xPixels - WallThicknessXPixels * 2), (int) (yPixels - WallThicknessYPixels * 2));
                        Point rectLocation = new((int) (currentX + WallThicknessXPixels), (int) (currentY + WallThicknessYPixels));
                        CvInvoke.Rectangle(layers[i], new Rectangle(rectLocation, rectSize),
                            EmguCvExtensions.BlackColor, -1);
                    }

                    if (i == 2 && DrainHoleArea > 0)
                    {
                        Size rectSize = new((int)xPixels, (int)(Yppmm * DrainHoleArea));
                        Point rectLocation = new(currentX, (int)(currentY + xPixels / 2 - rectSize.Height / 2));
                        CvInvoke.Rectangle(layers[i], new Rectangle(rectLocation, rectSize),
                            EmguCvExtensions.BlackColor, -1);
                    }
                }
            }
        }

        if (MirrorOutput)
        {
            var flip = SlicerFile.DisplayMirror;
            if (flip == FlipDirection.None) flip = FlipDirection.Horizontally;
            Parallel.ForEach(layers, CoreSettings.ParallelOptions, mat => CvInvoke.Flip(mat, mat, (FlipType)flip));
        }

        return layers;
    }

    public Mat GetThumbnail()
    {
        Mat thumbnail = EmguCvExtensions.InitMat(new Size(400, 200), 3);
        var fontFace = FontFace.HersheyDuplex;
        var fontScale = 1;
        var fontThickness = 2;
        const byte xSpacing = 45;
        const byte ySpacing = 45;
        CvInvoke.PutText(thumbnail, "UVtools", new Point(140, 35), fontFace, fontScale, new MCvScalar(255, 27, 245), fontThickness + 1);
        CvInvoke.Line(thumbnail, new Point(xSpacing, 0), new Point(xSpacing, ySpacing + 5), new MCvScalar(255, 27, 245), 3);
        CvInvoke.Line(thumbnail, new Point(xSpacing, ySpacing + 5), new Point(thumbnail.Width - xSpacing, ySpacing + 5), new MCvScalar(255, 27, 245), 3);
        CvInvoke.Line(thumbnail, new Point(thumbnail.Width - xSpacing, 0), new Point(thumbnail.Width - xSpacing, ySpacing + 5), new MCvScalar(255, 27, 245), 3);
        CvInvoke.PutText(thumbnail, "XYZ Accuracy Cal.", new Point(xSpacing, ySpacing * 2), fontFace, fontScale, new MCvScalar(0, 255, 255), fontThickness);
        CvInvoke.PutText(thumbnail, $"{Microns}um @ {BottomExposure}s/{NormalExposure}s", new Point(xSpacing, ySpacing * 3), fontFace, fontScale, EmguCvExtensions.WhiteColor, fontThickness);
        CvInvoke.PutText(thumbnail, $"{XSize} x {YSize} x {ZSize} mm", new Point(xSpacing, ySpacing * 4), fontFace, fontScale, EmguCvExtensions.WhiteColor, fontThickness);

        /*thumbnail.SetTo(EmguCvExtensions.Black3Byte);

            CvInvoke.Circle(thumbnail, new Point(400/2, 200/2), 200/2, EmguCvExtensions.White3Byte, -1);
            for (int angle = 0; angle < 360; angle+=20)
            {
                CvInvoke.Line(thumbnail, new Point(400 / 2, 200 / 2), new Point((int)(400 / 2 + 100 * Math.Cos(angle * Math.PI / 180)), (int)(200 / 2 + 100 * Math.Sin(angle * Math.PI / 180))), new MCvScalar(255, 27, 245), 3);
            }

            thumbnail.Save("D:\\Thumbnail.png");*/
        return thumbnail;
    }

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        progress.ItemCount = LayerCount;


        var newLayers = new Layer[LayerCount];

        var layers = GetLayers();

        var bottomLayer = new Layer(0, layers[0], SlicerFile)
        {
            IsModified = true
        };
        var layer = new Layer(0, layers[1], SlicerFile)
        {
            IsModified = true
        };
        var ventLayer = new Layer(0, layers[2], SlicerFile)
        {
            IsModified = true
        };


        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            progress.PauseOrCancelIfRequested();
            newLayers[layerIndex] = SlicerFile.GetBottomOrNormalValue(layerIndex, bottomLayer.Clone(),
                (HollowModel || CenterHoleRelief) && DrainHoleArea > 0 && layerIndex <= BottomLayers + (int)(DrainHoleArea / _layerHeight)
                    ? ventLayer.Clone() : layer.Clone());

            progress++;
        }

        foreach (var mat in layers)
        {
            mat.Dispose();
        }

        if (SlicerFile.ThumbnailsCount > 0)
        {
            using var thumbnail = GetThumbnail();
            SlicerFile.SetThumbnails(thumbnail);
        }

        SlicerFile.SuppressRebuildPropertiesWork(() =>
        {
            SlicerFile.LayerHeight = (float)LayerHeight;
            SlicerFile.BottomExposureTime = (float)BottomExposure;
            SlicerFile.ExposureTime = (float)NormalExposure;
            SlicerFile.BottomLayerCount = BottomLayers;
            SlicerFile.TransitionLayerCount = 0;
            SlicerFile.Layers = newLayers;
        }, true);

        return !progress.Token.IsCancellationRequested;
    }

    #endregion
}