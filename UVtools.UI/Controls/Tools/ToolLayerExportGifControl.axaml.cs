using Avalonia.Platform.Storage;
using UVtools.Core.Operations;

namespace UVtools.UI.Controls.Tools;

public partial class ToolLayerExportGifControl : ToolControl
{
    public OperationLayerExportGif Operation => (BaseOperation as OperationLayerExportGif)!;
    public ToolLayerExportGifControl()
    {
        BaseOperation = new OperationLayerExportGif(SlicerFile!);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }

    public async void ChooseFilePath()
    {
        using var file = await App.MainWindow.SaveFilePickerAsync(SlicerFile!, AvaloniaStatic.GifFileFilter);
        if (file?.TryGetLocalPath() is not { } filePath) return;

        Operation.FilePath = filePath;
    }
}