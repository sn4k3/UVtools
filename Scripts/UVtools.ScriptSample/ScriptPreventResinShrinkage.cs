using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;
using UVtools.Core.Scripting;

namespace UVtools.ScriptSample;

public class ScriptPreventResinShrinkage : ScriptGlobals
{
    readonly ScriptNumericalInput<ushort> GrainSize = new()
    {
        Label = "Size of the initial grains",
        Unit = "px",
        Minimum = 1,
        Maximum = 500,
        Increment = 1,
        Value = 11,
    };

    readonly ScriptNumericalInput<ushort> Spacing = new()
    {
        Label = "Free space between the grains",
        Unit = "px",
        Minimum = 1,
        Maximum = 500,
        Increment = 1,
        Value = 9,
    };

    /// <summary>
    /// Set configurations here, this function trigger just after load a script
    /// </summary>
    public void ScriptInit()
    {
        Script.Name = "Preventing the effects of resin shrinkage";
        Script.Description = "Cures a layer in multiple exposures to mitigate resin shrinkage effects";
        Script.Author = "Jan Mrázek";
        Script.Version = new Version(0, 3);
        Script.UserInputs.Add(GrainSize);
        Script.UserInputs.Add(Spacing);
    }

    /// <summary>
    /// Validate user inputs here, this function trigger when user click on execute
    /// </summary>
    /// <returns>A error message, empty or null if validation passes.</returns>
    public string? ScriptValidate()
    {
        return SlicerFile.CanUseLayerPositionZ ? null : "Your printer/file format is not supported: Unable to have multiple layers in same Z position.";
    }

    private Mat GenerateDotPattern() {
        var pattern = SlicerFile.CreateMat();

        var xStep = GrainSize.Value + Spacing.Value;
        var yStep = (GrainSize.Value + Spacing.Value) / 2;
        var evenRow = false;
        for (int y = 0; y < pattern.Size.Height; y += yStep) {
            for (int x = 0; x < pattern.Size.Width; x += xStep) {
                CvInvoke.Circle(pattern,
                    new Point(x + (evenRow ? xStep / 2 : 0), y),
                    GrainSize.Value / 2,
                    EmguExtensions.WhiteColor,
                    -1, LineType.FourConnected);
            }
            evenRow = !evenRow;
        }

        return pattern;
    }

    private Mat GenerateLinePattern() {
        var pattern = SlicerFile.CreateMat();

        var width = GrainSize.Value / 5;
        if (width == 0)
            width = 1;
        var step = GrainSize.Value + Spacing.Value;
        for (int x = 0; x < pattern.Size.Width; x += step) {
            CvInvoke.Line(pattern,
                new Point(x, 0),
                new Point(x + pattern.Size.Height, pattern.Size.Height),
                EmguExtensions.WhiteColor, width, LineType.FourConnected);
            CvInvoke.Line(pattern,
                new Point(x, pattern.Size.Height),
                new Point(x + pattern.Size.Height, 0),
                EmguExtensions.WhiteColor, width, LineType.FourConnected);
        }
        for (int y = 0; y < pattern.Size.Height; y += step) {
            CvInvoke.Line(pattern,
                new Point(0, y),
                new Point(pattern.Size.Height, y + pattern.Size.Height),
                EmguExtensions.WhiteColor, width, LineType.FourConnected);
            CvInvoke.Line(pattern,
                new Point(0, y),
                new Point(pattern.Size.Height, y - pattern.Size.Height),
                EmguExtensions.WhiteColor, width, LineType.FourConnected);
        }
        return pattern;
    }

    /// <summary>
    /// Execute the script, this function trigger when when user click on execute and validation passes
    /// </summary>
    /// <returns>True if executes successfully to the end, otherwise false.</returns>
    public bool ScriptExecute()
    {
        Progress.Reset("Changing layers", Operation.LayerRangeCount); // Sets the progress name and number of items to process

        var newLayers = new Layer[(int) SlicerFile.LayerCount * 3];

        var dotPattern = GenerateDotPattern();
        var linePattern = GenerateLinePattern();

        using var inverseDotPattern = new Mat();
        using var dotLinePattern = new Mat();

        CvInvoke.BitwiseNot(dotPattern, inverseDotPattern);
        CvInvoke.Dilate(inverseDotPattern, inverseDotPattern, EmguExtensions.Kernel3x3Rectangle,
            new Point(-1, -1), 1, BorderType.Reflect101, default);

        CvInvoke.BitwiseAnd(inverseDotPattern, linePattern, dotLinePattern);
        
        Parallel.For(Operation.LayerIndexStart, Operation.LayerIndexEnd + 1,
            CoreSettings.GetParallelOptions(Progress),
            layerIndex =>
        {
            Progress.PauseIfRequested();
            var fullLayer = SlicerFile[layerIndex];
            if (fullLayer.IsEmpty) 
            {
                newLayers[layerIndex * 3] = fullLayer;
                return; // Do not apply to empty layers
            }

            var coresLayer1 = fullLayer.Clone();
            var coresLayer2 = fullLayer.Clone();

            using var coresMat1 = fullLayer.LayerMat;
            
            // Ensure there is something we can attach to in the previous layer
            if (layerIndex > 0) {
                using var previousLayerMat = SlicerFile[layerIndex - 1].LayerMat;
                CvInvoke.BitwiseAnd(coresMat1, previousLayerMat, coresMat1);
            }
            using var coresMat2 = coresMat1.Clone();

            CvInvoke.BitwiseAnd(coresMat1, dotPattern, coresMat1);
            CvInvoke.BitwiseAnd(coresMat2, dotLinePattern, coresMat2);
            

            coresLayer1.LayerMat = coresMat1;
            coresLayer2.LayerMat = coresMat2;

            // Try to disable lifts for last two subsequent layers
            fullLayer.LiftHeightTotal = coresLayer2.LiftHeightTotal = SlicerFile.SupportGCode ? 0f : 0.1f;

            newLayers[layerIndex * 3] = coresLayer1;
            newLayers[layerIndex * 3 + 1] = coresLayer2;
            newLayers[layerIndex * 3 + 2] = fullLayer;

            Progress.LockAndIncrement();
        });

        // Remove null layers (Empty layers not replicated)
        newLayers = newLayers.Where(layer => layer is not null).ToArray();

        SlicerFile.SuppressRebuildPropertiesWork(() => {
            SlicerFile.Layers = newLayers;
        });
        // return true if not cancelled by user
        return !Progress.Token.IsCancellationRequested;
    }
}
