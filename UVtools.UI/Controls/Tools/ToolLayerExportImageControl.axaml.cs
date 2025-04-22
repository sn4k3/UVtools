using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using UVtools.Core.Operations;

namespace UVtools.UI.Controls.Tools;

public partial class ToolLayerExportImageControl : ToolControl
{
    public OperationLayerExportImage Operation => (BaseOperation as OperationLayerExportImage)!;
    public ToolLayerExportImageControl()
    {
        BaseOperation = new OperationLayerExportImage(SlicerFile!);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }

    public async Task ChooseFolder()
    {
        var folders = await App.MainWindow.OpenFolderPickerAsync(SlicerFile!);
        if (folders.Count == 0) return;
        Operation.OutputFolder = folders[0].TryGetLocalPath()!;
    }
}