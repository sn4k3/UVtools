using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Tools;

public partial class ToolTimelapseControl : ToolControl
{
    public OperationTimelapse Operation => BaseOperation as OperationTimelapse;

    public ToolTimelapseControl()
    {
        BaseOperation = new OperationTimelapse(SlicerFile);
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
            case ToolWindow.Callbacks.AfterLoadProfile:
                Operation.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(Operation.NumberOfLifts))
                    {
                        ParentWindow.ButtonOkEnabled = Operation.NumberOfLifts > 0;
                        return;
                    }
                };
                break;
        }
    }
}