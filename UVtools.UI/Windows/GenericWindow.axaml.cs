using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SukiUI.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UVtools.Core.FileFormats;

namespace UVtools.UI.Windows;

public partial class GenericWindow : SukiWindow, INotifyPropertyChanged
{
    public GenericWindow()
    {
        InitializeComponent();
    }

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

    public DialogResults DialogResult { get; set; } = DialogResults.Unknown;
    public enum DialogResults
    {
        Unknown,
        OK,
        Cancel
    }

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