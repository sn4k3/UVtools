/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    public sealed class OperationLayerImport : Operation
    {
        #region Overrides
        public override string Title => "Import Layers";

        public override string Description =>
            "Import layers from local files into the model at a selected layer height.\n\n" +
            "NOTE: Imported images must be greyscale and have the same resolution as the model.";

        public override string ConfirmationText => $"import {Count} layer{(Count!=1?"s":"")}?";

        public override string ProgressTitle =>
            $"Importing {Count} layer{(Count!=1 ? "s" : "")}";

        public override string ProgressAction => "Imported layers";

        public override uint LayerIndexStart => InsertAfterLayerIndex + (ReplaceStartLayer ? 0u : 1);
        public override uint LayerIndexEnd => (uint)(LayerIndexStart + Count - 1);

        public override StringTag Validate(params object[] parameters)
        {
            var result = new ConcurrentBag<string>();
            Parallel.ForEach(Files, file =>
            {
                using (Mat mat = CvInvoke.Imread(file, ImreadModes.AnyColor))
                {
                    if (mat.Size != FileResolution)
                    {
                        result.Add(file);
                    }
                }
            });

            if (result.IsEmpty) return null;
            var message = new StringBuilder();
            message.AppendLine($"The following {result.Count} files mismatched the target resolution of {FileResolution.Width}x{FileResolution.Height}:");
            message.AppendLine();
            uint count = 0;
            foreach (var file in result)
            {
                count++;
                if (count == 20)
                {
                    message.AppendLine("... Too many to show ...");
                    break;
                }
                message.AppendLine(Path.GetFileNameWithoutExtension(file));
            }

            return new StringTag(message.ToString(), result);
        }
        #endregion

        #region Properties

        public Size FileResolution { get; }
        public uint InsertAfterLayerIndex { get; set; }
        public bool ReplaceStartLayer { get; set; }
        public bool ReplaceSubsequentLayers { get; set; }
        public bool DiscardRemainingLayers { get; set; }

        public List<string> Files { get; } = new List<string>();

        public int Count => Files.Count;
        #endregion

        #region Constructor

        public OperationLayerImport()
        {
        }

        public OperationLayerImport(Size fileResolution)
        {
            FileResolution = fileResolution;
        }
        #endregion

        #region Methods
        public void Sort()
        {
            Files.Sort((file1, file2) => string.Compare(Path.GetFileNameWithoutExtension(file1), Path.GetFileNameWithoutExtension(file2), StringComparison.Ordinal));
        }
        

        public uint CalculateTotalLayers(uint totalLayers)
        {
            if (DiscardRemainingLayers)
            {
                return (uint) (1 + InsertAfterLayerIndex + Files.Count - (ReplaceStartLayer ? 1 : 0));
            }
            if (ReplaceSubsequentLayers)
            {
                uint result = (uint) (1 + InsertAfterLayerIndex + Files.Count - (ReplaceStartLayer ? 1 : 0));
                return result <= totalLayers ? totalLayers : result;
            }

            return (uint)(totalLayers + Files.Count - (ReplaceStartLayer ? 1 : 0));
        }

        

        #endregion
    }
}
