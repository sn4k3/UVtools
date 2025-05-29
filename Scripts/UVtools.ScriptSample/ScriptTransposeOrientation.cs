using Emgu.CV;
using System;
using System.Drawing;
using System.Threading.Tasks;
using Emgu.CV.CvEnum;
using UVtools.Core;
using UVtools.Core.Scripting;

namespace UVtools.ScriptSample;

public class ScriptTransposeOrientation : ScriptGlobals
{
    readonly ScriptToggleSwitchInput RotationInput = new()
    {
        Label = "Rotation",
        OnText = "Clockwise (CW)",
        OffText = "Counter-Clockwise (CWW)",
        Value = true,
    };

    readonly ScriptCheckBoxInput MirrorHorizontallyInput = new()
    {
        Label = "Horizontal Mirror",
        Value = false,
    };

    readonly ScriptCheckBoxInput MirrorVerticallyInput = new()
    {
        Label = "Vertical Mirror",
        Value = false,
    };

    public void ScriptInit()
    {
        Script.MinimumVersionToRun = new Version(0, 4, 0);
        Script.Name = "Transpose screen orientation";
        Script.Description = "Transposes the screen orientation and assign the swap properties to resolution and display.";
        Script.Author = "Tiago Conceição";
        Script.Version = new Version(0, 1);
        Script.UserInputs.Add(RotationInput);
        Script.UserInputs.Add(MirrorHorizontallyInput);
        Script.UserInputs.Add(MirrorVerticallyInput);
    }

    public string? ScriptValidate()
    {
        return SlicerFile.LayerCount == 0
            ? "This script requires at least a layer in order to run."
            : null;
    }

    public bool ScriptExecute()
    {
        Progress.Reset("Transposing layers", SlicerFile.LayerCount); // Sets the progress name and number of items to process

        Parallel.For(0, SlicerFile.LayerCount, CoreSettings.GetParallelOptions(Progress), layerIndex => {
            Progress.PauseIfRequested();

            var layer = SlicerFile[layerIndex];
            using var mat = layer.LayerMat;

            CvInvoke.Rotate(mat, mat, RotationInput.Value ? RotateFlags.Rotate90Clockwise : RotateFlags.Rotate90CounterClockwise);

            if (MirrorHorizontallyInput.Value && MirrorVerticallyInput.Value)
            {
                CvInvoke.Flip(mat, mat, FlipType.Both);
            }
            else if (MirrorHorizontallyInput.Value)
            {
                CvInvoke.Flip(mat, mat, FlipType.Horizontal);
            }
            else if (MirrorVerticallyInput.Value)
            {
                CvInvoke.Flip(mat, mat, FlipType.Vertical);
            }

            SlicerFile[layerIndex].LayerMat = mat;
            Progress.LockAndIncrement();
        });

        // Update the resolution and display properties based on the rotation
        SlicerFile.Display = new SizeF(SlicerFile.DisplayHeight, SlicerFile.DisplayWidth);
        SlicerFile.Resolution = SlicerFile[0].Resolution;

        // Ensure the bounding rectangle is reset, not required but good practice
        SlicerFile.BoundingRectangle = Rectangle.Empty;

        // return true if not cancelled by user
        return !Progress.Token.IsCancellationRequested;
    }
}
