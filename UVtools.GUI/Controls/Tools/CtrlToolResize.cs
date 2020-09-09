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
    public partial class CtrlToolResize : CtrlToolWindowContent
    {
        public OperationResize Operation { get; }


        public CtrlToolResize()
        {
            InitializeComponent();
            Operation = new OperationResize();
            SetOperation(Operation);
        }

        private void EventValueChanged(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, nmX))
            {
                if (cbConstrainXY.Checked)
                {
                    nmY.Value = nmX.Value;
                }
            }

            else if (ReferenceEquals(sender, cbConstrainXY))
            {
                lbY.Enabled = nmY.Enabled = !cbConstrainXY.Checked;
                EventValueChanged(nmX, null);
            }

            UpdateOperation();
            ButtonOkEnabled = Operation.CanValidate();
        }

        public override bool UpdateOperation()
        {
            base.UpdateOperation();
            Operation.X = nmX.Value;
            Operation.Y = nmY.Value;
            Operation.ConstrainXY = cbConstrainXY.Checked;
            Operation.IsFade = cbFade.Checked;
            return true;
        }
    }
}
