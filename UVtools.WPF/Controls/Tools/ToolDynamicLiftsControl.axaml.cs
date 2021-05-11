using System.Linq;
using Avalonia.Markup.Xaml;
using MoreLinq.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;
using UVtools.WPF.Extensions;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolDynamicLiftsControl : ToolControl
    {
        public OperationDynamicLifts Operation => BaseOperation as OperationDynamicLifts;
        public ToolDynamicLiftsControl()
        {
            InitializeComponent();
            BaseOperation = new OperationDynamicLifts(SlicerFile);
            if (!SlicerFile.HavePrintParameterPerLayerModifier(FileFormat.PrintParameterModifier.LiftHeight) ||
                !SlicerFile.HavePrintParameterPerLayerModifier(FileFormat.PrintParameterModifier.LiftSpeed))
            {
                App.MainWindow.MessageBoxInfo("Your printer/format does not support this tool.", "Dynamic lifts - Printer not supported");
                CanRun = false;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ViewSmallestLayer(bool isBottom)
        {
            var layerFound = Operation.GetSmallestLayer(isBottom);
            if (layerFound is null) return;
            App.MainWindow.ActualLayer = layerFound.Index;
        }

        public void ViewLargestLayer(bool isBottom)
        {
            var layerFound = Operation.GetLargestLayer(isBottom);
            if (layerFound is null) return;
            App.MainWindow.ActualLayer = layerFound.Index;
        }
    }
}
