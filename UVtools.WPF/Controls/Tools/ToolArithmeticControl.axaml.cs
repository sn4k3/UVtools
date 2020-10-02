using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolArithmeticControl : ToolControl
    {
        public OperationArithmetic Operation { get; }

        public ToolArithmeticControl()
        {
            InitializeComponent();
            BaseOperation = Operation = new OperationArithmetic();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
