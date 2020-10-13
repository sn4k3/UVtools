/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Windows.Forms;
using UVtools.Core.Operations;

namespace UVtools.GUI.Controls.Tools
{
    public partial class CtrlToolRepairLayers : CtrlToolWindowContent
    {
        public OperationRepairLayers Operation { get; }
        
        public CtrlToolRepairLayers()
        {
            InitializeComponent();
            Operation = new OperationRepairLayers();
            SetOperation(Operation);

            cbRepairIslands.Checked = Properties.Settings.Default.LayerRepairLayersIslands;
            cbRepairResinTraps.Checked = Properties.Settings.Default.LayerRepairResinTraps;
            cbRemoveEmptyLayers.Checked = Properties.Settings.Default.LayerRepairRemoveEmptyLayers;
            nmRemoveIslandsBelowEqualPixels.Value = Properties.Settings.Default.LayerRepairRemoveIslandsBelowEqualPixelsDefault;
            nmRemoveIslandsRecursiveIterations.Value = Properties.Settings.Default.LayerRepairRemoveIslandsRecursiveIterations;

            nmClosingIterations.Value = Properties.Settings.Default.LayerRepairDefaultClosingIterations;
            nmOpeningIterations.Value = Properties.Settings.Default.LayerRepairDefaultOpeningIterations;
            
            //nmClosingIterations.Select();
        }

        public override bool UpdateOperation()
        {
            base.UpdateOperation();
            Operation.RepairIslands = cbRepairIslands.Checked;
            Operation.RepairResinTraps = cbRepairResinTraps.Checked;
            Operation.RemoveEmptyLayers = cbRemoveEmptyLayers.Checked;
            Operation.RemoveIslandsBelowEqualPixelCount = (byte) nmRemoveIslandsBelowEqualPixels.Value;
            Operation.RemoveIslandsRecursiveIterations = (ushort) nmRemoveIslandsRecursiveIterations.Value;
            Operation.GapClosingIterations = (uint) nmClosingIterations.Value;
            Operation.NoiseRemovalIterations = (uint) nmOpeningIterations.Value;

            return true;
        }

        public override void ExtraActionCall(object sender)
        {
            if (ReferenceEquals(sender, ParentToolWindow.cbActionExtra))
            {
                LayerRangeVisible = groupAdvancedSettings.Visible = ParentToolWindow.cbActionExtra.Checked;
                return;
            }
        }
    }
}
