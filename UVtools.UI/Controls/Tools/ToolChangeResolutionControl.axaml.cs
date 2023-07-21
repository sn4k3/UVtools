using Avalonia.Threading;
using System.Collections.Generic;
using UVtools.Core.Operations;
using UVtools.Core.Printer;

namespace UVtools.UI.Controls.Tools;

public partial class ToolChangeResolutionControl : ToolControl
{
    public static IEnumerable<Machine> MachinePresets => Machine.Machines;

    public OperationChangeResolution Operation => (BaseOperation as OperationChangeResolution)!;
    private OperationChangeResolution.Resolution? _selectedPresetItem;
    public OperationChangeResolution.Resolution? SelectedPresetItem
    {
        get => _selectedPresetItem;
        set
        {
            RaiseAndSetIfChanged(ref _selectedPresetItem, value);
            if (_selectedPresetItem is null || _selectedPresetItem.IsEmpty) return;
            Operation.NewResolutionX = _selectedPresetItem.ResolutionX;
            Operation.NewResolutionY = _selectedPresetItem.ResolutionY;
            Dispatcher.UIThread.Post(() => SelectedPresetItem = null, DispatcherPriority.Loaded);
        }
    }

    private Machine? _selectedMachinePresetItem;
    public Machine? SelectedMachinePresetItem
    {
        get => _selectedMachinePresetItem;
        set
        {
            RaiseAndSetIfChanged(ref _selectedMachinePresetItem, value);
            if (_selectedMachinePresetItem is null) return;
            Operation.NewResolutionX = _selectedMachinePresetItem.ResolutionX;
            Operation.NewResolutionY = _selectedMachinePresetItem.ResolutionY;
            Operation.NewDisplayWidth = (decimal)_selectedMachinePresetItem.DisplayWidth;
            Operation.NewDisplayHeight = (decimal)_selectedMachinePresetItem.DisplayHeight;
            Operation.FixRatio = true;
            Dispatcher.UIThread.Post(() => SelectedMachinePresetItem = null, DispatcherPriority.Loaded);
        }
    }

    public ToolChangeResolutionControl()
    {
        BaseOperation = new OperationChangeResolution(SlicerFile!);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }
}