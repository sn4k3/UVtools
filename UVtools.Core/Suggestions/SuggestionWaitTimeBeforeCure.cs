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
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Operations;
using ZLinq;

namespace UVtools.Core.Suggestions;

public sealed partial class SuggestionWaitTimeBeforeCure : Suggestion
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
    #endregion

    #region Properties

    public override bool IsAvailable
    {
        get
        {
            if (SlicerFile is null) return false;
            return SlicerFile.CanUseAnyWaitTimeBeforeCure || SlicerFile.CanUseAnyLightOffDelay || SlicerFile.CanUseLayerAnyWaitTimeBeforeCure;
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
                    if (SlicerFile.CanUseBottomWaitTimeBeforeCure || SlicerFile.CanUseBottomLightOffDelay)
                    {
                        var waitTime = (decimal)SlicerFile.GetBottomWaitTimeBeforeCure();
                        if (waitTime < MinimumWaitTimeBeforeCure ||
                            waitTime > MaximumWaitTimeBeforeCure) return false;
                    }
                    if (SlicerFile.CanUseWaitTimeBeforeCure || SlicerFile.CanUseLightOffDelay)
                    {
                        var waitTime = (decimal)SlicerFile.GetNormalWaitTimeBeforeCure();
                        if (waitTime < MinimumWaitTimeBeforeCure ||
                            waitTime > MaximumWaitTimeBeforeCure) return false;
                    }

                    if (SlicerFile.CanUseLayerWaitTimeBeforeCure || SlicerFile.CanUseLayerLightOffDelay)
                    {
                        foreach (var layer in SlicerFile)
                        {
                            if (layer.IsDummy)
                            {
                                if (layer.GetWaitTimeBeforeCure() > 0.1) return false;
                            }
                            else
                            {
                                var waitTime = (decimal)layer.GetWaitTimeBeforeCure();
                                if (waitTime < MinimumWaitTimeBeforeCure || waitTime > MaximumWaitTimeBeforeCure) return false;
                            }
                        }
                    }

                    break;
                case SuggestionApplyWhen.Different:
                    if (SlicerFile.CanUseBottomWaitTimeBeforeCure || SlicerFile.CanUseBottomLightOffDelay)
                    {
                        if (Math.Abs(SlicerFile.GetBottomWaitTimeBeforeCure() - CalculateWaitTime(LayerGroup.Bottom)) > 0.1) return false;
                    }
                    if (SlicerFile.CanUseWaitTimeBeforeCure || SlicerFile.CanUseLightOffDelay)
                    {
                        if (Math.Abs(SlicerFile.GetNormalWaitTimeBeforeCure() - CalculateWaitTime(LayerGroup.Normal)) > 0.1) return false;
                    }

                    if (SlicerFile.CanUseLayerWaitTimeBeforeCure || SlicerFile.CanUseLayerLightOffDelay)
                    {
                        foreach (var layer in SlicerFile)
                        {
                            if (layer.IsDummy)
                            {
                                if (layer.GetWaitTimeBeforeCure() > 0.1) return false;
                            }
                            else
                            {
                                if (Math.Abs(layer.GetWaitTimeBeforeCure() - CalculateWaitTime(layer)) > 0.1) return false;
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

    public override string Title => "Wait time before cure";
    public override string Description => "Rest some time before cure the layer is crucial to let the resin settle after the lift sequence and allow some time for the arm settle at the correct Z position as the resin will offer some resistance and push the structure.\n" +
                                          "This lead to better quality with more successful prints, less lamination problems, better first layers with more success of stick to the build plate and less elephant foot effect.";

    public override string Message
    {
        get
        {
            if(SlicerFile.CanUseLayerAnyWaitTimeBeforeCure
               || SlicerFile
                   is {CanUseBottomWaitTimeBeforeCure: true, CanUseWaitTimeBeforeCure: true}
                   or {CanUseBottomLightOffDelay: true, CanUseLightOffDelay: true})
            {
                return IsApplied
                    ? $"{GlobalAppliedMessage}: {SlicerFile.GetBottomWaitTimeBeforeCure()}/{SlicerFile.GetNormalWaitTimeBeforeCure()}s"
                    : $"{GlobalNotAppliedMessage} of {SlicerFile.GetBottomWaitTimeBeforeCure()}/{SlicerFile.GetNormalWaitTimeBeforeCure()}s " +
                      $"is out of the recommended {CalculateWaitTime(LayerGroup.Bottom)}/{CalculateWaitTime(LayerGroup.Normal)}s";
            }

            // Single property
            return IsApplied
                ? $"{GlobalAppliedMessage}: {SlicerFile.GetNormalWaitTimeBeforeCure()}s"
                : $"{GlobalNotAppliedMessage} of {SlicerFile.GetNormalWaitTimeBeforeCure()}s " +
                  $"is out of the recommended {CalculateWaitTime(LayerGroup.Normal)}s";
        }
    }

    public override string ToolTip
    {
        get
        {
            if (SlicerFile.CanUseLayerAnyWaitTimeBeforeCure
                || SlicerFile
                    is {CanUseBottomWaitTimeBeforeCure: true, CanUseWaitTimeBeforeCure: true}
                    or {CanUseBottomLightOffDelay: true, CanUseLightOffDelay: true})
            {
                return (SetType == SuggestionWaitTimeBeforeCureSetType.Fixed
                           ? $"The recommended wait time must be {FixedBottomWaitTimeBeforeCure}/{FixedWaitTimeBeforeCure}s"
                           : $"The recommended wait time is a ratio of (wait time){ProportionalWaitTimeBeforeCure}s to (exposure time){ProportionalLayerArea}s") +
                       $" constrained from [Bottoms={MinimumBottomWaitTimeBeforeCure}s to {MaximumBottomWaitTimeBeforeCure}s] and [Normals={MinimumWaitTimeBeforeCure}s to {MaximumWaitTimeBeforeCure}s].\n" +
                       $"Explanation: {Description}";
            }

            // Single property
            return (SetType == SuggestionWaitTimeBeforeCureSetType.Fixed
                       ? $"The recommended wait time must be {FixedWaitTimeBeforeCure}s"
                       : $"The recommended wait time is a ratio of (wait time){ProportionalWaitTimeBeforeCure}s to (exposure time){ProportionalLayerArea}s") +
                   $" constrained from {MinimumWaitTimeBeforeCure}s to {MaximumWaitTimeBeforeCure}s.\n" +
                   $"Explanation: {Description}";
        }
    }

    public override string? ConfirmationMessage
    {
        get
        {
            if (SlicerFile.CanUseLayerAnyWaitTimeBeforeCure
                || SlicerFile
                    is {CanUseBottomWaitTimeBeforeCure: true, CanUseWaitTimeBeforeCure: true}
                    or {CanUseBottomLightOffDelay: true, CanUseLightOffDelay: true})
            {
                return $"{Title}: {SlicerFile.BottomWaitTimeBeforeCure}/{SlicerFile.WaitTimeBeforeCure}s » {CalculateWaitTime(LayerGroup.Bottom)}/{CalculateWaitTime(LayerGroup.Normal)}s";
            }

            // Single property
            return $"{Title}: {SlicerFile.WaitTimeBeforeCure}s » {CalculateWaitTime(LayerGroup.Normal)}s";

        }
    }

    public override string? InformationUrl => "https://blog.honzamrazek.cz/2022/01/prints-not-sticking-to-the-build-plate-layer-separation-rough-surface-on-a-resin-printer-resin-viscosity-the-common-denominator";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSetTypeFixed))]
    [NotifyPropertyChangedFor(nameof(IsSetTypeProportionalLayerPixels))]
    [NotifyPropertyChangedFor(nameof(IsSetTypeProportionalLayerArea))]
    public partial SuggestionWaitTimeBeforeCureSetType SetType { get; set; } = SuggestionWaitTimeBeforeCureSetType.ProportionalLayerArea;

    public bool IsSetTypeFixed => SetType == SuggestionWaitTimeBeforeCureSetType.Fixed;
    public bool IsSetTypeProportionalLayerPixels => SetType == SuggestionWaitTimeBeforeCureSetType.ProportionalLayerPixels;
    public bool IsSetTypeProportionalLayerArea => SetType == SuggestionWaitTimeBeforeCureSetType.ProportionalLayerArea;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsProportionalCalculateMassFromPrevious))]
    public partial SuggestionWaitTimeBeforeCureProportionalCalculateMassFrom ProportionalCalculateMassFrom { get; set; } = SuggestionWaitTimeBeforeCureProportionalCalculateMassFrom.Previous;

    public decimal ProportionalMassRelativeHeight
    {
        get;
        set => SetProperty(ref field, Math.Max(0, Math.Round(value, Layer.HeightPrecision)));
    } = 0.2m;

    public bool IsProportionalCalculateMassFromPrevious => ProportionalCalculateMassFrom == SuggestionWaitTimeBeforeCureProportionalCalculateMassFrom.Previous;

    public decimal BottomHeight
    {
        get;
        set => SetProperty(ref field, Math.Max(0, value));
    } = 1;

    public decimal FixedBottomWaitTimeBeforeCure
    {
        get;
        set
        {
            if (SetProperty(ref field, Math.Round(Math.Max(0, value), 2)))
            {
                OnPropertyChanged(nameof(WaitTimeBeforeCureTransitionDecrement));
            }
        }
    } = 15;

    public decimal FixedWaitTimeBeforeCure
    {
        get;
        set
        {
            if (SetProperty(ref field, Math.Round(Math.Max(0, value), 2)))
            {
                OnPropertyChanged(nameof(WaitTimeBeforeCureTransitionDecrement));
            }
        }
    } = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WaitTimeBeforeCureTransitionDecrement))]
    public partial byte WaitTimeBeforeCureTransitionLayerCount { get; set; } = 8;

    public decimal WaitTimeBeforeCureTransitionDecrement => WaitTimeBeforeCureTransitionLayerCount == 0 ? 0 : Math.Round((FixedBottomWaitTimeBeforeCure - FixedWaitTimeBeforeCure) / (WaitTimeBeforeCureTransitionLayerCount + 1), 2);

    public decimal ProportionalBottomWaitTimeBeforeCure
    {
        get;
        set => SetProperty(ref field, Math.Round(Math.Max(0, value), 2));
    } = 30;

    public decimal ProportionalWaitTimeBeforeCure
    {
        get;
        set => SetProperty(ref field, Math.Round(Math.Max(0, value), 2));
    } = 10;


    public uint ProportionalBottomLayerPixels
    {
        get;
        set => SetProperty(ref field, Math.Max(1, value));
    } = 5000000;

    public uint ProportionalLayerPixels
    {
        get;
        set => SetProperty(ref field, Math.Max(1, value));
    } = 5000000;

    public uint ProportionalBottomLayerArea
    {
        get;
        set => SetProperty(ref field, Math.Max(1, value));
    } = 10000;

    public uint ProportionalLayerArea
    {
        get;
        set => SetProperty(ref field, Math.Max(1, value));
    } = 10000;

    public decimal ProportionalBottomWaitTimeBeforeCureMaximumDifference
    {
        get;
        set => SetProperty(ref field, Math.Round(Math.Max(0, value), 2));
    } = 1;

    public decimal ProportionalWaitTimeBeforeCureMaximumDifference
    {
        get;
        set => SetProperty(ref field, Math.Round(Math.Max(0, value), 2));
    } = 1;

    public decimal MinimumBottomWaitTimeBeforeCure
    {
        get;
        set => SetProperty(ref field, Math.Round(Math.Max(0, value), 2));
    } = 10;

    public decimal MinimumWaitTimeBeforeCure
    {
        get;
        set => SetProperty(ref field, Math.Round(Math.Max(0, value), 2));
    } = 1;

    public decimal MaximumBottomWaitTimeBeforeCure
    {
        get;
        set => SetProperty(ref field, Math.Round(Math.Max(0, value), 2));
    } = 30;

    public decimal MaximumWaitTimeBeforeCure
    {
        get;
        set => SetProperty(ref field, Math.Round(Math.Max(0, value), 2));
    } = 4;

    [ObservableProperty]
    public partial bool CreateEmptyFirstLayer { get; set; } = true;

    #endregion

    #region Constructor

    public SuggestionWaitTimeBeforeCure()
    {
        ApplyWhen = SuggestionApplyWhen.Different;
    }

    #endregion

    #region Overrides

    public override string? Validate()
    {
        var sb = new StringBuilder();

        if (MinimumBottomWaitTimeBeforeCure > MaximumBottomWaitTimeBeforeCure)
        {
            sb.AppendLine("Minimum bottom limit can't be higher than maximum bottom limit");
        }

        if (MinimumWaitTimeBeforeCure > MaximumWaitTimeBeforeCure)
        {
            sb.AppendLine("Minimum normal limit can't be higher than maximum normal limit");
        }

        if (SetType is SuggestionWaitTimeBeforeCureSetType.ProportionalLayerPixels
            or SuggestionWaitTimeBeforeCureSetType.ProportionalLayerArea)
        {
            if (ProportionalWaitTimeBeforeCure <= 0)
            {
                sb.AppendLine("The proportional wait time must be higher than 0s");
            }
        }

        switch (SetType)
        {
            case SuggestionWaitTimeBeforeCureSetType.ProportionalLayerPixels:
            {
                if (ProportionalBottomLayerPixels <= 0)
                {
                    sb.AppendLine("The bottom proportional pixels must be higher than 0");
                }

                if (ProportionalLayerPixels <= 0)
                {
                    sb.AppendLine("The proportional pixels must be higher than 0");
                }

                break;
            }
            case SuggestionWaitTimeBeforeCureSetType.ProportionalLayerArea:
            {
                if (ProportionalBottomLayerArea <= 0)
                {
                    sb.AppendLine("The bottom proportional layer area must be higher than 0mm²");
                }

                if (ProportionalLayerArea <= 0)
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
        SlicerFile.SuppressRebuildPropertiesWork(() =>
        {
            if (SlicerFile.CanUseBottomWaitTimeBeforeCure || SlicerFile.CanUseBottomLightOffDelay)
            {
                SlicerFile.SetBottomWaitTimeBeforeCureOrLightOffDelay(CalculateWaitTime(LayerGroup.Bottom));
            }
            if (SlicerFile.CanUseWaitTimeBeforeCure || SlicerFile.CanUseLightOffDelay)
            {
                SlicerFile.SetNormalWaitTimeBeforeCureOrLightOffDelay(CalculateWaitTime(LayerGroup.Normal));
            }
        });

        if (SlicerFile.CanUseLayerWaitTimeBeforeCure || SlicerFile.CanUseLayerLightOffDelay)
        {
            foreach (var layer in SlicerFile)
            {
                layer.SetWaitTimeBeforeCureOrLightOffDelay(CalculateWaitTime(layer));
            }
        }

        if (CreateEmptyFirstLayer && SlicerFile is (ChituboxFile or CTBEncryptedFile or GooFile) and { CanUseLayerPositionZ: true, SupportGCode: false})
        {
            var firstLayer = SlicerFile.FirstLayer!;
            if (!firstLayer.IsDummy) // First layer is not blank as it seems, lets create one
            {
                firstLayer = firstLayer.Clone();
                using var mat = SlicerFile.CreateMatWithDummyPixelFromLayer(0);
                firstLayer.LayerMat = mat;
                firstLayer.LightPWM = SlicerFile.SupportGCode ? byte.MinValue : (byte)1;
                firstLayer.ExposureTime = SlicerFile.SupportGCode ? 0 : 0.01f;
                //firstLayer.LiftHeightTotal = SlicerFile.SupportsGCode ? 0 : 0.1f;
                SlicerFile.FirstLayer!.LiftHeightTotal = SlicerFile.SupportGCode ? 0 : 0.1f; // Already on position, try to not lift
                firstLayer.SetNoDelays();
                SlicerFile.SuppressRebuildPropertiesWork(() =>
                {
                    if (SlicerFile.BottomLayerCount is > 0 and <= 7)
                    {
                        // Increase bottom layer count as dummy layer will count as one but constrain to a maximum
                        SlicerFile.BottomLayerCount++;
                    }
                    SlicerFile.Prepend(firstLayer);
                });
            }
        }

        return true;
    }


    public float CalculateWaitTime(LayerGroup layerGroup, Layer? layer = null)
    {
        if (layer is not null)
        {
            // Reassign isBottomLayer given the layer
            layerGroup = layer.IsBottomLayer || (BottomHeight > 0 && (decimal)layer.PositionZ <= BottomHeight) ? LayerGroup.Bottom : LayerGroup.Normal;
        }

        if (SetType == SuggestionWaitTimeBeforeCureSetType.Fixed)
        {
            if(layer is null || layerGroup == LayerGroup.Bottom || WaitTimeBeforeCureTransitionLayerCount == 0) return (float)(layerGroup == LayerGroup.Bottom ? FixedBottomWaitTimeBeforeCure : FixedWaitTimeBeforeCure);

            // Check for transition layer
            var firstNormalLayer = SlicerFile.AsValueEnumerable().FirstOrDefault(target => (decimal) target.PositionZ > BottomHeight);
            if (firstNormalLayer is not null)
            {
                if (layer.Index >= firstNormalLayer.Index &&
                    layer.Index <= firstNormalLayer.Index + WaitTimeBeforeCureTransitionLayerCount)
                {
                    // Is transition layer
                    return (float)Math.Round(Math.Max(FixedBottomWaitTimeBeforeCure - WaitTimeBeforeCureTransitionDecrement * (layer.Index - firstNormalLayer.Index + 1), MinimumWaitTimeBeforeCure), 2);
                }
            }

            // Fallback
            return (float)(layerGroup == LayerGroup.Bottom ? FixedBottomWaitTimeBeforeCure : FixedWaitTimeBeforeCure);
        }

        if (layer is null || SetType == SuggestionWaitTimeBeforeCureSetType.Fixed)
        {
            return (float)(layerGroup == LayerGroup.Bottom ? FixedBottomWaitTimeBeforeCure : FixedWaitTimeBeforeCure);
        }

        if (layer.IsDummy) return 0; // Empty layer or not exposed, don't need wait time

        float mass = 0;
        if (layer.Index > 0 && ProportionalCalculateMassFrom != SuggestionWaitTimeBeforeCureProportionalCalculateMassFrom.Previous && ProportionalMassRelativeHeight > 0)
        {
            //var previousLayer = layer.GetPreviousLayerWithAtLeastPixelCountOf(2); // Skip all previous empty layer
            //if (previousLayer is not null) layer = previousLayer;
            uint count = 0;

            var previousLayer = layer;

            while ((previousLayer = previousLayer!.PreviousLayer) is not null && (layer.PositionZ - previousLayer.PositionZ) <= (float)ProportionalMassRelativeHeight)
            {
                if(previousLayer.IsDummy) continue; // Skip empty or not exposed layers

                count++;
                switch (ProportionalCalculateMassFrom)
                {
                    case SuggestionWaitTimeBeforeCureProportionalCalculateMassFrom.Average:
                        mass += SetType == SuggestionWaitTimeBeforeCureSetType.ProportionalLayerPixels
                            ? previousLayer.NonZeroPixelCount
                            : previousLayer.GetArea();
                        break;
                    case SuggestionWaitTimeBeforeCureProportionalCalculateMassFrom.Maximum:
                        mass = Math.Max(SetType == SuggestionWaitTimeBeforeCureSetType.ProportionalLayerPixels
                            ? previousLayer.NonZeroPixelCount
                            : previousLayer.GetArea(), mass);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (ProportionalCalculateMassFrom == SuggestionWaitTimeBeforeCureProportionalCalculateMassFrom.Average && mass > 0 && count > 0)
            {
                mass /= count;
            }
        }

        if (mass <= 0)
        {
            var previousLayer = layer.GetPreviousLayerWithAtLeastPixelCountOf(2) ?? layer.GetNextLayerWithAtLeastPixelCountOf(2); // Skip all empty layers
            if (previousLayer is null) // No parent layer to calculate from, set to fixed values
            {
                return layerGroup == LayerGroup.Bottom ? (float)FixedBottomWaitTimeBeforeCure : (float)FixedWaitTimeBeforeCure;
            }
            mass = SetType == SuggestionWaitTimeBeforeCureSetType.ProportionalLayerPixels
                ? previousLayer.NonZeroPixelCount
                : previousLayer.GetArea();
        }

        float value = SetType switch
        {
            SuggestionWaitTimeBeforeCureSetType.ProportionalLayerPixels => (float) (layerGroup == LayerGroup.Bottom
                    ? (decimal)mass * ProportionalBottomWaitTimeBeforeCure / ProportionalBottomLayerPixels
                    : (decimal)mass * ProportionalWaitTimeBeforeCure / ProportionalLayerPixels),
            SuggestionWaitTimeBeforeCureSetType.ProportionalLayerArea => (float) (layerGroup == LayerGroup.Bottom
                    ? (decimal)mass * ProportionalBottomWaitTimeBeforeCure / ProportionalBottomLayerArea
                    : (decimal)mass * ProportionalWaitTimeBeforeCure / ProportionalLayerArea),
            _ => throw new ArgumentOutOfRangeException()
        };

        if (layerGroup == LayerGroup.Bottom)
        {
            if (ProportionalBottomWaitTimeBeforeCureMaximumDifference > 0)
            {
                var previousLayer = layer.GetPreviousLayerWithAtLeastPixelCountOf(2);
                if(previousLayer is not null)
                {
                    value = Math.Clamp(value,
                        Math.Max(0, previousLayer.WaitTimeBeforeCure - (float)ProportionalBottomWaitTimeBeforeCureMaximumDifference),
                        previousLayer.WaitTimeBeforeCure + (float)ProportionalBottomWaitTimeBeforeCureMaximumDifference);
                }
            }
        }
        else
        {
            if (ProportionalWaitTimeBeforeCureMaximumDifference > 0)
            {
                var previousLayer = layer.GetPreviousLayerWithAtLeastPixelCountOf(2);
                if (previousLayer is not null)
                {
                    value = Math.Clamp(value,
                        Math.Max(0, previousLayer.WaitTimeBeforeCure - (float)ProportionalWaitTimeBeforeCureMaximumDifference),
                        previousLayer.WaitTimeBeforeCure + (float)ProportionalWaitTimeBeforeCureMaximumDifference);
                }
            }
        }

        return (float)Math.Clamp(Math.Round((decimal)value, 2),
            layerGroup == LayerGroup.Bottom ? MinimumBottomWaitTimeBeforeCure : MinimumWaitTimeBeforeCure,
            layerGroup == LayerGroup.Bottom ? MaximumBottomWaitTimeBeforeCure : MaximumWaitTimeBeforeCure);
    }

    public float CalculateWaitTime(Layer layer) => layer.IsDummy ? 0 : CalculateWaitTime(LayerGroup.Normal, layer);

    #endregion
}
