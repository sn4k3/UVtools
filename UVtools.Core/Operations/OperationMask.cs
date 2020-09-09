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
            "Masks the LCD output image given a greyscale (0-255) pixel input image.\n" +
            "Useful to correct light uniformity, but a proper mask must be created first based on real measurements per printer.\n" +
            "NOTE 1: Masks should respect printer resolution or they will be resized to fit.\n" +
            "NOTE 2: Run only this tool after all repairs and other transformations.";

        public override string ConfirmationText =>
            $"mask layers from {LayerIndexStart} to {LayerIndexEnd}";

        public override string ProgressTitle =>
            $"Masking layers from {LayerIndexStart} to {LayerIndexEnd}";

        public override string ProgressAction => "Masked layers";

        public override StringTag Validate(params object[] parameters)
        {
            var sb = new StringBuilder();
            if (Mask is null)
            {
                sb.AppendLine("The mask can't be empty.");
            }

            return new StringTag(sb.ToString());
        }

        public Mat Mask { get; set; }
    }
}
