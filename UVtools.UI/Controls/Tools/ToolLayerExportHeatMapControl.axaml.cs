using Avalonia.Controls;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using UVtools.Core.Operations;

namespace UVtools.UI.Controls.Tools;

public partial class ToolLayerExportHeatMapControl : ToolControl
{
    public OperationLayerExportHeatMap Operation => (BaseOperation as OperationLayerExportHeatMap)!;
    public ToolLayerExportHeatMapControl()
    {
        BaseOperation = new OperationLayerExportHeatMap(SlicerFile!);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }

    public async Task ChooseFilePath()
    {

        using var file = await App.MainWindow.SaveFilePickerAsync(SlicerFile!.DirectoryPath, $"{SlicerFile.FilenameNoExt}_heatmap.png",
                AvaloniaStatic.ImagesFullFileFilter);
        if (file?.TryGetLocalPath() is not { } filePath) return;

        Operation.FilePath = filePath;
    }
}