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
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using EmguExtensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public sealed partial class OperationCalibrateTolerance : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Members
    private decimal _displayWidth;
    private decimal _displayHeight;
    private decimal _layerHeight;
    private decimal _bottomExposure;
    private decimal _normalExposure;
    private decimal _zSize = 10;
    private decimal _femaleDiameter = 16;

    private decimal _maleThinnerOffset;
    private decimal _maleThinnerStep = -0.1M;
    private decimal _maleThickerOffset;
    private decimal _maleThickerStep = 0.1M;
    #endregion

    #region Overrides

    public override bool CanROI => false;

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;
    public override string IconClass => "CircleBox";
    public override string Title => "Tolerance";
    public override string Description =>
        "Generates test models with various strategies and increments to verify the part tolerances.\n" +
        "You must repeat this test when change any of the following: printer, LEDs, resin and exposure times.\n" +
        "Note: The current opened file will be overwritten with this test, use a dummy or a not needed file.";

    public override string ConfirmationText =>
        $"generate the tolerance test?";

    public override string ProgressTitle =>
        $"Generating the tolerance test";

    public override string ProgressAction => "Generated";

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        if (_displayWidth <= 0)
        {
            sb.AppendLine("Display width must be a positive value.");
        }

        if (_displayHeight <= 0)
        {
            sb.AppendLine("Display height must be a positive value.");
        }

        if (FemaleHoleDiameter >= _femaleDiameter)
        {
            sb.AppendLine("Hole diameter must be smaller than female diameter.");
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
                     $"[Z: {_zSize}] " +
                     $"[TB:{TopBottomMargin} LR:{LeftRightMargin} PM:{PartMargin}] " +
                     $"[Chamfer: {ChamferLayers}] [Erode: {ErodeBottomIterations}] " +
        $"[OSHD: {OutputSameDiameterPart}] [Fuse: {FuseParts}] [AA: {EnableAntiAliasing}] [Mirror: {MirrorOutput}]" +
                     $"[{Shape}, {_femaleDiameter}/{FemaleHoleDiameter}] " +
                     $"[tM: {MaleThinnerModels} O:{_maleThinnerOffset} S:{_maleThinnerStep}] " +
                     $"[TM: {MaleThickerModels} O:{_maleThickerOffset} S:{_maleThickerStep}] ";
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
            //OnPropertyChanged(nameof(ObservedZSize));
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

    public decimal ZSize
    {
        get => _zSize;
        set
        {
            if(!SetProperty(ref _zSize, Math.Round(value, 2))) return;
            OnPropertyChanged(nameof(LayerCount));
            OnPropertyChanged(nameof(RealZSize));
            //OnPropertyChanged(nameof(ObservedZSize));
        }
    }

    public uint LayerCount => (uint)(ZSize / LayerHeight);

    public decimal RealZSize => LayerCount * _layerHeight;

    [ObservableProperty]
    public partial ushort TopBottomMargin { get; set; } = 100;

    [ObservableProperty]
    public partial ushort LeftRightMargin { get; set; } = 100;

    [ObservableProperty]
    public partial byte ChamferLayers { get; set; } = 4;

    [ObservableProperty]
    public partial byte ErodeBottomIterations { get; set; } = 4;

    [ObservableProperty]
    public partial Shapes Shape { get; set; } = Shapes.Circle;

    [ObservableProperty]
    public partial ushort PartMargin { get; set; } = 50;

    [ObservableProperty]
    public partial bool OutputSameDiameterPart { get; set; } = true;

    [ObservableProperty]
    public partial bool FuseParts { get; set; }

    partial void OnFusePartsChanged(bool value)
    {
        if (!value) return;
        OutputSameDiameterPart = false;
        MaleThickerModels = 0;
    }

    [ObservableProperty]
    public partial bool EnableAntiAliasing { get; set; } = true;

    [ObservableProperty]
    public partial bool MirrorOutput { get; set; }

    public decimal FemaleDiameter
    {
        get => _femaleDiameter;
        set
        {
            if(!SetProperty(ref _femaleDiameter, Math.Round(value, 2))) return;
            OnPropertyChanged(nameof(FemaleDiameterXPixels));
            OnPropertyChanged(nameof(FemaleDiameterYPixels));
            OnPropertyChanged(nameof(FemaleDiameterRealXSize));
            OnPropertyChanged(nameof(FemaleDiameterRealYSize));
        }
    }

    public uint FemaleDiameterXPixels => (uint)(_femaleDiameter * Xppmm);
    public uint FemaleDiameterYPixels => (uint)(_femaleDiameter * Yppmm);

    public decimal FemaleDiameterRealXSize
    {
        get
        {
            decimal pixels = _femaleDiameter * Xppmm;
            return pixels <= 0 ? 0 : Math.Round(_femaleDiameter - (pixels - Math.Truncate(pixels)) / Xppmm, 2);
        }
    }

    public decimal FemaleDiameterRealYSize
    {
        get
        {
            decimal pixels = _femaleDiameter * Yppmm;
            return pixels <= 0 ? 0 : Math.Round(_femaleDiameter - (pixels - Math.Truncate(pixels)) / Yppmm, 2);
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FemaleHoleDiameterXPixels))]
    [NotifyPropertyChangedFor(nameof(FemaleHoleDiameterYPixels))]
    [NotifyPropertyChangedFor(nameof(FemaleHoleDiameterRealXSize))]
    [NotifyPropertyChangedFor(nameof(FemaleHoleDiameterRealYSize))]
    public partial decimal FemaleHoleDiameter { get; set; } = 10;

    public uint FemaleHoleDiameterXPixels => (uint)(FemaleHoleDiameter * Xppmm);
    public uint FemaleHoleDiameterYPixels => (uint)(FemaleHoleDiameter * Yppmm);

    public decimal FemaleHoleDiameterRealXSize
    {
        get
        {
            decimal pixels = FemaleHoleDiameter * Xppmm;
            return pixels <= 0 ? 0 : Math.Round(FemaleHoleDiameter - (pixels - Math.Truncate(pixels)) / Xppmm, 2);
        }
    }

    public decimal FemaleHoleDiameterRealYSize
    {
        get
        {
            decimal pixels = FemaleHoleDiameter * Yppmm;
            return pixels <= 0 ? 0 : Math.Round(FemaleHoleDiameter - (pixels - Math.Truncate(pixels)) / Yppmm, 2);
        }
    }

    [ObservableProperty]
    public partial ushort MaleThinnerModels { get; set; } = 5;

    public decimal MaleThinnerOffset
    {
        get => _maleThinnerOffset;
        set => SetProperty(ref _maleThinnerOffset, Math.Round(value, 2));
    }

    public decimal MaleThinnerStep
    {
        get => _maleThinnerStep;
        set => SetProperty(ref _maleThinnerStep, Math.Round(value, 2));
    }

    [ObservableProperty]
    public partial ushort MaleThickerModels { get; set; }

    public decimal MaleThickerOffset
    {
        get => _maleThickerOffset;
        set => SetProperty(ref _maleThickerOffset, Math.Round(value, 2));
    }

    public decimal MaleThickerStep
    {
        get => _maleThickerStep;
        set => SetProperty(ref _maleThickerStep, Math.Round(value, 2));
    }


    public uint OutputObjects =>
        (OutputSameDiameterPart ? 1u : 0) +
        MaleThinnerModels +
        MaleThickerModels +
            (FuseParts ? 0 : 1u);

    /*public decimal ObservedXSize
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


    public decimal ScaleXFactor => ObservedXSize > 0 && RealXSize > 0 ? Math.Round(RealXSize * 100 / ObservedXSize, 2) : 100;
    public decimal ScaleYFactor => ObservedYSize > 0 && RealYSize > 0 ? Math.Round(RealYSize * 100 / ObservedYSize, 2) : 100;
    public decimal ScaleZFactor => ObservedZSize > 0 && RealZSize > 0 ? Math.Round(RealZSize * 100 / ObservedZSize, 2) : 100;
    */
    #endregion

    #region Constructor

    public OperationCalibrateTolerance() { }

    public OperationCalibrateTolerance(FileFormat slicerFile) : base(slicerFile)
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

    public enum Shapes : byte
    {
        Circle,
        Square
    }

    public static Array ShapesItems => Enum.GetValues(typeof(Shapes));
    #endregion

    #region Equality

    private bool Equals(OperationCalibrateTolerance other)
    {
        return _layerHeight == other._layerHeight && BottomLayers == other.BottomLayers && _bottomExposure == other._bottomExposure && _normalExposure == other._normalExposure && _zSize == other._zSize && TopBottomMargin == other.TopBottomMargin && LeftRightMargin == other.LeftRightMargin && ChamferLayers == other.ChamferLayers && ErodeBottomIterations == other.ErodeBottomIterations && Shape == other.Shape && PartMargin == other.PartMargin && OutputSameDiameterPart == other.OutputSameDiameterPart && FuseParts == other.FuseParts && EnableAntiAliasing == other.EnableAntiAliasing && _femaleDiameter == other._femaleDiameter && FemaleHoleDiameter == other.FemaleHoleDiameter && MaleThinnerModels == other.MaleThinnerModels && _maleThinnerOffset == other._maleThinnerOffset && _maleThinnerStep == other._maleThinnerStep && MaleThickerModels == other.MaleThickerModels && _maleThickerOffset == other._maleThickerOffset && _maleThickerStep == other._maleThickerStep && MirrorOutput == other.MirrorOutput;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationCalibrateTolerance other && Equals(other);
    }

    #endregion

    #region Methods
    public Mat[] GetLayers()
    {
        var layers = new Mat[LayerCount];
        var layer = EmguCvExtensions.InitMat(SlicerFile.Resolution);

        ushort startX = Math.Max((ushort)2, LeftRightMargin);
        ushort startY = Math.Max((ushort)2, TopBottomMargin);
        int currentX = startX;
        int currentY = startY;

        const FontFace fontFace = FontFace.HersheyDuplex;
        const double fontScale = 1;
        const byte fontThickness = 2;
        LineType lineType = EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected;

        var kernel = EmguCvExtensions.Kernel3X3Rectangle;

        var pointTextList = new List<KeyValuePair<Point, string>>();

        if (!FuseParts)
        {
            switch (Shape)
            {
                case Shapes.Circle:
                    currentX += (int) FemaleDiameterXPixels / 2;
                    currentY += (int) FemaleDiameterXPixels / 2;
                    CvInvoke.Circle(layer, new Point(currentX, currentY), (int) (FemaleDiameterXPixels / 2), EmguCvExtensions.WhiteColor, -1, lineType);
                    CvInvoke.Circle(layer, new Point(currentX, currentY), (int) (FemaleHoleDiameterXPixels / 2), EmguCvExtensions.BlackColor, -1, lineType);
                    currentX += (int) FemaleDiameterXPixels / 2 + PartMargin;

                    break;
                case Shapes.Square:
                    int offsetX = (int) ((FemaleDiameterXPixels - FemaleHoleDiameterXPixels) / 2);
                    int offsetY = (int) ((FemaleDiameterYPixels - FemaleHoleDiameterYPixels) / 2);
                    CvInvoke.Rectangle(layer, new Rectangle(currentX, currentY, (int) FemaleDiameterXPixels, (int) FemaleDiameterXPixels), EmguCvExtensions.WhiteColor, -1, lineType);
                    CvInvoke.Rectangle(layer, new Rectangle(currentX + offsetX, currentY + offsetY, (int) FemaleHoleDiameterXPixels, (int) FemaleHoleDiameterYPixels), EmguCvExtensions.BlackColor, -1, lineType);
                    currentX += (int)FemaleDiameterXPixels + PartMargin;
                    currentY = startY + (int) FemaleDiameterYPixels / 2;
                    break;
            }
        }

        bool addPart(decimal step)
        {
            decimal millimeters = Math.Round(FemaleHoleDiameter + step, 2);
            if (millimeters <= 0) return false;
            int xPixels = (int)(millimeters * Xppmm);
            int yPixels = (int)(millimeters * Yppmm);
            Point partCenterText;

        if (FuseParts)
            {
                if (xPixels >= FemaleHoleDiameterXPixels || yPixels >= FemaleHoleDiameterYPixels) return false;
                if (currentX + FemaleDiameterXPixels + LeftRightMargin >= SlicerFile.Resolution.Width)
                {
                    currentX = startX;
                    currentY += (int)FemaleDiameterYPixels + PartMargin;
                }

                if (currentY + FemaleDiameterYPixels + TopBottomMargin >= SlicerFile.Resolution.Height)
                {
                    return false; // Insufficient size
                }

                int halfDiameterX = (int)(FemaleDiameterXPixels / 2);
                int halfDiameterY = (int)(FemaleDiameterYPixels / 2);

                switch (Shape)
                {
                    case Shapes.Circle:
                        CvInvoke.Circle(layer, new Point(currentX + halfDiameterX, currentY + halfDiameterY), (int)(FemaleDiameterXPixels / 2), EmguCvExtensions.WhiteColor, -1, lineType);
                        CvInvoke.Circle(layer, new Point(currentX + halfDiameterX, currentY + halfDiameterY), (int)(FemaleHoleDiameterXPixels / 2), EmguCvExtensions.BlackColor, -1, lineType);
                        CvInvoke.Circle(layer, new Point(currentX + halfDiameterX, currentY + halfDiameterY), xPixels / 2, EmguCvExtensions.WhiteColor, -1, lineType);
                        break;
                    case Shapes.Square:
                        int offsetX = (int)((FemaleDiameterXPixels - FemaleHoleDiameterXPixels) / 2);
                        int offsetY = (int)((FemaleDiameterYPixels - FemaleHoleDiameterYPixels) / 2);
                        CvInvoke.Rectangle(layer, new Rectangle(currentX, currentY, (int)FemaleDiameterXPixels, (int)FemaleDiameterXPixels), EmguCvExtensions.WhiteColor, -1, lineType);
                        CvInvoke.Rectangle(layer, new Rectangle(currentX + offsetX, currentY + offsetY, (int)FemaleHoleDiameterXPixels, (int)FemaleHoleDiameterYPixels), EmguCvExtensions.BlackColor, -1, lineType);
                        offsetX = (int)((FemaleDiameterXPixels - xPixels) / 2);
                        offsetY = (int)((FemaleDiameterYPixels - yPixels) / 2);
                        CvInvoke.Rectangle(layer, new Rectangle(currentX + offsetX, currentY + offsetY, xPixels, yPixels), EmguCvExtensions.WhiteColor, -1, lineType);
                        break;
                }

                partCenterText = new Point(currentX + halfDiameterX - 60, currentY + halfDiameterY + 10);
                currentX += (int)FemaleDiameterXPixels + PartMargin;
            }
            else
            {
                if (currentX + xPixels + LeftRightMargin >= SlicerFile.Resolution.Width)
                {
                    currentX = startX;
                    currentY += yPixels + PartMargin;
                }

                if (currentY + yPixels + TopBottomMargin >= SlicerFile.Resolution.Height)
                {
                    return false; // Insufficient size
                }

                int halfDiameterX = xPixels / 2;
                int halfDiameterY = yPixels / 2;

                switch (Shape)
                {
                    case Shapes.Circle:
                        CvInvoke.Circle(layer, new Point(currentX + halfDiameterX, currentY + halfDiameterY), halfDiameterX, EmguCvExtensions.WhiteColor, -1, lineType);
                        break;
                    case Shapes.Square:
                        CvInvoke.Rectangle(layer, new Rectangle(currentX, currentY, xPixels, yPixels), EmguCvExtensions.WhiteColor, -1, lineType);
                        break;
                }

                partCenterText = new Point(currentX + halfDiameterX - 60, currentY + halfDiameterY + 10);

                currentX += xPixels + PartMargin;
            }

            pointTextList.Add(new KeyValuePair<Point, string>(partCenterText, step > 0 ? $"+{step:F2}" : $"{step:F2}"));

            return true;
        }

        if (!FuseParts && OutputSameDiameterPart)
        {
            addPart(0);
        }

        for (int i = 1; i <= MaleThinnerModels; i++)
        {
            var step = _maleThinnerOffset + _maleThinnerStep * i;
            if (!addPart(step)) break;
        }

        for (int i = 1; i <= MaleThickerModels; i++)
        {
            var step = _maleThickerOffset + _maleThickerStep * i;
            if (!addPart(step)) break;
        }

        Parallel.For(0, layers.Length, CoreSettings.ParallelOptions, layerIndex =>
            //for (var i = 0; i < layers.Length; i++)
        {
            layers[layerIndex] = layer.Clone();
        });

        if (ErodeBottomIterations > 0)
        {
            Parallel.For(0, BottomLayers, CoreSettings.ParallelOptions, layerIndex =>
            {
                CvInvoke.Erode(layers[layerIndex], layers[layerIndex], kernel, EmguCvExtensions.AnchorCenter, ErodeBottomIterations, BorderType.Reflect101, default);
            });
        }

        if (ChamferLayers > 0)
        {
            Parallel.For(0, ChamferLayers, CoreSettings.ParallelOptions, layerIndexOffset =>
            {
                var iteration = ChamferLayers - layerIndexOffset;
                CvInvoke.Erode(layers[layerIndexOffset], layers[layerIndexOffset], kernel, EmguCvExtensions.AnchorCenter, iteration, BorderType.Reflect101, default);

                var layerIndex = layers.Length - 1 - layerIndexOffset;
                CvInvoke.Erode(layers[layerIndex], layers[layerIndex], kernel, EmguCvExtensions.AnchorCenter, iteration, BorderType.Reflect101, default);
            });
            /*byte iterations = ChamferLayers;
            var layerIndex = 0;
            for (; layerIndex < LayerCount && iterations > 0; layerIndex++)
            {
                CvInvoke.Erode(layers[layerIndex], layers[layerIndex], kernel, anchor, iterations--, BorderType.Reflect101, default);
            }

            iterations = ChamferLayers;
            for (int i = (int) (LayerCount - 1); i >= 0 && i > layerIndex && iterations > 0; i--)
            {
                CvInvoke.Erode(layers[i], layers[i], kernel, anchor, iterations--, BorderType.Reflect101, default);
            }*/
        }

        Parallel.For(Math.Max(0u, LayerCount - 15), LayerCount, CoreSettings.ParallelOptions, layerIndex =>
        {
            foreach (var keyValuePair in pointTextList)
            {
                CvInvoke.PutText(layers[layerIndex], keyValuePair.Value, keyValuePair.Key, fontFace, fontScale, EmguCvExtensions.BlackColor, fontThickness, lineType);
            }
        });

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
        CvInvoke.PutText(thumbnail, "Tolerance Cal.", new Point(xSpacing, ySpacing * 2), fontFace, fontScale, new MCvScalar(0, 255, 255), fontThickness);
        CvInvoke.PutText(thumbnail, $"{Microns}um @ {BottomExposure}s/{NormalExposure}s", new Point(xSpacing, ySpacing * 3), fontFace, fontScale, EmguCvExtensions.WhiteColor, fontThickness);
        CvInvoke.PutText(thumbnail, $"Objects: {OutputObjects}", new Point(xSpacing, ySpacing * 4), fontFace, fontScale, EmguCvExtensions.WhiteColor, fontThickness);

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

        Parallel.For(0, LayerCount, CoreSettings.GetParallelOptions(progress), layerIndex =>
        {
            progress.PauseIfRequested();
            newLayers[layerIndex] = new Layer((uint)layerIndex, layers[layerIndex], SlicerFile) {IsModified = true};
            layers[layerIndex].Dispose();
            progress.LockAndIncrement();
        });

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

        var moveOp = new OperationMove(SlicerFile);
        moveOp.Execute(progress);


        return !progress.Token.IsCancellationRequested;
    }

    #endregion
}
