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
using Emgu.CV.Util;
using System;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using EmguExtensions;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using PointExtensions = UVtools.Core.Extensions.PointExtensions;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public sealed partial class OperationCalibrateGrayscale : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Members
    private decimal _layerHeight;
    private decimal _bottomExposure;
    private decimal _normalExposure;

    #endregion

    #region Overrides

    public override bool CanROI => false;

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;
    public override string IconClass => "ChartPie";
    public override string Title => "Grayscale";
    public override string Description =>
        "Generates test models with various strategies and increments to verify the LED power against the grayscale levels.\n" +
        "You must repeat this test when change any of the following: printer, LEDs, resin and exposure times.\n" +
        "Note: The current opened file will be overwritten with this test, use a dummy or a not needed file.";

    public override string ConfirmationText =>
        $"generate the grayscale test?";

    public override string ProgressTitle =>
        $"Generating the grayscale test";

    public override string ProgressAction => "Generated";

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        if (StartBrightness > EndBrightness)
        {
            sb.AppendLine("Start brightness must be lower or equal to end brightness.");
        }
        if (Divisions <= 0)
        {
            sb.AppendLine("No divisions to output.");
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"[Layer Height: {_layerHeight}] " +
                     $"[Layers: {BottomLayers}/{InterfaceLayers}/{NormalLayers}] " +
                     $"[Exposure: {_bottomExposure}/{_normalExposure}] " +
                     $"[Margin: {OuterMargin}/{InnerMargin}] " +
                     $"[B: {StartBrightness}-{EndBrightness} S{BrightnessSteps}] " +
                     $"[AA: {EnableAntiAliasing}] [Mirror: {MirrorOutput}]";
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    #endregion

    #region Constructor

    public OperationCalibrateGrayscale() { }

    public OperationCalibrateGrayscale(FileFormat slicerFile) : base(slicerFile)
    { }

    public override void InitWithSlicerFile()
    {
        base.InitWithSlicerFile();
        if(_layerHeight <= 0) _layerHeight = (decimal)SlicerFile.LayerHeight;
        if(BottomLayers <= 0) BottomLayers = SlicerFile.BottomLayerCount;
        if(_bottomExposure <= 0) _bottomExposure = (decimal)SlicerFile.BottomExposureTime;
        if(_normalExposure <= 0) _normalExposure = (decimal)SlicerFile.ExposureTime;
        MirrorOutput = SlicerFile.DisplayMirror != FlipDirection.None;
    }

    #endregion

    #region Properties

    public decimal LayerHeight
    {
        get => _layerHeight;
        set
        {
            if(!SetProperty(ref _layerHeight, Layer.RoundHeight(value))) return;
            OnPropertyChanged(nameof(BottomHeight));
            OnPropertyChanged(nameof(InterfaceHeight));
            OnPropertyChanged(nameof(NormalHeight));
            OnPropertyChanged(nameof(TotalHeight));
        }
    }

    public ushort Microns => (ushort) (LayerHeight * 1000);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BottomHeight))]
    [NotifyPropertyChangedFor(nameof(TotalHeight))]
    [NotifyPropertyChangedFor(nameof(LayerCount))]
    public partial ushort BottomLayers { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InterfaceHeight))]
    [NotifyPropertyChangedFor(nameof(TotalHeight))]
    [NotifyPropertyChangedFor(nameof(LayerCount))]
    public partial ushort InterfaceLayers { get; set; } = 20;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NormalHeight))]
    [NotifyPropertyChangedFor(nameof(TotalHeight))]
    [NotifyPropertyChangedFor(nameof(LayerCount))]
    public partial ushort NormalLayers { get; set; } = 20;

    public uint LayerCount => (uint) (BottomLayers + InterfaceLayers + NormalLayers);

    public decimal BottomHeight => Layer.RoundHeight(LayerHeight * BottomLayers);
    public decimal InterfaceHeight => Layer.RoundHeight(LayerHeight * InterfaceLayers);
    public decimal NormalHeight => Layer.RoundHeight(LayerHeight * NormalLayers);

    public decimal TotalHeight => BottomHeight + InterfaceHeight + NormalHeight;

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
    public partial ushort OuterMargin { get; set; } = 200;

    [ObservableProperty]
    public partial ushort InnerMargin { get; set; } = 50;

    [ObservableProperty]
    public partial bool EnableAntiAliasing { get; set; } = false;

    [ObservableProperty]
    public partial bool MirrorOutput { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StartBrightnessPercent))]
    [NotifyPropertyChangedFor(nameof(Divisions))]
    [NotifyPropertyChangedFor(nameof(AngleStep))]
    public partial byte StartBrightness { get; set; } = 175;

    public float StartBrightnessPercent => MathF.Round(StartBrightness * 100 / 255.0f, 2);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EndBrightnessPercent))]
    [NotifyPropertyChangedFor(nameof(Divisions))]
    [NotifyPropertyChangedFor(nameof(AngleStep))]
    public partial byte EndBrightness { get; set; } = 255;

    public float EndBrightnessPercent => MathF.Round(EndBrightness * 100 / 255.0f, 2);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Divisions))]
    [NotifyPropertyChangedFor(nameof(AngleStep))]
    public partial byte BrightnessSteps { get; set; } = 10;

    public int Divisions => (int)((EndBrightness - StartBrightness) / (decimal)BrightnessSteps) + 1;
    public float AngleStep => 360f / Divisions;

    [ObservableProperty]
    public partial bool EnableCenterHoleRelief { get; set; } = true;

    [ObservableProperty]
    public partial ushort CenterHoleDiameter { get; set; } = 200;

    [ObservableProperty]
    public partial bool TextEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool ConvertBrightnessToExposureTime { get; set; }

    [ObservableProperty]
    public partial bool EnableLineDivisions { get; set; } = true;

    [ObservableProperty]
    public partial byte LineDivisionThickness { get; set; } = 30;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineDivisionBrightnessPercent))]
    public partial byte LineDivisionBrightness { get; set; } = 255;

    public float LineDivisionBrightnessPercent => MathF.Round(LineDivisionBrightness * 100 / 255.0f, 2);

    [ObservableProperty]
    public partial short TextXOffset { get; set; }

    #endregion

    #region Equality

    private bool Equals(OperationCalibrateGrayscale other)
    {
        return _layerHeight == other._layerHeight && BottomLayers == other.BottomLayers && InterfaceLayers == other.InterfaceLayers && NormalLayers == other.NormalLayers && _bottomExposure == other._bottomExposure && _normalExposure == other._normalExposure && OuterMargin == other.OuterMargin && InnerMargin == other.InnerMargin && EnableAntiAliasing == other.EnableAntiAliasing && MirrorOutput == other.MirrorOutput && StartBrightness == other.StartBrightness && EndBrightness == other.EndBrightness && BrightnessSteps == other.BrightnessSteps && EnableCenterHoleRelief == other.EnableCenterHoleRelief && CenterHoleDiameter == other.CenterHoleDiameter && TextEnabled == other.TextEnabled && ConvertBrightnessToExposureTime == other.ConvertBrightnessToExposureTime && EnableLineDivisions == other.EnableLineDivisions && LineDivisionThickness == other.LineDivisionThickness && LineDivisionBrightness == other.LineDivisionBrightness && TextXOffset == other.TextXOffset;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationCalibrateGrayscale other && Equals(other);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets the bottom and normal layers, 0 = bottom | 1 = normal
    /// </summary>
    /// <returns></returns>
    public Mat[] GetLayers()
    {
        Mat[] layers = new Mat[3];

        layers[0] = EmguCvExtensions.InitMat(SlicerFile.Resolution);

        int radius = Math.Max(100, Math.Min(SlicerFile.Resolution.Width, SlicerFile.Resolution.Height) - OuterMargin * 2) / 2 ;
        Point center = new(SlicerFile.Resolution.Width / 2, SlicerFile.Resolution.Height / 2);
        int innerRadius = Math.Max(100, radius - InnerMargin);
        double topLineLength = 0;

        LineType lineType = EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected;

        CvInvoke.Circle(layers[0], center, radius, EmguCvExtensions.WhiteColor, -1, lineType);
        layers[1] = layers[0].Clone();
        layers[2] = layers[0].Clone();



        int i = 0;
        for (ushort brightness = StartBrightness; brightness <= EndBrightness; brightness += BrightnessSteps)
        {
            var radians = new float[2];
            var degrees = new SizeF[2];
            for (int n = 0; n < 2; n++)
            {
                radians[n] = -AngleStep * (i + n);
                degrees[n] = new SizeF((float) Math.Cos(radians[n] * Math.PI / 180), (float) Math.Sin(radians[n] * Math.PI / 180));
            }

            Point[] points = new Point[3];
            points[0] = center;
            points[1] = new(center.X + (int) (innerRadius * degrees[0].Width), center.Y + (int) (innerRadius * degrees[0].Height));
            points[2] = new(center.X + (int) (innerRadius * degrees[1].Width), center.Y + (int) (innerRadius * degrees[1].Height));
            using var vec = new VectorOfPoint(points);

            if (topLineLength == 0) topLineLength = PointExtensions.FindLength(points[1], points[2]);

            CvInvoke.FillPoly(layers[2], vec, new MCvScalar(brightness), lineType);

            if (EnableLineDivisions && LineDivisionThickness > 0)
            {
                CvInvoke.Polylines(layers[2], vec, false, new MCvScalar(LineDivisionBrightness), LineDivisionThickness, lineType);
            }

            i++;
        }


        FontFace fontFace = FontFace.HersheyDuplex;
        double fontScale = 2;
        int fontThickness = 5;

        if (TextEnabled)
        {
            Point fontPoint = new((int)(center.X + radius / 2.5f + TextXOffset), (int)(center.Y + AngleStep / 1.5));

            var halfAngleStep = AngleStep / 2;
            var rotatedAngle = halfAngleStep;


            layers[2].RotateFromCenter(halfAngleStep);
            for (ushort brightness = StartBrightness; brightness <= EndBrightness; brightness += BrightnessSteps)
            {
                var text = brightness.ToString();
                if (ConvertBrightnessToExposureTime)
                {
                    text = $"{Math.Round(brightness * _normalExposure / byte.MaxValue, 2)}s";
                }

                CvInvoke.PutText(layers[2], text, fontPoint, fontFace, fontScale, EmguCvExtensions.BlackColor, fontThickness, lineType);
                rotatedAngle += AngleStep;
                layers[2].RotateFromCenter(AngleStep);
            }

            layers[2].RotateFromCenter(-rotatedAngle);
        }

        if (EnableCenterHoleRelief && CenterHoleDiameter > 1)
        {
            var holeRadius = Math.Min(radius, CenterHoleDiameter) / 2;
            if (InnerMargin > 0)
            {
                CvInvoke.Circle(layers[2], center, holeRadius + InnerMargin, EmguCvExtensions.WhiteColor, -1, lineType);
            }

            foreach (var layer in layers)
            {
                CvInvoke.Circle(layer, center, holeRadius, EmguCvExtensions.BlackColor, -1, lineType);
            }
        }

        fontScale = 1.5;
        fontThickness = 3;
        CvInvoke.PutText(layers[0], $"{Microns}um at {_bottomExposure}s/{_normalExposure}s",
            new Point(center.X - radius / 2, center.Y + radius / 2 +40),
            fontFace, fontScale, EmguCvExtensions.BlackColor, fontThickness, lineType, true);

        CvInvoke.PutText(layers[0], $"{StartBrightness}-{EndBrightness} S:{BrightnessSteps}",
            new Point(center.X - radius / 2, center.Y + radius / 2 - 40),
            fontFace, fontScale, EmguCvExtensions.BlackColor, fontThickness, lineType, true);

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
        CvInvoke.PutText(thumbnail, "Grayscale Cal.", new Point(xSpacing, ySpacing * 2), fontFace, fontScale, new MCvScalar(0, 255, 255), fontThickness);
        CvInvoke.PutText(thumbnail, $"{Microns}um @ {BottomExposure}s/{NormalExposure}s", new Point(xSpacing, ySpacing * 3), fontFace, fontScale, EmguCvExtensions.WhiteColor, fontThickness);
        CvInvoke.PutText(thumbnail, $"Divs:{Divisions} Angle:{AngleStep}", new Point(xSpacing, ySpacing * 4), fontFace, fontScale, EmguCvExtensions.WhiteColor, fontThickness);

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
        var interfaceLayer = InterfaceLayers > 0 ? new Layer(0, layers[1], SlicerFile)
        {
            IsModified = true
        } : null;
        var layer = new Layer(0, layers[2], SlicerFile)
        {
            IsModified = true
        };

        uint layerIndex = 0;
        for (uint i = 0; i < BottomLayers; i++)
        {
            progress.PauseOrCancelIfRequested();
            newLayers[layerIndex] = bottomLayer.Clone();
            progress++;
            layerIndex++;
        }

        for (uint i = 0; i < InterfaceLayers; i++)
        {
            progress.PauseOrCancelIfRequested();
            newLayers[layerIndex] = interfaceLayer!.Clone();
            progress++;
            layerIndex++;
        }


        for (uint i = 0; i < NormalLayers; i++)
        {
            progress.PauseOrCancelIfRequested();
            newLayers[layerIndex] = layer.Clone();
            progress++;
            layerIndex++;
        }

        foreach (var mat in layers)
        {
            mat?.Dispose();
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