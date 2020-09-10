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
            "Manipulate pixel values based on a threshold.\n\n" +
            "Pixles brighter than the theshold will be set to the Max value, " +
            "all other pixels will be set to 0.\n\n" +
            "See https://docs.opencv.org/master/d7/d4d/tutorial_py_thresholding.html";

        public override string ConfirmationText =>
            $"apply threshold {Threshold} with max {Maximum} from layers {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressTitle =>
            $"Applying threshold {Threshold} with max {Maximum} from layers {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressAction => "Thresholded layers";
        
        public byte Threshold { get; set; }
        public byte Maximum { get; set; }

        public ThresholdType Type { get; set; }


    }
}
