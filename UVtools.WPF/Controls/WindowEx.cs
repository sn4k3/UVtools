/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Styling;
using UVtools.Core.FileFormats;
using UVtools.WPF.Extensions;

namespace UVtools.WPF.Controls
{
    public class WindowEx : Window, INotifyPropertyChanged, IStyleable
    {
        #region BindableBase
        /// <summary>
        ///     Multicast event for property change notifications.
        /// </summary>
        private PropertyChangedEventHandler _propertyChanged;
        private readonly List<string> events = new();

        public new event PropertyChangedEventHandler PropertyChanged
        {
            add { _propertyChanged += value; events.Add("added"); }
            remove { _propertyChanged -= value; events.Add("removed"); }
        }

        protected bool RaiseAndSetIfChanged<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            RaisePropertyChanged(propertyName);
            return true;
        }


        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
        }

        /// <summary>
        ///     Notifies listeners that a property value has changed.
        /// </summary>
        /// <param name="propertyName">
        ///     Name of the property used to notify listeners.  This
        ///     value is optional and can be provided automatically when invoked from compilers
        ///     that support <see cref="CallerMemberNameAttribute" />.
        /// </param>
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            var e = new PropertyChangedEventArgs(propertyName);
            OnPropertyChanged(e);
            _propertyChanged?.Invoke(this, e);
        }
        #endregion
        
        Type IStyleable.StyleKey => typeof(Window);

        public DialogResults DialogResult { get; set; } = DialogResults.Unknown;
        public enum DialogResults
        {
            Unknown,
            OK,
            Cancel
        }

        public double WindowMaxWidth => this.GetScreenWorkingArea().Width - UserSettings.Instance.General.WindowsHorizontalMargin;

        public double WindowMaxHeight => this.GetScreenWorkingArea().Height - UserSettings.Instance.General.WindowsVerticalMargin;

        public UserSettings Settings => UserSettings.Instance;

        public FileFormat SlicerFile
        {
            get => App.SlicerFile;
            set => App.SlicerFile = value;
        }

        public WindowEx()
        {
#if DEBUG
            this.AttachDevTools(new KeyGesture(Key.F12, KeyModifiers.Control));
#endif
            //TransparencyLevelHint = WindowTransparencyLevel.AcrylicBlur;
        }
        
        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            if (!CanResize && WindowState == WindowState.Normal)
            {
                MaxWidth = WindowMaxWidth;
                MaxHeight = WindowMaxHeight;
            }
        }

        public void CloseWithResult()
        {
            Close(DialogResult);
        }

        public virtual void ResetDataContext(object newObject = null)
        {
            var old = DataContext;
            DataContext = null;
            DataContext = newObject ?? old;
        }
    }
}
