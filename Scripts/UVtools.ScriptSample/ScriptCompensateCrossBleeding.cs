using Emgu.CV;
using System;
using System.Drawing;
using System.Threading.Tasks;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.Scripting;

namespace UVtools.ScriptSample;

public class ScriptCompensateCrossBleeding : ScriptGlobals
{
    readonly ScriptNumericalInput<ushort> LayerBleed = new()
    {
        Label = "Number of layers the exposure bleeds through",
        Unit = "layers",
        Minimum = 1,
        Maximum = 500,
        Increment = 1,
        Value = 5,
    };

    public void ScriptInit()
    {
        Script.Name = "Mitigates effects of cross-layer bleeding";
        Script.Description = "Adjusts overhands so we can compensate for cross-layer curing";
        Script.Author = "Jan Mrázek";
        Script.Version = new Version(0, 1);
        Script.UserInputs.Add(LayerBleed);
    }

    public string? ScriptValidate()
    {
        return SlicerFile.LayerCount < 2 
            ? "This script requires at least 2 layers in order to run." 
            : null;
    }

    public bool ScriptExecute()
    {
        Progress.Reset("Changing layers", Operation.LayerRangeCount); // Sets the progress name and number of items to process

        var originalLayers = SlicerFile.CloneLayers();
        Parallel.For(Operation.LayerIndexStart, Operation.LayerIndexEnd + 1,
            CoreSettings.GetParallelOptions(Progress),
            layerIndex =>
            {
                var layersBelowCount = layerIndex > LayerBleed.Value ? LayerBleed.Value : layerIndex;

                using var sourceMat = originalLayers[layerIndex].LayerMat;
                var source = sourceMat.GetDataByteSpan();

                using var targetMat = sourceMat.NewBlank();
                var target = targetMat.GetDataByteSpan();

                using var occupancyMat = sourceMat.NewBlank();
                var occupancy = occupancyMat.GetDataByteSpan();

                var sumRectangle = Rectangle.Empty;
                for (int i = 0; i < layersBelowCount; i++)
                {
                    using var mat = originalLayers[layerIndex - i - 1].LayerMat;
                    CvInvoke.Threshold(mat, mat, 1, 1, Emgu.CV.CvEnum.ThresholdType.Binary);
                    CvInvoke.Add(mat, occupancyMat, occupancyMat);
                    sumRectangle = sumRectangle.IsEmpty 
                        ? originalLayers[layerIndex - i - 1].BoundingRectangle 
                        : Rectangle.Union(sumRectangle, originalLayers[layerIndex - i - 1].BoundingRectangle);
                }

                // Spare a few useless cycles depending on model volume on LCD
                var optimizedStatingPixelIndex = sourceMat.GetPixelPos(sumRectangle.Location);
                var optimizedEndingPixelIndex = sourceMat.GetPixelPos(sumRectangle.Right, sumRectangle.Bottom);
                for (var i = optimizedStatingPixelIndex; i < optimizedEndingPixelIndex; i++)
                {
                    if (layersBelowCount == 0 || occupancy[i] == layersBelowCount)
                        target[i] = source[i];
                }

                SlicerFile[layerIndex].LayerMat = targetMat;
                Progress.LockAndIncrement();
            });

        // return true if not cancelled by user
        return !Progress.Token.IsCancellationRequested;
    }
}
