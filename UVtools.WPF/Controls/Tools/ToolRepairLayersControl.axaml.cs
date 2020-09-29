using System.Diagnostics;
using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolRepairLayersControl : ToolControl
    {
        public OperationRepairLayers Operation { get; }

        public ToolRepairLayersControl()
        {
            InitializeComponent();
            BaseOperation = Operation = new OperationRepairLayers
            {
                RepairIslands = UserSettings.Instance.LayerRepair.RepairIslands,
                RepairResinTraps = UserSettings.Instance.LayerRepair.RepairResinTraps,
                RemoveEmptyLayers = UserSettings.Instance.LayerRepair.RemoveEmptyLayers,
                RemoveIslandsBelowEqualPixelCount = UserSettings.Instance.LayerRepair.RemoveIslandsBelowEqualPixels,
                GapClosingIterations = UserSettings.Instance.LayerRepair.ClosingIterations,
                NoiseRemovalIterations = UserSettings.Instance.LayerRepair.OpeningIterations,
            };
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
                    ParentWindow.LayerRangeVisible = false;
                    ParentWindow.IsCheckBox1Visible = true;
                    DataContext = this;
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
