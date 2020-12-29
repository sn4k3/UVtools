/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public sealed class OperationLayerImport : Operation
    {
        private uint _insertAfterLayerIndex;
        private bool _replaceStartLayer;
        private bool _replaceSubsequentLayers;
        private bool _discardRemainingLayers;
        private bool _mergeImages;
        private ObservableCollection<StringTag> _files = new ObservableCollection<StringTag>();

        #region Overrides

        public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.None;
        public override bool CanROI => false;
        public override string Title => "Import Layers";

        public override string Description =>
            "Import layers from local files into the model at a selected layer height.\n\n" +
            "NOTE: Imported images must be greyscale and have the same resolution as the model.";

        public override string ConfirmationText => $"import {Count} layer{(Count!=1?"s":"")}?";

        public override string ProgressTitle =>
            $"Importing {Count} layer{(Count!=1 ? "s" : "")}";

        public override string ProgressAction => "Imported layers";

        public override bool CanCancel => true;

        public override uint LayerIndexStart => InsertAfterLayerIndex + (ReplaceStartLayer ? 0u : 1);
        public override uint LayerIndexEnd => (uint)(LayerIndexStart + Count - 1);

        public override bool CanHaveProfiles => false;

        public override StringTag Validate(params object[] parameters)
        {
            var result = new ConcurrentBag<StringTag>();
            Parallel.ForEach(Files, file =>
            {
                using (Mat mat = CvInvoke.Imread(file.TagString, ImreadModes.AnyColor))
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
                message.AppendLine(file.Content);
            }

            return new StringTag(message.ToString(), result);
        }
        #endregion

        #region Properties

        public Size FileResolution { get; }

        public uint InsertAfterLayerIndex
        {
            get => _insertAfterLayerIndex;
            set => RaiseAndSetIfChanged(ref _insertAfterLayerIndex, value);
        }

        public bool ReplaceStartLayer
        {
            get => _replaceStartLayer;
            set => RaiseAndSetIfChanged(ref _replaceStartLayer, value);
        }

        public bool ReplaceSubsequentLayers
        {
            get => _replaceSubsequentLayers;
            set => RaiseAndSetIfChanged(ref _replaceSubsequentLayers, value);
        }

        public bool DiscardRemainingLayers
        {
            get => _discardRemainingLayers;
            set => RaiseAndSetIfChanged(ref _discardRemainingLayers, value);
        }

        public bool MergeImages
        {
            get => _mergeImages;
            set => RaiseAndSetIfChanged(ref _mergeImages, value);
        }

        public ObservableCollection<StringTag> Files
        {
            get => _files;
            set => RaiseAndSetIfChanged(ref _files, value);
        }

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

        public void AddFile(string file)
        {
            Files.Add(new StringTag(Path.GetFileNameWithoutExtension(file), file));
        }

        public void Sort()
        {
            var sortedFiles = Files.ToList();
            sortedFiles.Sort((file1, file2) => string.Compare(Path.GetFileNameWithoutExtension(file1.TagString), Path.GetFileNameWithoutExtension(file2.TagString), StringComparison.Ordinal));
            Files.Clear();
            foreach (var file in sortedFiles)
            {
                Files.Add(file);
            }
            //Files.Sort((file1, file2) => string.Compare(Path.GetFileNameWithoutExtension(file1), Path.GetFileNameWithoutExtension(file2), StringComparison.Ordinal));
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

        public override string ToString()
        {
            var result = $"[Files: {Count}]" + LayerRangeString;
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }

        public override bool Execute(FileFormat slicerFile, OperationProgress progress = null)
        {
            progress ??= new OperationProgress();
            progress.Reset(ProgressAction, (uint)Count);

            var oldLayers = slicerFile.LayerManager.Layers;
            uint newLayerCount = CalculateTotalLayers((uint)oldLayers.Length);
            uint startIndex = LayerIndexStart;
            var layers = new Layer[newLayerCount];

            // Keep same layers up to InsertAfterLayerIndex
            for (uint i = 0; i <= InsertAfterLayerIndex; i++)
            {
                layers[i] = oldLayers[i];
            }

            // Keep all old layers if not discarding them
            if (ReplaceSubsequentLayers)
            {
                if (!DiscardRemainingLayers)
                {
                    for (uint i = InsertAfterLayerIndex + 1; i < oldLayers.Length; i++)
                    {
                        layers[i] = oldLayers[i];
                    }
                }
            }
            else // Push remaining layers to the end of imported layers
            {
                uint oldLayerIndex = InsertAfterLayerIndex;
                for (uint i = LayerIndexEnd + 1; i < newLayerCount; i++)
                {
                    oldLayerIndex++;
                    layers[i] = oldLayers[oldLayerIndex];
                }
            }


            Parallel.For(0, Count,
                //new ParallelOptions{MaxDegreeOfParallelism = 1},
                i =>
                {
                    var mat = CvInvoke.Imread(Files[i].TagString, ImreadModes.Grayscale);
                    uint layerIndex = (uint)(startIndex + i);
                    if (MergeImages)
                    {
                        if (layers[layerIndex] is not null)
                        {
                            using var oldMat = layers[layerIndex].LayerMat;
                            CvInvoke.Add(oldMat, mat, mat);
                        }
                    }
                    layers[layerIndex] = new Layer(layerIndex, mat, slicerFile.LayerManager);

                    lock (progress.Mutex)
                    {
                        progress++;
                    }
                });

            slicerFile.LayerManager.Layers = layers;
            slicerFile.RequireFullEncode = true;

            progress.Token.ThrowIfCancellationRequested();

            return true;
        }

        #endregion
    }
}
