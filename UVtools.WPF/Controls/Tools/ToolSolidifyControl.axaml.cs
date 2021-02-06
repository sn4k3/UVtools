using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolSolidifyControl : ToolControl
    {
        public OperationSolidify Operation => BaseOperation as OperationSolidify;
        public ToolSolidifyControl()
        {
            InitializeComponent();
            BaseOperation = new OperationSolidify(SlicerFile);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
