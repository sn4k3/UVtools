using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public partial class ToolTimelapseControl : ToolControl
    {
        public OperationTimelapse Operation => BaseOperation as OperationTimelapse;

        public ToolTimelapseControl()
        {
            BaseOperation = new OperationTimelapse(SlicerFile);
            if (!ValidateSpawn()) return;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
