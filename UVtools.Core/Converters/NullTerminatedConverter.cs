/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using BinarySerialization;

namespace UVtools.Core.Converters;

public class NullTerminatedConverter : IValueConverter
{
    public object Convert(object value, object converterParameter, BinarySerializationContext context)
    {
        return value.ToString()!.TrimEnd(char.MinValue);
    }

    public object ConvertBack(object value, object converterParameter, BinarySerializationContext context)
    {
        return $"{value}{char.MinValue}";
    }
}