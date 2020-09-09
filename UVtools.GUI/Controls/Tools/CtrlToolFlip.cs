/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using UVtools.Core;
using UVtools.Core.Operations;

namespace UVtools.GUI.Controls.Tools
{
    public partial class CtrlToolFlip : CtrlToolWindowContent
    {
        public OperationFlip Operation { get; }
        
        public CtrlToolFlip()
        {
            InitializeComponent();
            Operation = new OperationFlip();
            SetOperation(Operation);

            var directions = Enum.GetValues(typeof(Enumerations.FlipDirection));

            cbFlipDirection.BeginUpdate();
            foreach (var direction in directions)
            {
                cbFlipDirection.Items.Add(direction);
            }
            cbFlipDirection.EndUpdate();

            cbFlipDirection.SelectedIndex = 0;
        }

        public override bool UpdateOperation()
        {
            base.UpdateOperation();
            Operation.FlipDirection = (Enumerations.FlipDirection)cbFlipDirection.SelectedItem;
            Operation.MakeCopy = cbMakeCopy.Checked;
            return true;
        }
    }
}
