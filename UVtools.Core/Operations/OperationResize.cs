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
            "Resizes layer images in a X and/or Y factor, starting from 100% value.\n" +
            "NOTE 1: Build volume bounds are not validated after operation, please ensure scaling stays inside your limits.\n" +
            "NOTE 2: X and Y are applied to original image, not to the rotated preview (If enabled).";

        public override string ConfirmationText =>
            $"resize model from layers {LayerIndexStart} to {LayerIndexEnd}?\n" +
            $"X:{X}%  Y:{Y}%";

        public override string ProgressTitle =>
            $"Resizing from layers {LayerIndexStart} to {LayerIndexEnd} at X:{X}%  Y:{Y}% ";

        public override StringTag Validate(params object[] parameters)
        {
            var sb = new StringBuilder();

            if (X == 100m && Y == 100m)
            {
                sb.AppendLine("X and Y can't be 100% together.");
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
