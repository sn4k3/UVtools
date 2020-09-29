using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolMoveControl : ToolControl
    {
        public OperationMove Operation { get; }

        public ToolMoveControl()
        {
            InitializeComponent();
            BaseOperation = Operation = new OperationMove();
            DataContext = this;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
