using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using UVtools.Core.Operations;

namespace UVtools.UI.Controls.Tools
{
    public partial class ToolLayerExportHtmlControl : ToolControl
    {
        public OperationLayerExportHtml Operation => (BaseOperation as OperationLayerExportHtml)!;
        public ToolLayerExportHtmlControl()
        {
            BaseOperation = new OperationLayerExportHtml(SlicerFile!);
            if (!ValidateSpawn()) return;
            InitializeComponent();
        }

        public async Task ChooseFilePath()
        {
            using var file = await App.MainWindow.SaveFilePickerAsync(SlicerFile!, AvaloniaStatic.HtmlFileFilter);
            if (file?.TryGetLocalPath() is not { } filePath) return;

            Operation.FilePath = filePath;
        }
    }
}
