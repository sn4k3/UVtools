/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;

namespace UVtools.Core.Suggestions
{
    public sealed class SuggestionBottomLayerCount : Suggestion
    {
        #region Members

        private decimal _targetBottomHeight = 0.25m;
        private decimal _minimumBottomHeight = 0.07m;
        private decimal _maximumBottomHeight = 0.4m;
        private byte _minimumBottomLayerCount = 3;
        private byte _maximumBottomLayerCount = 7;

        #endregion

        #region Properties

        public override bool IsAvailable => SlicerFile?.CanUseBottomLayerCount ?? false;

        public override bool IsApplied
        {
            get
            {
                if (SlicerFile is null) return false;
                var bottomHeight = (decimal)SlicerFile.BottomLayersHeight;
                return bottomHeight >= _minimumBottomHeight && bottomHeight <= _maximumBottomHeight
                    && SlicerFile.BottomLayerCount >= _minimumBottomLayerCount && SlicerFile.BottomLayerCount <= _maximumBottomLayerCount;
            }
        }

        public override string Title => "Bottom layer count";
        public override string Message => IsApplied 
            ? $"{GlobalAppliedMessage}: {SlicerFile.BottomLayerCount} / {SlicerFile.BottomLayersHeight}mm" 
            : $"{GlobalNotAppliedMessage} ({SlicerFile.BottomLayerCount}) is out of the recommended {BottomLayerCountValue} layers";

        public override string ToolTip => $"The recommended total height for the bottom layers must be between {_minimumBottomHeight}mm and {_maximumBottomHeight}mm constrained from {_minimumBottomLayerCount} to {_maximumBottomLayerCount} layers.\n" +
                                          "Explanation: Bottom layers should be kept to a minimum, usually from 2 or 3, it function is to provide a good adhesion to the build plate, and that happens on the first layer, using a high count have disadvantages.";

        public override string InformationUrl => "https://ameralabs.com/blog/default-3d-printing-raft-settings";

        public override string ConfirmationMessage => $"{Title}: {SlicerFile.BottomLayerCount} » {BottomLayerCountValue}";

        public decimal TargetBottomHeight
        {
            get => _targetBottomHeight;
            set => RaiseAndSetIfChanged(ref _targetBottomHeight, value);
        }

        public decimal MinimumBottomHeight
        {
            get => _minimumBottomHeight;
            set => RaiseAndSetIfChanged(ref _minimumBottomHeight, value);
        }

        public decimal MaximumBottomHeight
        {
            get => _maximumBottomHeight;
            set => RaiseAndSetIfChanged(ref _maximumBottomHeight, value);
        }

        public byte MinimumBottomLayerCount
        {
            get => _minimumBottomLayerCount;
            set => RaiseAndSetIfChanged(ref _minimumBottomLayerCount, value);
        }

        public byte MaximumBottomLayerCount
        {
            get => _maximumBottomLayerCount;
            set => RaiseAndSetIfChanged(ref _maximumBottomLayerCount, value);
        }

        public ushort BottomLayerCountValue => Math.Clamp((ushort)Math.Ceiling((float)_targetBottomHeight / SlicerFile.LayerHeight), _minimumBottomLayerCount, _maximumBottomLayerCount);

        #endregion

        #region Methods

        protected override bool ExecuteInternally()
        {
            SlicerFile.BottomLayerCount = BottomLayerCountValue;
            return true;
        }

        #endregion
    }
}
