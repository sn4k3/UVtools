/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Globalization;
using System.Reflection;

namespace UVtools.Core.Extensions;

public static class ReflectionExtensions
{
    public static bool SetValueFromString(this PropertyInfo attribute, object obj, string value)
    {
        if (attribute.PropertyType == typeof(string))
        {
            attribute.SetValue(obj, value.Convert<string>());
            return true;
        }

        if (string.IsNullOrEmpty(value)) return false;

        if (attribute.PropertyType.IsEnum)
        {
            if (Enum.TryParse(attribute.PropertyType, value, true, out var enumValue))
            {
                attribute.SetValue(obj, Enum.Parse(attribute.PropertyType, enumValue?.ToString() ?? string.Empty));
                return true;
            }

            throw new ArgumentException($"The requested enum name '{value}' was not found.\nAvailable names: ({string.Join(", ", Enum.GetNames(attribute.PropertyType))}).");
        }

        if (attribute.PropertyType == typeof(bool))
        {
            //if (value == "!") attribute.SetValue(obj, !bool.Parse(attribute.GetValue(obj).ToString()));
            if (value.Length == 1 && char.IsDigit(value[0])) attribute.SetValue(obj, value[0] != '0');
            else attribute.SetValue(obj, value.Equals("true", StringComparison.OrdinalIgnoreCase));
            return true;
        }

        if (attribute.PropertyType == typeof(byte))
        {
            if (value.Equals("true", StringComparison.OrdinalIgnoreCase)) attribute.SetValue(obj, (byte)1);
            else if (value.Equals("false", StringComparison.OrdinalIgnoreCase)) attribute.SetValue(obj, byte.MinValue);
            else attribute.SetValue(obj, value.Convert<byte>());
            return true;
        }

        if (attribute.PropertyType == typeof(sbyte))
        {
            attribute.SetValue(obj, value.Convert<sbyte>());
            return true;
        }

        if (attribute.PropertyType == typeof(ushort))
        {
            attribute.SetValue(obj, value.Convert<ushort>());
            return true;
        }

        if (attribute.PropertyType == typeof(short))
        {
            attribute.SetValue(obj, value.Convert<short>());
            return true;
        }

        if (attribute.PropertyType == typeof(uint))
        {
            attribute.SetValue(obj, value.Convert<uint>());
            return true;
        }

        if (attribute.PropertyType == typeof(int))
        {
            attribute.SetValue(obj, value.Convert<int>());
            return true;
        }

        if (attribute.PropertyType == typeof(ulong))
        {
            attribute.SetValue(obj, value.Convert<ulong>());
            return true;
        }

        if (attribute.PropertyType == typeof(long))
        {
            attribute.SetValue(obj, value.Convert<long>());
            return true;
        }

        if (attribute.PropertyType == typeof(Half))
        {
            attribute.SetValue(obj, Half.Parse(value, CultureInfo.InvariantCulture.NumberFormat));
            return true;
        }

        if (attribute.PropertyType == typeof(float))
        {
            attribute.SetValue(obj, float.Parse(value, CultureInfo.InvariantCulture.NumberFormat));
            return true;
        }

        if (attribute.PropertyType == typeof(double))
        {
            attribute.SetValue(obj, double.Parse(value, CultureInfo.InvariantCulture.NumberFormat));
            return true;
        }

        if (attribute.PropertyType == typeof(decimal))
        {
            attribute.SetValue(obj, decimal.Parse(value, CultureInfo.InvariantCulture.NumberFormat));
            return true;
        }

        throw new Exception($"Data type '{attribute.PropertyType.Name}' not recognized nor implemented.");
    }

    public static bool SetValueFromString(this PropertyInfo attribute, object obj, object? value) =>
        attribute.SetValueFromString(obj, value?.ToString() ?? string.Empty);
}