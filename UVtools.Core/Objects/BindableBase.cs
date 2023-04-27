/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UVtools.Core.Objects;

/// <summary>
///     Implementation of <see cref="INotifyPropertyChanged" /> to simplify models.
/// </summary>
public abstract class BindableBase : INotifyPropertyChanged
{
    /// <summary>
    ///     Multicast event for property change notifications.
    /// </summary>
    private PropertyChangedEventHandler? _propertyChanged;

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add
        {
            _propertyChanged -= value;
            _propertyChanged += value;
        }
        remove => _propertyChanged -= value;
    }

    public void ClearPropertyChangedListeners()
    {
        _propertyChanged = null;
        /*var invocationList = _propertyChanged?.GetInvocationList();
        if (invocationList is null) return;
        foreach (var t in invocationList)
        {
            _propertyChanged -= (PropertyChangedEventHandler)t;
        }*/
    }

    protected bool RaiseAndSetIfChanged<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        RaisePropertyChanged(propertyName);
        return true;
    }

    protected void RaiseAndSet<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        field = value;
        RaisePropertyChanged(propertyName);
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
}