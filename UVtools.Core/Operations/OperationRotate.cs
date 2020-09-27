/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;

namespace UVtools.Core.Operations
{
    public class OperationRotate : Operation
    {
        private decimal _angleDegrees = 90;
        public override string Title => "Rotate";
        public override string Description =>
            "Rotate the layers of the model.\n";

        public override string ConfirmationText =>
            $"rotate layers {LayerIndexStart} through {LayerIndexEnd} {(AngleDegrees < 0?"counter-clockwise":"clockwise")} by {Math.Abs(AngleDegrees)}°?";

        public override string ProgressTitle =>
            $"Rotating layers {LayerIndexStart} through {LayerIndexEnd} {(AngleDegrees < 0 ? "counter-clockwise" : "clockwise")} by {Math.Abs(AngleDegrees)}°";

        public override string ProgressAction => "Rotated layers";

        public decimal AngleDegrees
        {
            get => _angleDegrees;
            set => RaiseAndSetIfChanged(ref _angleDegrees, value);
        }
    }
}
