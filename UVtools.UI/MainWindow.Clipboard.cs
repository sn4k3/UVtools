/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */


using Avalonia.Input;
using Avalonia.Threading;
using System;
using System.ComponentModel;
using UVtools.Core.Dialogs;
using UVtools.UI.Extensions;

namespace UVtools.UI;

public partial class MainWindow
{

    public void InitClipboardLayers()
    {
        ClipboardManager.PropertyChanged += ClipboardOnPropertyChanged;
    }

    private void ClipboardOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ClipboardManager.CurrentIndex))
        {
            if (ClipboardManager.CurrentIndex < 0 || ClipboardManager.SuppressRestore) return;

            AddLogVerbose($"Clipboard change: {ClipboardManager.CurrentIndex}");

            if (ClipboardManager.ReallocatedLayerCount)
            {
                DispatcherTimer.RunOnce(() =>
                {
                    RefreshProperties();
                    ResetDataContext();
                }, TimeSpan.FromMilliseconds(1));
            }

            ShowLayer();
            return;
        }
    }

    public void ClipboardUndo()
    {
        CanSave = true;
        if ((_globalModifiers & KeyModifiers.Shift) != 0)
        {
            ClipboardUndoAndRerun(true);
            return;
        }
        ClipboardManager.Undo();
    } 

    public async void ClipboardUndoAndRerun(bool rerun)
    {
        CanSave = true;
        var clip = ClipboardManager.CurrentClip;
        ClipboardManager.Undo();
        if (!rerun)
        {
            return;
        }
        if (clip?.Operation is null) return;
        /*if (clip.Operation.HaveROI)
        {
            ROI = GetTransposedRectangle(clip.Operation.ROI);
        }

        if (clip.Operation.HaveMask)
        {
            AddMaskPoints(clip.Operation.MaskPoints);
        }*/

        var operation = await ShowRunOperation(clip.Operation.GetType(), clip.Operation);
        if (operation is null)
        {
            ClipboardManager.Redo();
            CanSave = false;
        }
    }

    public void ClipboardRedo()
    {
        CanSave = true;
        ClipboardManager.Redo();
    }

    public async void ClipboardClear()
    {
        if (await this.MessageBoxQuestion("Are you sure you want to clear the clipboard?\n" +
                                          "Current layers will be placed as original layers\n" +
                                          "This action is permanent!", "Clear clipboard?") != MessageButtonResult.Yes) return;
        ClipboardManager.Clear(true);
    }
}