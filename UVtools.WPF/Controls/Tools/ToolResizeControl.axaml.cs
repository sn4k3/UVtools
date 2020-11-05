using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolResizeControl : ToolControl
    {
        public OperationResize Operation => BaseOperation as OperationResize;

        public ToolResizeControl()
        {
            InitializeComponent();
            BaseOperation = new OperationResize();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
