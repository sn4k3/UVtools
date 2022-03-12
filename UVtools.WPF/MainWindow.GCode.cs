/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using MessageBox.Avalonia.Enums;
using UVtools.Core.SystemOS;
using UVtools.WPF.Extensions;
using Helpers = UVtools.WPF.Controls.Helpers;

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

        var dialog = new SaveFileDialog
        {
            Filters = Helpers.IniFileFilter,
            Directory = Path.GetDirectoryName(SlicerFile.FileFullPath),
            InitialFileName = $"{Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath)}_gcode.txt"
        };
        var file = await dialog.ShowAsync(this);

        if (string.IsNullOrEmpty(file)) return;

        try
        {
            await using TextWriter tw = new StreamWriter(file);
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
        if (result != ButtonResult.Yes) return;

        SystemAware.StartProcess(file);
    }

    public void OnClickGCodeSaveClipboard()
    {
        if (!HaveGCode) return;
        Application.Current.Clipboard.SetTextAsync(SlicerFile.GCodeStr);
    }
}