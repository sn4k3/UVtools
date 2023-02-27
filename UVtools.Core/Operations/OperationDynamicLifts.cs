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
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;

namespace UVtools.Core.Operations;


public sealed class OperationDynamicLifts : Operation
{
    #region Enums
    public enum DynamicLiftsSetMethod : byte
    {
        // Reduces maximal lift height with the number of pixels in layer divided by maximal number of pixels in any layer. Increases the minimal speed with the same ratio.
        [Description("Traditional: Reduces maximal lift height with the surface area divided by the maximal of all layers")]
        Traditional,
        //Squeezes lift height and lift speed within full range of min/max values. E.g. the layer with the least pixels gets minimal lift height and maximal lift speed. The layer with the most pixels gets maximal lift height and minimal lift speed.
        [Description("Full Range: Squeezes lift height and lift speed within full range of smallest/largest values")]
        FullRange
    }
    #endregion

    #region Members

    private DynamicLiftsSetMethod _setMethod = DynamicLiftsSetMethod.Traditional;
    private float _smallestBottomLiftHeight;
    private float _largestBottomLiftHeight;
    private float _smallestLiftHeight;
    private float _largestLiftHeight;
    private float _slowestBottomLiftSpeed;
    private float _fastestBottomLiftSpeed;
    private float _slowestLiftSpeed;
    private float _fastestLiftSpeed;

    #endregion

    #region Overrides

    public override bool CanRunInPartialMode => true;

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.Normal;
    public override string IconClass => "fa-solid fa-chart-line";
    public override string Title => "Dynamic lifts";

    public override string Description =>
        "Generate dynamic lift height and speeds for each layer given it surface area.\n" +
        "Larger surface areas requires more lift height and less speed while smaller surface areas can go with shorter lift height and more speed.\n" +
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
            $"[Method: {_setMethod}]" +
            $" [Bottom height: {_smallestBottomLiftHeight}/{_largestBottomLiftHeight}mm]" +
            $" [Bottom speed: {_slowestBottomLiftSpeed}/{_fastestBottomLiftSpeed}mm/min]" +
            $" [Height: {_smallestLiftHeight}/{_largestLiftHeight}mm]" +
            $" [Speed: {_slowestLiftSpeed}/{_fastestLiftSpeed}mm/min]" +
            LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    #endregion

    #region Properties

    public DynamicLiftsSetMethod SetMethod
    {
        get => _setMethod;
        set => RaiseAndSetIfChanged(ref _setMethod, value);
    }

    public float SmallestBottomLiftHeight
    {
        get => _smallestBottomLiftHeight;
        set => RaiseAndSetIfChanged(ref _smallestBottomLiftHeight, (float)Math.Round(value, 2));
    }

    public float LargestBottomLiftHeight
    {
        get => _largestBottomLiftHeight;
        set => RaiseAndSetIfChanged(ref _largestBottomLiftHeight, (float)Math.Round(value, 2));
    }

    public float SmallestLiftHeight
    {
        get => _smallestLiftHeight;
        set => RaiseAndSetIfChanged(ref _smallestLiftHeight, (float)Math.Round(value, 2));
    }

    public float LargestLiftHeight
    {
        get => _largestLiftHeight;
        set => RaiseAndSetIfChanged(ref _largestLiftHeight, (float)Math.Round(value, 2));
    }

    public float SlowestBottomLiftSpeed
    {
        get => _slowestBottomLiftSpeed;
        set => RaiseAndSetIfChanged(ref _slowestBottomLiftSpeed, (float)Math.Round(value, 2));
    }

    public float FastestBottomLiftSpeed
    {
        get => _fastestBottomLiftSpeed;
        set => RaiseAndSetIfChanged(ref _fastestBottomLiftSpeed, (float)Math.Round(value, 2));
    }

    public float SlowestLiftSpeed
    {
        get => _slowestLiftSpeed;
        set => RaiseAndSetIfChanged(ref _slowestLiftSpeed, (float)Math.Round(value, 2));
    }

    public float FastestLiftSpeed
    {
        get => _fastestLiftSpeed;
        set => RaiseAndSetIfChanged(ref _fastestLiftSpeed, (float)Math.Round(value, 2));
    }

    //public uint MinBottomLayerPixels => SlicerFile.Where(layer => layer.IsBottomLayer && !layer.IsEmpty && layer.Index >= LayerIndexStart && layer.Index <= LayerIndexEnd).Max(layer => layer.NonZeroPixelCount);
    public uint MinBottomLayerPixels => (from layer in SlicerFile
        where layer.IsBottomLayer
        where !layer.IsEmpty
        where layer.Index >= LayerIndexStart
        where layer.Index <= LayerIndexEnd
        select layer.NonZeroPixelCount).Min();

    //public uint MinNormalLayerPixels => SlicerFile.Where(layer => layer.IsNormalLayer && !layer.IsEmpty && layer.Index >= LayerIndexStart && layer.Index <= LayerIndexEnd).Max(layer => layer.NonZeroPixelCount);
    public uint MinNormalLayerPixels => (from layer in SlicerFile
        where layer.IsNormalLayer
        where !layer.IsEmpty
        where layer.Index >= LayerIndexStart
        where layer.Index <= LayerIndexEnd
        select layer.NonZeroPixelCount).Min();

    //public uint MaxBottomLayerPixels => SlicerFile.Where(layer => layer.IsBottomLayer && layer.Index >= LayerIndexStart && layer.Index <= LayerIndexEnd).Max(layer => layer.NonZeroPixelCount);
    public uint MaxBottomLayerPixels => (from layer in SlicerFile
        where layer.IsBottomLayer
        where !layer.IsEmpty
        where layer.Index >= LayerIndexStart
        where layer.Index <= LayerIndexEnd
        select layer.NonZeroPixelCount).Max();
    //public uint MaxNormalLayerPixels => SlicerFile.Where(layer => layer.IsNormalLayer && layer.Index >= LayerIndexStart && layer.Index <= LayerIndexEnd).Max(layer => layer.NonZeroPixelCount);
    public uint MaxNormalLayerPixels => (from layer in SlicerFile
        where layer.IsNormalLayer
        where !layer.IsEmpty
        where layer.Index >= LayerIndexStart
        where layer.Index <= LayerIndexEnd
        select layer.NonZeroPixelCount).Max();

    #endregion

    #region Constructor

    public OperationDynamicLifts()
    { }

    public OperationDynamicLifts(FileFormat slicerFile) : base(slicerFile)
    { }

    public override void InitWithSlicerFile()
    {
        base.InitWithSlicerFile();

        if(_smallestBottomLiftHeight <= 0) _smallestBottomLiftHeight = SlicerFile.BottomLiftHeightTotal;
        if (_largestBottomLiftHeight <= 0 || _largestBottomLiftHeight < _smallestBottomLiftHeight) _largestBottomLiftHeight = _smallestBottomLiftHeight;

        if (_smallestLiftHeight <= 0) _smallestLiftHeight = SlicerFile.LiftHeightTotal;
        if (_largestLiftHeight <= 0 || _largestLiftHeight < _smallestLiftHeight) _largestLiftHeight = _smallestLiftHeight;

        if (_slowestBottomLiftSpeed <= 0) _slowestBottomLiftSpeed = SlicerFile.BottomLiftSpeed;
        if (_fastestBottomLiftSpeed <= 0 || _fastestBottomLiftSpeed < _slowestBottomLiftSpeed) _fastestBottomLiftSpeed = _slowestBottomLiftSpeed;

        if (_slowestLiftSpeed <= 0) _slowestLiftSpeed = SlicerFile.LiftSpeed;
        if (_fastestLiftSpeed <= 0 || _fastestLiftSpeed < _slowestLiftSpeed) _fastestLiftSpeed = _slowestLiftSpeed;
    }

    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        uint minBottomPixels = 0;
        uint minNormalPixels = 0;
        uint maxBottomPixels = 0;
        uint maxNormalPixels = 0;

        try
        {
            minBottomPixels = MinBottomLayerPixels;
        }
        catch
        {
        }

        try
        {
            minNormalPixels = MinNormalLayerPixels;
        }
        catch
        {
        }

        try
        {
            maxBottomPixels = MaxBottomLayerPixels;
        }
        catch
        {
        }

        try
        {
            maxNormalPixels = MaxNormalLayerPixels;
        }
        catch
        {
        }

        float liftHeight = 0;
        float liftSpeed = 0;

        //uint max = (from layer in SlicerFile where !layer.IsBottomLayer where !layer.IsEmpty where layer.Index >= LayerIndexStart where layer.Index <= LayerIndexEnd select layer).Aggregate<Layer, uint>(0, (current, layer) => Math.Max(layer.NonZeroPixelCount, current));

        for (uint layerIndex = LayerIndexStart; layerIndex <= LayerIndexEnd; layerIndex++)
        {
            progress.PauseOrCancelIfRequested();
            var calculateLayer = SlicerFile[layerIndex == 0 ? 0 : layerIndex - 1];
            var setLayer = SlicerFile[layerIndex];


            // Height
            // min - largestpixelcount
            //  x  - pixelcount

            // Speed
            // max - minpixelCount
            //  x  - pixelcount
            if (setLayer.IsBottomLayer)
            {
                switch (_setMethod)
                {
                    case DynamicLiftsSetMethod.Traditional:
                        liftHeight = Math.Clamp(_largestBottomLiftHeight * calculateLayer.NonZeroPixelCount / maxBottomPixels, _smallestBottomLiftHeight, _largestBottomLiftHeight);
                        liftSpeed = Math.Clamp(_fastestBottomLiftSpeed - (_fastestBottomLiftSpeed * calculateLayer.NonZeroPixelCount / maxBottomPixels), _slowestBottomLiftSpeed, _fastestBottomLiftSpeed);
                        break;
                    case DynamicLiftsSetMethod.FullRange:
                        var pixelRatio = (calculateLayer.NonZeroPixelCount - minBottomPixels) / (float)(maxBottomPixels - minBottomPixels); // pixel_ratio is between 0 and 1
                        liftHeight = Math.Clamp(_smallestBottomLiftHeight + (_largestBottomLiftHeight - _smallestBottomLiftHeight) * pixelRatio, _smallestBottomLiftHeight, _largestBottomLiftHeight);
                        liftSpeed = Math.Clamp(_fastestBottomLiftSpeed - (_fastestBottomLiftSpeed - _slowestBottomLiftSpeed) * pixelRatio, _slowestBottomLiftSpeed, _fastestBottomLiftSpeed);
                        break;
                    default:
                        throw new NotImplementedException(nameof(SetMethod));
                }
                    
            }
            else
            {
                switch (_setMethod)
                {
                    case DynamicLiftsSetMethod.Traditional:
                        liftHeight = Math.Clamp(_largestLiftHeight * calculateLayer.NonZeroPixelCount / maxNormalPixels, _smallestLiftHeight, _largestLiftHeight);
                        liftSpeed = Math.Clamp(_fastestLiftSpeed - _fastestLiftSpeed * calculateLayer.NonZeroPixelCount / maxNormalPixels, _slowestLiftSpeed, _fastestLiftSpeed);
                        break;
                    case DynamicLiftsSetMethod.FullRange:
                        var pixelRatio = (calculateLayer.NonZeroPixelCount - minNormalPixels) / (float)(maxNormalPixels - minNormalPixels); // pixel_ratio is between 0 and 1
                        liftHeight = Math.Clamp(_smallestLiftHeight + (_largestLiftHeight - _smallestLiftHeight) * pixelRatio, _smallestLiftHeight, _largestLiftHeight);
                        liftSpeed =  Math.Clamp(_fastestLiftSpeed - (_fastestLiftSpeed - _slowestLiftSpeed) * pixelRatio, _slowestLiftSpeed, _fastestLiftSpeed);
                        break;
                    default:
                        throw new NotImplementedException(nameof(SetMethod));
                }
            }

            if (!float.IsNaN(liftHeight))
            {
                setLayer.RetractHeight2 = 0;
                setLayer.LiftHeightTotal = (float) Math.Round(liftHeight, 1);
                if (!float.IsNaN(liftSpeed)) setLayer.LiftSpeed = (float)Math.Round(liftSpeed, 1);
            }
            

            progress++;
        }

        return !progress.Token.IsCancellationRequested;
    }

    public Layer? GetSmallestLayer(bool isBottom)
    {
        return SlicerFile.Where((layer, index) => !layer.IsEmpty && layer.IsBottomLayer == isBottom && index >= LayerIndexStart && index <= LayerIndexEnd).MinBy(layer => layer.NonZeroPixelCount);
    }

    public Layer? GetLargestLayer(bool isBottom)
    {
        return SlicerFile.Where((layer, index) => !layer.IsEmpty && layer.IsBottomLayer == isBottom && index >= LayerIndexStart && index <= LayerIndexEnd).MaxBy(layer => layer.NonZeroPixelCount);
    }

    #endregion

    #region Equality


    private bool Equals(OperationDynamicLifts other)
    {
        return _setMethod == other._setMethod && _smallestBottomLiftHeight.Equals(other._smallestBottomLiftHeight) && _largestBottomLiftHeight.Equals(other._largestBottomLiftHeight) && _smallestLiftHeight.Equals(other._smallestLiftHeight) && _largestLiftHeight.Equals(other._largestLiftHeight) && _slowestBottomLiftSpeed.Equals(other._slowestBottomLiftSpeed) && _fastestBottomLiftSpeed.Equals(other._fastestBottomLiftSpeed) && _slowestLiftSpeed.Equals(other._slowestLiftSpeed) && _fastestLiftSpeed.Equals(other._fastestLiftSpeed);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationDynamicLifts other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add((int)_setMethod);
        hashCode.Add(_smallestBottomLiftHeight);
        hashCode.Add(_largestBottomLiftHeight);
        hashCode.Add(_smallestLiftHeight);
        hashCode.Add(_largestLiftHeight);
        hashCode.Add(_slowestBottomLiftSpeed);
        hashCode.Add(_fastestBottomLiftSpeed);
        hashCode.Add(_slowestLiftSpeed);
        hashCode.Add(_fastestLiftSpeed);
        return hashCode.ToHashCode();
    }

    #endregion
}