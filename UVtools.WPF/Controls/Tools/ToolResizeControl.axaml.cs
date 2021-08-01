using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolResizeControl : ToolControl
    {
        public OperationResize Operation => BaseOperation as OperationResize;

        public ToolResizeControl()
        {
            BaseOperation = new OperationResize(SlicerFile);
            if (!ValidateSpawn()) return;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
