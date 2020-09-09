/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Windows.Forms;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;

namespace UVtools.GUI.Controls.Tools
{
    public partial class CtrlToolLayerReHeight : CtrlToolWindowContent
    {
        public OperationLayerReHeight Operation { get; }


        public CtrlToolLayerReHeight()
        {
            InitializeComponent();
            Operation = new OperationLayerReHeight();
            SetOperation(Operation);

            lbCurrent.Text = $"Current layers: {Program.SlicerFile.LayerCount} at {Program.SlicerFile.LayerHeight}mm";

            var items = OperationLayerReHeight.GetItems(Program.SlicerFile.LayerCount,
                (decimal) Program.SlicerFile.LayerHeight);

            if (items.Length > 0)
            {
                cbMultiplier.Items.AddRange(items);
                cbMultiplier.SelectedIndex = 0;
            }
            else
            {
                GUIExtensions.MessageBoxInformation("Not possible to re-height", "No valid configuration to be able to re-height, closing this tool now.");
                CanRun = false;
            }
        }

        private void EventValueChanged(object sender, EventArgs e)
        {
            ButtonOkEnabled = cbMultiplier.SelectedIndex >= 0;
        }

        public override void UpdateOperation()
        {
            base.UpdateOperation();
            Operation.Item = (OperationLayerReHeight.OperationLayerReHeightItem)cbMultiplier.SelectedItem;
        }
    }
}
