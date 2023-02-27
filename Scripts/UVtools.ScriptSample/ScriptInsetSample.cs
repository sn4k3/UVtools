/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.Scripting;

namespace UVtools.ScriptSample;

/// <summary>
/// Performs a black inset around objects
/// </summary>
public class ScriptInsetSample : ScriptGlobals
{
    readonly ScriptNumericalInput<ushort> InsetMarginFromEdge = new()
    {
        Label = "Inset from edge",
        ToolTip = "Margin in pixels to inset from object edge",
        Unit = "px",
        Minimum = 1,
        Maximum = ushort.MaxValue,
        Increment = 1,
        Value = 10
    };

    readonly ScriptNumericalInput<ushort> InsetThickness = new()
    {
        Label = "Inset line thickness",
        ToolTip = "Inset line thickness in pixels",
        Unit = "px",
        Minimum = 1,
        Maximum = ushort.MaxValue,
        Increment = 1,
        Value = 5
    };

    /// <summary>
    /// Set configurations here, this function trigger just after load a script
    /// </summary>
    public void ScriptInit()
    {
        Script.Name = "Inset";
        Script.Description = "Performs a black inset around objects";
        Script.Author = "Tiago Conceição";
        Script.Version = new Version(0, 1);
        Script.UserInputs.AddRange(new[]
        {
            InsetMarginFromEdge,
            InsetThickness  
        });
    }

    /// <summary>
    /// Validate user inputs here, this function trigger when user click on execute
    /// </summary>
    /// <returns>A error message, empty or null if validation passes.</returns>
    public string? ScriptValidate()
    {
        StringBuilder sb = new();
            
        if (InsetMarginFromEdge.Value < InsetMarginFromEdge.Minimum)
        {
            sb.AppendLine($"Inset edge margin must be at least {InsetMarginFromEdge.Minimum}{InsetMarginFromEdge.Unit}");
        }
        if (InsetThickness.Value < InsetThickness.Minimum)
        {
            sb.AppendLine($"Inset thickness must be at least {InsetThickness.Minimum}{InsetThickness.Unit}");
        }
            
        return sb.ToString();
    }

    /// <summary>
    /// Execute the script, this function trigger when when user click on execute and validation passes
    /// </summary>
    /// <returns>True if executes successfully to the end, otherwise false.</returns>
    public bool ScriptExecute()
    {
        var kernel =
            CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), EmguExtensions.AnchorCenter); // Rectangle 3x3 kernel
        Progress.Reset("Inset layers", Operation.LayerRangeCount); // Sets the progress name and number of items to process

            
        // Loop user selected layers in parallel, this will put each core of CPU working here on parallel
        Parallel.For(Operation.LayerIndexStart, Operation.LayerIndexEnd+1, CoreSettings.GetParallelOptions(Progress), layerIndex =>
        {
            Progress.PauseIfRequested();
            var layer = SlicerFile[layerIndex]; // Unpack and expose layer variable for easier use
            using var mat = layer.LayerMat;     // Gets this layer mat/image
            var original = mat.Clone();     // Keep a original mat copy
            using var erodeMat = new Mat();     // Creates a temporary mat for the eroded image
            using var wallMat = new Mat();      // Creates a temporary mat for the wall image

            var target = Operation.GetRoiOrDefault(mat); // Get ROI from mat if user selected an region

            // Erode original image by InsetMarginFromEdge pixels, so we get the offset margin from image and put new image on erodeMat
            CvInvoke.Erode(target, erodeMat, kernel, EmguExtensions.AnchorCenter, InsetMarginFromEdge.Value, BorderType.Reflect101, default);

            // Now erode the eroded image with InsetThickness pixels, so we get the original-margin-thickness image and put the new image on wallMat
            CvInvoke.Erode(erodeMat, wallMat, kernel, EmguExtensions.AnchorCenter, InsetThickness.Value, BorderType.Reflect101, default);

            // Subtract walls image from eroded image, so we get only the inset line pixels in white and put back into wallMat
            CvInvoke.Subtract(erodeMat, wallMat, wallMat);

            // Invert pixels of wallMat so the whites will become black and blacks whites
            CvInvoke.BitwiseNot(wallMat, wallMat);

            // Bitwise And original image with the modified image and put back into mat
            // This will keep only the pixels that are positive in both mat's, so mat[n] & wallMat[n] must both have a positive pixel value (> 0)
            CvInvoke.BitwiseAnd(target, wallMat, target);

            // Apply the results only to the selected masked area, if user selected one
            Operation.ApplyMask(original, target);

            // Set current layer image with the modified mat we just manipulated
            layer.LayerMat = mat;

            // Increment progress bar by 1
            Progress.LockAndIncrement();
        });

        // return true if not cancelled by user
        return !Progress.Token.IsCancellationRequested;
    }
}