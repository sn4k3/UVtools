using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System.Timers;
using Avalonia.Platform.Storage;
using EmguExtensions.Avalonia;
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
        set
        {
            var previous = _previewImage;
            if (!RaiseAndSetIfChanged(ref _previewImage, value)) return;
            previous.DisposeDeferred();
        }
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

    private int _previewVersion;

    public void UpdatePreview() => _ = UpdatePreviewAsync();

    /// <summary>
    /// Renders the preview off the UI thread, an outdated render (the settings changed meanwhile) is discarded.
    /// </summary>
    private async Task UpdatePreviewAsync()
    {
        var version = ++_previewVersion;
        try
        {
            using var mat = await Task.Run(() => Operation.GetTargetMat());
            if (version != _previewVersion) return; // A newer preview was requested
            PreviewImage = mat?.ToBitmap();
        }
        catch (System.Exception e)
        {
            System.Diagnostics.Debug.WriteLine(e);
        }
    }

    public async Task SelectFile()
    {
        var files = await App.MainWindow.OpenFilePickerAsync(AvaloniaStatic.ImagesFileFilter);
        if (files.Count == 0 || files[0].TryGetLocalPath() is not {} filePath) return;
        Operation.FilePath = filePath;
    }
}