/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;
using UVtools.Core.Scripting;

namespace UVtools.ScriptSample;

/// <summary>
/// Change layer properties to random values
/// </summary>
public class ScriptTimelapseSample : ScriptGlobals
{
    readonly ScriptNumericalInput<float> InputPositionZ = new()
    {
        Label = "Z position to lift to",
        Unit = "mm",
        Minimum = 0.01f,
        Maximum = 1000,
        Increment = 1f,
        Value = 0.5f,
        DecimalPlates = 2
    };

    readonly ScriptNumericalInput<ushort> InputRaiseEveryLayerN = new()
    {
        Label = "Raise every",
        Unit = "layer(s)",
        Minimum = 1,
        Maximum = 1000,
        Increment = 1,
        Value = 10,
        DecimalPlates = 0
    };

    readonly ScriptNumericalInput<float> InputWaitTime = new()
    {
        Label = "Time to wait on still position",
        Unit = "s",
        ToolTip = "Note: Not always possible to wait in some cases",
        Minimum = 0,
        Maximum = 30,
        Increment = 1,
        Value = 2,
        DecimalPlates = 2
    };

    readonly ScriptToggleSwitchInput InputUseVirtualLayer = new()
    {
        OnText = "Use blank layers to go to the target height",
        OffText = "Use lift movement to go to the target height",
        ToolTip = "Use this option if you printer is unable to use large lifts or waits after lift"
    };

    readonly ScriptNumericalInput<float> InputLiftSpeed = new()
    {
        Label = "Virtual layer lift speed",
        Unit = "mm/min",
        Minimum = 50,
        Maximum = 1000,
        Increment = 10,
        Value = 200,
        DecimalPlates = 2
    };

    readonly ScriptNumericalInput<float> InputRetractSpeed = new()
    {
        Label = "Virtual layer retract speed",
        Unit = "mm/min",
        Minimum = 50,
        Maximum = 1000,
        Increment = 10,
        Value = 200,
        DecimalPlates = 2
    };

    /// <summary>
    /// Set configurations here, this function trigger just after load a script
    /// </summary>
    public void ScriptInit()
    {
        Script.Name = "Timelapse position setter";
        Script.Description = "Raises the build platform to a set position to take a timelapse photo every n layers.\n" +
                             "Do not execute this script twice!";
        Script.Author = "Tiago Conceição";
        Script.Version = new Version(0, 1);
        Script.MinimumVersionToRun = new Version(3, 0, 0);

        InputPositionZ.Value = (float)Math.Round(SlicerFile.PrintHeight + 1, 2);
        InputPositionZ.Minimum = (float) Math.Round(SlicerFile.PrintHeight + 0.1, 2);
        Script.UserInputs.Add(InputPositionZ);
        Script.UserInputs.Add(InputRaiseEveryLayerN);
        Script.UserInputs.Add(InputWaitTime);

        if (!SlicerFile.SupportsGCode)
        {
            InputUseVirtualLayer.Value = true;
        }

        if (SlicerFile.CanUseLayerLiftHeight)
        {
            Script.UserInputs.Add(InputUseVirtualLayer);
        }
        else
        {
            InputUseVirtualLayer.Value = true; // Must use layer height
        }

        Script.UserInputs.Add(InputLiftSpeed);
        Script.UserInputs.Add(InputRetractSpeed);
    }

    /// <summary>
    /// Validate user inputs here, this function trigger when user click on execute
    /// </summary>
    /// <returns>A error message, empty or null if validation passes.</returns>
    public string? ScriptValidate()
    {
        if (!SlicerFile.SupportPerLayerSettings) return "This script is not compatible with your printer / file format";
        if (InputPositionZ.Value <= SlicerFile.PrintHeight) return $"{InputPositionZ.Label} must be greater than {SlicerFile.PrintHeight}mm";
        return null;
    }

    /// <summary>
    /// Execute the script, this function trigger when when user click on execute and validation passes
    /// </summary>
    /// <returns>True if executes successfully to the end, otherwise false.</returns>
    public bool ScriptExecute()
    {
        if (InputUseVirtualLayer.Value)
        {
            using var mat = EmguExtensions.InitMat(SlicerFile.Resolution);
            var pixelPos = SlicerFile.BoundingRectangle.Center();
            mat.SetByte(pixelPos.X, pixelPos.Y, 1); // Print a very fade pixel to ignore empty layer detection
            var layer = new Layer(SlicerFile.LayerCount, mat, SlicerFile)
            {
                PositionZ = InputPositionZ.Value,
                ExposureTime = SlicerFile.SupportsGCode ? 0 : 0.05f,
                LiftSpeed = InputLiftSpeed.Value,
                RetractSpeed = InputRetractSpeed.Value
            };

            if (InputWaitTime.Value > 0)
            {
                if (SlicerFile.CanUseWaitTimeBeforeCure)
                {
                    layer.WaitTimeBeforeCure = InputWaitTime.Value;
                }
                else
                {
                    layer.ExposureTime = InputWaitTime.Value; 
                }
            }


            SlicerFile.SuppressRebuildPropertiesWork(() =>
            {
                uint createdLayers = 0;
                for (uint layerIndex = Math.Max(1, Operation.LayerIndexStart + InputRaiseEveryLayerN.Value); layerIndex <= Operation.LayerIndexEnd; layerIndex += InputRaiseEveryLayerN.Value)
                {
                    SlicerFile.Insert((int)(layerIndex + createdLayers), layer.Clone());
                    createdLayers++;
                    Progress.ProcessedItems = layerIndex;
                }
            });
                
        }
        else
        {
            for (uint layerIndex = Math.Max(1, Operation.LayerIndexStart + InputRaiseEveryLayerN.Value - 1); layerIndex <= Operation.LayerIndexEnd; layerIndex += InputRaiseEveryLayerN.Value)
            {
                var layer = SlicerFile[layerIndex];
                layer.LiftHeightTotal = Math.Max(SlicerFile.LiftHeightTotal, InputPositionZ.Value - layer.PositionZ);
                if (SlicerFile.CanUseLayerWaitTimeAfterLift && InputWaitTime.Value > 0)
                {
                    layer.WaitTimeAfterLift = InputWaitTime.Value;
                }
                Progress.ProcessedItems = layerIndex;
            }
        }

        // return true if not cancelled by user
        return !Progress.Token.IsCancellationRequested;
    }
}