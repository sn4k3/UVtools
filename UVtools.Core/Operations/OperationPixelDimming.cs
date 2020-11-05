/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Text;
using System.Xml.Serialization;
using Emgu.CV;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public class OperationPixelDimming : Operation
    {
        private uint _borderSize = 5;
        private bool _bordersOnly;
        private Matrix<byte> _evenPattern;
        private Matrix<byte> _oddPattern;

        #region Overrides
        public override string Title => "Pixel dimming";
        public override string Description =>
            "Dim white pixels in a chosen pattern applied over the print area.\n\n" +
            "The selected pattern will tiled over the image.  Benefits are:\n" +
            "1) Reduced layer expansion for large layer objects\n" +
            "2) Reduced cross layer exposure\n" +
            "3) Extended pixel life of the LCD\n\n" +
            "NOTE: Run this tool only after repairs and all other transformations.";

        public override string ConfirmationText =>
            $"dim pixels from layers {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressTitle =>
            $"Dimming from layers {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressAction => "Dimmed layers";

        public override StringTag Validate(params object[] parameters)
        {
            var sb = new StringBuilder();
            if (BorderSize == 0 && BordersOnly)
            {
                sb.AppendLine("Border size must be positive in order to use \"Dim only borders\" function.");
            }

            if (EvenPattern is null && OddPattern is null)
            {
                sb.AppendLine("Either even or odd pattern must contain a valid matrix.");
            }

            return new StringTag(sb.ToString());
        }
        #endregion

        #region Properties

        public uint BorderSize
        {
            get => _borderSize;
            set => RaiseAndSetIfChanged(ref _borderSize, value);
        }

        public bool BordersOnly
        {
            get => _bordersOnly;
            set => RaiseAndSetIfChanged(ref _bordersOnly, value);
        }

        [XmlIgnore]
        public Matrix<byte> EvenPattern
        {
            get => _evenPattern;
            set => RaiseAndSetIfChanged(ref _evenPattern, value);
        }

        [XmlIgnore]
        public Matrix<byte> OddPattern
        {
            get => _oddPattern;
            set => RaiseAndSetIfChanged(ref _oddPattern, value);
        }

        #endregion

        public override string ToString()
        {
            var result = $"[Border: {_borderSize}px] [Only borders: {_bordersOnly}]" + LayerRangeString;
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }

        #region Equality

        protected bool Equals(OperationPixelDimming other)
        {
            return _borderSize == other._borderSize && _bordersOnly == other._bordersOnly;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((OperationPixelDimming) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) _borderSize * 397) ^ _bordersOnly.GetHashCode();
            }
        }

        #endregion
    }
}
