using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolRotateControl : ToolControl
    {
        public OperationRotate Operation => BaseOperation as OperationRotate;

        public ToolRotateControl()
        {
            BaseOperation = new OperationRotate(SlicerFile);
            if (!ValidateSpawn()) return;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
