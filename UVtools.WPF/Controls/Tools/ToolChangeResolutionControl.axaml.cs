using System.Diagnostics;
using System.Timers;
using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools;

public class ToolChangeResolutionControl : ToolControl
{
    private OperationChangeResolution.Resolution _selectedPresetItem;
    public OperationChangeResolution Operation => BaseOperation as OperationChangeResolution;

    public OperationChangeResolution.Resolution SelectedPresetItem
    {
        get => _selectedPresetItem;
        set
        {
            RaiseAndSetIfChanged(ref _selectedPresetItem, value);
            if (_selectedPresetItem is null || _selectedPresetItem.IsEmpty) return;
            Operation.NewResolutionX = _selectedPresetItem.ResolutionX;
            Operation.NewResolutionY = _selectedPresetItem.ResolutionY;
                
            //SelectedPresetItem = null;
            Timer timer = new(1);
            timer.Elapsed += (sender, args) =>
            {
                SelectedPresetItem = null;
                timer.Dispose();
            };
            timer.Start();
        }
    }

    public ToolChangeResolutionControl()
    {
        BaseOperation = new OperationChangeResolution(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}