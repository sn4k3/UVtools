/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.ComponentModel;
using System.Text;
using UVtools.Core.Operations;

namespace UVtools.Core.Suggestions;

public sealed class SuggestionWaitTimeAfterCure : Suggestion
{
    #region Enums
    public enum SuggestionWaitTimeAfterCureSetType
    {
        [Description("Fixed: Use a fixed time")]
        Fixed,
        [Description("Proportional: Proportional to an exposure time")]
        ProportionalExposure
    }
    #endregion

    #region Members
    private SuggestionWaitTimeAfterCureSetType _setType = SuggestionWaitTimeAfterCureSetType.Fixed;
    private decimal _fixedBottomWaitTimeAfterCure = 7;
    private decimal _fixedWaitTimeAfterCure = 1;
    private decimal _proportionalWaitTimeAfterCure = 1;
    private decimal _proportionalExposureTime = 3;
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

            switch (_applyWhen)
            {
                case SuggestionApplyWhen.OutsideLimits:
                    if (SlicerFile.CanUseBottomWaitTimeAfterCure)
                    {
                        if ((decimal)SlicerFile.BottomWaitTimeAfterCure < _minimumWaitTimeAfterCure ||
                            (decimal)SlicerFile.BottomWaitTimeAfterCure > _maximumWaitTimeAfterCure) return false;
                    }
                    if (SlicerFile.CanUseWaitTimeAfterCure)
                    {
                        if ((decimal)SlicerFile.WaitTimeAfterCure < _minimumWaitTimeAfterCure ||
                            (decimal)SlicerFile.WaitTimeAfterCure > _maximumWaitTimeAfterCure) return false;
                    }

                    if (SlicerFile.CanUseLayerWaitTimeAfterCure)
                    {
                        foreach (var layer in SlicerFile)
                        {
                            if (layer.IsDummy)
                            {
                                if (layer.WaitTimeAfterCure > 0.1) return false;
                            }
                            else
                            {
                                if ((decimal) layer.WaitTimeAfterCure < _minimumWaitTimeAfterCure ||
                                    (decimal) layer.WaitTimeAfterCure > _maximumWaitTimeAfterCure) return false;
                            }
                        }
                    }

                    break;
                case SuggestionApplyWhen.Different:
                    if (SlicerFile.CanUseBottomWaitTimeAfterCure)
                    {
                        if (Math.Abs(SlicerFile.BottomWaitTimeAfterCure - CalculateWaitTime(true, (decimal)SlicerFile.BottomExposureTime)) > 0.1) return false;
                    }
                    if (SlicerFile.CanUseWaitTimeAfterCure)
                    {
                        if (Math.Abs(SlicerFile.WaitTimeAfterCure - CalculateWaitTime(false, (decimal)SlicerFile.ExposureTime)) > 0.1) return false;
                    }

                    if (SlicerFile.CanUseLayerWaitTimeAfterCure)
                    {
                        foreach (var layer in SlicerFile)
                        {
                            if (layer.IsDummy)
                            {
                                if (layer.WaitTimeAfterCure > 0.1) return false;
                            }
                            else
                            {
                                if (Math.Abs(layer.WaitTimeAfterCure - CalculateWaitTime(layer.IsBottomLayer, (decimal)layer.ExposureTime)) > 0.1) return false;
                            }
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
                
            return true;
        }
    }

    public override string Title => "Wait time after cure";
    public override string Description => "Rest some time after curing the layer and before the lift sequence may allow the resin reaction to settle, harden and cool down a bit to detach more easily from the FEP, then yields less tension on the model, better print quality and easier lift's.";

    public override string Message => IsApplied 
        ? $"{GlobalAppliedMessage}: {SlicerFile.BottomWaitTimeAfterCure}/{SlicerFile.WaitTimeAfterCure}s" 
        : $"{GlobalNotAppliedMessage} of {SlicerFile.BottomWaitTimeAfterCure}/{SlicerFile.WaitTimeAfterCure}s " +
          $"is out of the recommended {CalculateWaitTime(true, (decimal) SlicerFile.BottomExposureTime)}/{CalculateWaitTime(false, (decimal)SlicerFile.ExposureTime)}s";

    public override string ToolTip => (_setType == SuggestionWaitTimeAfterCureSetType.Fixed 
                                          ? $"The recommended wait time must be {_fixedBottomWaitTimeAfterCure}/{_fixedWaitTimeAfterCure}s"
                                          : $"The recommended wait time is a ratio of (wait time){_proportionalWaitTimeAfterCure}s to (exposure time){_proportionalExposureTime}s") +
                                      $" constrained from [Bottoms={_minimumBottomWaitTimeAfterCure}s to {_maximumBottomWaitTimeAfterCure}s] and [Normals={_minimumWaitTimeAfterCure}s to {_maximumWaitTimeAfterCure}s].\n" +
                                      $"Explanation: {Description}";

    public override string? ConfirmationMessage => $"{Title}: {SlicerFile.BottomWaitTimeAfterCure}/{SlicerFile.WaitTimeAfterCure}s » {CalculateWaitTime(true, (decimal)SlicerFile.BottomWaitTimeAfterCure)}/{CalculateWaitTime(false, (decimal)SlicerFile.WaitTimeAfterCure)}s";

    public SuggestionWaitTimeAfterCureSetType SetType
    {
        get => _setType;
        set
        {
            if(!RaiseAndSetIfChanged(ref _setType, value)) return;
            RaisePropertyChanged(nameof(IsSetTypeFixed));
            RaisePropertyChanged(nameof(IsSetTypeProportionalExposure));
        }
    }

    public bool IsSetTypeFixed => _setType == SuggestionWaitTimeAfterCureSetType.Fixed;
    public bool IsSetTypeProportionalExposure => _setType == SuggestionWaitTimeAfterCureSetType.ProportionalExposure;

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

    public decimal ProportionalWaitTimeAfterCure
    {
        get => _proportionalWaitTimeAfterCure;
        set => RaiseAndSetIfChanged(ref _proportionalWaitTimeAfterCure, Math.Round(value, 2));
    }

    public decimal ProportionalExposureTime
    {
        get => _proportionalExposureTime;
        set => RaiseAndSetIfChanged(ref _proportionalExposureTime, Math.Round(value, 2));
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

    #region Constructor

    public SuggestionWaitTimeAfterCure()
    {
        _applyWhen = SuggestionApplyWhen.Different;
    }

    #endregion

    #region Overrides

    public override string? Validate()
    {
        var sb = new StringBuilder();

        if (_minimumBottomWaitTimeAfterCure > _maximumBottomWaitTimeAfterCure)
        {
            sb.AppendLine("Minimum bottom limit can't be higher than maximum bottom limit");
        }

        if (_minimumWaitTimeAfterCure > _maximumWaitTimeAfterCure)
        {
            sb.AppendLine("Minimum normal limit can't be higher than maximum normal limit");
        }

        if (_setType == SuggestionWaitTimeAfterCureSetType.ProportionalExposure)
        {
            if (_proportionalWaitTimeAfterCure <= 0)
            {
                sb.AppendLine("The proportional wait time must be higher than 0s");
            }

            if (_proportionalExposureTime <= 0)
            {
                sb.AppendLine("The proportional exposure time must be higher than 0s");
            }
        }

        return sb.ToString();
    }

    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
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
                layer.WaitTimeAfterCure = layer.IsDummy ? 0 : CalculateWaitTime(layer.IsBottomLayer, (decimal) layer.ExposureTime);
            }
        }

        return true;
    }
        

    public float CalculateWaitTime(bool isBottomLayer, decimal exposureTime)
    {
        return _setType switch
        {
            SuggestionWaitTimeAfterCureSetType.Fixed => (float) (isBottomLayer
                ? _fixedBottomWaitTimeAfterCure
                : _fixedWaitTimeAfterCure),
            SuggestionWaitTimeAfterCureSetType.ProportionalExposure => 
                (float)Math.Clamp(
                    Math.Round(exposureTime * _proportionalWaitTimeAfterCure / _proportionalExposureTime, 2),
                    isBottomLayer ? _minimumBottomWaitTimeAfterCure : _minimumWaitTimeAfterCure,
                    isBottomLayer ? _maximumBottomWaitTimeAfterCure : _maximumWaitTimeAfterCure
                ),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    #endregion
}