using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolLayerReHeightControl : ToolControl
    {
        public OperationLayerReHeight Operation => BaseOperation as OperationLayerReHeight;

        public string CurrentLayers => $"Current layers: {App.SlicerFile.LayerCount} at {App.SlicerFile.LayerHeight}mm";

        public ToolLayerReHeightControl()
        {
            BaseOperation = new OperationLayerReHeight(SlicerFile);
            if (!ValidateSpawn()) return;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
