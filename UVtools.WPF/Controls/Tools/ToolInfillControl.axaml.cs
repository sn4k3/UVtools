using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolInfillControl : ToolControl
    {
        public OperationInfill Operation => BaseOperation as OperationInfill;

        public ToolInfillControl()
        {
            InitializeComponent();
            BaseOperation = new OperationInfill(SlicerFile);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
