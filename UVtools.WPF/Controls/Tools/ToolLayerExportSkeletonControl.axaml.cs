using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.IO;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools;

public partial class ToolLayerExportSkeletonControl : ToolControl
{
    public OperationLayerExportSkeleton Operation => BaseOperation as OperationLayerExportSkeleton;
    public ToolLayerExportSkeletonControl()
    {
        BaseOperation = new OperationLayerExportSkeleton(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public async void ChooseFilePath()
    {
        var dialog = new SaveFileDialog
        {
            Filters = Helpers.ImagesFullFileFilter,
            InitialFileName = Path.GetFileName(SlicerFile.FileFullPath) + ".skeleton.png",
            Directory = Path.GetDirectoryName(SlicerFile.FileFullPath),
        };
        var file = await dialog.ShowAsync(ParentWindow);
        if (string.IsNullOrWhiteSpace(file)) return;
        Operation.FilePath = file;
    }
}