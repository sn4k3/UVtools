/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Text;
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;

namespace UVtools.Core.Suggestions;

public sealed partial class SuggestionTransitionLayerCount : Suggestion
{
    #region Members

    #endregion

    #region Properties

    public override bool IsAvailable
    {
        get
        {
            if (SlicerFile is null) return false;
            return SlicerFile.CanUseLayerExposureTime && SlicerFile.LayerCount != 0;
        }
    }

    public override bool IsApplied
    {
        get
        {
            if (SlicerFile is null) return false;

            // Can't apply
            if (SlicerFile.LayerCount < 3
                || SlicerFile.BottomLayerCount == 0
                || SlicerFile.BottomExposureTime <= 0
                || SlicerFile.ExposureTime <= 0
                || SlicerFile.BottomExposureTime <= SlicerFile.ExposureTime
                || SlicerFile.BottomLayerCount + MinimumTransitionLayerCount > SlicerFile.LayerCount
                || SlicerFile.MaximumPossibleTransitionLayerCount < MinimumTransitionLayerCount) return true;

            var actualTransitionLayerCount = SlicerFile.TransitionLayerType == FileFormat.TransitionLayerTypes.Firmware 
                ? SlicerFile.TransitionLayerCount 
                : SlicerFile.ParseTransitionLayerCountFromLayers();
            var suggestedTransitionLayerCount = TransitionLayerCount;

            if (actualTransitionLayerCount == suggestedTransitionLayerCount) return true;
            
            return ApplyWhen switch
            {
                SuggestionApplyWhen.OutsideLimits => actualTransitionLayerCount >= MinimumTransitionLayerCount &&
                                                     actualTransitionLayerCount <= MaximumTransitionLayerCount,
                SuggestionApplyWhen.Different => actualTransitionLayerCount == suggestedTransitionLayerCount,
                    
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public override string Title => "Transition layers";

    public override string Description => "If you are printing flat on the build plate your model will print better when using a smooth transition exposure time instead of a harsh variation, resulting in reduced layer line effect and avoid possible problems due the large exposure difference.\n" +
                                          "This is not so important when your model print raised under a raft/supports unaffected by the bottom exposure, in that case, it's fine to ignore this.";

    public override string Message
    {
        get
        {
            var actualTransitionDecrementTime = SlicerFile.TransitionLayerType == FileFormat.TransitionLayerTypes.Firmware
                ? SlicerFile.GetTransitionStepTime()
                : SlicerFile.ParseTransitionStepTimeFromLayers();
            var actualTransitionLayerCount = SlicerFile.TransitionLayerType == FileFormat.TransitionLayerTypes.Firmware
                ? SlicerFile.TransitionLayerCount
                : SlicerFile.ParseTransitionLayerCountFromLayers();

            var suggestedTransitionLayerCount = TransitionLayerCount;
            var suggestedTransitionDecrementTime = SlicerFile.GetTransitionStepTimeFromLayers(suggestedTransitionLayerCount);

            var bottomExposureTime = SlicerFile.LastBottomLayer?.ExposureTime ?? SlicerFile.BottomExposureTime;
            var exposureTime = SlicerFile.ExposureTime;

            var layerIndex = SlicerFile.TransitionLayerCount > 0
                ? SlicerFile.BottomLayerCount + SlicerFile.TransitionLayerCount
                : SlicerFile.BottomLayerCount;

            if (SlicerFile.ContainsLayer(layerIndex))
            {
                exposureTime = SlicerFile[layerIndex].ExposureTime;
            }

            return IsApplied
                ? $"{GlobalAppliedMessage}: {bottomExposureTime}s » {(actualTransitionDecrementTime <= 0 || actualTransitionLayerCount == 0 ? string.Empty : $"[-{actualTransitionDecrementTime}s/{actualTransitionLayerCount} layers] » ")}{exposureTime}s"
                : $"{GlobalNotAppliedMessage} {bottomExposureTime}s » {(actualTransitionDecrementTime <= 0 || actualTransitionLayerCount == 0 ? string.Empty : $"[-{actualTransitionDecrementTime}s/{actualTransitionLayerCount} layers] » ")}{exposureTime}s is out of the recommended {bottomExposureTime}s » {(suggestedTransitionDecrementTime <= 0 || suggestedTransitionLayerCount == 0 ? string.Empty : $"[-{suggestedTransitionDecrementTime}s/{suggestedTransitionLayerCount} layers] » ")}{exposureTime}s";
        }
    }

    public override string ToolTip => $"The recommended transition time is ±{TransitionStepTime}s constrained over [{MinimumTransitionLayerCount} to {MaximumTransitionLayerCount}] layers.\n" +
                                      $"Explanation: {Description}";

    public override string? InformationUrl => "https://ameralabs.com/blog/9-settings-to-change-for-faster-resin-3d-printing";

    public override string? ConfirmationMessage
    {
        get
        {
            var actualTransitionDecrementTime = SlicerFile.TransitionLayerType == FileFormat.TransitionLayerTypes.Firmware
                ? SlicerFile.GetTransitionStepTime()
                : SlicerFile.ParseTransitionStepTimeFromLayers();
            var actualTransitionLayerCount = SlicerFile.TransitionLayerType == FileFormat.TransitionLayerTypes.Firmware
                ? SlicerFile.TransitionLayerCount
                : SlicerFile.ParseTransitionLayerCountFromLayers();

            var suggestedTransitionLayerCount = TransitionLayerCount;
            var suggestedTransitionDecrementTime = SlicerFile.GetTransitionStepTimeFromLayers(suggestedTransitionLayerCount);

            var bottomExposureTime = SlicerFile.LastBottomLayer?.ExposureTime ?? SlicerFile.BottomExposureTime;
            var exposureTime = SlicerFile.ExposureTime;

            var layerIndex = SlicerFile.TransitionLayerCount > 0
                ? SlicerFile.BottomLayerCount + SlicerFile.TransitionLayerCount
                : SlicerFile.BottomLayerCount;

            if (SlicerFile.ContainsLayer(layerIndex))
            {
                exposureTime = SlicerFile[layerIndex].ExposureTime;
            }

            return $"{Title}: ({bottomExposureTime}s » {(actualTransitionDecrementTime <= 0 || actualTransitionLayerCount == 0 ? string.Empty : $"[-{actualTransitionDecrementTime}s/{actualTransitionLayerCount} layers] » ")}{exposureTime}s) » ({bottomExposureTime}s » {(suggestedTransitionDecrementTime <= 0 || suggestedTransitionLayerCount == 0 ? string.Empty : $"[-{suggestedTransitionDecrementTime}s/{suggestedTransitionLayerCount} layers] » ")}{exposureTime}s)";
        }
    }

    public ushort TransitionLayerCount =>
        (ushort)Math.Min(
            Math.Clamp(
                SlicerFile.GetTransitionLayerCountFromLayers((float)TransitionStepTime, false),
                MinimumTransitionLayerCount,
                MaximumTransitionLayerCount)
            , SlicerFile.MaximumPossibleTransitionLayerCount);

    public decimal TransitionStepTime
    {
        get;
        set => SetProperty(ref field, Math.Max(0, Math.Round(value, 2)));
    } = 2;

    [ObservableProperty]
    public partial ushort MinimumTransitionLayerCount { get; set; } = 3;

    [ObservableProperty]
    public partial ushort MaximumTransitionLayerCount { get; set; } = 10;

    #endregion

    #region Override

    public override string? Validate()
    {
        var sb = new StringBuilder();


        if (TransitionStepTime < 0)
        {
            sb.AppendLine("Decrement time (s) must be zero or a positive value");
        }

        /*if (_minimumTransitionTimeDecrement < 0)
        {
            sb.AppendLine("Minimum limit (s) can't be a negative value");
        }

        if (_maximumTransitionTimeDecrement < 0)
        {
            sb.AppendLine("Maximum limit (s) can't be a negative value");
        }

        if (_minimumTransitionTimeDecrement > _maximumTransitionTimeDecrement)
        {
            sb.AppendLine("Minimum limit (s) can't be higher than maximum limit (s)");
        }*/

        /*if (_minimumTransitionLayerCount == 0)
        {
            sb.AppendLine("Minimum limit (layers) must be a positive value");
        }*/

        if (MinimumTransitionLayerCount > MaximumTransitionLayerCount)
        {
            sb.AppendLine("Minimum limit (layers) can't be higher than maximum limit (layers)");
        }

        return sb.ToString();
    }

    #endregion

    #region Constructor
    public SuggestionTransitionLayerCount()
    {
        ApplyWhen = SuggestionApplyWhen.Different;
    }
    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        SlicerFile.TransitionLayerCount = TransitionLayerCount;
        return true;
    }

    #endregion
}
