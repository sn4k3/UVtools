/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using UVtools.Core.Scripting;

namespace UVtools.ScriptSample;

/// <summary>
/// Change layer properties to random values
/// </summary>
public class ScriptSetLiftHeightSample : ScriptGlobals
{
    readonly ScriptNumericalInput<float> BottomLiftHeight = new()
    {
        Label = "Bottom lift height",
        Unit = "mm",
        Minimum = 0,
        Maximum = 300,
        Increment = 0.5f,
        Value = 0.5f,
        DecimalPlates = 2
    };

    readonly ScriptNumericalInput<float> LiftHeight = new()
    {
        Label = "Lift height",
        Unit = "mm",
        Minimum = 0,
        Maximum = 300,
        Increment = 0.5f,
        Value = 0.5f,
        DecimalPlates = 2
    };

    /// <summary>
    /// Set configurations here, this function trigger just after load a script
    /// </summary>
    public void ScriptInit()
    {
        Script.Name = "Change lift height properties";
        Script.Description = "Change file lift height";
        Script.Author = "Tiago Conceição";
        Script.Version = new Version(0, 1);

        Script.UserInputs.AddRange([BottomLiftHeight , LiftHeight]);
    }

    /// <summary>
    /// Validate user inputs here, this function trigger when user click on execute
    /// </summary>
    /// <returns>A error message, empty or null if validation passes.</returns>
    public string? ScriptValidate()
    {
        return null;
    }

    /// <summary>
    /// Execute the script, this function trigger when when user click on execute and validation passes
    /// </summary>
    /// <returns>True if executes successfully to the end, otherwise false.</returns>
    public bool ScriptExecute()
    {
        SlicerFile.BottomLiftHeight = BottomLiftHeight.Value;
        SlicerFile.LiftHeight = LiftHeight.Value;

        // return true if not cancelled by user
        return !Progress.Token.IsCancellationRequested;
    }
}