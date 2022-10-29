using Avalonia;
using Avalonia.Markup.Xaml;
using UVtools.Core.Objects;
using UVtools.WPF.Extensions;

namespace UVtools.WPF.Controls;

public class KernelControl : UserControlEx
{
    private KernelConfiguration _kernel;

    public static readonly DirectProperty<KernelControl, KernelConfiguration> KernelProperty =
        AvaloniaProperty.RegisterDirect<KernelControl, KernelConfiguration>(
            nameof(Kernel),
            o => o.Kernel,
            (o, v) => o.Kernel = v);

    public KernelConfiguration Kernel
    {
        get => _kernel;
        set => SetAndRaise(KernelProperty, ref _kernel, value);
    }

        

    public KernelControl()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void GenerateKernel()
    {
        if (Kernel.MatrixWidth <= Kernel.AnchorX || Kernel.MatrixHeight <= Kernel.AnchorY)
        {
            App.MainWindow.MessageBoxError("Anchor position X/Y can't be higher or equal than size X/Y\nPlease fix the values.", "Invalid anchor position").ConfigureAwait(false);
            return;
        }

        Kernel.GenerateKernelText();
        Kernel.KernelMat?.Dispose();
        Kernel.KernelMat = null;
    }

    /*public Kernel GetKernel()
    {
        var matrix = GetMatrix();
        return matrix is null ? null : new Kernel(matrix, Anchor, UseDynamicKernel);
    }*/
}