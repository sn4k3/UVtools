/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using UVtools.Core.Objects;
using ZLinq;

namespace UVtools.Core.Extensions;

public static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        var attributes = value.GetType().GetField(value.ToString())?.GetCustomAttributes(typeof(DescriptionAttribute), false);
        if (attributes is not null && attributes.AsValueEnumerable().Any())
        {
            if (attributes.AsValueEnumerable().First() is DescriptionAttribute attr) return attr.Description;
        }

        // If no description is found, the least we can do is replace underscores with spaces
        // You can add your own custom default formatting logic here
        var ti = CultureInfo.CurrentCulture.TextInfo;
        return ti.ToTitleCase(ti.ToLower(value.ToString().Replace("_", " ")));
    }

    public static ValueDescription[] GetAllValuesAndDescriptions(Type t)
    {
        if (!t.IsEnum)
            throw new ArgumentException($"{nameof(t)} must be an enum type");

        return Enum.GetValues(t).AsValueEnumerable<Enum>()
            .OrderBy(e => e)
            .Select(e => new ValueDescription(e, e.GetDescription()))
            .ToArray();
    }
}