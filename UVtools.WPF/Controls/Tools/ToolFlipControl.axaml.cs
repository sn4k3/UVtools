using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolFlipControl : ToolControl
    {
        public OperationFlip Operation => BaseOperation as OperationFlip;

        public ToolFlipControl()
        {
            InitializeComponent();
            BaseOperation = new OperationFlip(SlicerFile);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
