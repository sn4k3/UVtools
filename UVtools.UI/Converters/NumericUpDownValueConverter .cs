/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace UVtools.UI.Converters;

internal class NumericUpDownValueConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return default(decimal);
        return (decimal?)((IConvertible)value).ToDecimal(culture);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            if (targetType == typeof(byte))    return byte.MinValue;
            if (targetType == typeof(sbyte))   return (sbyte)0;
            if (targetType == typeof(ushort))  return ushort.MinValue;
            if (targetType == typeof(short))   return (short)0;
            if (targetType == typeof(uint))    return uint.MinValue;
            if (targetType == typeof(int))     return 0;
            if (targetType == typeof(ulong))   return ulong.MinValue;
            if (targetType == typeof(long))    return 0L;
            if (targetType == typeof(float))   return 0f;
            if (targetType == typeof(double))  return 0d;
            if (targetType == typeof(decimal)) return 0M;
            throw new ArgumentNullException($"Unsupported type: {targetType.FullName}");
        }

        if (targetType == typeof(byte))    return ((IConvertible)value).ToByte(culture);
        if (targetType == typeof(sbyte))   return ((IConvertible)value).ToSByte(culture);
        if (targetType == typeof(ushort))  return ((IConvertible)value).ToUInt16(culture);
        if (targetType == typeof(short))   return ((IConvertible)value).ToInt16(culture);
        if (targetType == typeof(uint))    return ((IConvertible)value).ToUInt32(culture);
        if (targetType == typeof(int))     return ((IConvertible)value).ToInt32(culture);
        if (targetType == typeof(ulong))   return ((IConvertible)value).ToUInt64(culture);
        if (targetType == typeof(long))    return ((IConvertible)value).ToInt64(culture);
        if (targetType == typeof(float))   return ((IConvertible)value).ToSingle(culture);
        if (targetType == typeof(double))  return ((IConvertible)value).ToDouble(culture);
        if (targetType == typeof(decimal)) return ((IConvertible)value).ToDecimal(culture);

        throw new ArgumentNullException($"Unsupported type: {targetType.FullName}");
    }
}