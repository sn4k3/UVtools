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
    public partial class CtrlToolRotate : CtrlToolWindowContent
    {
        public OperationRotate Operation { get; }
        
        public CtrlToolRotate()
        {
            InitializeComponent();
            Operation = new OperationRotate();
            SetOperation(Operation);
        }

        public override void UpdateOperation()
        {
            base.UpdateOperation();
            Operation.AngleDegrees = nmDegrees.Value;
        }

        private void EventValueChanged(object sender, EventArgs e)
        {
            ButtonOkEnabled = nmDegrees.Value != 0 && nmDegrees.Value != 360 && nmDegrees.Value != -360;
        }
    }
}
