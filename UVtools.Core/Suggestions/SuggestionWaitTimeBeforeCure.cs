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
using UVtools.Core.Extensions;
using UVtools.Core.Layers;

namespace UVtools.Core.Suggestions;

public sealed class SuggestionWaitTimeBeforeCure : Suggestion
{
    #region Enums
    public enum SuggestionWaitTimeBeforeCureSetType
    {
        [Description("Fixed: Use a fixed time")]
        Fixed,
        [Description("Proportional to layer pixels")]
        ProportionalLayerPixels,
        [Description("Proportional to layer area")]
        ProportionalLayerArea,
    }
    #endregion

    #region Members
    private SuggestionWaitTimeBeforeCureSetType _setType = SuggestionWaitTimeBeforeCureSetType.Fixed;
    private decimal _bottomHeight = 1;
    private decimal _fixedBottomWaitTimeBeforeCure = 20;
    private decimal _fixedWaitTimeBeforeCure = 2;
    private decimal _proportionalBottomWaitTimeBeforeCure = 20;
    private decimal _proportionalWaitTimeBeforeCure = 2;
    private uint _proportionalBottomLayerPixels = 1000000;
    private uint _proportionalLayerPixels = 1000000;
    private uint _proportionalBottomLayerArea = 1000;
    private uint _proportionalLayerArea = 1000;
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
                            var waitTime = (decimal)layer.GetBottomWaitTimeBeforeCure();
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
                            if (Math.Abs(layer.GetBottomWaitTimeBeforeCure() - CalculateWaitTime(layer)) > 0.1) return false;
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
                                      $" constrained from [Bottoms={_minimumBottomWaitTimeBeforeCure}/{_maximumBottomWaitTimeBeforeCure}s] and [Normals={_minimumWaitTimeBeforeCure}/{_maximumWaitTimeBeforeCure}s].\n" +
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
        
    public decimal BottomHeight
    {
        get => _bottomHeight;
        set => RaiseAndSetIfChanged(ref _bottomHeight, value);
    }

    public decimal FixedBottomWaitTimeBeforeCure
    {
        get => _fixedBottomWaitTimeBeforeCure;
        set => RaiseAndSetIfChanged(ref _fixedBottomWaitTimeBeforeCure, Math.Round(value, 2));
    }

    public decimal FixedWaitTimeBeforeCure
    {
        get => _fixedWaitTimeBeforeCure;
        set => RaiseAndSetIfChanged(ref _fixedWaitTimeBeforeCure, Math.Round(value, 2));
    }

    public decimal ProportionalBottomWaitTimeBeforeCure
    {
        get => _proportionalBottomWaitTimeBeforeCure;
        set => RaiseAndSetIfChanged(ref _proportionalBottomWaitTimeBeforeCure, Math.Round(value, 2));
    }

    public decimal ProportionalWaitTimeBeforeCure
    {
        get => _proportionalWaitTimeBeforeCure;
        set => RaiseAndSetIfChanged(ref _proportionalWaitTimeBeforeCure, Math.Round(value, 2));
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

    public decimal MinimumBottomWaitTimeBeforeCure
    {
        get => _minimumBottomWaitTimeBeforeCure;
        set => RaiseAndSetIfChanged(ref _minimumBottomWaitTimeBeforeCure, Math.Round(value, 2));
    }

    public decimal MinimumWaitTimeBeforeCure
    {
        get => _minimumWaitTimeBeforeCure;
        set => RaiseAndSetIfChanged(ref _minimumWaitTimeBeforeCure, Math.Round(value, 2));
    }

    public decimal MaximumBottomWaitTimeBeforeCure
    {
        get => _maximumBottomWaitTimeBeforeCure;
        set => RaiseAndSetIfChanged(ref _maximumBottomWaitTimeBeforeCure, Math.Round(value, 2));
    }

    public decimal MaximumWaitTimeBeforeCure
    {
        get => _maximumWaitTimeBeforeCure;
        set => RaiseAndSetIfChanged(ref _maximumWaitTimeBeforeCure, Math.Round(value, 2));
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

    protected override bool ExecuteInternally()
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

        if (layer is null || _setType == SuggestionWaitTimeBeforeCureSetType.Fixed)
        {
            return (float)(isBottomLayer ? _fixedBottomWaitTimeBeforeCure : _fixedWaitTimeBeforeCure);
        }

        if (layer.NonZeroPixelCount <= 1) return 0; // Empty layer, don't need wait time
            
        return _setType switch
        {
            SuggestionWaitTimeBeforeCureSetType.ProportionalLayerPixels => (float)Math.Round(
                (isBottomLayer 
                    ? layer.NonZeroPixelCount * _proportionalBottomWaitTimeBeforeCure / _proportionalBottomLayerPixels 
                    : layer.NonZeroPixelCount * _proportionalWaitTimeBeforeCure / _proportionalLayerPixels).Clamp(
                    isBottomLayer ? _minimumBottomWaitTimeBeforeCure : _minimumWaitTimeBeforeCure,
                    isBottomLayer ? _maximumBottomWaitTimeBeforeCure : _maximumWaitTimeBeforeCure), 2),
            SuggestionWaitTimeBeforeCureSetType.ProportionalLayerArea => (float) Math.Round(
                (isBottomLayer 
                    ? (decimal)layer.GetArea() * _proportionalBottomWaitTimeBeforeCure / _proportionalBottomLayerArea
                    : (decimal)layer.GetArea() * _proportionalWaitTimeBeforeCure / _proportionalLayerArea).Clamp(
                    isBottomLayer ? _minimumBottomWaitTimeBeforeCure : _minimumWaitTimeBeforeCure,
                    isBottomLayer ? _maximumBottomWaitTimeBeforeCure : _maximumWaitTimeBeforeCure), 2),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public float CalculateWaitTime(Layer layer) => CalculateWaitTime(false, layer);

    #endregion
}