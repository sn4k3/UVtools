using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolFlipControl : ToolControl
    {
        public OperationFlip Operation { get; }

        public ToolFlipControl()
        {
            InitializeComponent();
            BaseOperation = Operation = new OperationFlip();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
