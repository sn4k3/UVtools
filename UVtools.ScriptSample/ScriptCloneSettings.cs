/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using UVtools.Core.Scripting;
using System.IO;
using System.Collections.Generic;
using UVtools.Core.FileFormats;
using System.Diagnostics;
using System.Linq;

namespace UVtools.ScriptSample;

/// <summary>
/// Performs a black inset around objects
/// </summary>
public class ScriptCloneSettings : ScriptGlobals
{
    readonly ScriptCheckBoxInput Recursive = new()
    {
        Label = "Recursive",
        ToolTip = "If unchecked, only files in the same folder are modified; if checked, files in all sub-folders are modified too",
        Value = true
    };

    readonly ScriptCheckBoxInput Report = new()
    {
        Label = "Generate report file",
        ToolTip = "Optionally generate a report file in the same directory as the open file",
        Value = true
    };

    readonly ScriptCheckBoxInput OpenReport = new()
    {
        Label = "Open report file",
        ToolTip = "Optionally open the result file when completed",
        Value = true
    };

    readonly ScriptOpenFolderDialogInput FolderPath = new()
    {
        Label = "Folder path",
        ToolTip = "The folder path to process",
    };

    private enum ResultStatus
    {
        Exception,
        UnknownFileType,
        DifferentMachineTypes,
        Success,
        Unchanged,
    }

    /// <summary>
    /// Set configurations here, this function trigger just after load a script
    /// </summary>
    public void ScriptInit()
    {
        Script.Name = "Clone Settings";
        Script.Description = "Copies the print settings from the current file to all files in the same directory and, optionally, recursively below it";
        Script.Author = "Gina Venolia";
        Script.Version = new Version(0, 2);
        Script.UserInputs.Add(Recursive);
        Script.UserInputs.Add(Report);
        Script.UserInputs.Add(OpenReport);
        Script.UserInputs.Add(FolderPath);
        FolderPath.Value = SlicerFile.DirectoryPath;
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
        Progress.CanCancel = true;

        // Gather the list of directories to operate on
        Progress.Reset("Processing files...");

        var directoryPaths = new List<string> {Path.GetDirectoryName(SlicerFile.FileFullPath)!};

        if (Recursive.Value)
        {
            for (var i = 0; i < directoryPaths.Count; i++)
            {
                directoryPaths.AddRange(Directory.EnumerateDirectories(directoryPaths[i]));
            }
        }

        // Gather the list of files to operate on
        var filePaths = new List<string>();
        foreach (var directoryPath in directoryPaths)
        {
            filePaths.AddRange(Directory.EnumerateFiles(directoryPath));
        }

        // Except for the file we started with
        var normalizedFilePath = Path.GetFullPath(SlicerFile.FileFullPath!);
        filePaths.RemoveAll(x => Path.GetFullPath(x) == normalizedFilePath);

        // Process the files
        Progress.Reset("Processing files...", (uint)filePaths.Count, 0);

        var results = new List<Tuple<ResultStatus, string, List<string>?>>();
        foreach (var filePath in filePaths)
        {
            if (Progress.Token.IsCancellationRequested) return false;

            Tuple<ResultStatus, string, List<string>?> result;
            try
            {
                result = ProcessFile(filePath);
            }
            catch (Exception ex)
            {
                var deets = new List<string>
                {
                    ex.GetType().Name,
                    ex.Message
                };
                result = new Tuple<ResultStatus, string, List<string>?>(ResultStatus.Exception, filePath, deets);
            }

            results.Add(result);

            Progress++;
        }

        // Generate the report
        if (Report.Value)
        {
            var reportDirectory = Path.GetDirectoryName(SlicerFile.FileFullPath);
            var reportFilePath = Path.Combine(reportDirectory!, "CloneSettingsReport.txt");
            using (var writer = new StreamWriter(reportFilePath))
            {
                writer.WriteLine(Script.Name + " " + Script.Version.ToString() + " by " + Script.Author);
                writer.WriteLine("Source file: " + SlicerFile.FileFullPath);
                writer.WriteLine(DateTime.Now.ToLongDateString() + ", " + DateTime.Now.ToLongTimeString());
                writer.WriteLine("Recursive: " + Recursive.Value.ToString());
                writer.WriteLine("Directories: " + directoryPaths.Count);
                writer.WriteLine("Files: " + filePaths.Count);
                writer.WriteLine();

                foreach (var statusGroup in results.GroupBy(x => x.Item1))
                {
                    writer.WriteLine(statusGroup.Key.ToString());
                    foreach (var result in statusGroup.OrderBy(x => x.Item2))
                    {
                        writer.WriteLine("\t" + result.Item2);
                        if (result.Item3 is not null)
                        {
                            foreach (var detail in result.Item3)
                            {
                                writer.WriteLine("\t\t" + detail);
                            }
                        }
                    }
                }
            }

            if (OpenReport.Value)
            {
                var startInfo = new ProcessStartInfo(reportFilePath)
                {
                    UseShellExecute = true
                };
                Process.Start(startInfo);
            }
        }

        // return true if not cancelled by user
        return true;
    }

    private Tuple<ResultStatus, string, List<string>?> ProcessFile(string filePath)
    {
        // Determine the type of file
        var file = FileFormat.FindByExtensionOrFilePath(filePath, true);
        if (file is null)
        {
            return new Tuple<ResultStatus, string, List<string>?>(ResultStatus.UnknownFileType, filePath, null);
        }

        // Load the file
        file.Decode(filePath);
        if (string.Compare(file.MachineName, SlicerFile.MachineName, true) != 0)
        {
            var deets = new List<string>
            {
                "Source machine type: " + SlicerFile.MachineName,
                "Destination machine type: " + file.MachineName
            };
            return new Tuple<ResultStatus, string, List<string>?>(ResultStatus.DifferentMachineTypes, filePath, deets);
        }

        // TODO: Validate that some parameters are the same in both files?

        // Change the parameters
        var changed = false;
        var details = new List<string>();

        if (file.CanUseBottomLayerCount && file.BottomLayerCount != SlicerFile.BottomLayerCount)
        {
            details.Add($"Bottom Layer Count: {file.BottomLayerCount} to {SlicerFile.BottomLayerCount}");
            file.BottomLayerCount = SlicerFile.BottomLayerCount;
            changed = true;
        }

        if (file.CanUseBottomLightOffDelay && file.BottomLightOffDelay != SlicerFile.BottomLightOffDelay)
        {
            details.Add($"Bottom Light Off Delay: {file.BottomLightOffDelay} to {SlicerFile.BottomLightOffDelay}");
            file.BottomLightOffDelay = SlicerFile.BottomLightOffDelay;
            changed = true;
        }

        if (file.CanUseLightOffDelay && file.LightOffDelay != SlicerFile.LightOffDelay)
        {
            details.Add($"Light Off Delay: {file.LightOffDelay} to {SlicerFile.LightOffDelay}");
            file.LightOffDelay = SlicerFile.LightOffDelay;
            changed = true;
        }

        if (file.CanUseBottomWaitTimeBeforeCure && file.BottomWaitTimeBeforeCure != SlicerFile.BottomWaitTimeBeforeCure)
        {
            details.Add($"Bottom Wait Time Before Cure: {file.BottomWaitTimeBeforeCure} to {SlicerFile.BottomWaitTimeBeforeCure}");
            file.BottomWaitTimeBeforeCure = SlicerFile.BottomWaitTimeBeforeCure;
            changed = true;
        }

        if (file.CanUseWaitTimeBeforeCure && file.WaitTimeBeforeCure != SlicerFile.WaitTimeBeforeCure)
        {
            details.Add($"Wait Time Before Cure: {file.WaitTimeBeforeCure} to {SlicerFile.WaitTimeBeforeCure}");
            file.WaitTimeBeforeCure = SlicerFile.WaitTimeBeforeCure;
            changed = true;
        }

        if (file.CanUseBottomExposureTime && file.BottomExposureTime != SlicerFile.BottomExposureTime)
        {
            details.Add($"Bottom Exposure Time: {file.BottomExposureTime} to {SlicerFile.BottomExposureTime}");
            file.BottomExposureTime = SlicerFile.BottomExposureTime;
            changed = true;
        }

        if (file.CanUseExposureTime && file.ExposureTime != SlicerFile.ExposureTime)
        {
            details.Add($"Exposure Time: {file.ExposureTime} to {SlicerFile.ExposureTime}");
            file.ExposureTime = SlicerFile.ExposureTime;
            changed = true;
        }

        if (file.CanUseBottomWaitTimeAfterCure && file.BottomWaitTimeAfterCure != SlicerFile.BottomWaitTimeAfterCure)
        {
            details.Add($"Bottom Wait Time After Cure: {file.BottomWaitTimeAfterCure} to {SlicerFile.BottomWaitTimeAfterCure}");
            file.BottomWaitTimeAfterCure = SlicerFile.BottomWaitTimeAfterCure;
            changed = true;
        }

        if (file.CanUseWaitTimeAfterCure && file.WaitTimeAfterCure != SlicerFile.WaitTimeAfterCure)
        {
            details.Add($"Wait Time After Cure: {file.WaitTimeAfterCure} to {SlicerFile.WaitTimeAfterCure}");
            file.WaitTimeAfterCure = SlicerFile.WaitTimeAfterCure;
            changed = true;
        }

        if (file.CanUseBottomLiftHeight && file.BottomLiftHeight != SlicerFile.BottomLiftHeight)
        {
            details.Add($"Bottom Lift Height: {file.BottomLiftHeight} to {SlicerFile.BottomLiftHeight}");
            file.BottomLiftHeight = SlicerFile.BottomLiftHeight;
            changed = true;
        }

        if (file.CanUseBottomLiftSpeed && file.BottomLiftSpeed != SlicerFile.BottomLiftSpeed)
        {
            details.Add($"Bottom Lift Speed: {file.BottomLiftSpeed} to {SlicerFile.BottomLiftSpeed}");
            file.BottomLiftSpeed = SlicerFile.BottomLiftSpeed;
            changed = true;
        }

        if (file.CanUseLiftHeight && file.LiftHeight != SlicerFile.LiftHeight)
        {
            details.Add($"Lift Height: {file.LiftHeight} to {SlicerFile.LiftHeight}");
            file.LiftHeight = SlicerFile.LiftHeight;
            changed = true;
        }

        if (file.CanUseLiftSpeed && file.LiftSpeed != SlicerFile.LiftSpeed)
        {
            details.Add($"Lift Speed: {file.LiftSpeed} to {SlicerFile.LiftSpeed}");
            file.LiftSpeed = SlicerFile.LiftSpeed;
            changed = true;
        }

        if (file.CanUseBottomLiftHeight2 && file.BottomLiftHeight2 != SlicerFile.BottomLiftHeight2)
        {
            details.Add($"Bottom Lift Height 2: {file.BottomLiftHeight2} to {SlicerFile.BottomLiftHeight2}");
            file.BottomLiftHeight2 = SlicerFile.BottomLiftHeight2;
            changed = true;
        }

        if (file.CanUseBottomLiftSpeed2 && file.BottomLiftSpeed2 != SlicerFile.BottomLiftSpeed2)
        {
            details.Add($"Bottom Lift Speed2: {file.BottomLiftSpeed2} to {SlicerFile.BottomLiftSpeed2}");
            file.BottomLiftSpeed2 = SlicerFile.BottomLiftSpeed2;
            changed = true;
        }

        if (file.CanUseLiftHeight2 && file.LiftHeight2 != SlicerFile.LiftHeight2)
        {
            details.Add($"Lift Height 2: {file.LiftHeight2} to {SlicerFile.LiftHeight2}");
            file.LiftHeight2 = SlicerFile.LiftHeight2;
            changed = true;
        }

        if (file.CanUseLiftSpeed2 && file.LiftSpeed2 != SlicerFile.LiftSpeed2)
        {
            details.Add($"Lift Speed 2: {file.LiftSpeed2} to {SlicerFile.LiftSpeed2}");
            file.LiftSpeed2 = SlicerFile.LiftSpeed2;
            changed = true;
        }

        if (file.CanUseBottomWaitTimeAfterLift && file.BottomWaitTimeAfterLift != SlicerFile.BottomWaitTimeAfterLift)
        {
            details.Add($"Bottom Wait Time After Lift: {file.BottomWaitTimeAfterLift} to {SlicerFile.BottomWaitTimeAfterLift}");
            file.BottomWaitTimeAfterLift = SlicerFile.BottomWaitTimeAfterLift;
            changed = true;
        }

        if (file.CanUseWaitTimeAfterLift && file.WaitTimeAfterLift != SlicerFile.WaitTimeAfterLift)
        {
            details.Add($"Wait Time After Lift: {file.WaitTimeAfterLift} to {SlicerFile.WaitTimeAfterLift}");
            file.WaitTimeAfterLift = SlicerFile.WaitTimeAfterLift;
            changed = true;
        }

        if (file.CanUseBottomRetractSpeed && file.BottomRetractSpeed != SlicerFile.BottomRetractSpeed)
        {
            details.Add($"Bottom Retract Speed: {file.BottomRetractSpeed} to {SlicerFile.BottomRetractSpeed}");
            file.BottomRetractSpeed = SlicerFile.BottomRetractSpeed;
            changed = true;
        }

        if (file.CanUseRetractSpeed && file.RetractSpeed != SlicerFile.RetractSpeed)
        {
            details.Add($"Retract Speed: {file.RetractSpeed} to {SlicerFile.RetractSpeed}");
            file.RetractSpeed = SlicerFile.RetractSpeed;
            changed = true;
        }

        if (file.CanUseBottomRetractHeight2 && file.BottomRetractHeight2 != SlicerFile.BottomRetractHeight2)
        {
            details.Add($"Bottom Retract Height 2: {file.BottomRetractHeight2} to {SlicerFile.BottomRetractHeight2}");
            file.BottomRetractHeight2 = SlicerFile.BottomRetractHeight2;
            changed = true;
        }

        if (file.CanUseBottomRetractSpeed2 && file.BottomRetractSpeed2 != SlicerFile.BottomRetractSpeed2)
        {
            details.Add($"Bottom Retract Speed2: {file.BottomRetractSpeed2} to {SlicerFile.BottomRetractSpeed2}");
            file.BottomRetractSpeed2 = SlicerFile.BottomRetractSpeed2;
            changed = true;
        }

        if (file.CanUseRetractHeight2 && file.RetractHeight2 != SlicerFile.RetractHeight2)
        {
            details.Add($"Retract Height 2: {file.RetractHeight2} to {SlicerFile.RetractHeight2}");
            file.RetractHeight2 = SlicerFile.RetractHeight2;
            changed = true;
        }

        if (file.CanUseRetractSpeed2 && file.RetractSpeed2 != SlicerFile.RetractSpeed2)
        {
            details.Add($"Retract Speed 2: {file.RetractSpeed2} to {SlicerFile.RetractSpeed2}");
            file.RetractSpeed2 = SlicerFile.RetractSpeed2;
            changed = true;
        }

        if (file.CanUseBottomLightPWM && file.BottomLightPWM != SlicerFile.BottomLightPWM)
        {
            details.Add($"Bottom Light PWM: {file.BottomLightPWM} to {SlicerFile.BottomLightPWM}");
            file.BottomLightPWM = SlicerFile.BottomLightPWM;
            changed = true;
        }

        if (file.CanUseLightPWM && file.LightPWM != SlicerFile.LightPWM)
        {
            details.Add($"Light PWM: {file.LightPWM} to {SlicerFile.LightPWM}");
            file.LightPWM = SlicerFile.LightPWM;
            changed = true;
        }

        // if (file.CanUseXXX && file.XXX != SlicerFile.XXX)
        // {
        //     details.Add($"XXX: {file.XXX} to {SlicerFile.XXX}");
        //     file.XXX = SlicerFile.XXX;
        //     changed = true;
        // }

        // Bail out if not changed
        if (!changed)
        {
            return new Tuple<ResultStatus, string, List<string>?>(ResultStatus.Unchanged, filePath, null);
        }

        // Save the changed file
        file.Save();

        return new Tuple<ResultStatus, string, List<string>?>(ResultStatus.Success, filePath, details);
    }

}