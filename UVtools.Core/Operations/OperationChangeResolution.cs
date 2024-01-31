/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using System;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;


public sealed class OperationChangeResolution : Operation
{
    #region Members
    private uint _newResolutionX;
    private uint _newResolutionY;
    private bool _fixRatio;
    private decimal _newDisplayWidth;
    private decimal _newDisplayHeight;

    #endregion

    #region Subclasses
    public class Resolution
    {
        public uint ResolutionX { get; }
        public uint ResolutionY { get; }
        public string? Name { get; }
        public bool IsEmpty => ResolutionX == 0 && ResolutionY == 0;

        public Resolution(uint resolutionX, uint resolutionY, string? name = null)
        {
            ResolutionX = resolutionX;
            ResolutionY = resolutionY;
            Name = name;
        }

        public override string ToString()
        {
            if(IsEmpty) return string.Empty;
            var str = $"{ResolutionX} x {ResolutionY}";
            if (!string.IsNullOrEmpty(Name))
            {
                str += $" ({Name})";
            }
            return str;
        }
    }
    #endregion

    #region Overrides

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;
    public override bool CanROI => false;
    public override string IconClass => "mdi-resize";
    public override string Title => "Change print resolution";
    public override string Description =>
        "Crops or resizes all layer images to fit an alternate print resolution.\n" +
        "Useful to make files printable on a different printer than they were originally sliced for without the need to re-slice.\n\n" +
        "NOTE: Please ensure that the actual model will fit within the new print resolution. The operation will be aborted if it will result in any of the actual model being clipped.\n" +
        "Only use this tool if both source and target printer have the same pixel pitch spec, otherwise the model size will be invalidated and result in a different size than the originally sliced for.\n" +
        "As alternative, set the correct display size of the target printer or select an machine preset to match the new pixel pitch and let it auto fix the pixel ratio. It's always good practice to set the correct display size, even if you don't want to fix the pixel ratio, that is a critical information for both software and printers.";

    public override string ConfirmationText => 
        $"change print resolution from {SlicerFile.ResolutionX}x{SlicerFile.ResolutionY} to {NewResolutionX}x{NewResolutionY}?";

    public override string ProgressTitle =>
        $"Changing print resolution from ({SlicerFile.ResolutionX}x{SlicerFile.ResolutionY}) to ({NewResolutionX}x{NewResolutionY})";

    public override string ProgressAction => "Changed layers";

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();
        if (SlicerFile.ResolutionX == NewResolutionX && SlicerFile.ResolutionY == NewResolutionY)
        {
            sb.AppendLine($"The new resolution must be different from current resolution ({SlicerFile.ResolutionX} x {SlicerFile.ResolutionY}).");
        }

        var finalBoundsWidth = FinalBoundsWidth;
        var finalBoundsHeight = FinalBoundsHeight;
        if (NewResolutionX < finalBoundsWidth || NewResolutionY < finalBoundsHeight)
        {
            sb.AppendLine($"The new resolution ({NewResolutionX} x {NewResolutionY}) is not large enough to hold the model volume ({finalBoundsWidth} x {finalBoundsHeight}), continuing operation would clip the model.");
            sb.AppendLine("To fix this, try to rotate the object and/or resize to fit on this new resolution.");
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"{_newResolutionX}x{_newResolutionY} [Display: {_newDisplayWidth}x{_newDisplayHeight}] [Fix ratio: {_fixRatio}]";
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    #endregion

    #region Properties
    public uint NewResolutionX
    {
        get => _newResolutionX;
        set
        {
            if(!RaiseAndSetIfChanged(ref _newResolutionX, Math.Max(1, value))) return;
            RaisePropertyChanged(nameof(NewPixelSizeMicrons));
            RaisePropertyChanged(nameof(NewFixedRatioX));
            RaisePropertyChanged(nameof(FinalBoundsWidth));
        }
    }

    public uint NewResolutionY
    {
        get => _newResolutionY;
        set
        {
            if(!RaiseAndSetIfChanged(ref _newResolutionY, Math.Max(1, value))) return;
            RaisePropertyChanged(nameof(NewPixelSizeMicrons));
            RaisePropertyChanged(nameof(NewFixedRatioY));
            RaisePropertyChanged(nameof(FinalBoundsHeight));
        }
    }

    public decimal NewDisplayWidth
    {
        get => _newDisplayWidth;
        set
        {
            if(!RaiseAndSetIfChanged(ref _newDisplayWidth, Math.Max(0, value))) return;
            RaisePropertyChanged(nameof(NewPixelSizeMicrons));
            RaisePropertyChanged(nameof(NewFixedRatioX));
        }
    }

    public decimal NewDisplayHeight
    {
        get => _newDisplayHeight;
        set
        {
            if (!RaiseAndSetIfChanged(ref _newDisplayHeight, Math.Max(0, value))) return;
            RaisePropertyChanged(nameof(NewPixelSizeMicrons));
            RaisePropertyChanged(nameof(NewFixedRatioY));
        }
    }

    public SizeF NewPixelSizeMicrons =>
        new (_newResolutionX <= 0 || SlicerFile.Display.Width <= 0 || _newDisplayWidth <= 0
                ? SlicerFile.PixelWidthMicrons
                : (float) Math.Round((float) _newDisplayWidth / _newResolutionX * 1000, 3),
            _newResolutionY <= 0 || SlicerFile.Display.Height <= 0 || _newDisplayHeight <= 0
                ? SlicerFile.PixelHeightMicrons
                : (float) Math.Round((float) _newDisplayHeight / _newResolutionY * 1000, 3));

    public double NewFixedRatioX
    {

        get
        {
            if (_newResolutionX <= 0 || SlicerFile.Display.Width <= 0 || _newDisplayWidth <= 0) return 1;
            var ratio = SlicerFile.PixelWidth / ((double)_newDisplayWidth / _newResolutionX);
            return Math.Round(ratio, 3);
        }
    }

    public double NewFixedRatioY
    {
        get
        {
            if (_newResolutionY <= 0 || SlicerFile.Display.Height <= 0 || _newDisplayHeight <= 0) return 1;
            var ratio = SlicerFile.PixelHeight / ((double)_newDisplayHeight / _newResolutionY);
            return Math.Round(ratio, 3);
        }
    }

    public bool FixRatio
    {
        get => _fixRatio;
        set
        {
            if(!RaiseAndSetIfChanged(ref _fixRatio, value)) return;
            RaisePropertyChanged(nameof(FinalBoundsWidth));
            RaisePropertyChanged(nameof(FinalBoundsHeight));
        }
    }

    public uint FinalBoundsWidth => (uint)(_fixRatio ? SlicerFile.BoundingRectangle.Width * NewFixedRatioX : SlicerFile.BoundingRectangle.Width);
    public uint FinalBoundsHeight => (uint)(_fixRatio ? SlicerFile.BoundingRectangle.Height * NewFixedRatioY : SlicerFile.BoundingRectangle.Height);

    #endregion

    #region Constructor

    public OperationChangeResolution() { }

    public OperationChangeResolution(FileFormat slicerFile) : base(slicerFile)
    { }

    public override void InitWithSlicerFile()
    {
        base.InitWithSlicerFile();
        if(_newResolutionX <= 0) _newResolutionX = SlicerFile.ResolutionX;
        if(_newResolutionY <= 0) _newResolutionY = SlicerFile.ResolutionY;
        if(_newDisplayWidth <= 0) _newDisplayWidth = (decimal) SlicerFile.DisplayWidth;
        if(_newDisplayHeight <= 0) _newDisplayHeight = (decimal) SlicerFile.DisplayHeight;
    }

    #endregion

    #region Methods
    public static Resolution[] GetResolutions()
    {
        return new [] {
            //new Resolution(0, 0, string.Empty),
            new Resolution(854, 480, "FWVGA"),
            new Resolution(960, 1708),
            new Resolution(1080, 1920, "FHD"),
            new Resolution(1440, 2560, "QHD"),
            new Resolution(1600, 2560, "WQXGA"),
            new Resolution(1620, 2560, "WQXGA"),
            new Resolution(1920, 1080, "FHD"),
            new Resolution(2160, 3840, "4K UHD"),
            new Resolution(2531, 1410, "QHD"),
            new Resolution(2560, 1440, "QHD"),
            new Resolution(2560, 1600, "WQXGA"),
            new Resolution(2560, 1620, "WQXGA"),
            new Resolution(3840, 2160, "4K UHD"),
            new Resolution(3840, 2400, "WQUXGA"),
            new Resolution(4920, 2880, "5K UHD"),
            new Resolution(5448, 3064, "6K"),
            new Resolution(7680, 4320, "8K UHD"),
        };
    }

    public static Resolution[] Presets => GetResolutions();


    protected override bool ExecuteInternally(OperationProgress progress)
    {
        progress.ItemCount = SlicerFile.LayerCount;
        var newSize = new Size((int) NewResolutionX, (int) NewResolutionY);
        var finalBoundsWidth = FinalBoundsWidth;
        var finalBoundsHeight = FinalBoundsHeight;

        var finalBounds = new Size((int)finalBoundsWidth, (int)finalBoundsHeight);

        Parallel.For(0, SlicerFile.LayerCount, CoreSettings.GetParallelOptions(progress), layerIndex =>
        {
            progress.PauseIfRequested();
            using var mat = SlicerFile[layerIndex].LayerMat;

            if (mat.Size != newSize)
            {
                using var matDst = EmguExtensions.InitMat(newSize);

                var newFixedRatioX = NewFixedRatioX;
                var newFixedRatioY = NewFixedRatioY;
                if (_fixRatio && (newFixedRatioX != 1.0 || newFixedRatioY != 1.0))
                {
                    //mat.TransformFromCenter();
                    CvInvoke.Resize(mat, mat, SlicerFile.Resolution.Multiply(newFixedRatioX, newFixedRatioY));
                }

                mat.CopyCenterToCenter(finalBounds, matDst);

                SlicerFile[layerIndex].LayerMat = matDst;
            }

            progress.LockAndIncrement();
        });


        SlicerFile.ResolutionX = _newResolutionX;
        SlicerFile.ResolutionY = _newResolutionY;
        if (_newDisplayWidth > 0)
        {
            SlicerFile.DisplayWidth = (float)_newDisplayWidth;
        }

        if (_newDisplayHeight > 0)
        {
            SlicerFile.DisplayHeight = (float)_newDisplayHeight;
        }

        SlicerFile.BoundingRectangle = Rectangle.Empty;

        return !progress.Token.IsCancellationRequested;
    }

    /*public override bool Execute(Mat mat, params object[] arguments)
    {
        //mat.Transform(1, 1, newResolutionX-mat.Width+roi.Width, newResolutionY-mat.Height - roi.Height/2, new Size((int) newResolutionX, (int) newResolutionY));
        using var matRoi = new Mat(mat, VolumeBonds);
        using var matDst = new Mat(new Size((int)NewResolutionX, (int)NewResolutionY), mat.Depth, mat.NumberOfChannels);
        using var matDstRoi = new Mat(matDst, 
                new Rectangle((int)(NewResolutionX / 2 - VolumeBonds.Width / 2),
                (int)NewResolutionY / 2 - VolumeBonds.Height / 2,
                VolumeBonds.Width, VolumeBonds.Height));
        matRoi.CopyTo(matDstRoi);

        return true;
    }*/

    #endregion

    #region Equality

    private bool Equals(OperationChangeResolution other)
    {
        return _newResolutionX == other._newResolutionX && _newResolutionY == other._newResolutionY && _fixRatio == other._fixRatio && _newDisplayWidth == other._newDisplayWidth && _newDisplayHeight == other._newDisplayHeight;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationChangeResolution other && Equals(other);
    }


    #endregion

}