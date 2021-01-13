using System;
using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolLayerCloneControl : ToolControl
    {
        public OperationLayerClone Operation => BaseOperation as OperationLayerClone;

        public uint ExtraLayers => (uint)Math.Max(0, ((int)Operation.LayerIndexEnd - Operation.LayerIndexStart + 1) * Operation.Clones);

        public string InfoLayersStr
        {
            get
            {
                uint extraLayers = ExtraLayers;
                return $"Layers: {App.SlicerFile.LayerCount} → {App.SlicerFile.LayerCount + extraLayers} (+ {extraLayers})";
            }
        }

        public string InfoHeightsStr
        {
            get
            {
                float extraHeight = (float)Math.Round(ExtraLayers * App.SlicerFile.LayerHeight, 2);
                return $"Height: {App.SlicerFile.PrintHeight}mm → {Math.Round(App.SlicerFile.PrintHeight + extraHeight, 2)}mm (+ {extraHeight}mm)";
            }
        }

        public ToolLayerCloneControl()
        {
            InitializeComponent();
            BaseOperation = new OperationLayerClone();
            Operation.PropertyChanged += (sender, args) =>
            {
                RaisePropertyChanged(nameof(InfoLayersStr));
                RaisePropertyChanged(nameof(InfoHeightsStr));
            };

        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
