/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV.CvEnum;

namespace UVtools.Core.Operations
{
    public class OperationThreshold : Operation
    {
        public override string Title => "Threshold pixels";
        public override string Description =>
            "Manipulates pixels values giving a threshold, maximum and a operation type.\n" +
            "If a pixel brightness is less or equal to the threshold value, set this pixel to 0, otherwise set to defined maximum value.\n" +
            "More info: https://docs.opencv.org/master/d7/d4d/tutorial_py_thresholding.html";

        public override string ConfirmationText =>
            $"threshold pixels ({Threshold}/{Maximum}) from layers {LayerIndexStart} to {LayerIndexEnd}";

        public override string ProgressTitle =>
            $"Thresholding pixels ({Threshold}/{Maximum}) from layers {LayerIndexStart} to {LayerIndexEnd}";

        public override string ProgressAction => "Thresholded layers";
        
        public byte Threshold { get; set; }
        public byte Maximum { get; set; }

        public ThresholdType Type { get; set; }


    }
}
