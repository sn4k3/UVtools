using Avalonia.Markup.Xaml;
using Avalonia.Media;
using UVtools.WPF.Controls;

namespace UVtools.WPF.Windows;

public class ColorPickerWindow : WindowEx
{
    public Color ResultColor { get; set; }

    public ColorPickerWindow()
    {
        DataContext = this;
        InitializeComponent();

    }
    public ColorPickerWindow(Color defaultColor)
    {
        ResultColor = defaultColor;
        DataContext = this;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void OnClickOk()
    {
        DialogResult = DialogResults.OK;
        CloseWithResult();
    }
}