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
    public sealed class OperationInfill : Operation
    {
        private InfillAlgorithm _infillType = InfillAlgorithm.CubicDynamicLink;
        private ushort _wallThickness = 64;
        private ushort _infillThickness = 45;
        private ushort _infillSpacing = 160;
        private ushort _infillBrightness = 255;

        #region Overrides

        public override string Title => "Infill";

        public override string Description =>
            $"Generate infill patterns in the model\n\nNOTES:\n1) You must exclude floor and ceil layers from the range.\n2) You must take care of drain holes after the operation.";

        public override string ConfirmationText =>
            $"infill model with {InfillType} from layers {LayerIndexStart} through {LayerIndexEnd}?";

        public override string ProgressTitle =>
            $"Infill model with {InfillType} from layers {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressAction => "Infilled layers";

        #endregion

        #region Enums
        public enum InfillAlgorithm
        {
            //Rhombus,
            Cubic,
            CubicCenterLink,
            CubicDynamicLink,
            CubicInterlinked,
        }
        #endregion

        #region Properties
        public static Array InfillAlgorithmTypes => Enum.GetValues(typeof(InfillAlgorithm));
        public InfillAlgorithm InfillType
        {
            get => _infillType;
            set => RaiseAndSetIfChanged(ref _infillType, value);
        }

        public ushort WallThickness
        {
            get => _wallThickness;
            set => RaiseAndSetIfChanged(ref _wallThickness, value);
        }

        public ushort InfillBrightness
        {
            get => _infillBrightness;
            set => RaiseAndSetIfChanged(ref _infillBrightness, value);
        }

        public ushort InfillThickness
        {
            get => _infillThickness;
            set => RaiseAndSetIfChanged(ref _infillThickness, value);
        }

        public ushort InfillSpacing
        {
            get => _infillSpacing;
            set => RaiseAndSetIfChanged(ref _infillSpacing, value);
        }

        public override string ToString()
        {
            var result = $"[{_infillType}] [Wall: {_wallThickness}px] [B: {_infillBrightness}px] [T: {_infillThickness}px] [S: {_infillSpacing}px]" + LayerRangeString;
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }

        #endregion

        #region Equality
        
        #endregion
    }
}
