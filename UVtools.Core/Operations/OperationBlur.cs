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
    public sealed class OperationBlur : Operation
    {
        #region Overrides

        public override string Title => "Blur";
        public override string Description =>
            $"Blur and averaging images with various low pass filters\n" +
            "Note: Printer must support AntiAliasing on firmware to able to use this function\n" +
            "More information: https://docs.opencv.org/master/d4/d13/tutorial_py_filtering.html";

        public override string ConfirmationText =>
            $"blur model with {BlurOperation} from layers {LayerIndexStart} to {LayerIndexEnd}?";

        public override string ProgressTitle =>
            $"Bluring model with {BlurOperation} layers from {LayerIndexStart} to {LayerIndexEnd}";

        public override string ProgressAction => "Blured layers";

        public override StringTag Validate(params object[] parameters)
        {
            var sb = new StringBuilder();

            if (BlurOperation == BlurAlgorithm.GaussianBlur ||
                BlurOperation == BlurAlgorithm.MedianBlur)
            {
                if (Size % 2 != 1)
                {
                    sb.AppendLine("Size must be a odd number.");
                }
            }

            if (BlurOperation == BlurAlgorithm.Filter2D)
            {
                if (Kernel is null)
                {
                    sb.AppendLine("Kernel can't be empty.");
                }
            }

            return new StringTag(sb.ToString());
        }

        #endregion

        #region Properties


        public static StringTag[] BlurTypes => new[]
        {
            new StringTag("Blur: Normalized box filter", BlurAlgorithm.Blur),
            new StringTag("Pyramid: Down/up-sampling step of Gaussian pyramid decomposition", BlurAlgorithm.Pyramid),
            new StringTag("Median Blur: Each pixel becomes the median of its surrounding pixels", BlurAlgorithm.MedianBlur),
            new StringTag("Gaussian Blur: Each pixel is a sum of fractions of each pixel in its neighborhood", BlurAlgorithm.GaussianBlur),
            new StringTag("Filter 2D: Applies an arbitrary linear filter to an image", BlurAlgorithm.Filter2D),
        };

        public BlurAlgorithm BlurOperation { get; set; } = BlurAlgorithm.Blur;

        public uint Size { get; set; } = 1;

        public Kernel Kernel { get; set; } = new Kernel();

        #endregion

        #region Enums
        public enum BlurAlgorithm
        {
            Blur,
            Pyramid,
            MedianBlur,
            GaussianBlur,
            Filter2D
        }
        #endregion
    }
}
