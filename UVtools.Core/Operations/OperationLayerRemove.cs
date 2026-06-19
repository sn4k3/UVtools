/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public sealed partial class OperationLayerRemove : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Members
    #endregion

    #region Overrides

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.Current;
    public override bool CanROI => false;
    public override bool PassActualLayerIndex => true;
    public override string IconClass => "LayersRemove";
    public override string Title => "Remove layers";

    public override string Description =>
        "Remove layers in a given range.";

    public override string ConfirmationText =>
        $"remove layers {LayerIndexStart} through {LayerIndexEnd}"+
        (UseThreshold ? $" with an pixel threshold of {PixelThreshold}px" : string.Empty)
        +"?";

    public override string ProgressTitle =>
        $"Removing layers {LayerIndexStart} through {LayerIndexEnd}" +
        (UseThreshold ? $" with an pixel threshold of {PixelThreshold}px" : string.Empty);

    public override string ProgressAction => "Removed layers";

    public override bool CanCancel => false;

    //public override bool CanHaveProfiles => false;

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        var layersToRemove = LayerRemoveCount;
        if (layersToRemove == 0)
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LayerRemoveCount))]
    public partial bool UseThreshold { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LayerRemoveCount))]
    public partial uint PixelThreshold { get; set; }

    public uint LayerRemoveCount
    {
        get
        {
            if (!UseThreshold) return LayerRangeCount;
            uint layers = 0;
            for (uint layerIndex = LayerIndexStart; layerIndex <= LayerIndexEnd; layerIndex++)
            {
                if (SlicerFile[layerIndex].NonZeroPixelCount > PixelThreshold) continue;
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
        return UseThreshold == other.UseThreshold && PixelThreshold == other.PixelThreshold;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationLayerRemove other && Equals(other);
    }

    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        progress.CanCancel = false;
        var layersRemove = new List<uint>();
        for (uint layerIndex = LayerIndexStart; layerIndex <= LayerIndexEnd; layerIndex++)
        {
            if(UseThreshold && SlicerFile[layerIndex].NonZeroPixelCount > PixelThreshold) continue;
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