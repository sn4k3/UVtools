/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using UVtools.Core.Extensions;

namespace UVtools.Core.Suggestions
{
    public sealed class SuggestionWaitTimeAfterCure : Suggestion
    {
        #region Enums
        public enum SuggestionWaitTimeAfterCureApplyType
        {
            Fixed,
            ExposureProportional
        }
        #endregion

        #region Members
        private SuggestionWaitTimeAfterCureApplyType _applyType = SuggestionWaitTimeAfterCureApplyType.Fixed;
        private decimal _fixedBottomWaitTimeAfterCure = 7;
        private decimal _fixedWaitTimeAfterCure = 1;
        private decimal _proportionalExposureTime = 2;
        private decimal _proportionalWaitTimeAfterCure = 1;
        private decimal _minimumBottomWaitTimeAfterCure = 3;
        private decimal _minimumWaitTimeAfterCure = 1;
        private decimal _maximumBottomWaitTimeAfterCure = 20;
        private decimal _maximumWaitTimeAfterCure = 12;
        #endregion

        #region Properties

        public override bool IsAvailable => SlicerFile?.CanUseAnyWaitTimeAfterCure ?? false;

        public override bool IsApplied
        {
            get
            {
                if (SlicerFile is null) return false;
                if (SlicerFile.CanUseBottomWaitTimeAfterCure)
                {
                    if ((decimal) SlicerFile.BottomWaitTimeAfterCure < _minimumWaitTimeAfterCure ||
                        (decimal) SlicerFile.BottomWaitTimeAfterCure > _maximumWaitTimeAfterCure ||
                        Math.Abs(SlicerFile.BottomWaitTimeAfterCure - CalculateWaitTime(true, (decimal)SlicerFile.BottomExposureTime)) > 0.1) return false;
                }
                if (SlicerFile.CanUseWaitTimeAfterCure)
                {
                    if ((decimal)SlicerFile.WaitTimeAfterCure < _minimumWaitTimeAfterCure ||
                        (decimal)SlicerFile.WaitTimeAfterCure > _maximumWaitTimeAfterCure ||
                        Math.Abs(SlicerFile.WaitTimeAfterCure - CalculateWaitTime(false, (decimal)SlicerFile.ExposureTime)) > 0.1) return false;
                }

                if (SlicerFile.CanUseLayerWaitTimeAfterCure)
                {
                    foreach (var layer in SlicerFile)
                    {
                        if ((decimal)layer.WaitTimeAfterCure < _minimumWaitTimeAfterCure ||
                            (decimal)layer.WaitTimeAfterCure > _maximumWaitTimeAfterCure ||
                            Math.Abs(layer.WaitTimeAfterCure - CalculateWaitTime(layer.IsBottomLayer, (decimal)layer.ExposureTime)) > 0.1) return false;
                    }
                }
                return true;
            }
        }

        public override string Title => "Wait time after cure";
        public override string Message => IsApplied 
            ? $"{GlobalAppliedMessage}: {SlicerFile.BottomWaitTimeAfterCure}/{SlicerFile.WaitTimeAfterCure}s" 
            : $"{GlobalNotAppliedMessage} of {SlicerFile.BottomWaitTimeAfterCure}/{SlicerFile.WaitTimeAfterCure}s " +
              $"is out of the recommended {CalculateWaitTime(true, (decimal) SlicerFile.BottomWaitTimeAfterCure)}/{CalculateWaitTime(false, (decimal)SlicerFile.WaitTimeAfterCure)}s";

        //public override string ToolTip => $"The recommended total height for the bottom layers must be between {_minimumBottomHeight}mm and {_maximumBottomHeight}mm constrained from {_minimumBottomLayerCount} to {_maximumBottomLayerCount} layers.\n" +
        //                                  "Explanation: Bottom layers should be kept to a minimum, usually from 2 or 3, it function is to provide a good adhesion to the build plate, and that happens on the first layer, using a high count have disadvantages.";

        public override string InformationUrl => "https://ameralabs.com/blog/default-3d-printing-raft-settings";

        public override string ConfirmationMessage => $"{Title}: {SlicerFile.BottomWaitTimeAfterCure}/{SlicerFile.WaitTimeAfterCure}s » {CalculateWaitTime(true, (decimal)SlicerFile.BottomWaitTimeAfterCure)}/{CalculateWaitTime(false, (decimal)SlicerFile.WaitTimeAfterCure)}s";

        private SuggestionWaitTimeAfterCureApplyType ApplyType
        {
            get => _applyType;
            set => RaiseAndSetIfChanged(ref _applyType, value);
        }

        public decimal FixedBottomWaitTimeAfterCure
        {
            get => _fixedBottomWaitTimeAfterCure;
            set => RaiseAndSetIfChanged(ref _fixedBottomWaitTimeAfterCure, Math.Round(value, 2));
        }

        public decimal FixedWaitTimeAfterCure
        {
            get => _fixedWaitTimeAfterCure;
            set => RaiseAndSetIfChanged(ref _fixedWaitTimeAfterCure, Math.Round(value, 2));
        }

        public decimal ProportionalExposureTime
        {
            get => _proportionalExposureTime;
            set => RaiseAndSetIfChanged(ref _proportionalExposureTime, Math.Round(value, 2));
        }

        public decimal ProportionalWaitTimeAfterCure
        {
            get => _proportionalWaitTimeAfterCure;
            set => RaiseAndSetIfChanged(ref _proportionalWaitTimeAfterCure, Math.Round(value, 2));
        }

        public decimal MinimumBottomWaitTimeAfterCure
        {
            get => _minimumBottomWaitTimeAfterCure;
            set => RaiseAndSetIfChanged(ref _minimumBottomWaitTimeAfterCure, Math.Round(value, 2));
        }

        public decimal MinimumWaitTimeAfterCure
        {
            get => _minimumWaitTimeAfterCure;
            set => RaiseAndSetIfChanged(ref _minimumWaitTimeAfterCure, Math.Round(value, 2));
        }

        public decimal MaximumBottomWaitTimeAfterCure
        {
            get => _maximumBottomWaitTimeAfterCure;
            set => RaiseAndSetIfChanged(ref _maximumBottomWaitTimeAfterCure, Math.Round(value, 2));
        }

        public decimal MaximumWaitTimeAfterCure
        {
            get => _maximumWaitTimeAfterCure;
            set => RaiseAndSetIfChanged(ref _maximumWaitTimeAfterCure, Math.Round(value, 2));
        }

        #endregion

        #region Methods

        protected override bool ExecuteInternally()
        {
            if (SlicerFile.CanUseBottomWaitTimeAfterCure)
            {
                SlicerFile.BottomWaitTimeAfterCure = CalculateWaitTime(true, (decimal)SlicerFile.BottomExposureTime);
            }
            if (SlicerFile.CanUseWaitTimeAfterCure)
            {
                SlicerFile.WaitTimeAfterCure = CalculateWaitTime(false, (decimal)SlicerFile.ExposureTime);
            }

            if (SlicerFile.CanUseLayerWaitTimeAfterCure)
            {
                foreach (var layer in SlicerFile)
                {
                    layer.WaitTimeAfterCure = CalculateWaitTime(layer.IsBottomLayer, (decimal) layer.ExposureTime);
                }
            }

            return true;
        }
        

        public float CalculateWaitTime(bool isBottomLayer, decimal exposureTime)
        {
            return _applyType switch
            {
                SuggestionWaitTimeAfterCureApplyType.Fixed => (float) (isBottomLayer
                    ? _fixedBottomWaitTimeAfterCure
                    : _fixedWaitTimeAfterCure),
                SuggestionWaitTimeAfterCureApplyType.ExposureProportional => (float) Math.Round(
                    (exposureTime * _proportionalWaitTimeAfterCure / _proportionalExposureTime).Clamp(
                        isBottomLayer ? _minimumBottomWaitTimeAfterCure : _minimumWaitTimeAfterCure,
                        isBottomLayer ? _maximumBottomWaitTimeAfterCure : _maximumWaitTimeAfterCure), 2),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        #endregion
    }
}
