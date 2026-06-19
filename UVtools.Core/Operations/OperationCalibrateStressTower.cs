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
public sealed partial class OperationCalibrateStressTower : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Members
    private decimal _displayWidth;
    private decimal _displayHeight;
    private decimal _layerHeight;
    private decimal _bottomExposure;
    private decimal _normalExposure;

    #endregion

    #region Overrides

    public override bool CanROI => false;

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;
    public override string IconClass => "ChessRook";
    public override string Title => "Stress tower";
    public override string Description =>
        "Generates a stress tower to test the printer capabilities.\n" +
        "Note: The current opened file will be overwritten with this test, use a dummy or a not needed file.";

    public override string ConfirmationText =>
        $"generate the stress tower?";

    public override string ProgressTitle =>
        $"Generating the stress tower";

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

        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"[Layer Height: {_layerHeight}] " +
                     $"[Bottom layers: {BottomLayers}] " +
                     $"[Exposure: {_bottomExposure}/{_normalExposure}] " +
                     $"[Base: H:{BaseHeight} D:{BaseDiameter}] " +
                     $"[Ceil: {CeilHeight}] [Body: {BodyHeight}] " +
                     $"[Chamfer: {ChamferLayers}] " +
                     $"[Spirals: {Spirals} Dir: {SpiralDirection} D:{SpiralDiameter} Angle: {SpiralAngleStepPerLayer}º]" +
                     $"[AA: {EnableAntiAliasing}] [Mirror: {MirrorOutput}]";
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    #endregion

    #region Constructor

    public OperationCalibrateStressTower() { }

    public OperationCalibrateStressTower(FileFormat slicerFile) : base(slicerFile)
    { }

    public override void InitWithSlicerFile()
    {
        base.InitWithSlicerFile();
        if(_layerHeight <= 0) _layerHeight = (decimal)SlicerFile.LayerHeight;
        if(BottomLayers <= 0) BottomLayers = SlicerFile.BottomLayerCount;
        if(_bottomExposure <= 0) _bottomExposure = (decimal)SlicerFile.BottomExposureTime;
        if(_normalExposure <= 0) _normalExposure = (decimal)SlicerFile.ExposureTime;
        MirrorOutput = SlicerFile.DisplayMirror != FlipDirection.None;

        if (SlicerFile.DisplayWidth > 0)
            DisplayWidth = (decimal)SlicerFile.DisplayWidth;
        if (SlicerFile.DisplayHeight > 0)
            DisplayHeight = (decimal)SlicerFile.DisplayHeight;
    }

    #endregion

    #region Properties

    public decimal DisplayWidth
    {
        get => _displayWidth;
        set
        {
            if(!SetProperty(ref _displayWidth, FileFormat.RoundDisplaySize(value))) return;
        }
    }

    public decimal DisplayHeight
    {
        get => _displayHeight;
        set
        {
            if(!SetProperty(ref _displayHeight, FileFormat.RoundDisplaySize(value))) return;
        }
    }

    public decimal LayerHeight
    {
        get => _layerHeight;
        set
        {
            if(!SetProperty(ref _layerHeight, Layer.RoundHeight(value))) return;
            OnPropertyChanged(nameof(BottomLayersMM));
            OnPropertyChanged(nameof(LayerCount));
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

    public uint LayerCount => (uint)((BaseHeight + BodyHeight + CeilHeight) / LayerHeight);

    public decimal TotalHeight => BaseHeight + BodyHeight + CeilHeight;

    [ObservableProperty]
    public partial decimal BaseDiameter { get; set; } = 30;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalHeight))]
    public partial decimal BaseHeight { get; set; } = 3;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalHeight))]
    public partial decimal BodyHeight { get; set; } = 50;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalHeight))]
    public partial decimal CeilHeight { get; set; } = 3;

    [ObservableProperty]
    public partial byte ChamferLayers { get; set; } = 6;

    [ObservableProperty]
    public partial bool EnableAntiAliasing { get; set; } = true;

    [ObservableProperty]
    public partial bool MirrorOutput { get; set; }

    [ObservableProperty]
    public partial byte Spirals { get; set; } = 2;

    [ObservableProperty]
    public partial decimal SpiralDiameter { get; set; } = 2;

    [ObservableProperty]
    public partial SpiralDirections SpiralDirection { get; set; } = SpiralDirections.Both;

    [ObservableProperty]
    public partial decimal SpiralAngleStepPerLayer { get; set; } = 1;

    #endregion

    #region Enums

    public enum SpiralDirections : byte
    {
        Clockwise,
        Alternate,
        Both
    }

    public static Array SpiralDirectionsItems => Enum.GetValues(typeof(SpiralDirections));
    #endregion

    #region Equality

    private bool Equals(OperationCalibrateStressTower other)
    {
        return _layerHeight == other._layerHeight && BottomLayers == other.BottomLayers && _bottomExposure == other._bottomExposure && _normalExposure == other._normalExposure && BaseDiameter == other.BaseDiameter && BaseHeight == other.BaseHeight && BodyHeight == other.BodyHeight && CeilHeight == other.CeilHeight && ChamferLayers == other.ChamferLayers && EnableAntiAliasing == other.EnableAntiAliasing && MirrorOutput == other.MirrorOutput && Spirals == other.Spirals && SpiralDiameter == other.SpiralDiameter && SpiralDirection == other.SpiralDirection && SpiralAngleStepPerLayer == other.SpiralAngleStepPerLayer;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationCalibrateStressTower other && Equals(other);
    }

    #endregion

    #region Methods
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
        CvInvoke.PutText(thumbnail, "Stress Tower", new Point(xSpacing, ySpacing * 2), fontFace, fontScale, new MCvScalar(0, 255, 255), fontThickness);
        CvInvoke.PutText(thumbnail, $"{Microns}um @ {BottomExposure}s/{NormalExposure}s", new Point(xSpacing, ySpacing * 3), fontFace, fontScale, EmguCvExtensions.WhiteColor, fontThickness);
        CvInvoke.PutText(thumbnail, $"{Spirals} Spirals @ {SpiralAngleStepPerLayer}deg", new Point(xSpacing, ySpacing * 4), fontFace, fontScale, EmguCvExtensions.WhiteColor, fontThickness);
        return thumbnail;
    }

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        progress.ItemCount = LayerCount;

        Slicer.Slicer slicer = new(SlicerFile.Resolution, new SizeF((float)DisplayWidth, (float)DisplayHeight));
        Point center = new(SlicerFile.Resolution.Width / 2, SlicerFile.Resolution.Height / 2);
        uint baseRadius = slicer.PixelsFromMillimeters(BaseDiameter) / 2;
        uint baseLayers = (ushort)(BaseHeight / _layerHeight);
        uint bodyLayers = (ushort)(BodyHeight / _layerHeight);
        uint spiralLayers = (uint)(SpiralDiameter / _layerHeight);
        uint ceilLayers = (ushort)(CeilHeight / _layerHeight);

        uint basePlusBodyLayers = baseLayers + bodyLayers;

        decimal spiralOffsetAngle = 360m / Spirals;
        uint spiralRadius = slicer.PixelsFromMillimeters(SpiralDiameter) / 2;

        var flip = SlicerFile.DisplayMirror;
        if (flip == FlipDirection.None) flip = FlipDirection.Horizontally;

        /*const FontFace fontFace = FontFace.HersheyDuplex;
        const double fontScale = 1;
        const byte fontThickness = 2;
        LineType lineType = EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected;

        var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), EmguCvExtensions.AnchorCenter);*/
        SlicerFile.Init(LayerCount);

        Parallel.For(0, LayerCount, CoreSettings.GetParallelOptions(progress), layerIndex =>
        {
            progress.PauseIfRequested();
            using var mat = SlicerFile.CreateMat();

            if (layerIndex < baseLayers)
            {
                int chamferOffset = (int)Math.Max(0, ChamferLayers - layerIndex);
                CvInvoke.Circle(mat, center, (int)baseRadius - chamferOffset, EmguCvExtensions.WhiteColor, -1, EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
            }
            else if (layerIndex < basePlusBodyLayers)
            {
                decimal angle = (layerIndex * SpiralAngleStepPerLayer) % 360m;

                for (byte spiral = 0; spiral < Spirals; spiral++)
                {
                    decimal spiralAngle = (spiralOffsetAngle * spiral + angle) % 360;
                    if (SpiralDirection == SpiralDirections.Alternate && spiral % 2 == 0)
                    {
                        spiralAngle = -spiralAngle;
                    }
                    Point location = center with { X = (int)(center.X - baseRadius + spiralRadius) };
                    var locationCW = location.Rotate((double)spiralAngle, center);
                    var locationCCW = location.Rotate((double)-spiralAngle, center);

                    uint maxLayer = (uint)Math.Min(layerIndex + spiralLayers, baseLayers + bodyLayers);

                    //for (uint spiralLayerIndex = (uint)layerIndex; spiralLayerIndex < maxLayer; spiralLayerIndex++)
                    //{

                    CvInvoke.Circle(mat, locationCW, (int)spiralRadius, EmguCvExtensions.WhiteColor, -1, EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                    if (SpiralDirection == SpiralDirections.Both)
                    {
                        spiralAngle = -spiralAngle;
                        CvInvoke.Circle(mat, locationCCW, (int)spiralRadius, EmguCvExtensions.WhiteColor, -1, EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                    }
                    //}
                }
            }
            else
            {
                CvInvoke.Circle(mat, center, (int)baseRadius, EmguCvExtensions.WhiteColor, -1, EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
            }

            if (MirrorOutput) CvInvoke.Flip(mat, mat, (FlipType)flip);

            SlicerFile[layerIndex] = new Layer((uint)layerIndex, mat, SlicerFile);
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
        }, true);

        return !progress.Token.IsCancellationRequested;
    }

    #endregion
}