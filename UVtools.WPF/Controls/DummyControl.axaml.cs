using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace UVtools.WPF.Controls;

public class DummyControl : UserControl
{
    public DummyControl()
    {
        this.InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}