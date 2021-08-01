using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolFlipControl : ToolControl
    {
        public OperationFlip Operation => BaseOperation as OperationFlip;

        public ToolFlipControl()
        {
            BaseOperation = new OperationFlip(SlicerFile);
            if (!ValidateSpawn()) return;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
