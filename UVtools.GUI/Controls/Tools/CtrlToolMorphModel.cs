/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using Emgu.CV.CvEnum;
using UVtools.Core.Objects;
using UVtools.Core.Operations;

namespace UVtools.GUI.Controls.Tools
{
    public partial class CtrlToolMorphModel : CtrlToolWindowContent
    {
        public OperationMorphModel Operation { get; }


        public CtrlToolMorphModel()
        {
            InitializeComponent();
            Operation = new OperationMorphModel();
            SetOperation(Operation);


            cbMorphOperation.Items.AddRange(OperationMorphModel.MorphOperations);
            cbMorphOperation.SelectedIndex = 0;
        }


        public override void UpdateOperation()
        {
            base.UpdateOperation();
            Operation.IterationsStart = (uint) nmIterationsStart.Value;
            Operation.IterationsEnd = (uint) nmIterationsEnd.Value;
            Operation.FadeInOut = cbIterationsFade.Checked;
            Operation.MorphOperation = (MorphOp)((StringTag) cbMorphOperation.SelectedItem).Tag;
            Operation.Kernel.Anchor = ctrlKernel.KernelAnchor;
            Operation.Kernel.Matrix = ctrlKernel.GetMatrix();
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
