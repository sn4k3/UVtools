/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Text;
using UVtools.Core.Operations;

namespace UVtools.Core.Suggestions;

public sealed class SuggestionTransitionLayerCount : Suggestion
{
    #region Members

    private decimal _transitionStepTime = 2;
    private ushort _minimumTransitionLayerCount = 3;
    private ushort _maximumTransitionLayerCount = 10;

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
            if (SlicerFile.BottomLayerCount == 0
                || SlicerFile.BottomExposureTime <= 0
                || SlicerFile.ExposureTime <= 0
                || SlicerFile.BottomExposureTime < SlicerFile.ExposureTime
                || SlicerFile.BottomLayerCount + _minimumTransitionLayerCount > SlicerFile.LayerCount 
                || SlicerFile.MaximumPossibleTransitionLayerCount < _minimumTransitionLayerCount) return true;

            var actualTransitionLayerCount = SlicerFile.ParseTransitionLayerCountFromLayers();
            var suggestedTransitionLayerCount = TransitionLayerCount;

            if (actualTransitionLayerCount == suggestedTransitionLayerCount) return true;

            return _applyWhen switch
            {
                SuggestionApplyWhen.OutsideLimits => actualTransitionLayerCount >= _minimumTransitionLayerCount &&
                                                     actualTransitionLayerCount <= _maximumTransitionLayerCount,
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
            var actualTransitionDecrementTime = SlicerFile.ParseTransitionStepTimeFromLayers();
            var actualTransitionLayerCount = SlicerFile.ParseTransitionLayerCountFromLayers();

            var suggestedTransitionLayerCount = TransitionLayerCount;
            var suggestedTransitionDecrementTime = SlicerFile.GetTransitionStepTime(suggestedTransitionLayerCount);

            return IsApplied
                ? $"{GlobalAppliedMessage}: {SlicerFile.BottomExposureTime}s » {(actualTransitionDecrementTime <= 0 || actualTransitionLayerCount == 0 ? string.Empty : $"[-{actualTransitionDecrementTime}s/{actualTransitionLayerCount} layers] » ")}{SlicerFile.ExposureTime}s"
                : $"{GlobalNotAppliedMessage} {SlicerFile.BottomExposureTime}s » {(actualTransitionDecrementTime <= 0 || actualTransitionLayerCount == 0 ? string.Empty : $"[-{actualTransitionDecrementTime}s/{actualTransitionLayerCount} layers] » ")}{SlicerFile.ExposureTime}s is out of the recommended {SlicerFile.BottomExposureTime}s » {(suggestedTransitionDecrementTime <= 0 || suggestedTransitionLayerCount == 0 ? string.Empty : $"[-{suggestedTransitionDecrementTime}s/{suggestedTransitionLayerCount} layers] » ")}{SlicerFile.ExposureTime}s";
        }
    }

    public override string ToolTip => $"The recommended transition time is ±{_transitionStepTime}s constrained over [{_minimumTransitionLayerCount} to {_maximumTransitionLayerCount}] layers.\n" +
                                      $"Explanation: {Description}";

    public override string? InformationUrl => "https://ameralabs.com/blog/9-settings-to-change-for-faster-resin-3d-printing";

    public override string? ConfirmationMessage
    {
        get
        {
            var actualTransitionDecrementTime = SlicerFile.ParseTransitionStepTimeFromLayers();
            var actualTransitionLayerCount = SlicerFile.ParseTransitionLayerCountFromLayers();

            var suggestedTransitionLayerCount = TransitionLayerCount;
            var suggestedTransitionDecrementTime = SlicerFile.GetTransitionStepTime(suggestedTransitionLayerCount);

            return
                $"{Title}: ({SlicerFile.BottomExposureTime}s » {(actualTransitionDecrementTime <= 0 || actualTransitionLayerCount == 0 ? string.Empty : $"[-{actualTransitionDecrementTime}s/{actualTransitionLayerCount} layers] » ")}{SlicerFile.ExposureTime}s) » ({SlicerFile.BottomExposureTime}s » {(suggestedTransitionDecrementTime <= 0 || suggestedTransitionLayerCount == 0 ? string.Empty : $"[-{suggestedTransitionDecrementTime}s/{suggestedTransitionLayerCount} layers] » ")}{SlicerFile.ExposureTime}s)";
        }
    }

    public ushort TransitionLayerCount =>
        (ushort)Math.Min(
            Math.Clamp(
                SlicerFile.GetTransitionLayerCount((float)_transitionStepTime, false),
                _minimumTransitionLayerCount,
                _maximumTransitionLayerCount)
            , SlicerFile.MaximumPossibleTransitionLayerCount);

    public decimal TransitionStepTime
    {
        get => _transitionStepTime;
        set => RaiseAndSetIfChanged(ref _transitionStepTime, Math.Max(0, Math.Round(value, 2)));
    }

    public ushort MinimumTransitionLayerCount
    {
        get => _minimumTransitionLayerCount;
        set => RaiseAndSetIfChanged(ref _minimumTransitionLayerCount, value);
    }

    public ushort MaximumTransitionLayerCount
    {
        get => _maximumTransitionLayerCount;
        set => RaiseAndSetIfChanged(ref _maximumTransitionLayerCount, value);
    }

    #endregion

    #region Override

    public override string? Validate()
    {
        var sb = new StringBuilder();


        if (_transitionStepTime < 0)
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

        if (_minimumTransitionLayerCount > _maximumTransitionLayerCount)
        {
            sb.AppendLine("Minimum limit (layers) can't be higher than maximum limit (layers)");
        }

        return sb.ToString();
    }

    #endregion

    #region Constructor
    public SuggestionTransitionLayerCount()
    {
        _applyWhen = SuggestionApplyWhen.Different;
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