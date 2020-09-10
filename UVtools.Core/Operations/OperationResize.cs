/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System.Text;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    public class OperationResize : Operation
    {
        public override string Title => "Resize";
        public override string Description =>
            "Resize the model by a percentage in the X/Y plane.\n\n" +
            "NOTE: This operation is applied based on the original orientation of the layers in the model file. " +
            "If the image is rotated 90° in the Layer Preview, you will need to compensate by inverting " +
            "the X and Y values.  The print bounds will also not be validated as part of this operation, so please " +
            "ensure that the scaling factor does not result in the model being clipped.";

        public override string ConfirmationText =>
            $"resize model layers {LayerIndexStart} through {LayerIndexEnd} by " +
            $"X={X}% and Y={Y}%";

        public override string ProgressTitle =>
            $"Resizing from layers {LayerIndexStart} to {LayerIndexEnd} at X:{X}%  Y:{Y}% ";

        public override string ProgressAction => "Reiszed layers";

        public override StringTag Validate(params object[] parameters)
        {
            var sb = new StringBuilder();

            if (X == 100m && Y == 100m)
            {
                sb.AppendLine("X and Y can't both be 100%.");
            }

            return new StringTag(sb.ToString());
        }


        public decimal X { get; set; }
        public decimal Y { get; set; }

        public bool ConstrainXY { get; set; }
        public bool IsFade { get; set; }


        public OperationResize()
        {
        }
    }
}
