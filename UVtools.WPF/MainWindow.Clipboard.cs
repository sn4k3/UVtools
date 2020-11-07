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
                if (Clipboard.CurrentIndex < 0 || Clipboard.ChangedByClip) return;

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

        public async void ClipboardClear()
        {
            if (await this.MessageBoxQuestion("Are you sure you want to clear the clipboard?\n" +
                                              "Current layers will be placed as original layers\n" +
                                              "This action is permanent!", "Clear clipboard?") != ButtonResult.Yes) return;
            Clipboard.Clear(true);
        }
    }
}
