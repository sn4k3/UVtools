/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Windows.Forms;
using Emgu.CV.CvEnum;
using UVtools.Core.Objects;
using UVtools.Core.Operations;

namespace UVtools.GUI.Controls.Tools
{
    public partial class CtrlToolMorph : CtrlToolWindowContent
    {
        public OperationMorph Operation { get; }


        public CtrlToolMorph()
        {
            InitializeComponent();
            Operation = new OperationMorph();
            SetOperation(Operation);


            cbMorphOperation.Items.AddRange(OperationMorph.MorphOperations);
            cbMorphOperation.SelectedIndex = 0;
        }


        public override bool UpdateOperation()
        {
            base.UpdateOperation();
            Operation.IterationsStart = (uint) nmIterationsStart.Value;
            Operation.IterationsEnd = (uint) nmIterationsEnd.Value;
            Operation.FadeInOut = cbIterationsFade.Checked;
            Operation.MorphOperation = (MorphOp)((StringTag) cbMorphOperation.SelectedItem).Tag;
            Operation.Kernel.Anchor = ctrlKernel.KernelAnchor;
            Operation.Kernel.Matrix = ctrlKernel.GetMatrix();
            return true;
        }

        public override void ExtraActionCall(object sender)
        {
            if (sender is CheckBox checkbox)
            {
                ctrlKernel.Visible = checkbox.Checked;
                return;
            }
        }

        private void EventCheckedChanged(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, cbIterationsFade))
            {
                lbIterationsStop.Enabled =
                nmIterationsEnd.Enabled =
                cbIterationsFade.Checked;

                return;
            }
        }
    }
}
