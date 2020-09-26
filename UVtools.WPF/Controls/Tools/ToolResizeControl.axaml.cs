using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolResizeControl : ToolControl
    {
        public OperationResize Operation { get; }

        public ToolResizeControl()
        {
            InitializeComponent();
            BaseOperation = Operation = new OperationResize();
            DataContext = this;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
