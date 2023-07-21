using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System.Timers;
using Avalonia.Platform.Storage;
using UVtools.Core.Operations;
using UVtools.UI.Extensions;
using UVtools.UI.Windows;

namespace UVtools.UI.Controls.Tools;

public partial class ToolLithophaneControl : ToolControl
{
    public OperationLithophane Operation => (BaseOperation as OperationLithophane)!;

    private readonly Timer _timer = null!;

    private Bitmap? _previewImage;
    public Bitmap? PreviewImage
    {
        get => _previewImage;
        set => RaiseAndSetIfChanged(ref _previewImage, value);
    }

    public ToolLithophaneControl()
    {
        BaseOperation = new OperationLithophane(SlicerFile!);
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
        using var mat = Operation.GetTargetMat();
        _previewImage?.Dispose();
        PreviewImage = mat?.ToBitmapParallel();
    }

    public async void SelectFile()
    {
        var files = await App.MainWindow.OpenFilePickerAsync(AvaloniaStatic.ImagesFileFilter);
        if (files.Count == 0 || files[0].TryGetLocalPath() is not {} filePath) return;
        Operation.FilePath = filePath;
    }
}