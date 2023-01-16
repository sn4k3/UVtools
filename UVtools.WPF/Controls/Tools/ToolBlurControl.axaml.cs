using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools;

public class ToolBlurControl : ToolControl
{
    public OperationBlur Operation => BaseOperation as OperationBlur;

    public ToolBlurControl()
    {
        BaseOperation = new OperationBlur(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}