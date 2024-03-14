/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using UVtools.Core.Extensions;
using UVtools.Core.Scripting;

namespace UVtools.ScriptSample;

/// <summary>
/// Change layer properties to random values
/// </summary>
public class ScriptDebandingZSample : ScriptGlobals
{
    readonly ScriptCheckBoxInput CreateEmptyLayerInput = new()
    {
        Label = "Create a first empty layer to overcome printer firmware limitation",
        ToolTip = "Some printers will not respect wait time for the first layer, introducing the problem once again. Use this option to by pass that",
        Value = true
    };

    readonly ScriptNumericalInput<decimal> BottomSafeDebandingHeightInput = new()
    {
        Label = "Safe height for the large debanding time",
        //ToolTip = "Margin in pixels to inset from object edge",
        Unit = "mm",
        Minimum = 0.1m,
        Maximum = 50,
        Increment = 0.5m,
        Value = 0.5m,
        DecimalPlates = 2,
    };

    readonly ScriptNumericalInput<decimal> BottomWaitTimeBeforeCureInput = new()
    {
        Label = "Large debanding wait time before cure",
        ToolTip = "Time to wait before cure a debanding layer",
        Unit = "s",
        Minimum = 5,
        Maximum = 300,
        Increment = 1,
        Value = 40,
        DecimalPlates = 2,
    };

    readonly ScriptNumericalInput<decimal> NormalWaitTimeBeforeCureInput = new()
    {
        Label = "Normal wait time before cure",
        ToolTip = "Time to wait before cure a normal layer",
        Unit = "s",
        Minimum = 1,
        Maximum = 300,
        Increment = 1,
        Value = 3,
        DecimalPlates = 2,
    };

    readonly ScriptNumericalInput<decimal> BottomWaitTimeAfterCureInput = new()
    {
        Label = "Bottom wait time after cure",
        ToolTip = "Time to wait after cure a bottom layer",
        Unit = "s",
        Minimum = 0,
        Maximum = 100,
        Increment = 1,
        Value = 5,
        DecimalPlates = 2,
    };

    readonly ScriptNumericalInput<decimal> NormalWaitTimeAfterCureInput = new()
    {
        Label = "Normal wait time after cure",
        ToolTip = "Time to wait after cure a normal layer",
        Unit = "s",
        Minimum = 0,
        Maximum = 100,
        Increment = 1,
        Value = 2,
        DecimalPlates = 2,
    };

    /// <summary>
    /// Set configurations here, this function trigger just after load a script
    /// </summary>
    public void ScriptInit()
    {
        Script.Name = "Debanding Z with wait time";
        Script.Description = "Applies wait time at certain layers to help layer adhesion and debanding the Z axis.\n" +
                             "Based on the guide: https://bit.ly/3nkXAOa\n\n" +
                             "NOTE: Do not use this script! it's outdated and replaced by inbuilt UVtools suggestion (Wait time before cure).\n" +
                             "Check the Shield icon/tab and look for 'Wait time before cure' suggestion, you can also configure it and offers more possibilities.";
        Script.Author = "Tiago Conceição";
        Script.Version = new Version(0, 3);
        if (SlicerFile.SupportGCode) CreateEmptyLayerInput.Value = false;
        Script.UserInputs.Add(CreateEmptyLayerInput);
        Script.UserInputs.Add(BottomSafeDebandingHeightInput);
        Script.UserInputs.Add(BottomWaitTimeBeforeCureInput);
        Script.UserInputs.Add(NormalWaitTimeBeforeCureInput);
        if(SlicerFile.CanUseBottomWaitTimeAfterCure) Script.UserInputs.Add(BottomWaitTimeAfterCureInput);
        if(SlicerFile.CanUseWaitTimeAfterCure) Script.UserInputs.Add(NormalWaitTimeAfterCureInput);
    }

    /// <summary>
    /// Validate user inputs here, this function trigger when user click on execute
    /// </summary>
    /// <returns>A error message, empty or null if validation passes.</returns>
    public string? ScriptValidate()
    {
        return SlicerFile.CanUseAnyLightOffDelay || SlicerFile.CanUseAnyWaitTimeBeforeCure ? null : "Your printer/file format is not supported.";
    }

    /// <summary>
    /// Execute the script, this function trigger when when user click on execute and validation passes
    /// </summary>
    /// <returns>True if executes successfully to the end, otherwise false.</returns>
    public bool ScriptExecute()
    {
        throw new NotSupportedException(
            "Do not use this script! it's outdated and replaced by inbuilt UVtools suggestion (Wait time before cure).\n" +
            "Check the Shield icon/tab and look for 'Wait time before cure' suggestion, you can also configure it and offers more possibilities.");
#pragma warning disable CS0162 // Unreachable code detected
        Progress.Reset("Changing layers", Operation.LayerRangeCount); // Sets the progress name and number of items to process
#pragma warning restore CS0162 // Unreachable code detected

        if (SlicerFile.CanUseAnyWaitTime)
        {
            SlicerFile.BottomLightOffDelay = 0;
            SlicerFile.LightOffDelay = 0;
            SlicerFile.BottomWaitTimeBeforeCure = (float) BottomWaitTimeBeforeCureInput.Value;
            SlicerFile.WaitTimeBeforeCure = (float)NormalWaitTimeBeforeCureInput.Value;
        }
        else
        {
            SlicerFile.SetBottomLightOffDelay((float)BottomWaitTimeBeforeCureInput.Value);
            SlicerFile.SetNormalLightOffDelay((float)NormalWaitTimeBeforeCureInput.Value);
        }

        if (SlicerFile.CanUseBottomWaitTimeAfterCure) SlicerFile.BottomWaitTimeAfterCure = (float) BottomWaitTimeAfterCureInput.Value;
        if (SlicerFile.CanUseWaitTimeAfterCure) SlicerFile.WaitTimeAfterCure = (float)NormalWaitTimeAfterCureInput.Value;

        foreach (var layer in SlicerFile)
        {
            if((decimal)layer.PositionZ > BottomSafeDebandingHeightInput.Value) break;
            layer.SetWaitTimeBeforeCureOrLightOffDelay((float) BottomWaitTimeBeforeCureInput.Value);
        }

        if (CreateEmptyLayerInput.Value)
        {
            var firstLayer = SlicerFile.FirstLayer;
            if (firstLayer is not null)
            {
                if (!firstLayer.IsDummy) // First layer is not blank as it seems, lets create one
                {
                    firstLayer = firstLayer.Clone();
                    using var mat = EmguExtensions.InitMat(SlicerFile.Resolution);
                    var pixelPos = firstLayer.BoundingRectangle.Center();
                    mat.SetByte(pixelPos.X, pixelPos.Y,
                        1); // Print a very fade pixel to ignore empty layer detection
                    firstLayer.LayerMat = mat;
                    firstLayer.ExposureTime = SlicerFile.SupportGCode ? 0 : 0.05f;
                    firstLayer.SetNoDelays();
                    SlicerFile.SuppressRebuildPropertiesWork(() => { SlicerFile.Prepend(firstLayer); });
                }
            }
        }

        // return true if not cancelled by user
        return !Progress.Token.IsCancellationRequested;
    }
}