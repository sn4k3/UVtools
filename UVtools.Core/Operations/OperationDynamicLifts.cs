/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Text;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using ZLinq;

namespace UVtools.Core.Operations;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public sealed partial class OperationDynamicLifts : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Enums

    public enum DynamicLiftsSetMethod : byte
    {
        // Reduces maximal lift height with the number of pixels in layer divided by maximal number of pixels in any layer. Increases the minimal speed with the same ratio.
        [Description(
            "Traditional: Reduces maximal lift height with the surface area divided by the maximal of all layers")]
        Traditional,

        //Squeezes lift height and lift speed within full range of min/max values. E.g. the layer with the least pixels gets minimal lift height and maximal lift speed. The layer with the most pixels gets maximal lift height and minimal lift speed.
        [Description("Full Range: Squeezes lift height and lift speed within full range of smallest/largest values")]
        FullRange
    }

    #endregion

    #region Members

    private float _smallestBottomLiftHeight;
    private float _largestBottomLiftHeight;
    private float _smallestLiftHeight;
    private float _largestLiftHeight;
    private float _slowestBottomLiftSpeed;
    private float _fastestBottomLiftSpeed;
    private float _slowestLiftSpeed;
    private float _fastestLiftSpeed;
    private float _bottomRetractSpeed;
    private float _bottomRetractHeight2Percentage = -1;
    private float _bottomRetractSpeed2;
    private float _retractSpeed;
    private float _retractHeight2Percentage = -1;
    private float _retractSpeed2;

    #endregion

    #region Overrides

    public override bool CanRunInPartialMode => true;

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.Normal;
    public override string IconClass => "ChartBellCurve";
    public override string Title => "Dynamic lifts";

    public override string Description =>
        "Generate dynamic lift height and speeds for each layer given it surface area.\n" +
        "Larger surface areas requires more lift height and less speed while smaller surface areas can go with shorter lift height and more speed.\n" +
        "Retraction can use a fast first movement followed by a second movement near the vat, specified as a percentage of the generated lift height.\n" +
        "If you have a raft, start after it layer number to not influence the calculations.\n" +
        "Note: Only few printers support this. Running this on an unsupported printer will cause no harm.";

    public override string ConfirmationText =>
        $"generate dynamic lifts from layers {LayerIndexStart} through {LayerIndexEnd}?";

    public override string ProgressTitle =>
        $"Generating dynamic lifts from layers {LayerIndexStart} through {LayerIndexEnd}";

    public override string ProgressAction => "Generated lifts";

    public override string? ValidateSpawn()
    {
        if (!SlicerFile.CanUseLayerLiftHeight || !SlicerFile.CanUseLayerLiftSpeed)
        {
            return NotSupportedMessage;
        }

        return null;
    }

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        if (_smallestBottomLiftHeight > _largestBottomLiftHeight)
        {
            sb.AppendLine("Smallest bottom lift height can't be higher than the largest.");
        }

        if (_slowestBottomLiftSpeed > _fastestBottomLiftSpeed)
        {
            sb.AppendLine("Slowest bottom lift speed can't be higher than the fastest.");
        }

        if (_smallestLiftHeight > _largestLiftHeight)
        {
            sb.AppendLine("Smallest lift height can't be higher than the largest.");
        }

        if (_slowestLiftSpeed > _fastestLiftSpeed)
        {
            sb.AppendLine("Slowest lift speed can't be higher than the fastest.");
        }

        if (_smallestBottomLiftHeight == _largestBottomLiftHeight &&
            _slowestBottomLiftSpeed == _fastestBottomLiftSpeed &&
            _smallestLiftHeight == _largestLiftHeight &&
            _slowestLiftSpeed == _fastestLiftSpeed)
        {
            sb.AppendLine("The selected smallest/largest settings are all equal and will not produce a change.");
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var result =
            $"[Method: {SetMethod}]" +
            $" [Bottom height: {_smallestBottomLiftHeight}/{_largestBottomLiftHeight}mm]" +
            $" [Bottom speed: {_slowestBottomLiftSpeed}/{_fastestBottomLiftSpeed}mm/min]" +
            $" [Bottom retract: {_bottomRetractSpeed}mm/min + {_bottomRetractHeight2Percentage}% at {_bottomRetractSpeed2}mm/min]" +
            $" [Height: {_smallestLiftHeight}/{_largestLiftHeight}mm]" +
            $" [Speed: {_slowestLiftSpeed}/{_fastestLiftSpeed}mm/min]" +
            $" [Retract: {_retractSpeed}mm/min + {_retractHeight2Percentage}% at {_retractSpeed2}mm/min]" +
            LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    #endregion

    #region Properties

    [ObservableProperty]
    public partial DynamicLiftsSetMethod SetMethod { get; set; } = DynamicLiftsSetMethod.Traditional;

    public float SmallestBottomLiftHeight
    {
        get => _smallestBottomLiftHeight;
        set => SetProperty(ref _smallestBottomLiftHeight, MathF.Round(value, 2));
    }

    public float LargestBottomLiftHeight
    {
        get => _largestBottomLiftHeight;
        set => SetProperty(ref _largestBottomLiftHeight, MathF.Round(value, 2));
    }

    public float SmallestLiftHeight
    {
        get => _smallestLiftHeight;
        set => SetProperty(ref _smallestLiftHeight, MathF.Round(value, 2));
    }

    public float LargestLiftHeight
    {
        get => _largestLiftHeight;
        set => SetProperty(ref _largestLiftHeight, MathF.Round(value, 2));
    }

    public float SlowestBottomLiftSpeed
    {
        get => _slowestBottomLiftSpeed;
        set => SetProperty(ref _slowestBottomLiftSpeed, MathF.Round(value, 2));
    }

    public float FastestBottomLiftSpeed
    {
        get => _fastestBottomLiftSpeed;
        set => SetProperty(ref _fastestBottomLiftSpeed, MathF.Round(value, 2));
    }

    public float SlowestLiftSpeed
    {
        get => _slowestLiftSpeed;
        set => SetProperty(ref _slowestLiftSpeed, MathF.Round(value, 2));
    }

    public float FastestLiftSpeed
    {
        get => _fastestLiftSpeed;
        set => SetProperty(ref _fastestLiftSpeed, MathF.Round(value, 2));
    }

    public float BottomRetractSpeed
    {
        get => _bottomRetractSpeed;
        set => SetProperty(ref _bottomRetractSpeed, MathF.Round(Math.Max(0, value), 2));
    }

    public float BottomRetractHeight2Percentage
    {
        get => _bottomRetractHeight2Percentage;
        set => SetProperty(ref _bottomRetractHeight2Percentage, MathF.Round(Math.Clamp(value, 0, 100), 2));
    }

    public float BottomRetractSpeed2
    {
        get => _bottomRetractSpeed2;
        set => SetProperty(ref _bottomRetractSpeed2, MathF.Round(Math.Max(0, value), 2));
    }

    public float RetractSpeed
    {
        get => _retractSpeed;
        set => SetProperty(ref _retractSpeed, MathF.Round(Math.Max(0, value), 2));
    }

    public float RetractHeight2Percentage
    {
        get => _retractHeight2Percentage;
        set => SetProperty(ref _retractHeight2Percentage, MathF.Round(Math.Clamp(value, 0, 100), 2));
    }

    public float RetractSpeed2
    {
        get => _retractSpeed2;
        set => SetProperty(ref _retractSpeed2, MathF.Round(Math.Max(0, value), 2));
    }

    public uint MinBottomLayerPixels => GetPixelStats(true).Min;
    public uint MinNormalLayerPixels => GetPixelStats(false).Min;
    public uint MaxBottomLayerPixels => GetPixelStats(true).Max;
    public uint MaxNormalLayerPixels => GetPixelStats(false).Max;

    #endregion

    #region Constructor

    public OperationDynamicLifts()
    {
    }

    public OperationDynamicLifts(FileFormat slicerFile) : base(slicerFile)
    {
    }

    public override void InitWithSlicerFile()
    {
        base.InitWithSlicerFile();

        if (_smallestBottomLiftHeight <= 0) _smallestBottomLiftHeight = SlicerFile.BottomLiftHeightTotal;
        if (_largestBottomLiftHeight <= 0 || _largestBottomLiftHeight < _smallestBottomLiftHeight)
            _largestBottomLiftHeight = _smallestBottomLiftHeight;

        if (_smallestLiftHeight <= 0) _smallestLiftHeight = SlicerFile.LiftHeightTotal;
        if (_largestLiftHeight <= 0 || _largestLiftHeight < _smallestLiftHeight)
            _largestLiftHeight = _smallestLiftHeight;

        if (_slowestBottomLiftSpeed <= 0) _slowestBottomLiftSpeed = SlicerFile.BottomLiftSpeed;
        if (_fastestBottomLiftSpeed <= 0 || _fastestBottomLiftSpeed < _slowestBottomLiftSpeed)
            _fastestBottomLiftSpeed = _slowestBottomLiftSpeed;

        if (_slowestLiftSpeed <= 0) _slowestLiftSpeed = SlicerFile.LiftSpeed;
        if (_fastestLiftSpeed <= 0 || _fastestLiftSpeed < _slowestLiftSpeed) _fastestLiftSpeed = _slowestLiftSpeed;

        if (_bottomRetractSpeed <= 0) _bottomRetractSpeed = SlicerFile.BottomRetractSpeed;
        if (_bottomRetractHeight2Percentage < 0)
        {
            _bottomRetractHeight2Percentage =
                GetPercentage(SlicerFile.BottomRetractHeight2, SlicerFile.BottomRetractHeightTotal);
        }

        if (_bottomRetractSpeed2 <= 0) _bottomRetractSpeed2 = SlicerFile.BottomRetractSpeed2;

        if (_retractSpeed <= 0) _retractSpeed = SlicerFile.RetractSpeed;
        if (_retractHeight2Percentage < 0)
        {
            _retractHeight2Percentage = GetPercentage(SlicerFile.RetractHeight2, SlicerFile.RetractHeightTotal);
        }

        if (_retractSpeed2 <= 0) _retractSpeed2 = SlicerFile.RetractSpeed2;
    }

    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        var bottomStats = GetPixelStats(true);
        var normalStats = GetPixelStats(false);

        for (uint layerIndex = LayerIndexStart; layerIndex <= LayerIndexEnd; layerIndex++)
        {
            progress.PauseOrCancelIfRequested();
            var calculateLayer = SlicerFile[layerIndex == 0 ? 0 : layerIndex - 1];
            var setLayer = SlicerFile[layerIndex];
            var stats = setLayer.IsBottomLayer ? bottomStats : normalStats;
            var pixelRatio = SetMethod == DynamicLiftsSetMethod.FullRange
                ? stats.Max > stats.Min
                    ? Math.Clamp((calculateLayer.NonZeroPixelCount - (double)stats.Min) / (stats.Max - stats.Min), 0, 1)
                    : stats.Max > 0
                        ? 1
                        : 0
                : stats.Max > 0
                    ? Math.Clamp(calculateLayer.NonZeroPixelCount / (double)stats.Max, 0, 1)
                    : 0;

            float liftHeight;
            float liftSpeed;
            if (setLayer.IsBottomLayer)
            {
                switch (SetMethod)
                {
                    case DynamicLiftsSetMethod.Traditional:
                        liftHeight = Math.Clamp(_largestBottomLiftHeight * (float)pixelRatio, _smallestBottomLiftHeight,
                            _largestBottomLiftHeight);
                        liftSpeed = Math.Clamp(_fastestBottomLiftSpeed * (1 - (float)pixelRatio),
                            _slowestBottomLiftSpeed, _fastestBottomLiftSpeed);
                        break;
                    case DynamicLiftsSetMethod.FullRange:
                        liftHeight =
                            Math.Clamp(
                                _smallestBottomLiftHeight + (_largestBottomLiftHeight - _smallestBottomLiftHeight) *
                                (float)pixelRatio, _smallestBottomLiftHeight, _largestBottomLiftHeight);
                        liftSpeed = Math.Clamp(
                            _fastestBottomLiftSpeed -
                            (_fastestBottomLiftSpeed - _slowestBottomLiftSpeed) * (float)pixelRatio,
                            _slowestBottomLiftSpeed, _fastestBottomLiftSpeed);
                        break;
                    default:
                        throw new NotImplementedException(nameof(SetMethod));
                }
            }
            else
            {
                switch (SetMethod)
                {
                    case DynamicLiftsSetMethod.Traditional:
                        liftHeight = Math.Clamp(_largestLiftHeight * (float)pixelRatio, _smallestLiftHeight,
                            _largestLiftHeight);
                        liftSpeed = Math.Clamp(_fastestLiftSpeed * (1 - (float)pixelRatio), _slowestLiftSpeed,
                            _fastestLiftSpeed);
                        break;
                    case DynamicLiftsSetMethod.FullRange:
                        liftHeight =
                            Math.Clamp(
                                _smallestLiftHeight + (_largestLiftHeight - _smallestLiftHeight) * (float)pixelRatio,
                                _smallestLiftHeight, _largestLiftHeight);
                        liftSpeed = Math.Clamp(
                            _fastestLiftSpeed - (_fastestLiftSpeed - _slowestLiftSpeed) * (float)pixelRatio,
                            _slowestLiftSpeed, _fastestLiftSpeed);
                        break;
                    default:
                        throw new NotImplementedException(nameof(SetMethod));
                }
            }

            setLayer.RetractHeight2 = 0;
            setLayer.LiftHeightTotal = MathF.Round(liftHeight, 1);
            setLayer.LiftSpeed = MathF.Round(liftSpeed, 1);

            var retractSpeed = setLayer.IsBottomLayer ? _bottomRetractSpeed : _retractSpeed;
            if (SlicerFile.CanUseLayerRetractSpeed)
            {
                setLayer.RetractSpeed = retractSpeed;
            }

            if (SlicerFile.CanUseLayerRetractHeight2 && SlicerFile.CanUseLayerRetractSpeed2)
            {
                var retractHeight2Percentage = setLayer.IsBottomLayer
                    ? _bottomRetractHeight2Percentage
                    : _retractHeight2Percentage;
                setLayer.RetractHeight2 = MathF.Round(setLayer.LiftHeightTotal * retractHeight2Percentage / 100, 2);
                setLayer.RetractSpeed2 = setLayer.IsBottomLayer ? _bottomRetractSpeed2 : _retractSpeed2;
            }

            progress++;
        }

        return !progress.Token.IsCancellationRequested;
    }

    private static float GetPercentage(float value, float total)
    {
        return total > 0 ? MathF.Round(Math.Clamp(value / total * 100, 0, 100), 2) : 0;
    }

    private (uint Min, uint Max) GetPixelStats(bool isBottom)
    {
        var min = uint.MaxValue;
        uint max = 0;
        var found = false;
        for (var layerIndex = LayerIndexStart; layerIndex <= LayerIndexEnd; layerIndex++)
        {
            var layer = SlicerFile[layerIndex];
            if (layer.IsEmpty || layer.IsBottomLayer != isBottom) continue;
            min = Math.Min(min, layer.NonZeroPixelCount);
            max = Math.Max(max, layer.NonZeroPixelCount);
            found = true;
        }

        return found ? (min, max) : (0, 0);
    }

    public Layer? GetSmallestLayer(bool isBottom)
    {
        return SlicerFile.AsValueEnumerable()
            .Where((layer, index) => !layer.IsEmpty && layer.IsBottomLayer == isBottom && index >= LayerIndexStart &&
                                     index <= LayerIndexEnd).MinBy(layer => layer.NonZeroPixelCount);
    }

    public Layer? GetLargestLayer(bool isBottom)
    {
        return SlicerFile.AsValueEnumerable()
            .Where((layer, index) => !layer.IsEmpty && layer.IsBottomLayer == isBottom && index >= LayerIndexStart &&
                                     index <= LayerIndexEnd).MaxBy(layer => layer.NonZeroPixelCount);
    }

    #endregion

    #region Equality

    private bool Equals(OperationDynamicLifts other)
    {
        return SetMethod == other.SetMethod && _smallestBottomLiftHeight.Equals(other._smallestBottomLiftHeight) &&
               _largestBottomLiftHeight.Equals(other._largestBottomLiftHeight) &&
               _smallestLiftHeight.Equals(other._smallestLiftHeight) &&
               _largestLiftHeight.Equals(other._largestLiftHeight) &&
               _slowestBottomLiftSpeed.Equals(other._slowestBottomLiftSpeed) &&
               _fastestBottomLiftSpeed.Equals(other._fastestBottomLiftSpeed) &&
               _slowestLiftSpeed.Equals(other._slowestLiftSpeed) && _fastestLiftSpeed.Equals(other._fastestLiftSpeed) &&
               _bottomRetractSpeed.Equals(other._bottomRetractSpeed) &&
               _bottomRetractHeight2Percentage.Equals(other._bottomRetractHeight2Percentage) &&
               _bottomRetractSpeed2.Equals(other._bottomRetractSpeed2) && _retractSpeed.Equals(other._retractSpeed) &&
               _retractHeight2Percentage.Equals(other._retractHeight2Percentage) &&
               _retractSpeed2.Equals(other._retractSpeed2);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationDynamicLifts other && Equals(other);
    }

    #endregion
}