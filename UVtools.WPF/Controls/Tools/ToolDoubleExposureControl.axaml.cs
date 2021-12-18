using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public partial class ToolDoubleExposureControl : ToolControl
    {
        public OperationDoubleExposure Operation => BaseOperation as OperationDoubleExposure;

        public ToolDoubleExposureControl()
        {
            BaseOperation = new OperationDoubleExposure(SlicerFile);
            if (!ValidateSpawn()) return;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
