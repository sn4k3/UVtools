using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;

namespace UVtools.Core.Operations
{
    public sealed class OperationLayerImport
    {
        public uint InsertAfterLayerIndex { get; set; }
        public bool ReplaceStartLayer { get; set; }
        public bool ReplaceSubsequentLayers { get; set; }
        public bool DiscardRemainingLayers { get; set; }

        public List<string> Files { get; } = new List<string>();

        public void Sort()
        {
            Files.Sort((file1, file2) => string.Compare(Path.GetFileNameWithoutExtension(file1), Path.GetFileNameWithoutExtension(file2), StringComparison.Ordinal));
        }

        public ConcurrentBag<string> Validate(Size resolution)
        {
            var result = new ConcurrentBag<string>();
            Parallel.ForEach(Files, file =>
            {
                using (Mat mat = CvInvoke.Imread(file, ImreadModes.AnyColor))
                {
                    if (mat.Size != resolution)
                    {
                        result.Add(file);
                    }
                }
            });
            return result;
        }

        public uint CalculateTotalLayers(uint totalLayers)
        {
            if (DiscardRemainingLayers)
            {
                return (uint) (1 + InsertAfterLayerIndex + Files.Count - (ReplaceStartLayer ? 1u : 0u));
            }
            if (!ReplaceSubsequentLayers)
            {
                return (uint)(totalLayers + Files.Count - (ReplaceStartLayer ? 1u : 0u));
            }

            // Need to calculate the total layer count after subsequent replacing, taking in account that layer count can grow


            return 0;
        }
    }
}
