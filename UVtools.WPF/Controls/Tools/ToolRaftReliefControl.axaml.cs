using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolRaftReliefControl : ToolControl
    {
        public OperationRaftRelief Operation => BaseOperation as OperationRaftRelief;

        public ToolRaftReliefControl()
        {
            BaseOperation = new OperationRaftRelief(SlicerFile);
            if (!ValidateSpawn()) return;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void UseCurrentLayerAsMask()
        {
            Operation.MaskLayerIndex = App.MainWindow.ActualLayer;
        }
    }
}
