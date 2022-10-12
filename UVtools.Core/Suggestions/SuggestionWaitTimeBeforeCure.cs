/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;
using UVtools.Core.Operations;

namespace UVtools.Core.Suggestions;

public sealed class SuggestionWaitTimeBeforeCure : Suggestion
{
    #region Enums
    public enum SuggestionWaitTimeBeforeCureSetType : byte
    {
        [Description("Fixed: Use a fixed time")]
        Fixed,
        [Description("Proportional to layer pixels")]
        ProportionalLayerPixels,
        [Description("Proportional to layer area")]
        ProportionalLayerArea,
    }

    public enum SuggestionWaitTimeBeforeCureProportionalCalculateMassFrom : byte
    {
        [Description("Previous mass")]
        Previous,
        [Description("Average of previous masses")]
        Average,
        [Description("Maximum of previous masses")]
        Maximum,
    }
    #endregion

    #region Members
    private SuggestionWaitTimeBeforeCureSetType _setType = SuggestionWaitTimeBeforeCureSetType.Fixed;
    
    private decimal _bottomHeight = 1;
    private decimal _fixedBottomWaitTimeBeforeCure = 20;
    private decimal _fixedWaitTimeBeforeCure = 2;
    private byte _waitTimeBeforeCureTransitionLayerCount = 8;
    private decimal _proportionalBottomWaitTimeBeforeCure = 20;
    private decimal _proportionalWaitTimeBeforeCure = 2;
    private uint _proportionalBottomLayerPixels = 1000000;
    private uint _proportionalLayerPixels = 1000000;
    private uint _proportionalBottomLayerArea = 1000;
    private uint _proportionalLayerArea = 1000;
    private decimal _proportionalBottomWaitTimeBeforeCureMaximumDifference = 1;
    private decimal _proportionalWaitTimeBeforeCureMaximumDifference = 1;
    private SuggestionWaitTimeBeforeCureProportionalCalculateMassFrom _proportionalCalculateMassFrom = SuggestionWaitTimeBeforeCureProportionalCalculateMassFrom.Previous;
    private decimal _proportionalMassRelativeHeight = 0.2m;
    private decimal _minimumBottomWaitTimeBeforeCure = 5;
    private decimal _minimumWaitTimeBeforeCure = 1;
    private decimal _maximumBottomWaitTimeBeforeCure = 120;
    private decimal _maximumWaitTimeBeforeCure = 12;
    private bool _createEmptyFirstLayer = true;
    
    #endregion

    #region Properties

    public override bool IsAvailable
    {
        get
        {
            if (SlicerFile is null) return false;
            return SlicerFile.CanUseAnyWaitTimeBeforeCure || SlicerFile.CanUseAnyLightOffDelay;
        }
    }

    public override bool IsApplied
    {
        get
        {
            if (SlicerFile is null) return false;

            switch (_applyWhen)
            {
                case SuggestionApplyWhen.OutsideLimits:
                    if (SlicerFile.CanUseBottomWaitTimeAfterCure || SlicerFile.CanUseBottomLightOffDelay)
                    {
                        var waitTime = (decimal)SlicerFile.GetBottomWaitTimeBeforeCure();
                        if (waitTime < _minimumWaitTimeBeforeCure ||
                            waitTime > _maximumWaitTimeBeforeCure) return false;
                    }
                    if (SlicerFile.CanUseWaitTimeAfterCure || SlicerFile.CanUseLightOffDelay)
                    {
                        var waitTime = (decimal)SlicerFile.GetNormalWaitTimeBeforeCure();
                        if (waitTime < _minimumWaitTimeBeforeCure ||
                            waitTime > _maximumWaitTimeBeforeCure) return false;
                    }

                    if (SlicerFile.CanUseLayerWaitTimeAfterCure || SlicerFile.CanUseLayerLightOffDelay)
                    {
                        foreach (var layer in SlicerFile)
                        {
                            if (layer.NonZeroPixelCount <= 1) continue; // Ignore empty layers
                            var waitTime = (decimal)layer.GetWaitTimeBeforeCure();
                            if (waitTime < _minimumWaitTimeBeforeCure || waitTime > _maximumWaitTimeBeforeCure) return false;
                        }
                    }

                    break;
                case SuggestionApplyWhen.Different:
                    if (SlicerFile.CanUseBottomWaitTimeAfterCure || SlicerFile.CanUseBottomLightOffDelay)
                    {
                        if (Math.Abs(SlicerFile.GetBottomWaitTimeBeforeCure() - CalculateWaitTime(true)) > 0.1) return false;
                    }
                    if (SlicerFile.CanUseWaitTimeAfterCure || SlicerFile.CanUseLightOffDelay)
                    {
                        if (Math.Abs(SlicerFile.GetNormalWaitTimeBeforeCure() - CalculateWaitTime(false)) > 0.1) return false;
                    }

                    if (SlicerFile.CanUseLayerWaitTimeAfterCure || SlicerFile.CanUseLayerLightOffDelay)
                    {
                        foreach (var layer in SlicerFile)
                        {
                            if (layer.NonZeroPixelCount <= 1) continue; // Ignore empty layers
                            if (Math.Abs(layer.GetWaitTimeBeforeCure() - CalculateWaitTime(layer)) > 0.1) return false;
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
                
            return true;
        }
    }

    public override string Title => "Wait time before cure";
    public override string Description => "Rest some time before cure the layer is crucial to let the resin settle after the lift sequence and allow some time for the arm settle at the correct Z position as the resin will offer some resistance and push the structure.\n" +
                                          "This lead to better quality with more successful prints, less lamination problems, better first layers with more success of stick to the build plate and less elephant foot effect.";

    public override string Message => IsApplied 
        ? $"{GlobalAppliedMessage}: {SlicerFile.GetBottomWaitTimeBeforeCure()}/{SlicerFile.GetNormalWaitTimeBeforeCure()}s" 
        : $"{GlobalNotAppliedMessage} of {SlicerFile.GetBottomWaitTimeBeforeCure()}/{SlicerFile.GetNormalWaitTimeBeforeCure()}s " +
          $"is out of the recommended {CalculateWaitTime(true)}/{CalculateWaitTime(false)}s";

    public override string ToolTip => (_setType == SuggestionWaitTimeBeforeCureSetType.Fixed 
                                          ? $"The recommended wait time must be {_fixedBottomWaitTimeBeforeCure}/{_fixedWaitTimeBeforeCure}s"
                                          : $"The recommended wait time is a ratio of (wait time){_proportionalWaitTimeBeforeCure}s to (exposure time){_proportionalLayerArea}s") +
                                      $" constrained from [Bottoms={_minimumBottomWaitTimeBeforeCure}s to {_maximumBottomWaitTimeBeforeCure}s] and [Normals={_minimumWaitTimeBeforeCure}s to {_maximumWaitTimeBeforeCure}s].\n" +
                                      $"Explanation: {Description}";

    public override string? ConfirmationMessage => $"{Title}: {SlicerFile.BottomWaitTimeAfterCure}/{SlicerFile.WaitTimeAfterCure}s » {CalculateWaitTime(true)}/{CalculateWaitTime(false)}s";

    public override string? InformationUrl => "https://blog.honzamrazek.cz/2022/01/prints-not-sticking-to-the-build-plate-layer-separation-rough-surface-on-a-resin-printer-resin-viscosity-the-common-denominator";

    public SuggestionWaitTimeBeforeCureSetType SetType
    {
        get => _setType;
        set
        {
            if(!RaiseAndSetIfChanged(ref _setType, value)) return;
            RaisePropertyChanged(nameof(IsSetTypeFixed));
            RaisePropertyChanged(nameof(IsSetTypeProportionalLayerPixels));
            RaisePropertyChanged(nameof(IsSetTypeProportionalLayerArea));
        }
    }

    public bool IsSetTypeFixed => _setType == SuggestionWaitTimeBeforeCureSetType.Fixed;
    public bool IsSetTypeProportionalLayerPixels => _setType == SuggestionWaitTimeBeforeCureSetType.ProportionalLayerPixels;
    public bool IsSetTypeProportionalLayerArea => _setType == SuggestionWaitTimeBeforeCureSetType.ProportionalLayerArea;

    public SuggestionWaitTimeBeforeCureProportionalCalculateMassFrom ProportionalCalculateMassFrom
    {
        get => _proportionalCalculateMassFrom;
        set
        {
            if (!RaiseAndSetIfChanged(ref _proportionalCalculateMassFrom, value)) return;
            RaisePropertyChanged(nameof(IsProportionalCalculateMassFromPrevious));
        }
    }

    public decimal ProportionalMassRelativeHeight
    {
        get => _proportionalMassRelativeHeight;
        set => RaiseAndSetIfChanged(ref _proportionalMassRelativeHeight, Math.Max(0, Math.Round(value, Layer.HeightPrecision)));
    }

    public bool IsProportionalCalculateMassFromPrevious => _proportionalCalculateMassFrom == SuggestionWaitTimeBeforeCureProportionalCalculateMassFrom.Previous;

    public decimal BottomHeight
    {
        get => _bottomHeight;
        set => RaiseAndSetIfChanged(ref _bottomHeight, Math.Max(0, value));
    }

    public decimal FixedBottomWaitTimeBeforeCure
    {
        get => _fixedBottomWaitTimeBeforeCure;
        set
        {
            if(!RaiseAndSetIfChanged(ref _fixedBottomWaitTimeBeforeCure, Math.Round(Math.Max(0, value), 2))) return;
            RaisePropertyChanged(nameof(WaitTimeBeforeCureTransitionDecrement));
        }
    }

    public decimal FixedWaitTimeBeforeCure
    {
        get => _fixedWaitTimeBeforeCure;
        set
        {
            if (!RaiseAndSetIfChanged(ref _fixedWaitTimeBeforeCure, Math.Round(Math.Max(0, value), 2))) return;
            RaisePropertyChanged(nameof(WaitTimeBeforeCureTransitionDecrement));
        }
    }

    public byte WaitTimeBeforeCureTransitionLayerCount
    {
        get => _waitTimeBeforeCureTransitionLayerCount;
        set
        {
            if(!RaiseAndSetIfChanged(ref _waitTimeBeforeCureTransitionLayerCount, value)) return;
            RaisePropertyChanged(nameof(WaitTimeBeforeCureTransitionDecrement));
        }
    }

    public decimal WaitTimeBeforeCureTransitionDecrement => _waitTimeBeforeCureTransitionLayerCount == 0 ? 0 : Math.Round((_fixedBottomWaitTimeBeforeCure - _fixedWaitTimeBeforeCure) / (_waitTimeBeforeCureTransitionLayerCount + 1), 2);

    public decimal ProportionalBottomWaitTimeBeforeCure
    {
        get => _proportionalBottomWaitTimeBeforeCure;
        set => RaiseAndSetIfChanged(ref _proportionalBottomWaitTimeBeforeCure, Math.Round(Math.Max(0, value), 2));
    }

    public decimal ProportionalWaitTimeBeforeCure
    {
        get => _proportionalWaitTimeBeforeCure;
        set => RaiseAndSetIfChanged(ref _proportionalWaitTimeBeforeCure, Math.Round(Math.Max(0, value), 2));
    }


    public uint ProportionalBottomLayerPixels
    {
        get => _proportionalBottomLayerPixels;
        set => RaiseAndSetIfChanged(ref _proportionalBottomLayerPixels, Math.Max(1, value));
    }

    public uint ProportionalLayerPixels
    {
        get => _proportionalLayerPixels;
        set => RaiseAndSetIfChanged(ref _proportionalLayerPixels, Math.Max(1, value));
    }

    public uint ProportionalBottomLayerArea
    {
        get => _proportionalBottomLayerArea;
        set => RaiseAndSetIfChanged(ref _proportionalBottomLayerArea, Math.Max(1, value));
    }

    public uint ProportionalLayerArea
    {
        get => _proportionalLayerArea;
        set => RaiseAndSetIfChanged(ref _proportionalLayerArea, Math.Max(1, value));
    }

    public decimal ProportionalBottomWaitTimeBeforeCureMaximumDifference
    {
        get => _proportionalBottomWaitTimeBeforeCureMaximumDifference;
        set => RaiseAndSetIfChanged(ref _proportionalBottomWaitTimeBeforeCureMaximumDifference, Math.Round(Math.Max(0, value), 2));
    }

    public decimal ProportionalWaitTimeBeforeCureMaximumDifference
    {
        get => _proportionalWaitTimeBeforeCureMaximumDifference;
        set => RaiseAndSetIfChanged(ref _proportionalWaitTimeBeforeCureMaximumDifference, Math.Round(Math.Max(0, value), 2));
    }

    public decimal MinimumBottomWaitTimeBeforeCure
    {
        get => _minimumBottomWaitTimeBeforeCure;
        set => RaiseAndSetIfChanged(ref _minimumBottomWaitTimeBeforeCure, Math.Round(Math.Max(0, value), 2));
    }

    public decimal MinimumWaitTimeBeforeCure
    {
        get => _minimumWaitTimeBeforeCure;
        set => RaiseAndSetIfChanged(ref _minimumWaitTimeBeforeCure, Math.Round(Math.Max(0, value), 2));
    }

    public decimal MaximumBottomWaitTimeBeforeCure
    {
        get => _maximumBottomWaitTimeBeforeCure;
        set => RaiseAndSetIfChanged(ref _maximumBottomWaitTimeBeforeCure, Math.Round(Math.Max(0, value), 2));
    }

    public decimal MaximumWaitTimeBeforeCure
    {
        get => _maximumWaitTimeBeforeCure;
        set => RaiseAndSetIfChanged(ref _maximumWaitTimeBeforeCure, Math.Round(Math.Max(0, value), 2));
    }

    public bool CreateEmptyFirstLayer
    {
        get => _createEmptyFirstLayer;
        set => RaiseAndSetIfChanged(ref _createEmptyFirstLayer, value);
    }

    #endregion

    #region Constructor

    public SuggestionWaitTimeBeforeCure()
    {
        _applyWhen = SuggestionApplyWhen.Different;
    }

    #endregion

    #region Overrides

    public override string? Validate()
    {
        var sb = new StringBuilder();

        if (_minimumBottomWaitTimeBeforeCure > _maximumBottomWaitTimeBeforeCure)
        {
            sb.AppendLine("Minimum bottom limit can't be higher than maximum bottom limit");
        }

        if (_minimumWaitTimeBeforeCure > _maximumWaitTimeBeforeCure)
        {
            sb.AppendLine("Minimum normal limit can't be higher than maximum normal limit");
        }

        if (_setType is SuggestionWaitTimeBeforeCureSetType.ProportionalLayerPixels
            or SuggestionWaitTimeBeforeCureSetType.ProportionalLayerArea)
        {
            if (_proportionalWaitTimeBeforeCure <= 0)
            {
                sb.AppendLine("The proportional wait time must be higher than 0s");
            }
        }

        switch (_setType)
        {
            case SuggestionWaitTimeBeforeCureSetType.ProportionalLayerPixels:
            {
                if (_proportionalBottomLayerPixels <= 0)
                {
                    sb.AppendLine("The bottom proportional pixels must be higher than 0");
                }

                if (_proportionalLayerPixels <= 0)
                {
                    sb.AppendLine("The proportional pixels must be higher than 0");
                }

                break;
            }
            case SuggestionWaitTimeBeforeCureSetType.ProportionalLayerArea:
            {
                if (_proportionalBottomLayerArea <= 0)
                {
                    sb.AppendLine("The bottom proportional layer area must be higher than 0mm²");
                }

                if (_proportionalLayerArea <= 0)
                {
                    sb.AppendLine("The proportional layer area must be higher than 0mm²");
                }

                break;
            }
        }

        return sb.ToString();
    }

    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        if (SlicerFile.CanUseBottomWaitTimeAfterCure || SlicerFile.CanUseBottomLightOffDelay)
        {
            SlicerFile.SetBottomWaitTimeBeforeCureOrLightOffDelay(CalculateWaitTime(true));
        }
        if (SlicerFile.CanUseWaitTimeAfterCure || SlicerFile.CanUseLightOffDelay)
        {
            SlicerFile.SetNormalWaitTimeBeforeCureOrLightOffDelay(CalculateWaitTime(false));
        }

        if (SlicerFile.CanUseLayerWaitTimeAfterCure || SlicerFile.CanUseLayerLightOffDelay)
        {
            foreach (var layer in SlicerFile)
            {
                if (layer.NonZeroPixelCount <= 1) continue; // Ignore empty layers
                layer.SetWaitTimeBeforeCureOrLightOffDelay(CalculateWaitTime(layer));
            }
        }

        if (_createEmptyFirstLayer && SlicerFile.CanUseLayerPositionZ && !SlicerFile.SupportsGCode)
        {
            var firstLayer = SlicerFile.FirstLayer!;
            if (firstLayer.NonZeroPixelCount > 1) // First layer is not blank as it seems, lets create one
            {
                firstLayer = firstLayer.Clone();
                using var mat = SlicerFile.CreateMatWithDummyPixel();
                firstLayer.LayerMat = mat;
                firstLayer.ExposureTime = SlicerFile.SupportsGCode ? 0 : 0.01f;
                //firstLayer.LiftHeightTotal = SlicerFile.SupportsGCode ? 0 : 0.1f;
                SlicerFile.FirstLayer!.LiftHeightTotal = SlicerFile.SupportsGCode ? 0 : 0.1f; // Already on position, try to not lift
                firstLayer.SetNoDelays();
                SlicerFile.SuppressRebuildPropertiesWork(() => { SlicerFile.Prepend(firstLayer); });
            }

        }

        return true;
    }
        

    public float CalculateWaitTime(bool isBottomLayer, Layer? layer = null)
    {
        if (layer is not null)
        {
            // Reassign isBottomLayer given the layer
            isBottomLayer = layer.IsBottomLayer || (_bottomHeight > 0 && (decimal)layer.PositionZ <= _bottomHeight);
        }

        if (_setType == SuggestionWaitTimeBeforeCureSetType.Fixed)
        {
            if(layer is null || isBottomLayer || _waitTimeBeforeCureTransitionLayerCount == 0) return (float)(isBottomLayer ? _fixedBottomWaitTimeBeforeCure : _fixedWaitTimeBeforeCure);
            
            // Check for transition layer
            var firstNormalLayer = SlicerFile.FirstOrDefault(target => (decimal) target.PositionZ > _bottomHeight);
            if (firstNormalLayer is not null)
            {
                if (layer.Index >= firstNormalLayer.Index &&
                    layer.Index <= firstNormalLayer.Index + _waitTimeBeforeCureTransitionLayerCount)
                {
                    // Is transition layer
                    return (float)Math.Round(Math.Max(_fixedBottomWaitTimeBeforeCure - WaitTimeBeforeCureTransitionDecrement * (layer.Index - firstNormalLayer.Index + 1), _minimumWaitTimeBeforeCure), 2);
                }
            }

            // Fallback
            return (float)(isBottomLayer ? _fixedBottomWaitTimeBeforeCure : _fixedWaitTimeBeforeCure);
        }

        if (layer is null || _setType == SuggestionWaitTimeBeforeCureSetType.Fixed)
        {
            return (float)(isBottomLayer ? _fixedBottomWaitTimeBeforeCure : _fixedWaitTimeBeforeCure);
        }

        if (layer.NonZeroPixelCount <= 1) return 0; // Empty layer, don't need wait time

        float mass = 0;
        if (layer.Index > 0 && _proportionalCalculateMassFrom != SuggestionWaitTimeBeforeCureProportionalCalculateMassFrom.Previous && _proportionalMassRelativeHeight > 0)
        {
            //var previousLayer = layer.GetPreviousLayerWithAtLeastPixelCountOf(2); // Skip all previous empty layer
            //if (previousLayer is not null) layer = previousLayer;
            uint count = 0;

            Layer? previousLayer = layer;

            while ((previousLayer = previousLayer!.PreviousLayer) is not null && (layer.PositionZ - previousLayer.PositionZ) <= (float)_proportionalMassRelativeHeight)
            {
                if(previousLayer.NonZeroPixelCount < 2) continue; // Skip empty layers

                count++;
                switch (_proportionalCalculateMassFrom)
                {
                    case SuggestionWaitTimeBeforeCureProportionalCalculateMassFrom.Average:
                        mass += _setType == SuggestionWaitTimeBeforeCureSetType.ProportionalLayerPixels
                            ? previousLayer.NonZeroPixelCount
                            : previousLayer.GetArea();
                        break;
                    case SuggestionWaitTimeBeforeCureProportionalCalculateMassFrom.Maximum:
                        mass = Math.Max(_setType == SuggestionWaitTimeBeforeCureSetType.ProportionalLayerPixels
                            ? previousLayer.NonZeroPixelCount
                            : previousLayer.GetArea(), mass);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (_proportionalCalculateMassFrom == SuggestionWaitTimeBeforeCureProportionalCalculateMassFrom.Average && mass > 0 && count > 0)
            {
                mass /= count;
            }
        }

        if (mass <= 0)
        {
            var previousLayer = layer.GetPreviousLayerWithAtLeastPixelCountOf(2); // Skip all previous empty layer
            if (previousLayer is null)
            {
                return isBottomLayer ? (float)_fixedBottomWaitTimeBeforeCure : (float)_fixedWaitTimeBeforeCure;
            }
            mass = _setType == SuggestionWaitTimeBeforeCureSetType.ProportionalLayerPixels
                ? previousLayer.NonZeroPixelCount
                : previousLayer.GetArea();
        }

        float value = _setType switch
        {
            SuggestionWaitTimeBeforeCureSetType.ProportionalLayerPixels => (float) (isBottomLayer 
                    ? (decimal)mass * _proportionalBottomWaitTimeBeforeCure / _proportionalBottomLayerPixels 
                    : (decimal)mass * _proportionalWaitTimeBeforeCure / _proportionalLayerPixels),
            SuggestionWaitTimeBeforeCureSetType.ProportionalLayerArea => (float) (isBottomLayer 
                    ? (decimal)mass * _proportionalBottomWaitTimeBeforeCure / _proportionalBottomLayerArea
                    : (decimal)mass * _proportionalWaitTimeBeforeCure / _proportionalLayerArea),
            _ => throw new ArgumentOutOfRangeException()
        };

        if (isBottomLayer)
        {
            if (_proportionalBottomWaitTimeBeforeCureMaximumDifference > 0)
            {
                var previousLayer = layer.GetPreviousLayerWithAtLeastPixelCountOf(2);
                if(previousLayer is not null)
                {
                    value = value.Clamp(
                        Math.Max(0, previousLayer.WaitTimeBeforeCure - (float)_proportionalBottomWaitTimeBeforeCureMaximumDifference),
                        previousLayer.WaitTimeBeforeCure + (float)_proportionalBottomWaitTimeBeforeCureMaximumDifference);
                }
            }
        }
        else
        {
            if (_proportionalWaitTimeBeforeCureMaximumDifference > 0)
            {
                var previousLayer = layer.GetPreviousLayerWithAtLeastPixelCountOf(2);
                if (previousLayer is not null)
                {
                    value = value.Clamp(
                        Math.Max(0, previousLayer.WaitTimeBeforeCure - (float)_proportionalWaitTimeBeforeCureMaximumDifference),
                        previousLayer.WaitTimeBeforeCure + (float)_proportionalWaitTimeBeforeCureMaximumDifference);
                }
            }
        }

        return (float)Math.Round((decimal)value, 2).Clamp(
            isBottomLayer ? _minimumBottomWaitTimeBeforeCure : _minimumWaitTimeBeforeCure,
            isBottomLayer ? _maximumBottomWaitTimeBeforeCure : _maximumWaitTimeBeforeCure);
    }

    public float CalculateWaitTime(Layer layer) => CalculateWaitTime(false, layer);

    #endregion
}