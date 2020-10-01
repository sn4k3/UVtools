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
    public partial class CtrlToolArithmetic : CtrlToolWindowContent
    {
        public OperationArithmetic Operation { get; }


        public CtrlToolArithmetic()
        {
            InitializeComponent();
            Operation = new OperationArithmetic();
            SetOperation(Operation);
        }

        public override bool UpdateOperation()
        {
            base.UpdateOperation();
            Operation.Sentence = tbSentence.Text;

            foreach (var operation in Operation.Operations)
            {
                if(operation.LayerIndex < 0 || operation.LayerIndex > Program.SlicerFile.LastLayerIndex)
                {
                    return ValidateFormFromString($"Layer {operation.LayerIndex} does not exists, please fix your sentence.");
                }
            }

            return true;
        }
    }
}
