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
        var layerIndexes = layersRemove
            .Where(layerIndex => layerIndex < slicerFile.LayerCount)
            .Distinct()
            .OrderByDescending(layerIndex => layerIndex)
            .ToArray();
        if (layerIndexes.Length == 0) return false;
        if (layerIndexes.Length >= slicerFile.LayerCount)
            throw new InvalidOperationException("At least one layer must remain in the file.");

        var ownsProgress = progress is null;
        progress ??= new OperationProgress(false);

        try
        {
            progress.Reset("Removed layers", (uint)layerIndexes.Length);

            var layers = slicerFile.ToList();
            var removedBottomLayers = 0;
            var removedLayerHeights = layerIndexes.ToDictionary(
                layerIndex => layerIndex,
                layerIndex => slicerFile[layerIndex].RelativePositionZ);
            //uint lastRemovedBottomLayerIndex = 0;

            var lastBottomLayer = slicerFile.LastBottomLayer;

            // Register bottom layers
            if (slicerFile.BottomLayerCount > 0)
            {
                foreach (var layerIndex in layerIndexes)
                {
                    if (!slicerFile[layerIndex].IsBottomLayer) continue;
                    removedBottomLayers++;
                    //lastRemovedBottomLayerIndex = layerIndex;
                }
            }

            var removedHeight = 0f;
            var removeIndex = layerIndexes.Length - 1;
            for (uint layerIndex = 0; layerIndex < slicerFile.LayerCount; layerIndex++)
            {
                if (removeIndex >= 0 && layerIndex == layerIndexes[removeIndex])
                {
                    removedHeight += removedLayerHeights[layerIndex];
                    removeIndex--;
                    continue;
                }

                if (removedHeight > 0)
                    slicerFile[layerIndex].PositionZ -= removedHeight;
            }

            foreach (var layerIndex in layerIndexes)
            {
                layers.RemoveAt((int)layerIndex);
                progress++;
            }

            // Should never happen, still use this safe-check
            if (slicerFile.LayerCount != layers.Count)
            {
                // Try to copy bottom parameters to shifted new bottom layers
                if (removedBottomLayers > 0 && lastBottomLayer is not null)
                {
                    var bottomLayersToReplace = removedBottomLayers;
                    for (var layerIndex = lastBottomLayer.Index + 1;
                         layerIndex < slicerFile.LayerCount && bottomLayersToReplace > 0;
                         layerIndex++)
                    {
                        if (removedLayerHeights.ContainsKey(layerIndex)) continue;
                        lastBottomLayer.CopyParametersTo(slicerFile[layerIndex]);
                        bottomLayersToReplace--;
                    }
                }

                slicerFile.SuppressRebuildPropertiesWork(() => slicerFile.Layers = layers.ToArray());
            }

            return true;
        }
        finally
        {
            if (ownsProgress)
            {
                progress.Dispose();
            }
        }
    }
    #endregion
}
