/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */


using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using MessageBox.Avalonia.Enums;
using UVtools.WPF.Extensions;

namespace UVtools.WPF
{
    public partial class MainWindow
    {
        public ListBox ClipboardList;

        public void InitClipboardLayers()
        {
            ClipboardList = this.FindControl<ListBox>(nameof(ClipboardList));
            Clipboard.PropertyChanged += ClipboardOnPropertyChanged;
        }

        private void ClipboardOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Clipboard.CurrentIndex))
            {
                if (Clipboard.CurrentIndex < 0 || Clipboard.SuppressRestore) return;

                AddLogVerbose($"Clipboard change: {Clipboard.CurrentIndex}");

                if (Clipboard.ReallocatedLayerCount)
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
                ClipboardUndo(true);
                return;
            }
            Clipboard.Undo();
        } 

        public async void ClipboardUndo(bool rerun)
        {
            CanSave = true;
            var clip = Clipboard.CurrentClip;
            Clipboard.Undo();
            if (!rerun)
            {
                return;
            }
            if (clip?.Operation is null) return;
            if (clip.Operation.HaveROI)
            {
                ROI = GetTransposedRectangle(clip.Operation.ROI);
            }

            if (clip.Operation.HaveMask)
            {
                AddMaskPoints(clip.Operation.MaskPoints);
            }

            var operation = await ShowRunOperation(clip.Operation.GetType(), clip.Operation);
            if (operation is null)
            {
                Clipboard.Redo();
                CanSave = false;
            }
        }

        public void ClipboardRedo()
        {
            CanSave = true;
            Clipboard.Redo();
        }

        public async void ClipboardClear()
        {
            if (await this.MessageBoxQuestion("Are you sure you want to clear the clipboard?\n" +
                                              "Current layers will be placed as original layers\n" +
                                              "This action is permanent!", "Clear clipboard?") != ButtonResult.Yes) return;
            Clipboard.Clear(true);
        }
    }
}
