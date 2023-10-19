using Avalonia;
using UVtools.Core.Objects;
using UVtools.UI.Extensions;

namespace UVtools.UI.Controls;

public partial class KernelControl : UserControlEx
{
    private KernelConfiguration? _kernel;

    public static readonly DirectProperty<KernelControl, KernelConfiguration?> KernelProperty =
        AvaloniaProperty.RegisterDirect<KernelControl, KernelConfiguration?>(
            nameof(Kernel),
            o => o.Kernel,
            (o, v) => o.Kernel = v);

    public KernelConfiguration? Kernel
    {
        get => _kernel;
        set => SetAndRaise(KernelProperty, ref _kernel, value);
    }


    public KernelControl()
    {
        InitializeComponent();
        DataContext = this;
    }

    public async void GenerateKernel()
    {
        if (_kernel is null) return;
        if (_kernel.MatrixWidth <= _kernel.AnchorX || _kernel.MatrixHeight <= _kernel.AnchorY)
        {
            await App.MainWindow.MessageBoxError("Anchor position X/Y can't be higher or equal than size X/Y\nPlease fix the values.", "Invalid anchor position");
            return;
        }

        _kernel.GenerateKernelText();
        _kernel.KernelMat?.Dispose();
        _kernel.KernelMat = null;
    }

    /*public Kernel GetKernel()
    {
        var matrix = GetMatrix();
        return matrix is null ? null : new Kernel(matrix, Anchor, UseDynamicKernel);
    }*/
}