/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using BinarySerialization;

namespace UVtools.Core.Converters;

public class NullTerminatedLengthConverter : IValueConverter
{
    // Read
    public object Convert(object value, object converterParameter, BinarySerializationContext context)
    {
        //var uintValue = System.Convert.ToUInt32(value);
        //if (uintValue == 0) return 0;
        //return uintValue - 1;
        return value;
    }

    // Write
    public object ConvertBack(object value, object converterParameter, BinarySerializationContext context)
    {
        var uintValue = System.Convert.ToUInt32(value);
        if (uintValue == 0) return 0;
        return uintValue + 1;
    }
}