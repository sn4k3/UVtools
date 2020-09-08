/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Drawing;
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;

namespace UVtools.GUI.Controls.Tools
{
    public partial class CtrlToolChangeResolution : CtrlToolWindowContent
    {
        public OperationChangeResolution Operation { get; }

        public uint NewResolutionX => (uint) nmNewX.Value;
        public uint NewResolutionY => (uint) nmNewY.Value;

        public CtrlToolChangeResolution()
        {
            InitializeComponent();
            ButtonOkEnabled = false;
            Operation = new OperationChangeResolution(Program.FrmMain.ActualLayerImage.Size, Program.SlicerFile.LayerManager.BoundingRectangle);
            SetOperation(Operation);

            lbCurrent.Text = $"Current resolution (X/Y): {Operation.OldResolution.Width} x {Operation.OldResolution.Height}";
            lbObjectVolume.Text = $"Object volume (X/Y): {Operation.VolumeBonds.Width} x {Operation.VolumeBonds.Height}";
            nmNewX.Value = Operation.OldResolution.Width;
            nmNewY.Value = Operation.OldResolution.Height;

            foreach (var resolution in OperationChangeResolution.GetResolutions())
            {
                cbPreset.Items.Add(resolution);
            }
        }

        private void EventValueChanged(object sender, System.EventArgs e)
        {
            if (ReferenceEquals(sender, nmNewX) || ReferenceEquals(sender, nmNewY))
            {
                ButtonOkEnabled = Operation.OldResolution.Width != NewResolutionX ||
                                  Operation.OldResolution.Height != NewResolutionY;
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

        public override void UpdateOperation()
        {
            Operation.NewResolutionX = NewResolutionX;
            Operation.NewResolutionY = NewResolutionY;
        }
    }
}
