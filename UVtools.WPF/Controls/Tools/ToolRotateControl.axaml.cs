using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolRotateControl : ToolControl
    {
        public OperationRotate Operation => BaseOperation as OperationRotate;

        public ToolRotateControl()
        {
            InitializeComponent();
            BaseOperation = new OperationRotate();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
