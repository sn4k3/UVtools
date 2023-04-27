/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UVtools.Core.FileFormats;
using UVtools.Core.SystemOS;
using UVtools.WPF.Extensions;
using Size = Avalonia.Size;

namespace UVtools.WPF.Controls;

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
        add { _propertyChanged -= value; _propertyChanged += value; events.Add("added"); }
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

    #region Enum

    public enum WindowConstrainsMaxSizeType : byte
    {
        None,
        UserSettings,
        Ratio
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

    public Size WindowMaxSize
    {
        get
        {
            var size = this.GetScreenWorkingArea();
            return new Size(size.Width - UserSettings.Instance.General.WindowsHorizontalMargin,
                this.GetScreenWorkingArea().Height - UserSettings.Instance.General.WindowsVerticalMargin);
        }
    }

    public WindowConstrainsMaxSizeType WindowConstrainMaxSize { get; set; } = WindowConstrainsMaxSizeType.UserSettings;

    public double WindowsWidthMaxSizeRatio { get; set; } = 1;
    public double WindowsHeightMaxSizeRatio { get; set; } = 1;

    public bool IsDebug
    {
        get => Program.IsDebug;
        set => Program.IsDebug = value;
    }

    public UserSettings Settings => UserSettings.Instance;

    public virtual FileFormat SlicerFile
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

    protected override void OnInitialized()
    {
        ConstainsWindowMaxSize();
        base.OnInitialized();
    }

    protected override void OnOpened(EventArgs e)
    {
        // Fix positions and sizes
        if (WindowState == WindowState.Normal)
        {
            if (Position.X < 0) Position = Position.WithX(0);
            if (Position.Y < 0) Position = Position.WithY(0);

            if ((SizeToContent & SizeToContent.Height) == 0)
            {
                var workingArea = this.GetScreenWorkingArea();
                if (Bounds.Bottom > workingArea.Height)
                {
                    Height = workingArea.Height - Position.Y;
                }
            }
        }

        //Debug.WriteLine("OnOpened");
        base.OnOpened(e);
    }

    public void ConstainsWindowMaxSize()
    {
        /*if (WindowStartupLocation == WindowStartupLocation.CenterOwner && Owner is null)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }*/

        if (!CanResize && WindowState == WindowState.Normal && WindowConstrainMaxSize != WindowConstrainsMaxSizeType.None)
        {
            switch (WindowConstrainMaxSize)
            {
                case WindowConstrainsMaxSizeType.UserSettings:
                    MaxWidth = WindowMaxWidth;
                    MaxHeight = WindowMaxHeight;
                    break;
                case WindowConstrainsMaxSizeType.Ratio:
                    var size = this.GetScreenWorkingArea();
                    if (WindowsWidthMaxSizeRatio is > 0 and < 1) MaxWidth = size.Width * WindowsWidthMaxSizeRatio;
                    if (WindowsHeightMaxSizeRatio is > 0 and < 1) MaxHeight = size.Height * WindowsHeightMaxSizeRatio;
                    break;
            }
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

    public void OpenWebsite(string url)
    {
        SystemAware.OpenBrowser(url);
    }
}