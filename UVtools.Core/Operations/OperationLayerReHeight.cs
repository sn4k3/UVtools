/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;

namespace UVtools.Core.Operations;

[Serializable]
public sealed class OperationLayerReHeight : Operation
{
    #region Enums

    public enum OperationLayerReHeightMethod : byte
    {
        [Description("Re-height: Change layer height / thickness")]
        ReHeight,
        [Description("Offset - Change layers position by an offset")]
        OffsetPositionZ
    }

    public enum OperationLayerReHeightAntiAliasingType : byte
    {
        None,
        [Description("Difference - Compute anti-aliasing by the layers difference and perform a down/up sample over pixels")]
        Difference,
        [Description("Average - Compute anti-aliasing by averaging the layers pixels")]
        Average
    }
    #endregion

    #region Members

    private OperationLayerReHeightMethod _method;
    private decimal _positionZOffset = 0.01m;
    private OperationLayerReHeightItem? _selectedItem;
    private OperationLayerReHeightAntiAliasingType _antiAliasingType;
    private decimal _bottomExposure;
    private decimal _normalExposure;

    #endregion

    #region Overrides

    public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.None;
    public override bool CanROI => false;
    public override string IconClass => "mdi-arrow-split-horizontal";
    public override string Title => "Adjust layer height";
    public override string Description =>
        "Adjust the layer height of the model or offset layers position by a set value.\n\n" +
        "Adjusting to values lower than current height will reduce layer lines, adjusting to values higher" +
        " than current height will reduce model detail.\n" +
        "Different layer thickness will require different exposure times, adjust accordingly.\n\n" +
        "Note: Using dedicated slicer software to re-slice will usually yield better results.";
    public override string ConfirmationText =>
        _method == OperationLayerReHeightMethod.ReHeight 
            ? $"adjust layer height to {_selectedItem!.LayerHeight}mm?"
            : $"adjust layers position by a offset of {_positionZOffset}mm?";

    public override string ProgressTitle =>
        _method == OperationLayerReHeightMethod.ReHeight 
            ? $"Adjusting layer height to {_selectedItem!.LayerHeight}mm"
            : $"Adjusting layers position by a offset of {_positionZOffset}mm";

    public override string ProgressAction => "Height adjusted layers";

    public override bool CanHaveProfiles => false;

    public override string? ValidateSpawn()
    {
        if ((Presets is null || Presets.Length == 0) && !SlicerFile.CanUseLayerPositionZ)
        {
            return "No valid configuration to be able to re-height.\n" +
                   "As workaround clone first or last layer and try re run this tool.";
        }

        return null;
    }

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        switch (_method)
        {
            case OperationLayerReHeightMethod.ReHeight:
                if (Presets.Length == 0)
                {
                    sb.AppendLine("No valid configurations, unable to proceed.");
                }

                if (_selectedItem is null)
                {
                    sb.AppendLine("No new height was selected.");
                }

                break;
            case OperationLayerReHeightMethod.OffsetPositionZ:
                if (!SlicerFile.CanUseLayerPositionZ)
                {
                    sb.AppendLine($"This file format / printer is unable to use the {_method} method.");
                    break;
                }

                if (_positionZOffset == 0)
                {
                    sb.AppendLine("Position offset can't be 0, it have no effect.");
                    break;
                }
                
                for (var layerIndex = LayerIndexStart; layerIndex <= LayerIndexEnd; layerIndex++)
                {
                    if ((decimal) SlicerFile[layerIndex].PositionZ + _positionZOffset < 0)
                    {
                        sb.AppendLine($"Offset position by {_positionZOffset}mm will put layer {layerIndex} under 0mm.");
                        break;
                    }
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }


        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"[{_method}]" +
                     (_method == OperationLayerReHeightMethod.ReHeight 
                         ? $"[Layer Count: {_selectedItem!.LayerCount}] " +
                           $"[Layer Height: {_selectedItem!.LayerHeight}] " +
                           $"[Exposure: {_bottomExposure}/{_normalExposure}s]" :
                         $"[Offset: {_positionZOffset}mm]")
                     + LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }
    #endregion

    #region Properties

    public OperationLayerReHeightMethod Method
    {
        get => _method;
        set
        {
            if(!RaiseAndSetIfChanged(ref _method, value)) return;
            RaisePropertyChanged(nameof(IsReHeightMethod));
            RaisePropertyChanged(nameof(IsOffsetPositionZMethod));
        }
    }

    public bool IsReHeightMethod => _method is OperationLayerReHeightMethod.ReHeight;
    public bool IsOffsetPositionZMethod => _method is OperationLayerReHeightMethod.OffsetPositionZ;

    public decimal PositionZOffset
    {
        get => _positionZOffset;
        set => RaiseAndSetIfChanged(ref _positionZOffset, value);
    }

    public OperationLayerReHeightItem[] Presets { get; set; } = null!;

    public OperationLayerReHeightItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if(!RaiseAndSetIfChanged(ref _selectedItem, value)) return;
            RaisePropertyChanged(nameof(CanAntiAliasing));
        }
    }

    public bool CanAntiAliasing => _selectedItem?.IsMultiply ?? false;

    public OperationLayerReHeightAntiAliasingType AntiAliasingType
    {
        get => _antiAliasingType;
        set => RaiseAndSetIfChanged(ref _antiAliasingType, value);
    }

    public decimal BottomExposure
    {
        get => _bottomExposure;
        set => RaiseAndSetIfChanged(ref _bottomExposure, Math.Round(value, 2));
    }

    public decimal NormalExposure
    {
        get => _normalExposure;
        set => RaiseAndSetIfChanged(ref _normalExposure, Math.Round(value, 2));
    }


    public static OperationLayerReHeightItem[] GetItems(uint layerCount, decimal layerHeight)
    {
        var list = new List<OperationLayerReHeightItem>();
        for (byte i = 2; i < 255; i++) // Go lower heights
        {
            if (layerHeight / i < Layer.MinimumHeight) break;
            if ((layerCount * (decimal)i).DecimalDigits() > 0) continue; // Cant multiply layers, no half layers!
            if ((layerHeight / i).DecimalDigits() > Layer.HeightPrecision) continue; // Cant divide height, more than 3 digits

            var item = new OperationLayerReHeightItem(false, i, Layer.RoundHeight(layerHeight / i), layerCount * i);
            list.Add(item);
        }

        for (byte i = 2; i < 255; i++) // Go higher heights
        {
            if (layerHeight * i > Layer.MaximumHeight) break;
            if ((layerCount / (decimal)i).DecimalDigits() > 0) continue; // Cant divide layers, no half layers!
            if ((layerHeight * i).DecimalDigits() > Layer.HeightPrecision) continue; // Cant multiply height, more than 3 digits

            var item = new OperationLayerReHeightItem(true, i, Layer.RoundHeight(layerHeight * i), layerCount / i);
            list.Add(item);
        }

        return list.ToArray();
    }
    #endregion

    #region Constructor

    public OperationLayerReHeight() { }

    public OperationLayerReHeight(FileFormat slicerFile) : base(slicerFile)
    { }

    public override void InitWithSlicerFile()
    {
        base.InitWithSlicerFile();
        Presets = GetItems(SlicerFile.LayerCount, (decimal)SlicerFile.LayerHeight);
        if (Presets.Length > 0)
        {
            _selectedItem = Presets[0];
        }

        if (_bottomExposure <= 0) _bottomExposure = (decimal)SlicerFile.BottomExposureTime;
        if (_normalExposure <= 0) _normalExposure = (decimal)SlicerFile.ExposureTime;
    }

    #endregion

    #region Subclasses
    public class OperationLayerReHeightItem
    {
        public bool IsMultiply { get; }
        public bool IsDivision => !IsMultiply;
        public byte Modifier { get; }
        public decimal LayerHeight { get; }
        public uint LayerCount { get; }

        public OperationLayerReHeightItem(bool isMultiply, byte modifier, decimal layerHeight, uint layerCount)
        {
            IsMultiply = isMultiply;
            Modifier = modifier;
            LayerHeight = layerHeight;
            LayerCount = layerCount;
        }

        public override string ToString()
        {
            return (IsMultiply ? 'x' : '÷') + $" {Modifier} → {LayerCount} layers at {LayerHeight}mm";
        }
    }
    #endregion

    #region Methods
    protected override bool ExecuteInternally(OperationProgress progress)
    {
        if (_method == OperationLayerReHeightMethod.ReHeight)
        {
            progress.ItemCount = _selectedItem!.LayerCount;

            var layers = new Layer[_selectedItem.LayerCount];

            if (_selectedItem.IsDivision)
            {
                uint newLayerIndex = 0;
                for (uint layerIndex = 0; layerIndex < SlicerFile.LayerCount; layerIndex++)
                {
                    progress.ThrowIfCancellationRequested();

                    var oldLayer = SlicerFile[layerIndex];
                    for (byte i = 0; i < _selectedItem.Modifier; i++)
                    {
                        var newLayer = oldLayer.Clone();
                        //newLayer.Index = newLayerIndex;
                        //newLayer.PositionZ = (float)(Item.LayerHeight * (newLayerIndex + 1));
                        layers[newLayerIndex] = newLayer;
                        newLayerIndex++;
                        progress++;
                    }
                }
            }
            else
            {
                var layerIndexes = new uint[SlicerFile.LayerCount / _selectedItem.Modifier];
                for (uint i = 0; i < layerIndexes.Length; i++)
                {
                    layerIndexes[i] = i * _selectedItem.Modifier;
                }

                Parallel.ForEach(layerIndexes, CoreSettings.GetParallelOptions(progress), layerIndex =>
                {
                    var oldLayer = SlicerFile[layerIndex];
                    using var matSum = oldLayer.LayerMat;
                    Mat? matXorSum = null;
                    using Mat aaAverageSum = new();

                    if (_antiAliasingType == OperationLayerReHeightAntiAliasingType.Average)
                    {
                        matSum.ConvertTo(aaAverageSum, DepthType.Cv16U);
                    }

                    for (byte i = 1; i < _selectedItem.Modifier; i++)
                    {
                        using var nextMat = SlicerFile[layerIndex + i].LayerMat;

                        switch (_antiAliasingType)
                        {
                            case OperationLayerReHeightAntiAliasingType.None:
                                CvInvoke.Add(matSum, nextMat, matSum);
                                break;
                            case OperationLayerReHeightAntiAliasingType.Difference:
                            {
                                using var previousMat = SlicerFile[layerIndex + i - 1].LayerMat;
                                var matXor = new Mat();
                                //CvInvoke.Threshold(previousMat, previousMat, 127, 255, ThresholdType.Binary);
                                //CvInvoke.Threshold(nextMat, nextMat, 127, 255, ThresholdType.Binary);
                                CvInvoke.BitwiseXor(previousMat, nextMat, matXor);
                                matXor.SetTo(new MCvScalar((byte) (byte.MaxValue / _selectedItem.Modifier)),
                                    matXor);
                                if (matXorSum is null)
                                {
                                    matXorSum = matXor.Clone();
                                }
                                else
                                {
                                    CvInvoke.Add(matXorSum, matXorSum, matXorSum);
                                    CvInvoke.Add(matXorSum, matXor, matXorSum);
                                }

                                break;
                            }
                            case OperationLayerReHeightAntiAliasingType.Average:
                                nextMat.ConvertTo(nextMat, DepthType.Cv16U);
                                CvInvoke.Add(aaAverageSum, nextMat, aaAverageSum);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    switch (_antiAliasingType)
                    {
                        case OperationLayerReHeightAntiAliasingType.Difference:
                            CvInvoke.Add(matSum, matXorSum, matSum);
                            CvInvoke.PyrDown(matSum, matSum);
                            CvInvoke.PyrUp(matSum, matSum);
                            matXorSum!.Dispose();
                            break;
                        case OperationLayerReHeightAntiAliasingType.Average:
                            aaAverageSum.ConvertTo(matSum, DepthType.Cv8U, 1.0 / _selectedItem.Modifier);
                            CvInvoke.PyrDown(matSum, matSum);
                            CvInvoke.PyrUp(matSum, matSum);
                            break;
                    }

                    var newLayer = oldLayer.Clone();
                    //newLayer.Index = newLayerIndex;
                    //newLayer.PositionZ = (float)(Item.LayerHeight * (newLayerIndex + 1));
                    newLayer.LayerMat = matSum;
                    layers[layerIndex / _selectedItem.Modifier] = newLayer;

                    progress.LockAndIncrement();
                });
            }

            SlicerFile.SuppressRebuildPropertiesWork(() =>
            {
                SlicerFile.LayerHeight = (float)_selectedItem!.LayerHeight;
                SlicerFile.BottomExposureTime = (float) _bottomExposure;
                SlicerFile.ExposureTime = (float) _normalExposure;
                SlicerFile.Layers = layers;
            }, true);
        }
        else if (_method == OperationLayerReHeightMethod.OffsetPositionZ)
        {
            for (var layerIndex = LayerIndexStart; layerIndex <= LayerIndexEnd; layerIndex++)
            {
                SlicerFile[layerIndex].PositionZ += (float)_positionZOffset;
                progress++;
            }
        }

        return !progress.Token.IsCancellationRequested;
    }
    #endregion
}