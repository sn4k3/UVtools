/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using UVtools.Core.Extensions;
using ZLinq;

namespace UVtools.Core.Objects;

public sealed class ReflectionPropertyValue
{
    public string Name { get; init; }
    public string Value { get; init; }
    public bool Found { get; private set; }

    public ReflectionPropertyValue(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public void Deconstruct(out string name, out string value)
    {
        name = Name;
        value = Value;
    }

    public void SetFound() => Found = true;

    private bool Equals(ReflectionPropertyValue other)
    {
        return Name == other.Name && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is ReflectionPropertyValue other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Value);
    }

    public override string ToString()
    {
        return $"{Name}={Value}";
    }

    public static uint SetProperties(object obj, IEnumerable<ReflectionPropertyValue> properties)
    {
        if (!properties.AsValueEnumerable().Any()) return 0;
        uint count = 0;
        foreach (var propertyInfo in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            foreach (var property in properties)
            {
                if (propertyInfo.Name != property.Name) continue;
                propertyInfo.SetValueFromString(obj, property.Value);
                property.Found = true;
                count++;
            }
        }

        return count;
    }

    public static bool ParseFromString(string line, out ReflectionPropertyValue? property)
    {
        property = null;
        var split = line.Split(['=', ':'], 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (split.Length < 2) return false;
        property = new(split[0], split[1]);
        return true;
    }

    public static List<ReflectionPropertyValue> ParseFromString(IEnumerable<string> lines)
    {
        var result = new List<ReflectionPropertyValue>();
        foreach (var line in lines)
        {
            if (ParseFromString(line, out var propertyValue)) result.Add(propertyValue!);
        }

        return result;
    }
}