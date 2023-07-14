using Avalonia.Controls;
using System.IO;
using Avalonia.Platform.Storage;
using UVtools.Core.Operations;

namespace UVtools.UI.Controls.Tools;

public partial class ToolLayerExportSkeletonControl : ToolControl
{
    public OperationLayerExportSkeleton Operation => BaseOperation as OperationLayerExportSkeleton;
    public ToolLayerExportSkeletonControl()
    {
        BaseOperation = new OperationLayerExportSkeleton(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }

    public async void ChooseFilePath()
    {
        using var file = await App.MainWindow.SaveFilePickerAsync(SlicerFile.DirectoryPath, $"{SlicerFile.FilenameNoExt}_skeleton.png",
                AvaloniaStatic.ImagesFullFileFilter);

        if (file?.TryGetLocalPath() is not { } filePath) return;
        Operation.FilePath = filePath;
    }
}