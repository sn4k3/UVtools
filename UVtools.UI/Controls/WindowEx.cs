/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using UVtools.Core.FileFormats;
using UVtools.UI.Extensions;
using Size = Avalonia.Size;

namespace UVtools.UI.Controls;

public class WindowEx : Window, INotifyPropertyChanged
{
    #region BindableBase
    /// <summary>
    ///     Multicast event for property change notifications.
    /// </summary>
    private PropertyChangedEventHandler? _propertyChanged;

    public new event PropertyChangedEventHandler? PropertyChanged
    {
        add { _propertyChanged -= value; _propertyChanged += value; }
        remove => _propertyChanged -= value;
    }

    protected bool RaiseAndSetIfChanged<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
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
    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
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

    protected override Type StyleKeyOverride => typeof(Window);

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

    public virtual FileFormat? SlicerFile
    {
        get => App.SlicerFile;
        set => App.SlicerFile = value;
    }

    public WindowEx()
    {
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

    public virtual void ResetDataContext(object? newObject = null)
    {
        var old = DataContext;
        DataContext = null;
        DataContext = newObject ?? old;
    }

    public void OpenWebsite(object url)
    {
        GetTopLevel(this)!.Launcher.LaunchUriAsync(new Uri(url.ToString()!));
        //SystemAware.OpenBrowser((string)url);
    }

    public void OpenWebsite(string url)
    {
        GetTopLevel(this)!.Launcher.LaunchUriAsync(new Uri(url));
        //SystemAware.OpenBrowser(url);
    }

    public void OpenContextMenu(object name) => OpenContextMenu(name.ToString()!);
    public void OpenContextMenu(string name)
    {
        var menu = this.FindControl<ContextMenu>($"{name}ContextMenu");
        if (menu is null) return;
        var parent = this.FindControl<Button>($"{name}Button");
        if (parent is null) return;
        menu.Open(parent);
    }



    public async Task<IStorageFile?> SaveFilePickerAsync(string? directory, string? fileName, IReadOnlyList<FilePickerFileType>? filters, bool showOverwritePrompt = true)
    {
        IStorageFolder? storageFolder = null;
        if (directory is not null)
        {
            storageFolder = await StorageProvider.TryGetFolderFromPathAsync(directory);
        }

        return await SaveFilePickerAsync(storageFolder, fileName, filters, showOverwritePrompt);
    }

    public Task<IStorageFile?> SaveFilePickerAsync(FileFormat slicerFile, IReadOnlyList<FilePickerFileType>? filters, bool showOverwritePrompt = true)
    {
        return SaveFilePickerAsync(slicerFile.DirectoryPath, slicerFile.FilenameNoExt, filters, showOverwritePrompt);
    }


    public Task<IStorageFile?> SaveFilePickerAsync(IStorageFolder? directory, string? fileName, IReadOnlyList<FilePickerFileType>? filters, bool showOverwritePrompt = true)
    {
        string? defaultExt = null;
        if (filters?.Count > 0 && filters[0].Patterns?.Count > 0)
        {
            defaultExt = filters[0].Patterns![0][2..];
        }
        
        return StorageProvider.SaveFilePickerAsync(new()
        {
            SuggestedStartLocation = directory,
            SuggestedFileName = fileName,
            FileTypeChoices = filters,
            DefaultExtension = defaultExt,
            ShowOverwritePrompt = showOverwritePrompt
        });
    }

    public Task<IStorageFile?> SaveFilePickerAsync(string? fileName, IReadOnlyList<FilePickerFileType>? filters, bool showOverwritePrompt = true)
    {
        IStorageFolder? folder = null;
        return SaveFilePickerAsync(folder, fileName, filters, showOverwritePrompt);
    }

    public Task<IStorageFile?> SaveFilePickerAsync(IReadOnlyList<FilePickerFileType>? filters, bool showOverwritePrompt = true)
    {
        IStorageFolder? folder = null;
        return SaveFilePickerAsync(folder, null, filters, showOverwritePrompt);
    }

    public Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(IStorageFolder? directory = null, IReadOnlyList<FilePickerFileType>? filters = null, string? title = null, bool allowMultiple = false)
    {
        return StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            SuggestedStartLocation = directory,
            Title = title,
            AllowMultiple = allowMultiple,
            FileTypeFilter = filters
        });
    }

    public async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(string? directory, IReadOnlyList<FilePickerFileType>? filters = null, string? title = null, bool allowMultiple = false)
    {
        IStorageFolder? storageFolder = null;
        if (!string.IsNullOrEmpty(directory))
        {
            storageFolder = await StorageProvider.TryGetFolderFromPathAsync(directory);
        }

        return await OpenFilePickerAsync(storageFolder, filters, title, allowMultiple);
    }

    public Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(IReadOnlyList<FilePickerFileType>? filters, string? title = null, bool allowMultiple = false)
    {
        IStorageFolder? storageFolder = null;
        return OpenFilePickerAsync(storageFolder, filters, title, allowMultiple);
    }
    
    public Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(IStorageFolder? directory = null, string? title = null, bool allowMultiple = false)
    {
        return StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            SuggestedStartLocation = directory,
            Title = title,
            AllowMultiple = allowMultiple
        });
    }

    public async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(string? directory, string? title = null, bool allowMultiple = false)
    {
        IStorageFolder? storageFolder = null;
        if (!string.IsNullOrEmpty(directory))
        {
            storageFolder = await StorageProvider.TryGetFolderFromPathAsync(directory);
        }
        return await OpenFolderPickerAsync(storageFolder, title, allowMultiple);
    }

    public Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FileFormat slicerFile, string? title = null, bool allowMultiple = false)
    {
        return OpenFolderPickerAsync(slicerFile.DirectoryPath, title, allowMultiple);
    }
}