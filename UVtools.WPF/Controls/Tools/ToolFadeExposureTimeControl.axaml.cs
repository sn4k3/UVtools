using System;
using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Tools;

public partial class ToolFadeExposureTimeControl : ToolControl
{
    public OperationFadeExposureTime Operation => BaseOperation as OperationFadeExposureTime;

    public ToolFadeExposureTimeControl()
    {
        BaseOperation = new OperationFadeExposureTime(SlicerFile);
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
                ParentWindow.LayerIndexEnd = Operation.LayerIndexStart + Operation.LayerCount - 1;
                Operation.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName != nameof(Operation.LayerCount)) return;
                    ParentWindow.LayerIndexEnd = Operation.LayerIndexStart + Operation.LayerCount - 1;
                };
                ParentWindow.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName is nameof(ParentWindow.LayerIndexStart) or nameof(ParentWindow.LayerIndexEnd))
                    {
                        ParentWindow.LayerIndexEnd = Operation.LayerIndexStart + Operation.LayerCount - 1;
                    }
                };
                break;
        }

    }
}