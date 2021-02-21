using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolIPrintedThisFileControl : ToolControl
    {
        public OperationIPrintedThisFile Operation => BaseOperation as OperationIPrintedThisFile;

        public ToolIPrintedThisFileControl()
        {
            InitializeComponent();
            BaseOperation = new OperationIPrintedThisFile(SlicerFile);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
