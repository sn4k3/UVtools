using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolMorphControl : ToolControl
    {
        public OperationMorph Operation => BaseOperation as OperationMorph;

        private KernelControl _kernelCtrl;

        public ToolMorphControl()
        {
            BaseOperation = new OperationMorph(SlicerFile);
            if (!ValidateSpawn()) return;
            InitializeComponent();
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
