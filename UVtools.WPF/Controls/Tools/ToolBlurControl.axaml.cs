using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolBlurControl : ToolControl
    {
        private int _selectedAlgorithmIndex;
        private bool _isSizeEnabled = true;
        private bool _isKernelVisible;
        private KernelControl _kernelCtrl;
        public OperationBlur Operation { get; }

        public int SelectedAlgorithmIndex
        {
            get => _selectedAlgorithmIndex;
            set
            {
                if(!RaiseAndSetIfChanged(ref _selectedAlgorithmIndex, value) || value < 0) return;
                Operation.BlurOperation = (OperationBlur.BlurAlgorithm) OperationBlur.BlurTypes[_selectedAlgorithmIndex].Tag;
                IsKernelVisible = Operation.BlurOperation == OperationBlur.BlurAlgorithm.Filter2D;
                IsSizeEnabled = Operation.BlurOperation != OperationBlur.BlurAlgorithm.Pyramid &&
                                Operation.BlurOperation != OperationBlur.BlurAlgorithm.Filter2D;
            }
        }

        public bool IsSizeEnabled
        {
            get => _isSizeEnabled;
            set => RaiseAndSetIfChanged(ref _isSizeEnabled, value);
        }

        public bool IsKernelVisible
        {
            get => _isKernelVisible;
            set => RaiseAndSetIfChanged(ref _isKernelVisible, value);
        }

        public ToolBlurControl()
        {
            InitializeComponent();
            BaseOperation = Operation = new OperationBlur();
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
            return !(Operation.Kernel.Matrix is null);
        }
    }
}
