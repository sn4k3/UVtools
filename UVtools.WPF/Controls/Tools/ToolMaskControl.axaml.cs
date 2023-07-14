using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Drawing;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;
using UVtools.WPF.Extensions;
using UVtools.WPF.Windows;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace UVtools.WPF.Controls.Tools;

public partial class ToolMaskControl : ToolControl
{
    private bool _isMaskInverted;
    private byte _genMinimumBrightness = 200;
    private byte _genMaximumBrightness = byte.MaxValue;
    private uint _genDiameter;
    private Bitmap _maskImage;
    public OperationMask Operation => BaseOperation as OperationMask;

    public bool IsMaskInverted
    {
        get => _isMaskInverted;
        set
        {
            if(!RaiseAndSetIfChanged(ref _isMaskInverted, value)) return;
            if (!Operation.HaveInputMask) return;
            Operation.InvertMask();
            MaskImage = Operation.Mask.ToBitmapParallel();
        }
    }

    public byte GenMinimumBrightness
    {
        get => _genMinimumBrightness;
        set => RaiseAndSetIfChanged(ref _genMinimumBrightness, value);
    }

    public byte GenMaximumBrightness
    {
        get => _genMaximumBrightness;
        set => RaiseAndSetIfChanged(ref _genMaximumBrightness, value);
    }

    public uint GenDiameter
    {
        get => _genDiameter;
        set => RaiseAndSetIfChanged(ref _genDiameter, value);
    }

    public string InfoPrinterResolutionStr => $"Printer resolution: {App.SlicerFile.Resolution}";
    public string InfoMaskResolutionStr => $"Mask resolution: "+ (Operation.HaveInputMask ? Operation.Mask.Size.ToString() : "(Unloaded)");

    public Bitmap MaskImage
    {
        get => _maskImage;
        set
        {
            if(!RaiseAndSetIfChanged(ref _maskImage, value)) return;
            RaisePropertyChanged(nameof(InfoMaskResolutionStr));
            ParentWindow.ButtonOkEnabled = Operation.HaveInputMask;
        }
    }

    public ToolMaskControl()
    {
        BaseOperation = new OperationMask(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }


    public override void Callback(ToolWindow.Callbacks callback)
    {
        switch (callback)
        {
            case ToolWindow.Callbacks.Init:
                ParentWindow.ButtonOkEnabled = false;
                break;
            case ToolWindow.Callbacks.AfterLoadProfile:
            case ToolWindow.Callbacks.ClearROI:
                Operation.Mask = null;
                MaskImage = null;
                break;
        }
    }

    public async void ImportImageMask()
    {
        var files = await App.MainWindow.OpenFilePickerAsync(AvaloniaStatic.ImagesFileFilter);
        if (files.Count == 0 || files[0].TryGetLocalPath() is not { } filePath) return;

        try
        {
            Operation.LoadFromFile(filePath, _isMaskInverted, App.MainWindow.ROI.Size.IsEmpty ? SlicerFile.Resolution : App.MainWindow.ROI.Size);
            MaskImage = Operation.Mask.ToBitmapParallel();
        }
        catch (Exception e)
        {
            await ParentWindow.MessageBoxError(e.ToString(), "Error while trying to read the image");
        }
    }

    public void GenerateMask()
    {
        var roi = App.MainWindow.ROI;
        Operation.Mask = roi.IsEmpty ? App.MainWindow.LayerCache.Image.NewBlank() : new Mat(roi.Size, DepthType.Cv8U, 1);

        int radius = (int)_genDiameter;
        if (radius == 0)
        {
            radius = Math.Min(Operation.Mask.Width, Operation.Mask.Height) / 2;
        }
        else
        {
            radius = Math.Clamp(radius, 2, Math.Min(Operation.Mask.Width, Operation.Mask.Height)) / 2;
        }

        var maxScalar = new MCvScalar(_genMaximumBrightness);
        Operation.Mask.SetTo(maxScalar);

        var center = new Point(Operation.Mask.Width / 2, Operation.Mask.Height / 2);
        var colorDifference = _genMinimumBrightness - _genMaximumBrightness;
        //CvInvoke.Circle(Mask, center, radius, minScalar, -1);

        for (decimal i = 1; i < radius; i++)
        {
            int color = (int)(_genMinimumBrightness - i / radius * colorDifference); //or some another color calculation
            CvInvoke.Circle(Operation.Mask, center, (int)i, new MCvScalar(color), 2);
        }

        if (_isMaskInverted) Operation.InvertMask();
        MaskImage = Operation.Mask.ToBitmapParallel();
    }
}