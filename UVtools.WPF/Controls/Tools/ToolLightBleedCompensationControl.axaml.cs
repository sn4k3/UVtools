using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public partial class ToolLightBleedCompensationControl : ToolControl
    {
        public OperationLightBleedCompensation Operation => BaseOperation as OperationLightBleedCompensation;
        public ToolLightBleedCompensationControl()
        {
            InitializeComponent();
            BaseOperation = new OperationLightBleedCompensation(SlicerFile);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
