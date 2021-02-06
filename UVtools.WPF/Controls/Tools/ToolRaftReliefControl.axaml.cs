using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolRaftReliefControl : ToolControl
    {
        public OperationRaftRelief Operation => BaseOperation as OperationRaftRelief;

        public ToolRaftReliefControl()
        {
            this.InitializeComponent();
            BaseOperation = new OperationRaftRelief(SlicerFile);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
