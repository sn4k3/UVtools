/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using ZLinq;

namespace UVtools.Core.Operations;


public sealed partial class OperationLayerClone : Operation
{
    #region Members

    #endregion

    #region Overrides

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.Current;
    public override bool CanROI => false;
    public override bool PassActualLayerIndex => true;
    public override string IconClass => "LayersPlus";
    public override string Title => "Clone layers";
    public override string Description =>
        "Clone layers.\n\n" +
        "Useful to increase the height of the model or add additional structure by duplicating layers. For example, can be used to increase the raft height for added stability.";
    public override string ConfirmationText =>
        $"clone layers {LayerIndexStart} through {LayerIndexEnd}, {Clones} time{(Clones != 1 ? "s" : "")}?";

    public override string ProgressTitle =>
        $"Cloning layers {LayerIndexStart} through {LayerIndexEnd}, {Clones} time{(Clones != 1 ? "s" : "")}";

    public override string ProgressAction => "Cloned layers";

    //public override bool CanHaveProfiles => false;

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();
        if (Clones <= 0)
        {
            sb.AppendLine("Clones must be a positive number");
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"[Clones: {Clones}]" + LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets if cloned layers will keep same position z or get the height rebuilt
    /// </summary>
    [ObservableProperty]
    public partial bool KeepSamePositionZ { get; set; }

    /// <summary>
    /// Gets or sets the number of clones
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExtraLayers))]
    public partial uint Clones { get; set; } = 1;

    public uint ExtraLayers => (uint)Math.Max(0, ((int)LayerIndexEnd - LayerIndexStart + 1) * Clones);

    #endregion

    #region Constructor

    public OperationLayerClone() { }

    public OperationLayerClone(FileFormat slicerFile) : base(slicerFile) { }

    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        uint totalClones = (LayerIndexEnd - LayerIndexStart + 1) * Clones;
        progress.Reset(ProgressAction, totalClones);

        var oldLayers = SlicerFile.AsValueEnumerable().ToArray();
        var relativePositions = oldLayers.AsValueEnumerable().Select(layer => layer.RelativePositionZ).ToArray();
        var newLayers = new Layer[oldLayers.Length + totalClones];
        var newOriginalPositions = new float[oldLayers.Length];

        uint newLayerIndex = 0;
        float incrementedPositionZ = 0;
        for (uint layerIndex = 0; layerIndex < oldLayers.Length; layerIndex++)
        {
            progress.PauseOrCancelIfRequested();
            var layer = oldLayers[layerIndex];
            newOriginalPositions[layerIndex] = layer.PositionZ + incrementedPositionZ;
            newLayers[newLayerIndex++] = layer;

            if (layerIndex < LayerIndexStart || layerIndex > LayerIndexEnd) continue;
            float increment = relativePositions[layerIndex];
            if (increment == 0) increment = SlicerFile.LayerHeight;
            for (uint i = 0; i < Clones; i++)
            {
                var clone = layer.Clone();

                if (!KeepSamePositionZ)
                {
                    incrementedPositionZ += increment;
                    clone.PositionZ = newOriginalPositions[layerIndex] + increment * (i + 1);
                }

                newLayers[newLayerIndex++] = clone;
                progress++;
            }
        }

        if (progress.Token.IsCancellationRequested) return false;
        SlicerFile.SuppressRebuildPropertiesWork(() =>
        {
            if (!KeepSamePositionZ)
            {
                for (var i = 0; i < oldLayers.Length; i++)
                {
                    oldLayers[i].PositionZ = newOriginalPositions[i];
                }
            }
            SlicerFile.Layers = newLayers;
        });


        return !progress.Token.IsCancellationRequested;
    }

    #endregion
}
