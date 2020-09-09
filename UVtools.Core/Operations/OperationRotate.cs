/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

namespace UVtools.Core.Operations
{
    public class OperationRotate : Operation
    {
        public override string Title => "Rotate";
        public override string Description =>
            "Rotate layer images in a certain angle degrees.\n" +
            "Positive angle are clockwise (CW)\n" +
            "Negative angle are counter-clockwise (CCW)";

        public override string ConfirmationText =>
            $"rotate layers {LayerIndexStart} to {LayerIndexEnd} at {AngleDegrees} degrees";

        public override string ProgressTitle =>
            $"Rotating layers {LayerIndexStart} to {LayerIndexEnd} at {AngleDegrees} degrees";

        public decimal AngleDegrees { get; set; }
    }
}
