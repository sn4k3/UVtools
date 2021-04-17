using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolBlurControl : ToolControl
    {
        private KernelControl _kernelCtrl;
        public OperationBlur Operation => BaseOperation as OperationBlur;

        public ToolBlurControl()
        {
            InitializeComponent();
            BaseOperation = new OperationBlur(SlicerFile);
            _kernelCtrl = this.Find<KernelControl>("KernelCtrl");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override bool UpdateOperation()
        {
            Operation.Kernel.Matrix = _kernelCtrl.GetMatrix();
            Operation.Kernel.Anchor = _kernelCtrl.Anchor;
            return Operation.Kernel.Matrix is not null;
        }

    }
}
