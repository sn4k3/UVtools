using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public partial class ToolLayerExportHeatMapControl : ToolControl
    {
        public OperationLayerExportHeatMap Operation => BaseOperation as OperationLayerExportHeatMap;
        public ToolLayerExportHeatMapControl()
        {
            BaseOperation = new OperationLayerExportHeatMap(SlicerFile);
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
                InitialFileName = Path.GetFileName(SlicerFile.FileFullPath) + ".heatmap.png",
                Directory = Path.GetDirectoryName(SlicerFile.FileFullPath),
            };
            var file = await dialog.ShowAsync(ParentWindow);
            if (string.IsNullOrWhiteSpace(file)) return;
            Operation.FilePath = file;
        }
    }
}
