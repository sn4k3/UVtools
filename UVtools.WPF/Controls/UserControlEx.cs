using Avalonia.Controls;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UVtools.Core.FileFormats;

namespace UVtools.WPF.Controls;

public class UserControlEx : UserControl, INotifyPropertyChanged
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

    public FileFormat SlicerFile => App.SlicerFile;

    public void ResetDataContext()
    {
        var old = DataContext;
        DataContext = new object();
        DataContext = old;
    }
}