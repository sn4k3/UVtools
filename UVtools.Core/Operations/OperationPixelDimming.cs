/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Text;
using Emgu.CV;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    public class OperationPixelDimming : Operation
    {
        #region Overrides
        public override string Title => "Pixel dimming";
        public override string Description =>
            "Dims pixels in a chosen pattern over white pixels neighborhood. The selected pattern will be repeated over the image width and height as a mask. Benefits are:\n" +
            "1) Reduce layer expansion in big masses\n" +
            "2) Reduce cross layer exposure\n" +
            "3) Extend pixels life\n" +
            "NOTE: Run only this tool after all repairs and other transformations.";

        public override string ConfirmationText =>
            $"dim pixels from layers {LayerIndexStart} to {LayerIndexEnd}";

        public override string ProgressTitle =>
            $"Dimming from layers {LayerIndexStart} to {LayerIndexEnd}";

        public override string ProgressAction => "Dimmed layers";

        public override StringTag Validate(params object[] parameters)
        {
            var sb = new StringBuilder();
            if (BorderSize == 0 && BordersOnly)
            {
                sb.AppendLine("Border size must be positive in order to use \"Dims only the borders\" function.");
            }

            if (EvenPattern is null && OddPattern is null)
            {
                sb.AppendLine("Either even or odd pattern must contain a valid matrix.");
            }

            return new StringTag(sb.ToString());
        }
        #endregion

        #region Properties

        public uint BorderSize { get; set; }
        public bool BordersOnly { get; set; }
        public Matrix<byte> EvenPattern { get; set; }
        public Matrix<byte> OddPattern { get; set; }
        #endregion
    }
}
