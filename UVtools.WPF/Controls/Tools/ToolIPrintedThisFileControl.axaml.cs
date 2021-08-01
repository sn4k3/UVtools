using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolIPrintedThisFileControl : ToolControl
    {
        public OperationIPrintedThisFile Operation => BaseOperation as OperationIPrintedThisFile;

        public ToolIPrintedThisFileControl()
        {
            BaseOperation = new OperationIPrintedThisFile(SlicerFile);
            if (!ValidateSpawn()) return;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
