using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public partial class ToolCustomExposureTestControl : ToolControl
    {
        public OperationCustomExposureTest Operation => BaseOperation as OperationCustomExposureTest;

        public ToolCustomExposureTestControl()
        {
            BaseOperation = new OperationCustomExposureTest(SlicerFile);
            if (!ValidateSpawn()) return;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
