/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;

namespace UVtools.Core.Objects;

public class ValueDescription : BindableBase, IEquatable<ValueDescription>
{
    private object? _value;
    private string? _description;

    public object? Value
    {
        get => _value;
        set => RaiseAndSetIfChanged(ref _value, value);
    }

    public string? Description
    {
        get => _description;
        set => RaiseAndSetIfChanged(ref _description, value);
    }

    public string ValueAsString
    {
        get => Value?.ToString() ?? string.Empty;
        set => Value = value;
    } 

    public ValueDescription(object value, string? description = null)
    {
        Value = value;
        Description = description;
    }
    public ValueDescription(object value, object? description = null)
    {
        Value = value;
        Description = description?.ToString();
    }

    public bool Equals(ValueDescription? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals(_value, other._value) && _description == other._description;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ValueDescription) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_value, _description);
    }

    public override string? ToString()
    {
        return Description;
    }
}