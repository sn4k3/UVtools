/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;

namespace UVtools.Core.Operations;


public sealed class OperationLayerRemove : Operation
{
    #region Members
    private bool _useThreshold;
    private uint _pixelThreshold;
    #endregion

    #region Overrides

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.Current;
    public override bool CanROI => false;
    public override bool PassActualLayerIndex => true;
    public override string IconClass => "mdi-layers-remove";
    public override string Title => "Remove layers";

    public override string Description =>
        "Remove layers in a given range.";

    public override string ConfirmationText =>
        $"remove layers {LayerIndexStart} through {LayerIndexEnd}"+
        (_useThreshold ? $" with an pixel threshold of {_pixelThreshold}px" : string.Empty)
        +"?";

    public override string ProgressTitle => 
        $"Removing layers {LayerIndexStart} through {LayerIndexEnd}" +
        (_useThreshold ? $" with an pixel threshold of {_pixelThreshold}px" : string.Empty);

    public override string ProgressAction => "Removed layers";

    public override bool CanCancel => false;

    public override bool CanHaveProfiles => false;

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        var layersToRemove = LayerRemoveCount;
        if (LayerRemoveCount == 0)
        {
            sb.AppendLine("The used values will not remove any layer, please adjust.");
        }

        if (layersToRemove == SlicerFile.LayerCount)
        {
            sb.AppendLine("You can't remove all layers from the file. Keep at least one.");
        }

        return sb.ToString();
    }

    #endregion

    #region Properties

    public bool UseThreshold
    {
        get => _useThreshold;
        set
        {
            if(!RaiseAndSetIfChanged(ref _useThreshold, value)) return;
            RaisePropertyChanged(nameof(LayerRemoveCount));
        }
    }

    public uint PixelThreshold
    {
        get => _pixelThreshold;
        set
        {
            if(!RaiseAndSetIfChanged(ref _pixelThreshold, value)) return;
            RaisePropertyChanged(nameof(LayerRemoveCount));
        }
    }

    public uint LayerRemoveCount
    {
        get
        {
            if (!_useThreshold) return LayerRangeCount;
            uint layers = 0;
            for (uint layerIndex = LayerIndexStart; layerIndex <= LayerIndexEnd; layerIndex++)
            {
                if (SlicerFile[layerIndex].NonZeroPixelCount > _pixelThreshold) continue;
                layers++;
            }

            return layers;
        }
    }

    #endregion

    #region Constructor

    public OperationLayerRemove() { }

    public OperationLayerRemove(FileFormat slicerFile) : base(slicerFile) { }

    #endregion

    #region Equality
    private bool Equals(OperationLayerRemove other)
    {
        return _useThreshold == other._useThreshold && _pixelThreshold == other._pixelThreshold;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationLayerRemove other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_useThreshold, _pixelThreshold);
    }
    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        progress.CanCancel = false;
        var layersRemove = new List<uint>();
        for (uint layerIndex = LayerIndexStart; layerIndex <= LayerIndexEnd; layerIndex++)
        {
            if(_useThreshold && SlicerFile[layerIndex].NonZeroPixelCount > _pixelThreshold) continue;
            layersRemove.Add(layerIndex);
        }

        return RemoveLayers(SlicerFile, layersRemove, progress);
    }

    public static bool RemoveLayers(FileFormat slicerFile, IEnumerable<uint> layersRemove, OperationProgress? progress = null)
    {
        if (!layersRemove.Any()) return false;

        progress ??= new OperationProgress(false);

        progress.Reset("Removed layers", (uint)layersRemove.Count());

        var layers = slicerFile.ToList();
        int removedBottomLayers = 0;
        //uint lastRemovedBottomLayerIndex = 0;

        var lastBottomLayer = slicerFile.LastBottomLayer;

        // Register bottom layers
        if (slicerFile.BottomLayerCount > 0)
        {
            var layersRemoveAsc = layersRemove.OrderBy(index => index);
            foreach (var layerIndex in layersRemoveAsc)
            {
                if (!slicerFile[layerIndex].IsBottomLayer) continue;
                removedBottomLayers++;
                //lastRemovedBottomLayerIndex = layerIndex;
            }
        }

        // Remove layers
        var layersRemoveDesc = layersRemove.OrderByDescending(index => index);
        foreach (var layerIndex in layersRemoveDesc)
        {
            layers.RemoveAt((int)layerIndex);

            // Shift layer positions
            var relativeZ = slicerFile[layerIndex].RelativePositionZ;
            if (relativeZ <= 0) continue;
            for (uint i = layerIndex + 1; i < slicerFile.LayerCount; i++)
            {
                slicerFile[i].PositionZ -= relativeZ;
            }
            progress++;
        }

        // Should never happen, still use this safe-check
        if (slicerFile.LayerCount != layers.Count)
        {
            // Try to copy bottom parameters to shifted new bottom layers
            if (removedBottomLayers > 0 && lastBottomLayer is not null)
            {
                var startIndex = (uint) Math.Max(lastBottomLayer.Index + 1, layersRemove.Count());
                var endIndex = startIndex + removedBottomLayers;
                var copyFromFromLayerIndex = (uint)Math.Max(0, (int)lastBottomLayer.Index);
                for (var layerIndex = startIndex; layerIndex < endIndex && layerIndex < slicerFile.LayerCount; layerIndex++)
                {
                    slicerFile[copyFromFromLayerIndex].CopyParametersTo(slicerFile[layerIndex]);
                }
            }
            slicerFile.SuppressRebuildPropertiesWork(() => slicerFile.Layers = layers.ToArray());
        }

        return true;
    }
    #endregion
}