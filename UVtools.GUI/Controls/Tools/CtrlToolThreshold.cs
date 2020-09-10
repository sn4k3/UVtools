/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using Emgu.CV.CvEnum;
using UVtools.Core;
using UVtools.Core.Operations;

namespace UVtools.GUI.Controls.Tools
{
    public partial class CtrlToolThreshold : CtrlToolWindowContent
    {
        public OperationThreshold Operation { get; }
        
        public CtrlToolThreshold()
        {
            InitializeComponent();
            Operation = new OperationThreshold();
            SetOperation(Operation);

            var directions = Enum.GetValues(typeof(Enumerations.FlipDirection));

            cbThresholdType.BeginUpdate();
            foreach (var thresholdType in (ThresholdType[])Enum.GetValues(typeof(ThresholdType)))
            {
                cbThresholdType.Items.Add(thresholdType);
            }
            cbThresholdType.EndUpdate();
            
            cbThresholdType.SelectedItem = ThresholdType.Binary;

            cbPresetHelpers.SelectedItem = "Free use";
        }

        public override bool UpdateOperation()
        {
            base.UpdateOperation();
            Operation.Threshold = (byte) nmThreshold.Value;
            Operation.Maximum = (byte) nmMaximum.Value;
            Operation.Type = (ThresholdType)cbThresholdType.SelectedItem;
            return true;
        }

        private void cbPresetHelpers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbPresetHelpers.SelectedItem.ToString() == "Free use")
            {
                nmThreshold.Enabled =
                    nmMaximum.Enabled =
                        cbThresholdType.Enabled = true;
                return;
            }

            if (cbPresetHelpers.SelectedItem.ToString() == "Strip AntiAliasing")
            {
                nmThreshold.Enabled =
                    nmMaximum.Enabled =
                        cbThresholdType.Enabled = true;

                nmMaximum.Enabled = cbThresholdType.Enabled = false;

                nmThreshold.Value = 127;
                nmMaximum.Value = 255;
                cbThresholdType.SelectedItem = ThresholdType.Binary;
                nmThreshold.Focus();
                return;
            }

            if (cbPresetHelpers.SelectedItem.ToString() == "Set pixel brightness")
            {
                nmThreshold.Enabled =
                    nmMaximum.Enabled =
                        cbThresholdType.Enabled = true;

                nmThreshold.Enabled = cbThresholdType.Enabled = false;

                nmThreshold.Value = 254;
                nmMaximum.Value = 254;
                cbThresholdType.SelectedItem = ThresholdType.Binary;
                nmMaximum.Focus();
                return;
            }
        }
    }
}
