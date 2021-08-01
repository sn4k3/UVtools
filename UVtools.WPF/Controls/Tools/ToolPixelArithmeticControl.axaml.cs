using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public partial class ToolPixelArithmeticControl : ToolControl
    {
        public OperationPixelArithmetic Operation => BaseOperation as OperationPixelArithmetic;
        public ToolPixelArithmeticControl()
        {
            BaseOperation = new OperationPixelArithmetic(SlicerFile);
            if (!ValidateSpawn()) return;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
