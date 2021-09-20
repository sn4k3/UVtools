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
using System.Threading.Tasks;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public sealed class OperationLayerRemove : Operation
    {
        #region Overrides

        public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.Current;
        public override bool CanROI => false;
        public override bool PassActualLayerIndex => true;

        public override string Title => "Remove layers";

        public override string Description =>
            "Remove Layers in a given range.";

        public override string ConfirmationText =>
            $"remove layers {LayerIndexStart} through {LayerIndexEnd}?";

        public override string ProgressTitle =>
            $"Removing layers {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressAction => "Removed layers";

        public override bool CanCancel => false;

        public override bool CanHaveProfiles => false;

        public override string ValidateInternally()
        {
            var sb = new StringBuilder();

            if (LayerRangeCount == SlicerFile.LayerCount)
            {
                sb.AppendLine("You can't remove all layers from the file. Keep at least one.");
            }

            return sb.ToString();
        }

        #endregion

        #region Properties


        #endregion

        #region Constructor

        public OperationLayerRemove() { }

        public OperationLayerRemove(FileFormat slicerFile) : base(slicerFile) { }

        #endregion

        #region Methods

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            progress.CanCancel = false;
            var layersRemove = new List<uint>();
            for (uint layerIndex = LayerIndexStart; layerIndex <= LayerIndexEnd; layerIndex++)
            {
                layersRemove.Add(layerIndex);
            }

            return RemoveLayers(SlicerFile, layersRemove, progress);
        }

        public static bool RemoveLayers(FileFormat slicerFile, IEnumerable<uint> layersRemove, OperationProgress progress = null)
        {
            if (!layersRemove.Any()) return false;

            progress ??= new OperationProgress(false);

            progress.Reset("Removed layers", (uint)layersRemove.Count());

            foreach (var layerIndex in layersRemove)
            {
                slicerFile[layerIndex] = null;
                progress++;
            }

            slicerFile.LayerManager.RemoveNulls();

            /*var oldLayers = slicerFile.LayerManager.Layers;
            var layerHeight = slicerFile.LayerHeight;

            var layers = new Layer[oldLayers.Length - layersRemove.Count];

            // Re-set
            uint newLayerIndex = 0;
            for (uint layerIndex = 0; layerIndex < oldLayers.Length; layerIndex++)
            {
                if (layersRemove.Contains(layerIndex)) continue;
                layers[newLayerIndex] = oldLayers[layerIndex];
                layers[newLayerIndex].Index = newLayerIndex;

                // Re-Z
                float posZ = layerHeight;
                if (newLayerIndex > 0)
                {
                    if (oldLayers[layerIndex - 1].PositionZ == oldLayers[layerIndex].PositionZ)
                    {
                        posZ = layers[newLayerIndex - 1].PositionZ;
                    }
                    else
                    {
                        posZ = Layer.RoundHeight(layers[newLayerIndex - 1].PositionZ + layerHeight);
                    }
                }

                layers[newLayerIndex].PositionZ = posZ;
                layers[newLayerIndex].IsModified = true;

                newLayerIndex++;
                progress++;
            }

            slicerFile.LayerManager.Layers = layers;*/



            return true;
        }
        #endregion
    }
}
