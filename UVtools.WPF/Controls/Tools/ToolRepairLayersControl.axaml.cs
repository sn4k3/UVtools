using UVtools.Core.Operations;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Tools;

public partial class ToolRepairLayersControl : ToolControl
{
    public OperationRepairLayers Operation => BaseOperation as OperationRepairLayers;

    public ToolRepairLayersControl()
    {
        BaseOperation = new OperationRepairLayers(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }

    public static OperationRepairLayers GetOperationDisabledRepair() => new (App.SlicerFile)
    {
        RepairIslands = false,
        RepairResinTraps = false,
        RepairSuctionCups = false,
        RemoveEmptyLayers = false,
        RemoveIslandsBelowEqualPixelCount = 0,
        RemoveIslandsRecursiveIterations = 0,
        AttachIslandsBelowLayers = 0,
        ResinTrapsOverlapBy = 0,
        SuctionCupsVentHole = 0,
        GapClosingIterations = 0,
        NoiseRemovalIterations = 0,
        IssuesDetectionConfig = App.MainWindow.GetIssuesDetectionConfiguration()
    };

    public static OperationRepairLayers GetOperationRepairLayers() => new(App.SlicerFile)
    {
        RepairIslands = UserSettings.Instance.LayerRepair.RepairIslands,
        RepairResinTraps = UserSettings.Instance.LayerRepair.RepairResinTraps,
        RepairSuctionCups = UserSettings.Instance.LayerRepair.RepairSuctionCups,
        RemoveEmptyLayers = UserSettings.Instance.LayerRepair.RemoveEmptyLayers,
        RemoveIslandsBelowEqualPixelCount = UserSettings.Instance.LayerRepair.RemoveIslandsBelowEqualPixels,
        RemoveIslandsRecursiveIterations = UserSettings.Instance.LayerRepair.RemoveIslandsRecursiveIterations,
        AttachIslandsBelowLayers = UserSettings.Instance.LayerRepair.AttachIslandsBelowLayers,
        ResinTrapsOverlapBy = UserSettings.Instance.LayerRepair.ResinTrapsOverlapBy,
        SuctionCupsVentHole = UserSettings.Instance.LayerRepair.SuctionCupsVentHole,
        GapClosingIterations = UserSettings.Instance.LayerRepair.ClosingIterations,
        NoiseRemovalIterations = UserSettings.Instance.LayerRepair.OpeningIterations,
        IssuesDetectionConfig = App.MainWindow.GetIssuesDetectionConfiguration()
    };

    public void SetFromUserSettings()
    {
        Operation.RepairIslands = UserSettings.Instance.LayerRepair.RepairIslands;
        Operation.RepairResinTraps = UserSettings.Instance.LayerRepair.RepairResinTraps;
        Operation.RepairSuctionCups = UserSettings.Instance.LayerRepair.RepairSuctionCups;
        Operation.RemoveEmptyLayers = UserSettings.Instance.LayerRepair.RemoveEmptyLayers;
        Operation.RemoveIslandsBelowEqualPixelCount = UserSettings.Instance.LayerRepair.RemoveIslandsBelowEqualPixels;
        Operation.RemoveIslandsRecursiveIterations = UserSettings.Instance.LayerRepair.RemoveIslandsRecursiveIterations;
        Operation.AttachIslandsBelowLayers = UserSettings.Instance.LayerRepair.AttachIslandsBelowLayers;
        Operation.ResinTrapsOverlapBy = UserSettings.Instance.LayerRepair.ResinTrapsOverlapBy;
        Operation.SuctionCupsVentHole = UserSettings.Instance.LayerRepair.SuctionCupsVentHole;
        Operation.GapClosingIterations = UserSettings.Instance.LayerRepair.ClosingIterations;
        Operation.NoiseRemovalIterations = UserSettings.Instance.LayerRepair.OpeningIterations;
    }

    public override void Callback(ToolWindow.Callbacks callback)
    {
        switch (callback)
        {
            case ToolWindow.Callbacks.Init:
                ParentWindow.LayerRangeVisible = false;
                ParentWindow.IsCheckBox1Visible = true;

                SetFromUserSettings();
                Operation.IssuesDetectionConfig = App.MainWindow.GetIssuesDetectionConfiguration();
                break;
            case ToolWindow.Callbacks.AfterLoadProfile:
                Operation.IssuesDetectionConfig = App.MainWindow.GetIssuesDetectionConfiguration();
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