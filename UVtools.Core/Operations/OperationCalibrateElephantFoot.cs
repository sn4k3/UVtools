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
using EmguExtensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public sealed partial class OperationCalibrateElephantFoot : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Members
    private decimal _layerHeight;
    private decimal _bottomExposure;
    private decimal _normalExposure;
    private decimal _partScale = 1;

    #endregion

    #region Overrides

    public override bool CanROI => false;

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;
    public override string IconClass => "Elephant";
    public override string Title => "Elephant foot";
    public override string Description =>
        "Generates test models with various strategies and increments to verify the best method/values to remove the elephant foot.\n" +
        "Elephant foot is removed when bottom layers are flush and aligned with normal layers.\n" +
        "You must repeat this test when change any of the following: printer, LEDs, resin and exposure times.\n" +
        "Note: The current opened file will be overwritten with this test, use a dummy or a not needed file.";

    public override string ConfirmationText =>
        $"generate the elephant foot test?";

    public override string ProgressTitle =>
        $"Generating the elephant foot test";

    public override string ProgressAction => "Generated";

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        if (IsErodeEnabled && ErodeStartIteration > ErodeEndIteration)
        {
            sb.AppendLine("Erode start iterations can't be higher than end iterations.");
        }

        if (IsDimmingEnabled)
        {
            if (DimmingStartBrightness > DimmingEndBrightness)
            {
                sb.AppendLine("Wall dimming start brightness can't be higher than end brightness.");
            }

            if (SlicerFile is {IsAntiAliasingEmulated: true, AntiAliasing: < 2})
            {
                sb.AppendLine($"With a emulated anti-aliasing of {SlicerFile.AntiAliasing}x, is not possible to run the dimming method, use the erode instead.");
                sb.AppendLine("As alternative, re-slice the file with a AntiAliasing level greater than 1 and run this tool again.");
            }
        }


        if (ObjectCount <= 0)
        {
            sb.AppendLine("No objects to output, please adjust the settings.");
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"[Layer Height: {_layerHeight}] " +
                     $"[Layers: {BottomLayers}/{NormalLayers}] " +
                     $"[Exposure: {_bottomExposure}/{_normalExposure}] " +
                     $"[Extrude: {ExtrudeText} {TextHeight}mm]" +
                     $"[Scale: {_partScale}] [Margin: {Margin}] [ORI: {OutputOriginalPart}]" +
                     $"[E: {ErodeStartIteration}-{ErodeEndIteration} S{ErodeIterationSteps}] " +
                     $"[D: W{DimmingWallThickness} B{DimmingStartBrightness}-{DimmingEndBrightness} S{DimmingBrightnessSteps}] " +
                     $"[AA: {EnableAntiAliasing}] [Mirror: {MirrorOutput}]";
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
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
            OnPropertyChanged(nameof(NormalHeight));
            OnPropertyChanged(nameof(TotalHeight));
        }
    }

    public ushort Microns => (ushort) (LayerHeight * 1000);

    [ObservableProperty]
    public partial bool SyncLayers { get; set; }

    partial void OnSyncLayersChanged(bool value)
    {
        if (value) NormalLayers = BottomLayers;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BottomHeight))]
    [NotifyPropertyChangedFor(nameof(TotalHeight))]
    [NotifyPropertyChangedFor(nameof(LayerCount))]
    public partial ushort BottomLayers { get; set; }

    partial void OnBottomLayersChanged(ushort value)
    {
        if (SyncLayers) NormalLayers = value;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NormalHeight))]
    [NotifyPropertyChangedFor(nameof(TotalHeight))]
    [NotifyPropertyChangedFor(nameof(LayerCount))]
    public partial ushort NormalLayers { get; set; }

    partial void OnNormalLayersChanged(ushort value)
    {
        if (SyncLayers) BottomLayers = value;
    }

    public uint LayerCount => (uint)(BottomLayers + NormalLayers + (ExtrudeText ? Math.Floor(TextHeight / _layerHeight) : 0));

    public decimal BottomHeight => Layer.RoundHeight(LayerHeight * BottomLayers);
    public decimal NormalHeight => Layer.RoundHeight(LayerHeight * NormalLayers);

    public decimal TotalHeight => BottomHeight + NormalHeight;

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

    public decimal PartScale
    {
        get => _partScale;
        set => SetProperty(ref _partScale, Math.Round(value, 2));
    }

    [ObservableProperty]
    public partial byte Margin { get; set; } = 30;

    [ObservableProperty]
    public partial bool ExtrudeText { get; set; } = true;

    [ObservableProperty]
    public partial decimal TextHeight { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ObjectCount))]
    public partial bool OutputOriginalPart { get; set; } = true;

    [ObservableProperty]
    public partial bool EnableAntiAliasing { get; set; } = true;

    [ObservableProperty]
    public partial bool MirrorOutput { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ErodeObjects))]
    [NotifyPropertyChangedFor(nameof(ObjectCount))]
    public partial bool IsErodeEnabled { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ErodeObjects))]
    [NotifyPropertyChangedFor(nameof(ObjectCount))]
    public partial byte ErodeStartIteration { get; set; } = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ErodeObjects))]
    [NotifyPropertyChangedFor(nameof(ObjectCount))]
    public partial byte ErodeEndIteration { get; set; } = 6;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ErodeObjects))]
    [NotifyPropertyChangedFor(nameof(ObjectCount))]
    public partial byte ErodeIterationSteps { get; set; } = 1;

    public KernelConfiguration ErodeKernel { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DimmingObjects))]
    [NotifyPropertyChangedFor(nameof(ObjectCount))]
    public partial bool IsDimmingEnabled { get; set; } = true;

    [ObservableProperty]
    public partial byte DimmingWallThickness { get; set; } = 20;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DimmingStartBrightnessPercent))]
    [NotifyPropertyChangedFor(nameof(DimmingObjects))]
    [NotifyPropertyChangedFor(nameof(ObjectCount))]
    public partial byte DimmingStartBrightness { get; set; } = 140;

    public float DimmingStartBrightnessPercent => MathF.Round(DimmingStartBrightness * 100 / 255.0f, 2);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DimmingEndBrightnessPercent))]
    [NotifyPropertyChangedFor(nameof(DimmingObjects))]
    [NotifyPropertyChangedFor(nameof(ObjectCount))]
    public partial byte DimmingEndBrightness { get; set; } = 200;

    public float DimmingEndBrightnessPercent => MathF.Round(DimmingEndBrightness * 100 / 255.0f, 2);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DimmingObjects))]
    [NotifyPropertyChangedFor(nameof(ObjectCount))]
    public partial byte DimmingBrightnessSteps { get; set; } = 20;

    public KernelConfiguration DimmingKernel { get; set; } = new();

    public uint ErodeObjects => IsErodeEnabled ?
        (uint)((ErodeEndIteration - ErodeStartIteration) / (decimal)ErodeIterationSteps) + 1
        : 0;

    public uint DimmingObjects => IsDimmingEnabled ?
        (uint)((DimmingEndBrightness - DimmingStartBrightness) / (decimal) DimmingBrightnessSteps) + 1
        : 0;

    public uint ObjectCount => (OutputOriginalPart ? 1u : 0) + ErodeObjects + DimmingObjects;

    #endregion

    #region Constructor

    public OperationCalibrateElephantFoot() { }

    public OperationCalibrateElephantFoot(FileFormat slicerFile) : base(slicerFile)
    { }

    public override void InitWithSlicerFile()
    {
        base.InitWithSlicerFile();
        if(_layerHeight <= 0) _layerHeight = (decimal)SlicerFile.LayerHeight;
        if(_bottomExposure <= 0) _bottomExposure = (decimal)SlicerFile.BottomExposureTime;
        if(_normalExposure <= 0) _normalExposure = (decimal)SlicerFile.ExposureTime;
        if (BottomLayers <= 0) BottomLayers = (ushort) Slicer.Slicer.MillimetersToLayers(1M, _layerHeight);
        if (NormalLayers <= 0) NormalLayers = (ushort) Slicer.Slicer.MillimetersToLayers(3.5M, _layerHeight);

        MirrorOutput = SlicerFile.DisplayMirror != FlipDirection.None;
    }

    #endregion

    #region Equality

    private bool Equals(OperationCalibrateElephantFoot other)
    {
        return _layerHeight == other._layerHeight && SyncLayers == other.SyncLayers && BottomLayers == other.BottomLayers && NormalLayers == other.NormalLayers && _bottomExposure == other._bottomExposure && _normalExposure == other._normalExposure && _partScale == other._partScale && Margin == other.Margin && ExtrudeText == other.ExtrudeText && TextHeight == other.TextHeight && EnableAntiAliasing == other.EnableAntiAliasing && MirrorOutput == other.MirrorOutput && IsErodeEnabled == other.IsErodeEnabled && ErodeStartIteration == other.ErodeStartIteration && ErodeEndIteration == other.ErodeEndIteration && ErodeIterationSteps == other.ErodeIterationSteps && IsDimmingEnabled == other.IsDimmingEnabled && DimmingWallThickness == other.DimmingWallThickness && DimmingStartBrightness == other.DimmingStartBrightness && DimmingEndBrightness == other.DimmingEndBrightness && DimmingBrightnessSteps == other.DimmingBrightnessSteps && OutputOriginalPart == other.OutputOriginalPart;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationCalibrateElephantFoot other && Equals(other);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets the bottom and normal layers, 0 = bottom | 1 = normal
    /// </summary>
    /// <returns></returns>
    public Mat[] GetLayers()
    {
        var layers = new Mat[3];

        layers[0] = EmguCvExtensions.InitMat(SlicerFile.Resolution);
        layers[2] = layers[0].Clone();
        LineType lineType = EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected;
        int length = (int) (250 * _partScale);
        int triangleLength = (int) (50 * _partScale);
        const byte startX = 2;
        const byte startY = 2;
        int x = startX;
        int y = startY;

        int maxX = x;
        int maxY = y;

        var pointList = new List<Point> { new(x, y) };

        void addPoint()
        {
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
            pointList.Add(new Point(x, y));
        }

        x += length;
        addPoint();

        x -= triangleLength;
        y += triangleLength;
        addPoint();

        y += triangleLength;
        addPoint();

        x += triangleLength;
        y += triangleLength;
        addPoint();
        x -= triangleLength;
        y += triangleLength;
        addPoint();

        x += triangleLength;
        y += triangleLength;
        addPoint();
        x -= triangleLength;
        y += triangleLength;
        addPoint();

        x += triangleLength;
        addPoint();
        y += triangleLength;
        addPoint();
        x -= triangleLength;
        y += triangleLength;
        addPoint();

        x = startX;
        addPoint();


        int ellipseHeight = (int) (50 * _partScale);

        maxY += ellipseHeight;
        using Mat shape = EmguCvExtensions.InitMat(new Size(maxX + startX, maxY + startY));
        CvInvoke.FillPoly(shape, new VectorOfPoint(pointList.ToArray()), EmguCvExtensions.WhiteColor, lineType);
        shape.DrawCircle(new Point(0, 0), SlicerFile.PixelsToNormalizedPitch(length / 4), EmguCvExtensions.BlackColor, -1, lineType);
        CvInvoke.Ellipse(shape, new Point(maxX / 2, maxY - ellipseHeight), new Size(maxX / 3, ellipseHeight), 0, 0, 360,
            EmguCvExtensions.WhiteColor, -1, lineType);
        shape.DrawCircle(new Point(length / 2, (int) (maxY - 100 * _partScale)), SlicerFile.PixelsToNormalizedPitch(length / 5), EmguCvExtensions.BlackColor, -1, lineType);

        int currentX = 0;
        int currentY = 0;

        maxX = 0;

        const FontFace font = FontFace.HersheyDuplex;
        int fontMargin = (int)(42 * _partScale);
        int fontStartX = (int) (30 * _partScale);
        int fontStartY = length / 4 + fontMargin;
        double fontScale = 1.3 * (double) _partScale;
        int fontThickness = (int) (3 * _partScale);


        void addText(Mat mat, ushort number, params string[]? text)
        {
            var color = ExtrudeText ? EmguCvExtensions.WhiteColor : EmguCvExtensions.BlackColor;
            CvInvoke.PutText(mat, number.ToString(), new Point((int) (100 * _partScale), (int) (55 * _partScale)), font, 1.5 * (double) _partScale, color, (int) (4 * _partScale), lineType);
            CvInvoke.PutText(mat, "UVtools EP", new Point(fontStartX, fontStartY), font, 0.8 * (double) _partScale, color, (int) (2 * _partScale), lineType);
            CvInvoke.PutText(mat, $"{Microns}um", new Point(fontStartX, fontStartY + fontMargin), font, fontScale, color, fontThickness, lineType);
            CvInvoke.PutText(mat, $"{BottomExposure}|{NormalExposure}s", new Point(fontStartX, fontStartY + fontMargin * 2), font, fontScale, color, fontThickness, lineType);
            if (text is null) return;
            for (var i = 0; i < text.Length; i++)
            {
                CvInvoke.PutText(mat, text[i], new Point(fontStartX, fontStartY + fontMargin * (i + 3)), font,
                    fontScale, color, fontThickness, lineType);
            }

        }

        ushort count = 0;

        layers[1] = layers[0].Clone();

        if (OutputOriginalPart)
        {
            using var roi0 = new Mat(layers[0], new Rectangle(new Point(currentX, currentY), shape.Size));
            shape.CopyTo(roi0);

            using var roi1 = new Mat(layers[1], new Rectangle(new Point(currentX, currentY), shape.Size));
            shape.CopyTo(roi1);

            if (ExtrudeText)
            {
                using var roi2 = new Mat(layers[2], new Rectangle(new Point(currentX, currentY), shape.Size));
                addText(roi2, ++count, "ORI");
            }
            else
            {
                addText(roi1, ++count, "ORI");
            }
        }
        else
        {
            currentX -= shape.Width + Margin;
        }




        if (IsErodeEnabled)
        {
            for (int iteration = ErodeStartIteration;
                 iteration <= ErodeEndIteration;
                 iteration += ErodeIterationSteps)
            {
                currentX += shape.Width + Margin;
                maxX = Math.Max(maxX, currentX);

                if (currentX + shape.Width >= layers[0].Width)
                {
                    currentX = startX;
                    currentY += shape.Height + Margin;
                }

                if (currentY + shape.Height >= layers[0].Height)
                {
                    break; // Insufficient size
                }

                count++;
                using (var roi = new Mat(layers[1], new Rectangle(new Point(currentX, currentY), shape.Size)))
                {
                    shape.CopyTo(roi);
                    if (ExtrudeText)
                    {
                        using var roi1 = new Mat(layers[2], new Rectangle(new Point(currentX, currentY), shape.Size));
                        addText(roi1, count, $"E: {iteration}i");
                    }
                    else
                    {
                        addText(roi, count, $"E: {iteration}i");
                    }

                }

                using (var roi = layers[0].Roi(new Rectangle(new Point(currentX, currentY), shape.Size)))
                using (var erode = new Mat())
                {
                    var tempIterations = iteration;
                    var kernel = ErodeKernel.GetKernel(ref tempIterations);
                    CvInvoke.Erode(shape, erode, kernel, ErodeKernel.Anchor, tempIterations, BorderType.Reflect101, default);
                    erode.CopyTo(roi);
                    //addText(roi, count, $"E: {iteration}i");
                }
            }
        }

        if (IsDimmingEnabled)
        {
            for (int brightness = DimmingStartBrightness;
                 brightness <= DimmingEndBrightness;
                 brightness += DimmingBrightnessSteps)
            {
                currentX += shape.Width + Margin;

                if (currentX + shape.Width >= layers[0].Width)
                {
                    currentX = 0;
                    currentY += shape.Height + Margin;
                }

                if (currentY + shape.Height >= layers[0].Height)
                {
                    break; // Insufficient size
                }

                count++;
                using (var roi = layers[1].Roi(new Rectangle(new Point(currentX, currentY), shape.Size)))
                {
                    shape.CopyTo(roi);
                    if (ExtrudeText)
                    {
                        using var roi1 = new Mat(layers[2], new Rectangle(new Point(currentX, currentY), shape.Size));
                        addText(roi1, count, $"W: {DimmingWallThickness}", $"B: {brightness}");
                    }
                    else
                    {
                        addText(roi, count, $"W: {DimmingWallThickness}", $"B: {brightness}");
                    }
                }

                using (var roi = new Mat(layers[0], new Rectangle(new Point(currentX, currentY), shape.Size)))
                using (var erode = new Mat())
                using (var target = new Mat())
                using (var mask = shape.NewZeros())
                {
                    mask.SetTo(new MCvScalar(byte.MaxValue-brightness));
                    int tempIterations = DimmingWallThickness;
                    var kernel = DimmingKernel.GetKernel(ref tempIterations);
                    CvInvoke.Erode(shape, erode, kernel, EmguCvExtensions.AnchorCenter, tempIterations, BorderType.Reflect101, default);
                    //CvInvoke.Subtract(shape, erode, diff);
                    //CvInvoke.BitwiseAnd(diff, mask, target);
                    //CvInvoke.Add(erode, target, target);
                    CvInvoke.Subtract(shape, mask, target);
                    CvInvoke.Add(erode, target, target);
                    target.CopyTo(roi);
                    //addText(roi, count, $"W: {DimmingWallThickness}", $"B: {brightness}");
                }
            }
        }

        if (MirrorOutput)
        {
            var flip = SlicerFile.DisplayMirror;
            if (flip == FlipDirection.None) flip = FlipDirection.Horizontally;
            Parallel.ForEach(layers, CoreSettings.ParallelOptions, mat => CvInvoke.Flip(mat, mat, (FlipType)flip));
        }

        // Preview
        //layers[2] = new Mat(layers[0], new Rectangle(0, 0, Math.Min(layers[0].Width, maxX), Math.Min(layers[0].Height, currentY)));

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
        CvInvoke.PutText(thumbnail, "Elephant Foot Cal.", new Point(xSpacing, ySpacing * 2), fontFace, fontScale, new MCvScalar(0, 255, 255), fontThickness);
        CvInvoke.PutText(thumbnail, $"{Microns}um @ {BottomExposure}s/{NormalExposure}s", new Point(xSpacing, ySpacing * 3), fontFace, fontScale, EmguCvExtensions.WhiteColor, fontThickness);
        CvInvoke.PutText(thumbnail, $"{ObjectCount} Objects", new Point(xSpacing, ySpacing * 4), fontFace, fontScale, EmguCvExtensions.WhiteColor, fontThickness);

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
        progress.ItemCount = 3;

        var newLayers = new Layer[LayerCount];

        var layers = GetLayers();
        progress++;


        var bottomLayer = new Layer(0, layers[0], SlicerFile);

        Layer? extrudeLayer = null;
        var moveOp = new OperationMove(SlicerFile, bottomLayer.BoundingRectangle);
        moveOp.Execute(layers[0]);
        moveOp.Execute(layers[1]);

        var layer = new Layer(0, layers[1], SlicerFile)
        {
            IsModified = true
        };

        if (ExtrudeText)
        {
            moveOp.Execute(layers[2]);
            extrudeLayer = new Layer(0, layers[2], SlicerFile)
            {
                IsModified = true
            };

        }
        bottomLayer.LayerMat = layers[0];

        progress++;

        for (uint layerIndex = 0;
            layerIndex < BottomLayers + NormalLayers;
             layerIndex++)
        {
            progress.PauseOrCancelIfRequested();
            newLayers[layerIndex] = SlicerFile.GetBottomOrNormalValue(layerIndex, bottomLayer.Clone(), layer.Clone());
        }

        if (ExtrudeText)
        {
        for (uint layerIndex = (uint) (BottomLayers + NormalLayers); layerIndex < LayerCount; layerIndex++)
            {
                newLayers[layerIndex] = extrudeLayer!.Clone();
            }
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

        progress++;

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
