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
    public class OperationFlip : Operation
    {
        public override string Title => "Flip";
        public override string Description =>
            "Flips layer images vertically and/or horizontally.";

        public override string ConfirmationText =>
            $"flip {FlipDirection} from layers {LayerIndexStart} to {LayerIndexEnd}";

        public override string ProgressTitle =>
            $"Flipping {FlipDirection} from layers {LayerIndexStart} to {LayerIndexEnd}";

        

        public Enumerations.FlipDirection FlipDirection { get; set; }

        public bool MakeCopy { get; set; }

        public FlipType FlipTypeOpenCV
        {
            get
            {
                var flipType = FlipType.Horizontal;
                switch (FlipDirection)
                {
                    case Enumerations.FlipDirection.Horizontally:
                        flipType = FlipType.Horizontal;
                        break;
                    case Enumerations.FlipDirection.Vertically:
                        flipType = FlipType.Vertical;
                        break;
                    case Enumerations.FlipDirection.Both:
                        flipType = FlipType.Horizontal | FlipType.Vertical;
                        break;
                }

                return flipType;
            }
        }



    }
}
