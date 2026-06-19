/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel;
using System.Text;
using UVtools.Core.Operations;

namespace UVtools.Core.Suggestions;

public sealed partial class SuggestionWaitTimeAfterCure : Suggestion
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
    #endregion

    #region Properties

    public override bool IsAvailable
    {
        get
        {
            if (SlicerFile is null) return false;
            return SlicerFile.CanUseAnyWaitTimeAfterCure || SlicerFile.CanUseLayerWaitTimeAfterCure;
        }
    }

    public override bool IsApplied
    {
        get
        {
            if (SlicerFile is null) return false;

            switch (ApplyWhen)
            {
                case SuggestionApplyWhen.OutsideLimits:
                    if (SlicerFile.CanUseBottomWaitTimeAfterCure)
                    {
                        if ((decimal)SlicerFile.BottomWaitTimeAfterCure < MinimumWaitTimeAfterCure ||
                            (decimal)SlicerFile.BottomWaitTimeAfterCure > MaximumWaitTimeAfterCure) return false;
                    }
                    if (SlicerFile.CanUseWaitTimeAfterCure)
                    {
                        if ((decimal)SlicerFile.WaitTimeAfterCure < MinimumWaitTimeAfterCure ||
                            (decimal)SlicerFile.WaitTimeAfterCure > MaximumWaitTimeAfterCure) return false;
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
                                if ((decimal) layer.WaitTimeAfterCure < MinimumWaitTimeAfterCure ||
                                    (decimal) layer.WaitTimeAfterCure > MaximumWaitTimeAfterCure) return false;
                            }
                        }
                    }

                    break;
                case SuggestionApplyWhen.Different:
                    if (SlicerFile.CanUseBottomWaitTimeAfterCure)
                    {
                        if (Math.Abs(SlicerFile.BottomWaitTimeAfterCure - CalculateWaitTime(LayerGroup.Bottom, (decimal)SlicerFile.BottomExposureTime)) > 0.1) return false;
                    }
                    if (SlicerFile.CanUseWaitTimeAfterCure)
                    {
                        if (Math.Abs(SlicerFile.WaitTimeAfterCure - CalculateWaitTime(LayerGroup.Normal, (decimal)SlicerFile.ExposureTime)) > 0.1) return false;
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
                                if (Math.Abs(layer.WaitTimeAfterCure - CalculateWaitTime(layer.IsBottomLayer ? LayerGroup.Bottom : LayerGroup.Normal, (decimal)layer.ExposureTime)) > 0.1) return false;
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

    public override string Message
    {
        get
        {
            if (SlicerFile.CanUseLayerWaitTimeAfterCure || SlicerFile is {CanUseBottomWaitTimeAfterCure: true, CanUseWaitTimeAfterCure: true})
            {
                return IsApplied
                    ? $"{GlobalAppliedMessage}: {SlicerFile.BottomWaitTimeAfterCure}/{SlicerFile.WaitTimeAfterCure}s"
                    : $"{GlobalNotAppliedMessage} of {SlicerFile.BottomWaitTimeAfterCure}/{SlicerFile.WaitTimeAfterCure}s " +
                      $"is out of the recommended {CalculateWaitTime(LayerGroup.Bottom, (decimal) SlicerFile.BottomExposureTime)}/{CalculateWaitTime(LayerGroup.Normal, (decimal) SlicerFile.ExposureTime)}s";
            }

            // Single property
            return IsApplied
                ? $"{GlobalAppliedMessage}: {SlicerFile.WaitTimeAfterCure}s"
                : $"{GlobalNotAppliedMessage} of {SlicerFile.WaitTimeAfterCure}s " +
                  $"is out of the recommended {CalculateWaitTime(LayerGroup.Normal, (decimal)SlicerFile.ExposureTime)}s";
        }
    }

    public override string ToolTip
    {
        get
        {
            if (SlicerFile.CanUseLayerWaitTimeAfterCure || SlicerFile is {CanUseBottomWaitTimeAfterCure: true, CanUseWaitTimeAfterCure: true})
            {
                return (SetType == SuggestionWaitTimeAfterCureSetType.Fixed
                           ? $"The recommended wait time must be {FixedBottomWaitTimeAfterCure}/{FixedWaitTimeAfterCure}s"
                           : $"The recommended wait time is a ratio of (wait time){ProportionalWaitTimeAfterCure}s to (exposure time){ProportionalExposureTime}s") +
                       $" constrained from [Bottoms={MinimumBottomWaitTimeAfterCure}s to {MaximumBottomWaitTimeAfterCure}s] and [Normals={MinimumWaitTimeAfterCure}s to {MaximumWaitTimeAfterCure}s].\n" +
                       $"Explanation: {Description}";
            }

            return (SetType == SuggestionWaitTimeAfterCureSetType.Fixed
                       ? $"The recommended wait time must be {FixedWaitTimeAfterCure}s"
                       : $"The recommended wait time is a ratio of (wait time){ProportionalWaitTimeAfterCure}s to (exposure time){ProportionalExposureTime}s") +
                   $" constrained from {MinimumWaitTimeAfterCure}s to {MaximumWaitTimeAfterCure}s.\n" +
                   $"Explanation: {Description}";
        }
    }

    public override string? ConfirmationMessage
    {
        get
        {
            if (SlicerFile.CanUseLayerWaitTimeAfterCure || SlicerFile is {CanUseBottomWaitTimeAfterCure: true, CanUseWaitTimeAfterCure: true})
            {
                return $"{Title}: {SlicerFile.BottomWaitTimeAfterCure}/{SlicerFile.WaitTimeAfterCure}s » {CalculateWaitTime(LayerGroup.Bottom, (decimal) SlicerFile.BottomWaitTimeAfterCure)}/{CalculateWaitTime(LayerGroup.Normal, (decimal) SlicerFile.WaitTimeAfterCure)}s";
            }

            // Single property
            return $"{Title}: {SlicerFile.WaitTimeAfterCure}s » {CalculateWaitTime(LayerGroup.Normal, (decimal)SlicerFile.WaitTimeAfterCure)}s";
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSetTypeFixed))]
    [NotifyPropertyChangedFor(nameof(IsSetTypeProportionalExposure))]
    public partial SuggestionWaitTimeAfterCureSetType SetType { get; set; } = SuggestionWaitTimeAfterCureSetType.Fixed;

    public bool IsSetTypeFixed => SetType == SuggestionWaitTimeAfterCureSetType.Fixed;
    public bool IsSetTypeProportionalExposure => SetType == SuggestionWaitTimeAfterCureSetType.ProportionalExposure;

    public decimal FixedBottomWaitTimeAfterCure
    {
        get;
        set => SetProperty(ref field, Math.Round(value, 2));
    } = 7;

    public decimal FixedWaitTimeAfterCure
    {
        get;
        set => SetProperty(ref field, Math.Round(value, 2));
    } = 1;

    public decimal ProportionalWaitTimeAfterCure
    {
        get;
        set => SetProperty(ref field, Math.Round(value, 2));
    } = 1;

    public decimal ProportionalExposureTime
    {
        get;
        set => SetProperty(ref field, Math.Round(value, 2));
    } = 3;
        
    public decimal MinimumBottomWaitTimeAfterCure
    {
        get;
        set => SetProperty(ref field, Math.Round(value, 2));
    } = 3;

    public decimal MinimumWaitTimeAfterCure
    {
        get;
        set => SetProperty(ref field, Math.Round(value, 2));
    } = 1;

    public decimal MaximumBottomWaitTimeAfterCure
    {
        get;
        set => SetProperty(ref field, Math.Round(value, 2));
    } = 20;

    public decimal MaximumWaitTimeAfterCure
    {
        get;
        set => SetProperty(ref field, Math.Round(value, 2));
    } = 12;

    #endregion

    #region Constructor

    public SuggestionWaitTimeAfterCure()
    {
        ApplyWhen = SuggestionApplyWhen.Different;
    }

    #endregion

    #region Overrides

    public override string? Validate()
    {
        var sb = new StringBuilder();

        if (MinimumBottomWaitTimeAfterCure > MaximumBottomWaitTimeAfterCure)
        {
            sb.AppendLine("Minimum bottom limit can't be higher than maximum bottom limit");
        }

        if (MinimumWaitTimeAfterCure > MaximumWaitTimeAfterCure)
        {
            sb.AppendLine("Minimum normal limit can't be higher than maximum normal limit");
        }

        if (SetType == SuggestionWaitTimeAfterCureSetType.ProportionalExposure)
        {
            if (ProportionalWaitTimeAfterCure <= 0)
            {
                sb.AppendLine("The proportional wait time must be higher than 0s");
            }

            if (ProportionalExposureTime <= 0)
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
            SlicerFile.BottomWaitTimeAfterCure = CalculateWaitTime(LayerGroup.Bottom, (decimal)SlicerFile.BottomExposureTime);
        }
        if (SlicerFile.CanUseWaitTimeAfterCure)
        {
            SlicerFile.WaitTimeAfterCure = CalculateWaitTime(LayerGroup.Normal, (decimal)SlicerFile.ExposureTime);
        }
    
        if (SlicerFile.CanUseLayerWaitTimeAfterCure)
        {
            foreach (var layer in SlicerFile)
            {
                layer.WaitTimeAfterCure = layer.IsDummy ? 0 : CalculateWaitTime(layer.IsBottomLayer ? LayerGroup.Bottom : LayerGroup.Normal, (decimal) layer.ExposureTime);
            }
        }

        return true;
    }
        

    public float CalculateWaitTime(LayerGroup layerGroup, decimal exposureTime)
    {
        return SetType switch
        {
            SuggestionWaitTimeAfterCureSetType.Fixed => (float) (layerGroup == LayerGroup.Bottom
                ? FixedBottomWaitTimeAfterCure
                : FixedWaitTimeAfterCure),
            SuggestionWaitTimeAfterCureSetType.ProportionalExposure => 
                (float)Math.Clamp(
                    Math.Round(exposureTime * ProportionalWaitTimeAfterCure / ProportionalExposureTime, 2),
                    layerGroup == LayerGroup.Bottom ? MinimumBottomWaitTimeAfterCure : MinimumWaitTimeAfterCure,
                    layerGroup == LayerGroup.Bottom ? MaximumBottomWaitTimeAfterCure : MaximumWaitTimeAfterCure
                ),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    #endregion
}
