/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Text;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    public class OperationMask : Operation
    {
        public override string Title => "Mask";
        public override string Description =>
            "Mask the intensity of the LCD output using a greyscale input image.\n\n" +
            "Useful to correct LCD light uniformity for a specific printer.\n\n" +
            "NOTE:  This operation should be run only after repairs and other transformations.  The provided" +
            "input mask image must match the output resolution of the target printer.";

        public override string ConfirmationText =>
            $"mask layers from {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressTitle =>
            $"Masking layers from {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressAction => "Masked layers";

        public override StringTag Validate(params object[] parameters)
        {
            var sb = new StringBuilder();
            if (Mask is null)
            {
                sb.AppendLine("The mask can not be empty.");
            }

            return new StringTag(sb.ToString());
        }

        public Mat Mask { get; set; }

        public bool HaveMask => !(Mask is null);

        public void InvertMask()
        {
            if (!HaveMask) return;
            CvInvoke.BitwiseNot(Mask, Mask);
        }
    }
}
