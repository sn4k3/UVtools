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
using System.Drawing;
using System.Text;
using EmguExtensions;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public sealed partial class OperationCalibrateLiftHeight : Operation
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
    public override string IconClass => "TrayArrowUp";
    public override string Title => "Lift height";
    public override string Description =>
        "Generates test models with various strategies and increments to measure the optimal lift height or peel forces for layers given the printed area.\n" +
        "You must have a tool to measure the lift height / forces as it moves up, record the values to determine the lowest safe value for the lift.\n" +
        "After find the height where it peels, you must give from 1mm to 2mm more for safeness.\n" +
        "You must repeat this test when change any of the following: printer, LEDs, resin and exposure times.\n" +
        "Note: The current opened file will be overwritten with this test, use a dummy or a not needed file.";

    public override string ConfirmationText =>
        $"generate the lift height test?";

    public override string ProgressTitle =>
        $"Generating the lift height test";

    public override string ProgressAction => "Generated";

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        if (SlicerFile.ResolutionX - LeftRightMargin * 2 <= 0)
            sb.AppendLine("The top/bottom margin is too big, it overlaps the screen resolution.");

        if (SlicerFile.ResolutionY - TopBottomMargin * 2 <= 0)
            sb.AppendLine("The top/bottom margin is too big, it overlaps the screen resolution.");

        if (DecreaseImage)
        {
            if(DecreaseImageFactor is 0 or >= 100)
                sb.AppendLine("The image decrease factor must be between 1 and 99%");

            if(MinimumImageFactor is 0 or >= 100)
                sb.AppendLine("The minimum image decrease factor must be between 1 and 99%");
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"[Layer Height: {_layerHeight}] " +
                     $"[Layers: {BottomLayers}/{NormalLayers}] " +
                     $"[Exposure: {_bottomExposure}/{_normalExposure}s] " +
                     $"[Lift: {BottomLiftHeight}/{LiftHeight}mm @ {BottomLiftSpeed}/{LiftSpeed}mm/min]" +
                     $"[Retract speed: {RetractSpeed}mm/min]" +
                     $"[Decrease image: {DecreaseImage} @ {DecreaseImageFactor}-{MinimumImageFactor}%]";
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
    [NotifyPropertyChangedFor(nameof(BottomHeight))]
    [NotifyPropertyChangedFor(nameof(TotalHeight))]
    [NotifyPropertyChangedFor(nameof(LayerCount))]
    public partial ushort BottomLayers { get; set; } = 3;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NormalHeight))]
    [NotifyPropertyChangedFor(nameof(TotalHeight))]
    [NotifyPropertyChangedFor(nameof(LayerCount))]
    public partial ushort NormalLayers { get; set; } = 2;

    public uint LayerCount
    {
        get
        {
            uint layerCount = (uint)(BottomLayers + NormalLayers);
            if (DecreaseImage)
            {
                layerCount += (100u - MinimumImageFactor) / DecreaseImageFactor;
                //layerCount += (uint)Math.Ceiling((100.0 - MinimumImageFactor - DecreaseImageFactor) / DecreaseImageFactor);
                //for (int factor = 100 - DecreaseImageFactor; factor >= MinimumImageFactor; factor -= DecreaseImageFactor)
                //    layerCount++;
            }
            return layerCount;
        }
    }

    public decimal BottomHeight => Layer.RoundHeight(_layerHeight * BottomLayers);
    public decimal NormalHeight => Layer.RoundHeight(_layerHeight * (LayerCount - BottomLayers));

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

    [ObservableProperty]
    public partial decimal BottomLiftHeight { get; set; }

    [ObservableProperty]
    public partial decimal LiftHeight { get; set; }

    [ObservableProperty]
    public partial decimal BottomLiftSpeed { get; set; }

    [ObservableProperty]
    public partial decimal LiftSpeed { get; set; }

    [ObservableProperty]
    public partial decimal RetractSpeed { get; set; }

    [ObservableProperty]
    public partial ushort LeftRightMargin { get; set; } = 200;

    public ushort MaxLeftRightMargin => (ushort)((SlicerFile.ResolutionX - 100) / 2);

    [ObservableProperty]
    public partial ushort TopBottomMargin { get; set; } = 200;

    public ushort MaxTopBottomMargin => (ushort) ((SlicerFile.ResolutionY - 100) / 2);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalHeight))]
    [NotifyPropertyChangedFor(nameof(LayerCount))]
    public partial bool DecreaseImage { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalHeight))]
    [NotifyPropertyChangedFor(nameof(LayerCount))]
    public partial byte DecreaseImageFactor { get; set; } = 10;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalHeight))]
    [NotifyPropertyChangedFor(nameof(LayerCount))]
    public partial byte MinimumImageFactor { get; set; } = 10;

    public Rectangle WhiteBlock
    {
        get
        {
            int width = (int) (SlicerFile.ResolutionX - LeftRightMargin * 2);
            int height = (int) (SlicerFile.ResolutionY - TopBottomMargin * 2);
            int x = (int) (SlicerFile.ResolutionX - width) / 2;
            int y = (int) (SlicerFile.ResolutionY - height) / 2;

            return new Rectangle(x, y, width, height);
        }
    }

    #endregion

    #region Constructor

    public OperationCalibrateLiftHeight() { }

    public OperationCalibrateLiftHeight(FileFormat slicerFile) : base(slicerFile)
    { }

    public override void InitWithSlicerFile()
    {
        base.InitWithSlicerFile();
        if(_layerHeight <= 0) _layerHeight = (decimal)SlicerFile.LayerHeight;
        if(_bottomExposure <= 0) _bottomExposure = (decimal)SlicerFile.BottomExposureTime;
        if(_normalExposure <= 0) _normalExposure = (decimal)SlicerFile.ExposureTime;
        if (BottomLiftHeight <= 0) BottomLiftHeight = (decimal)SlicerFile.BottomLiftHeight;
        if (LiftHeight <= 0) LiftHeight = (decimal)SlicerFile.LiftHeight;
        if (BottomLiftSpeed <= 0) BottomLiftSpeed = (decimal) SlicerFile.BottomLiftSpeed;
        if (LiftSpeed <= 0) LiftSpeed = (decimal) SlicerFile.LiftSpeed;
        if (RetractSpeed <= 0) RetractSpeed = (decimal) SlicerFile.RetractSpeed;
    }

    #endregion

    #region Equality

    private bool Equals(OperationCalibrateLiftHeight other)
    {
        return _layerHeight == other._layerHeight && BottomLayers == other.BottomLayers && NormalLayers == other.NormalLayers && _bottomExposure == other._bottomExposure && _normalExposure == other._normalExposure && BottomLiftHeight == other.BottomLiftHeight && LiftHeight == other.LiftHeight && BottomLiftSpeed == other.BottomLiftSpeed && LiftSpeed == other.LiftSpeed && RetractSpeed == other.RetractSpeed && LeftRightMargin == other.LeftRightMargin && TopBottomMargin == other.TopBottomMargin && DecreaseImage == other.DecreaseImage && DecreaseImageFactor == other.DecreaseImageFactor && MinimumImageFactor == other.MinimumImageFactor;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationCalibrateLiftHeight other && Equals(other);
    }


    #endregion

    #region Methods

    /// <summary>
    /// Gets the bottom and normal layers, 0 = bottom | 1 = normal
    /// </summary>
    /// <returns></returns>
    public Mat[] GetLayers()
    {
        var layers = new Mat[1];

        layers[0] = EmguCvExtensions.InitMat(SlicerFile.Resolution);
        CvInvoke.Rectangle(layers[0], WhiteBlock, EmguCvExtensions.WhiteColor, -1);

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
        CvInvoke.PutText(thumbnail, "Lift Height Cal.", new Point(xSpacing, ySpacing * 2), fontFace, fontScale, new MCvScalar(0, 255, 255), fontThickness);
        CvInvoke.PutText(thumbnail, $"{Microns}um @ {BottomExposure}s/{NormalExposure}s", new Point(xSpacing, ySpacing * 3), fontFace, fontScale, EmguCvExtensions.WhiteColor, fontThickness);
        //CvInvoke.PutText(thumbnail, $"{ObjectCount} Objects", new Point(xSpacing, ySpacing * 4), fontFace, fontScale, EmguCvExtensions.WhiteColor, fontThickness);

        return thumbnail;
    }

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        progress.ItemCount = LayerCount;

        var newLayers = new Layer[LayerCount];

        var layers = GetLayers();
        progress++;

        var layer = new Layer(0, layers[0], SlicerFile)
        {
            IsModified = true
        };

        uint layerIndex = 0;
        for (; layerIndex < BottomLayers + NormalLayers; layerIndex++)
        {
            progress.PauseOrCancelIfRequested();
            newLayers[layerIndex] = layer.Clone();
            progress++;
        }

        if (DecreaseImage)
        {
            var rect = WhiteBlock;
            for (int factor = 100 - DecreaseImageFactor; factor >= MinimumImageFactor; factor -= DecreaseImageFactor)
            {
                using var mat = layers[0].NewZeros();

                // size -  100
                //  x   - factor

                int width = rect.Width * factor / 100;
                int height = rect.Height * factor / 100;
                int x = (int)(SlicerFile.ResolutionX - width) / 2;
                int y = (int)(SlicerFile.ResolutionY - height) / 2;

                CvInvoke.Rectangle(mat,
                    new Rectangle(x, y, width, height),
                    EmguCvExtensions.WhiteColor, -1);

                newLayers[layerIndex] = new Layer(0, mat, SlicerFile)
                {
                    IsModified = true
                };

                layerIndex++;
                progress++;
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
            SlicerFile.LayerHeight = (float)_layerHeight;
            SlicerFile.BottomExposureTime = (float)_bottomExposure;
            SlicerFile.ExposureTime = (float)_normalExposure;
            SlicerFile.BottomLiftHeight = (float)BottomLiftHeight;
            SlicerFile.LiftHeight = (float)LiftHeight;
            SlicerFile.BottomLiftSpeed = (float)BottomLiftSpeed;
            SlicerFile.LiftSpeed = (float)LiftSpeed;
            SlicerFile.RetractSpeed = (float)RetractSpeed;
            SlicerFile.BottomLayerCount = BottomLayers;
            SlicerFile.TransitionLayerCount = 0;

            SlicerFile.Layers = newLayers;
        }, true);

        return !progress.Token.IsCancellationRequested;
    }

    #endregion
}