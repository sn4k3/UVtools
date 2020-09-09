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
        }

        public override bool UpdateOperation()
        {
            base.UpdateOperation();
            Operation.Threshold = (byte) nmThreshold.Value;
            Operation.Maximum = (byte) nmMaximum.Value;
            Operation.Type = (ThresholdType)cbThresholdType.SelectedItem;
            return true;
        }

        private void EventClick(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, btnPresetFreeUse))
            {
                nmThreshold.Enabled =
                    nmMaximum.Enabled =
                        cbThresholdType.Enabled = true;
                return;
            }

            if (ReferenceEquals(sender, btnPresetStripAntiAliasing))
            {
                EventClick(btnPresetFreeUse, e);
                nmMaximum.Enabled = cbThresholdType.Enabled = false;

                nmThreshold.Value = 127;
                nmMaximum.Value = 255;
                cbThresholdType.SelectedItem = ThresholdType.Binary;
                nmThreshold.Focus();
                return;
            }

            if (ReferenceEquals(sender, btnPresetSetPixelsBrightness))
            {
                EventClick(btnPresetFreeUse, e);
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
