/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using Avalonia;
using Avalonia.Controls;
using System;
using System.IO;
using Avalonia.Platform.Storage;
using UVtools.Core.Dialogs;
using UVtools.Core.SystemOS;
using UVtools.WPF.Extensions;
using AvaloniaStatic = UVtools.WPF.Controls.AvaloniaStatic;

namespace UVtools.WPF;

public partial class MainWindow
{
    public bool HaveGCode => IsFileLoaded && SlicerFile.SupportsGCode;

    public uint GCodeLines => !HaveGCode ? 0 : SlicerFile.GCode.LineCount;

    public void OnClickRebuildGcode()
    {
        if (!HaveGCode) return;
        var temp = SlicerFile.SuppressRebuildGCode;
        SlicerFile.SuppressRebuildGCode = false;
        SlicerFile.RebuildGCode();
        SlicerFile.SuppressRebuildGCode = temp;
        RaisePropertyChanged(nameof(GCodeLines));

        CanSave = true;
    }

    public async void OnClickGCodeSaveFile()
    {
        if (!HaveGCode) return;

        using var file = await SaveFilePickerAsync(SlicerFile.DirectoryPath, $"{SlicerFile.FilenameNoExt}_gcode.txt", AvaloniaStatic.TxtFileFilter);
        if (file?.TryGetLocalPath() is not { } filePath) return;

        try
        {
            await using TextWriter tw = new StreamWriter(filePath);
            await tw.WriteAsync(SlicerFile.GCodeStr);
            tw.Close();
        }
        catch (Exception e)
        {
            await this.MessageBoxError(e.ToString(), "Error occur while save gcode");
            return;
        }

        var result = await this.MessageBoxQuestion(
            "GCode save was successful. Do you want open the file in the default editor?",
            "GCode save complete");
        if (result != MessageButtonResult.Yes) return;

        SystemAware.StartProcess(filePath);
    }

    public void OnClickGCodeSaveClipboard()
    {
        if (!HaveGCode) return;
        Clipboard?.SetTextAsync(SlicerFile.GCodeStr);
    }
}