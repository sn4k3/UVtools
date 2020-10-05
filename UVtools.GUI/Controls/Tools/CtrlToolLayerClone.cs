/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using UVtools.Core.Operations;

namespace UVtools.GUI.Controls.Tools
{
    public partial class CtrlToolLayerClone : CtrlToolWindowContent
    {
        public OperationLayerClone Operation { get; }


        public CtrlToolLayerClone()
        {
            InitializeComponent();
            Operation = new OperationLayerClone();
            SetOperation(Operation);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ParentToolWindow.nmLayerRangeStart.ValueChanged += EventValueChanged;
            ParentToolWindow.nmLayerRangeEnd.ValueChanged += EventValueChanged;
            EventValueChanged(this, EventArgs.Empty);
        }

        private void EventValueChanged(object sender, EventArgs e)
        {
            uint extraLayers = (uint)Math.Max(0, (ParentToolWindow.nmLayerRangeEnd.Value - ParentToolWindow.nmLayerRangeStart.Value + 1) * nmClones.Value);
            float extraHeight = (float)Math.Round(extraLayers * Program.SlicerFile.LayerHeight, 2);
            lbLayersCount.Text = $"Result layers: {Program.SlicerFile.LayerCount} → {Program.SlicerFile.LayerCount + extraLayers} (+ {extraLayers})";
            lbHeights.Text = $"Result heights: {Program.SlicerFile.TotalHeight}mm → {Program.SlicerFile.TotalHeight + extraHeight}mm (+ {extraHeight}mm)";
        }

        public override bool UpdateOperation()
        {
            base.UpdateOperation();
            Operation.Clones = (uint) nmClones.Value;
            return true;
        }
    }
}
