/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Drawing;
using UVtools.Core.Operations;

namespace UVtools.GUI.Controls.Tools
{
    public partial class CtrlToolChangeResolution : CtrlToolWindowContent
    {
        public OperationChangeResolution Operation { get; }

        public uint NewResolutionX => (uint) nmNewX.Value;
        public uint NewResolutionY => (uint) nmNewY.Value;

        public override string ConfirmationText => Operation.ConfirmationText;

        public CtrlToolChangeResolution(uint oldResolutionX, uint oldResolutionY, Rectangle volumeBonds)
        {
            InitializeComponent();
            Text = "Change Resolution";
            Operation = new OperationChangeResolution(oldResolutionX, oldResolutionY, volumeBonds);

            lbCurrent.Text = $"Current resolution (X/Y): {oldResolutionX} x {oldResolutionY}";
            lbObjectVolume.Text = $"Object volume (X/Y): {volumeBonds.Width} x {volumeBonds.Height}";
            nmNewX.Value = oldResolutionX;
            nmNewY.Value = oldResolutionY;

            foreach (var resolution in OperationChangeResolution.GetResolutions())
            {
                cbPreset.Items.Add(resolution);
            }
        }

        private void EventValueChanged(object sender, System.EventArgs e)
        {
            if (ReferenceEquals(sender, nmNewX) || ReferenceEquals(sender, nmNewY))
            {
                ButtonOkEnabled = Operation.OldResolutionX != NewResolutionX ||
                                  Operation.OldResolutionY != NewResolutionY;
                return;
            }

            if (ReferenceEquals(sender, cbPreset))
            {
                if (cbPreset.SelectedIndex < 0) return;
                var resolution = (OperationChangeResolution.Resolution) cbPreset.SelectedItem;
                nmNewX.Value = resolution.ResolutionX;
                nmNewY.Value = resolution.ResolutionY;
                cbPreset.SelectedIndex = -1;

                return;
            }
        }

        public override bool ValidateForm()
        {
            Operation.NewResolutionX = NewResolutionX;
            Operation.NewResolutionY = NewResolutionY;
            return ValidateFormFromString(Operation.Validate());
        }
    }
}
