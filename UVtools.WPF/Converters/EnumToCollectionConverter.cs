/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using Avalonia.Data.Converters;
using UVtools.Core.Extensions;

namespace UVtools.WPF.Converters;

public class EnumToCollectionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return EnumExtensions.GetAllValuesAndDescriptions(value.GetType());
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return null;
        //string parameterString = parameter.ToString();
        //return Enum.Parse(targetType, parameterString);
    }
}