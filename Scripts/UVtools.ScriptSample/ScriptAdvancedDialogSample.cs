/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Threading;
using UVtools.Core.Dialogs;
using UVtools.Core.Exceptions;
using UVtools.Core.Managers;
using UVtools.Core.Scripting;

namespace UVtools.ScriptSample;

/// <summary>
/// Change layer properties to random values
/// </summary>
public class ScriptAdvancedDialogSample : ScriptGlobals
{
    readonly ScriptNumericalInput<byte> Iterations = new()
    {
        Label = "Number of iterations to run",
        Unit = "iterations",
        Minimum = 1,
        Maximum = byte.MaxValue,
        Increment = 1,
        Value = 4,
    };

    /// <summary>
    /// Set configurations here, this function trigger just after load a script
    /// </summary>
    public void ScriptInit()
    {
        Script.Name = "Tests advanced dialogs";
        Script.Description = "This script does nothing other than show advanced dialogs and progress";
        Script.Author = "Tiago Conceição";
        Script.Version = new Version(0, 1);
        Script.MinimumVersionToRun = new Version(3, 11, 0); // Advanced dialogs started here

        Script.UserInputs.Add(Iterations);
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
        Progress.Reset("Some work", Iterations.Value); // Sets the progress name and number of items to process

        // Trigger an message box to user, will also show in console runs but in text form
        var result = MessageBoxManager.Standard.ShowDialog("This is my script",
            "Script is about to start, are you sure you want to continue?\n" +
            "This will destroy your file!", AbstractMessageBoxStandard.MessageButtons.YesNo).Result;

        // throw error without stack trace
        if (result != AbstractMessageBoxStandard.MessageButtonResult.Yes) throw new MessageException("User wanted to abort the script :(");

        // Write some text to show after the operation has completed with success
        Operation.AfterCompleteReport = "My operation has performed the following changes:\n";


        for (int i = 0; i < Iterations.Value; i++)
        {
            Progress.PauseOrCancelIfRequested();

            Thread.Sleep(1000);
            Progress.Log = $"Task {i}: Completed!\n{Progress.Log}";
            Operation.AfterCompleteReport += $"- Task {i}: Waited for 1s\n";
            Progress.LockAndIncrement();
        }

        Thread.Sleep(1000);

        // return true if not cancelled by user
        return !Progress.Token.IsCancellationRequested;
    }
}