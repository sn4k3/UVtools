using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System.Timers;
using UVtools.Core.Operations;
using UVtools.WPF.Controls.Tools;
using UVtools.WPF.Extensions;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Calibrators;

public partial class CalibrateLiftHeightControl : ToolControl
{
    public OperationCalibrateLiftHeight Operation => BaseOperation as OperationCalibrateLiftHeight;

    private readonly Timer _timer;

    private Bitmap _previewImage;
    public Bitmap PreviewImage
    {
        get => _previewImage;
        set => RaiseAndSetIfChanged(ref _previewImage, value);
    }

    public CalibrateLiftHeightControl()
    {
        BaseOperation = new OperationCalibrateLiftHeight(SlicerFile);
        if (!ValidateSpawn()) return;

        InitializeComponent();


        _timer = new Timer(20)
        {
            AutoReset = false
        };
        _timer.Elapsed += (sender, e) => Dispatcher.UIThread.InvokeAsync(UpdatePreview);
    }

    public override void Callback(ToolWindow.Callbacks callback)
    {
        if (App.SlicerFile is null) return;
        switch (callback)
        {
            case ToolWindow.Callbacks.Init:
            case ToolWindow.Callbacks.AfterLoadProfile:
                Operation.PropertyChanged += (sender, e) =>
                {
                    _timer.Stop();
                    _timer.Start();
                };
                _timer.Stop();
                _timer.Start();
                break;
        }
    }

    public void UpdatePreview()
    {
        var layers = Operation.GetLayers();
        _previewImage?.Dispose();
        PreviewImage = layers[0].ToBitmapParallel();
        foreach (var layer in layers)
        {
            layer.Dispose();
        }
    }
}