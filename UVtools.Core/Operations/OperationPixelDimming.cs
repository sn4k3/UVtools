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
        private uint _wallThickness = 5;
        private bool _wallsOnly;
        private Matrix<byte> _pattern;
        private Matrix<byte> _alternatePattern;
        private ushort _alternatePatternPerLayers = 1;

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
            if (WallThickness == 0 && WallsOnly)
            {
                sb.AppendLine("Border size must be positive in order to use \"Dim only borders\" function.");
            }

            if (Pattern is null && AlternatePattern is null)
            {
                sb.AppendLine("Either even or odd pattern must contain a valid matrix.");
            }

            return new StringTag(sb.ToString());
        }
        #endregion

        #region Properties

        public uint WallThickness
        {
            get => _wallThickness;
            set => RaiseAndSetIfChanged(ref _wallThickness, value);
        }

        public bool WallsOnly
        {
            get => _wallsOnly;
            set => RaiseAndSetIfChanged(ref _wallsOnly, value);
        }

        /// <summary>
        /// Use the alternate pattern every <see cref="AlternatePatternPerLayers"/> layers
        /// </summary>
        public ushort AlternatePatternPerLayers
        {
            get => _alternatePatternPerLayers;
            set => RaiseAndSetIfChanged(ref _alternatePatternPerLayers, Math.Max((ushort)1, value));
        }

        [XmlIgnore]
        public Matrix<byte> Pattern
        {
            get => _pattern;
            set => RaiseAndSetIfChanged(ref _pattern, value);
        }

        [XmlIgnore]
        public Matrix<byte> AlternatePattern
        {
            get => _alternatePattern;
            set => RaiseAndSetIfChanged(ref _alternatePattern, value);
        }

        #endregion

        #region Methods

        public bool IsNormalPattern(uint layerIndex)
        {
            return layerIndex / AlternatePatternPerLayers % 2 == 0;
        }

        public bool IsAlternatePattern(uint layerIndex) => !IsNormalPattern(layerIndex);

        public override string ToString()
        {
            var result = $"[Border: {_wallThickness}px] [Only borders: {_wallsOnly}] [Alternate every: {_alternatePatternPerLayers}]" + LayerRangeString;
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }

        #endregion

        #region Equality

        protected bool Equals(OperationPixelDimming other)
        {
            return _wallThickness == other._wallThickness && _wallsOnly == other._wallsOnly;
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
                return ((int) _wallThickness * 397) ^ _wallsOnly.GetHashCode();
            }
        }

        #endregion
    }
}
