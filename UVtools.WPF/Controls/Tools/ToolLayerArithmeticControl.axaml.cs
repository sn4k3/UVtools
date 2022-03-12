using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Tools;

public class ToolLayerArithmeticControl : ToolControl
{
    public OperationLayerArithmetic Operation => BaseOperation as OperationLayerArithmetic;

    public ToolLayerArithmeticControl()
    {
        BaseOperation = new OperationLayerArithmetic(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void Callback(ToolWindow.Callbacks callback)
    {
        switch (callback)
        {
            case ToolWindow.Callbacks.Init:
            case ToolWindow.Callbacks.Loaded:
                if(ParentWindow is not null) ParentWindow.ButtonOkEnabled = !string.IsNullOrWhiteSpace(Operation.Sentence);
                Operation.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(Operation.Sentence))
                    {
                        ParentWindow.ButtonOkEnabled = !string.IsNullOrWhiteSpace(Operation.Sentence);
                    }
                };
                break;
        }
    }
}