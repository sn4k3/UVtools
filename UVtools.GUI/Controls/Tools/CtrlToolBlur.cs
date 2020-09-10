/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using UVtools.Core.Objects;
using UVtools.Core.Operations;
using UVtools.GUI.Forms;

namespace UVtools.GUI.Controls.Tools
{
    public partial class CtrlToolBlur : CtrlToolWindowContent
    {
        public OperationBlur Operation { get; }


        public CtrlToolBlur()
        {
            InitializeComponent();
            Operation = new OperationBlur();
            SetOperation(Operation);


            cbBlurOperation.Items.AddRange(OperationBlur.BlurTypes);
            cbBlurOperation.SelectedIndex = 0;
        }

        public override bool UpdateOperation()
        {
            base.UpdateOperation();
            Operation.BlurOperation = (OperationBlur.BlurAlgorithm)((StringTag) cbBlurOperation.SelectedItem).Tag;
            Operation.Size = (uint) nmSize.Value;
            Operation.Kernel.Anchor = ctrlKernel.KernelAnchor;
            Operation.Kernel.Matrix = ctrlKernel.GetMatrix();

            return true;
        }

        private void cbBlurOperation_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            UpdateOperation();
            ctrlKernel.Visible  = ctrlKernel.Enabled = Operation.BlurOperation == OperationBlur.BlurAlgorithm.Filter2D;
            nmSize.Enabled = Operation.BlurOperation != OperationBlur.BlurAlgorithm.Pyramid && Operation.BlurOperation != OperationBlur.BlurAlgorithm.Filter2D;
        }
    }
}
