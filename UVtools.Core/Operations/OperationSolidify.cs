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
    [Serializable]
    public sealed class OperationSolidify : Operation
    {
        public enum AreaCheckTypes
        {
            More,
            Less
        }
        private uint _minimumArea = 1;
        private AreaCheckTypes _areaCheckType = AreaCheckTypes.More;

        #region Overrides

        public override string Title => "Solidify";

        public override string Description =>
            "Solidifies the selected layers, closing all interior holes.\n\n" +
            "NOTE: All open areas of the layer that are completely surrounded by pixels will be filled. Please ensure that none of the holes in the layer are required before proceeding.";

        public override string ConfirmationText =>
            $"solidify layers {LayerIndexStart} through {LayerIndexEnd}?";

        public override string ProgressTitle =>
            $"Solidifying layers {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressAction => "Solidified layers";

        /// <summary>
        /// Gets the minimum required area to solidify it
        /// </summary>
        public uint MinimumArea
        {
            get => _minimumArea;
            set => RaiseAndSetIfChanged(ref _minimumArea, Math.Max(1, value));
        }

        public AreaCheckTypes AreaCheckType
        {
            get => _areaCheckType;
            set => RaiseAndSetIfChanged(ref _areaCheckType, value);
        }

        public static Array AreaCheckTypeItems => Enum.GetValues(typeof(AreaCheckTypes));

        public override string ToString()
        {
            var result = $"[Area: ={_areaCheckType} than {_minimumArea}px²]" + LayerRangeString;
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }

        #endregion
    }
}
