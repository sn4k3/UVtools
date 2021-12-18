using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolMorphControl : ToolControl
    {
        public OperationMorph Operation => BaseOperation as OperationMorph;

        public ToolMorphControl()
        {
            BaseOperation = new OperationMorph(SlicerFile);
            if (!ValidateSpawn()) return;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
