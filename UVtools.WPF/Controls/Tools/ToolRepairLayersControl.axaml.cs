using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolRepairLayersControl : ToolControl
    {
        public OperationRepairLayers Operation => BaseOperation as OperationRepairLayers;

        public ToolRepairLayersControl()
        {
            BaseOperation = new OperationRepairLayers(SlicerFile);
            if (!ValidateSpawn()) return;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static OperationRepairLayers GetOperationRepairLayers() => new (App.SlicerFile)
        {
            RepairIslands = UserSettings.Instance.LayerRepair.RepairIslands,
            RepairResinTraps = UserSettings.Instance.LayerRepair.RepairResinTraps,
            RemoveEmptyLayers = UserSettings.Instance.LayerRepair.RemoveEmptyLayers,
            RemoveIslandsBelowEqualPixelCount = UserSettings.Instance.LayerRepair.RemoveIslandsBelowEqualPixels,
            RemoveIslandsRecursiveIterations = UserSettings.Instance.LayerRepair.RemoveIslandsRecursiveIterations,
            AttachIslandsBelowLayers = UserSettings.Instance.LayerRepair.AttachIslandsBelowLayers,
            ResinTrapsOverlapBy = UserSettings.Instance.LayerRepair.ResinTrapsOverlapBy,
            GapClosingIterations = UserSettings.Instance.LayerRepair.ClosingIterations,
            NoiseRemovalIterations = UserSettings.Instance.LayerRepair.OpeningIterations,
        };

        public void SetFromUserSettings()
        {
            Operation.RepairIslands = UserSettings.Instance.LayerRepair.RepairIslands;
            Operation.RepairResinTraps = UserSettings.Instance.LayerRepair.RepairResinTraps;
            Operation.RemoveEmptyLayers = UserSettings.Instance.LayerRepair.RemoveEmptyLayers;
            Operation.RemoveIslandsBelowEqualPixelCount = UserSettings.Instance.LayerRepair.RemoveIslandsBelowEqualPixels;
            Operation.RemoveIslandsRecursiveIterations = UserSettings.Instance.LayerRepair.RemoveIslandsRecursiveIterations;
            Operation.AttachIslandsBelowLayers = UserSettings.Instance.LayerRepair.AttachIslandsBelowLayers;
            Operation.ResinTrapsOverlapBy = UserSettings.Instance.LayerRepair.ResinTrapsOverlapBy;
            Operation.GapClosingIterations = UserSettings.Instance.LayerRepair.ClosingIterations;
            Operation.NoiseRemovalIterations = UserSettings.Instance.LayerRepair.OpeningIterations;
        }

        public override void Callback(ToolWindow.Callbacks callback)
        {
            switch (callback)
            {
                case ToolWindow.Callbacks.Init:
                case ToolWindow.Callbacks.Loaded:
                    if (callback is ToolWindow.Callbacks.Init)
                    {
                        ParentWindow.LayerRangeVisible = false;
                        ParentWindow.IsCheckBox1Visible = true;
                    }

                    SetFromUserSettings();
                    break;
                case ToolWindow.Callbacks.Checkbox1:
                    ParentWindow.LayerRangeVisible = ParentWindow.IsCheckBox1Checked;
                    if (!ParentWindow.IsCheckBox1Checked)
                    {
                        ParentWindow.SelectAllLayers();
                    }
                    break;
            }
        }
    }
}
