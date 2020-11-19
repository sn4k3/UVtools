/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using Emgu.CV.CvEnum;

namespace UVtools.Core.Operations
{
    [Serializable]
    public class OperationFlip : Operation
    {
        private bool _makeCopy;
        private Enumerations.FlipDirection _flipDirection = Enumerations.FlipDirection.Horizontally;

        public override string Title => "Flip";
        public override string Description =>
            "Flip the layers of the model vertically and/or horizontally.\n" +
            "Note: Before perform this operation, un-rotate the layer preview to see the real orientation.";

        public override string ConfirmationText =>
            FlipDirection == Enumerations.FlipDirection.Both
                ? $"flip {(MakeCopy == true? "and blend ":"")}layers {LayerIndexStart} through {LayerIndexEnd} Horizontally and Vertically?"
                : $"flip {(MakeCopy == true ? "and blend " : "")}layers {LayerIndexStart} through {LayerIndexEnd} {FlipDirection}?";

        public override string ProgressTitle =>
            FlipDirection == Enumerations.FlipDirection.Both
                ? $"Flipping {(MakeCopy == true ? "and blending " : "")}layers {LayerIndexStart} through {LayerIndexEnd} Horizontally and Vertically"
                : $"Flipping {(MakeCopy == true ? "and blending " : "")}layers {LayerIndexStart} through {LayerIndexEnd} {FlipDirection}";

        public override string ProgressAction => "Flipped layers";

        public Enumerations.FlipDirection FlipDirection
        {
            get => _flipDirection;
            set => RaiseAndSetIfChanged(ref _flipDirection, value);
        }

        public bool MakeCopy
        {
            get => _makeCopy;
            set => RaiseAndSetIfChanged(ref _makeCopy, value);
        }

        public static Array FlipDirections => Enum.GetValues(typeof(Enumerations.FlipDirection));

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

        public override string ToString()
        {
            var result = $"[{_flipDirection}] [Blend: {_makeCopy}]" + LayerRangeString;
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }

        #region Equality

        protected bool Equals(OperationFlip other)
        {
            return _makeCopy == other._makeCopy && _flipDirection == other._flipDirection;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((OperationFlip) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_makeCopy.GetHashCode() * 397) ^ (int) _flipDirection;
            }
        }

        #endregion
    }
}
