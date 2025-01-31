/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

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
                attribute.SetValue(obj, enumValue);
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
            attribute.SetValue(obj, Half.Parse(value, CultureInfo.InvariantCulture));
            return true;
        }

        if (attribute.PropertyType == typeof(float))
        {
            attribute.SetValue(obj, float.Parse(value, CultureInfo.InvariantCulture));
            return true;
        }

        if (attribute.PropertyType == typeof(double))
        {
            attribute.SetValue(obj, double.Parse(value, CultureInfo.InvariantCulture));
            return true;
        }

        if (attribute.PropertyType == typeof(decimal))
        {
            attribute.SetValue(obj, decimal.Parse(value, CultureInfo.InvariantCulture));
            return true;
        }

        if (attribute.PropertyType == typeof(nint))
        {
            attribute.SetValue(obj, nint.Parse(value, CultureInfo.InvariantCulture));
            return true;
        }

        if (attribute.PropertyType == typeof(nuint))
        {
            attribute.SetValue(obj, nuint.Parse(value, CultureInfo.InvariantCulture));
            return true;
        }

        if (attribute.PropertyType == typeof(Point))
        {
            var match = Regex.Match(value, string.Format("X={0},\\s?Y={0}", @"(\d+)"));
            if (match is { Success: true, Groups.Count: >= 3 })
            {
                if (int.TryParse(match.Groups[1].Value, out var x)
                    && int.TryParse(match.Groups[2].Value, out var y)
                   )
                {
                    attribute.SetValue(obj, new Point(x, y));
                    return true;
                }
            }

            return false;
        }

        if (attribute.PropertyType == typeof(PointF))
        {
            var match = Regex.Match(value, string.Format("X={0},\\s?Y={0}", "([+-]?([0-9]*[.])?[0-9]+)"));
            if (match is { Success: true, Groups.Count: >= 3 })
            {
                if (float.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var x)
                    && float.TryParse(match.Groups[2].Value, CultureInfo.InvariantCulture, out var y)
                   )
                {
                    attribute.SetValue(obj, new PointF(x, y));
                    return true;
                }
            }

            return false;
        }

        if (attribute.PropertyType == typeof(Size))
        {
            var match = Regex.Match(value, string.Format("Width={0},\\s?Height={0}", @"(\d+)"));
            if (match is { Success: true, Groups.Count: >= 3 })
            {
                if (int.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var width)
                    && int.TryParse(match.Groups[2].Value, CultureInfo.InvariantCulture, out var height)
                   )
                {
                    attribute.SetValue(obj, new Size(width, height));
                    return true;
                }
            }

            return false;
        }

        if (attribute.PropertyType == typeof(SizeF))
        {
            var match = Regex.Match(value, string.Format("Width={0},\\s?Height={0}", "([+-]?([0-9]*[.])?[0-9]+)"));
            if (match is { Success: true, Groups.Count: >= 3 })
            {
                if (float.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var width)
                    && float.TryParse(match.Groups[2].Value, CultureInfo.InvariantCulture, out var height)
                   )
                {
                    attribute.SetValue(obj, new SizeF(width, height));
                    return true;
                }
            }

            return false;
        }

        if (attribute.PropertyType == typeof(Rectangle))
        {
            var match = Regex.Match(value, string.Format("X={0},\\s?Y={0},\\s?Width={0},\\s?Height={0}", @"(\d+)"));
            if (match is { Success: true, Groups.Count: >= 5 })
            {
                if (int.TryParse(match.Groups[1].Value, out var x)
                    && int.TryParse(match.Groups[2].Value, out var y)
                    && int.TryParse(match.Groups[3].Value, out var width)
                    && int.TryParse(match.Groups[4].Value, out var height)
                   )
                {
                    attribute.SetValue(obj, new Rectangle(x, y, width, height));
                    return true;
                }
            }

            return false;
        }

        if (attribute.PropertyType == typeof(RectangleF))
        {
            var match = Regex.Match(value, string.Format("X={0},\\s?Y={0},\\s?Width={0},\\s?Height={0}", "([+-]?([0-9]*[.])?[0-9]+)"));
            if (match is { Success: true, Groups.Count: >= 5 })
            {
                if (float.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var x)
                    && float.TryParse(match.Groups[2].Value, CultureInfo.InvariantCulture, out var y)
                    && float.TryParse(match.Groups[3].Value, CultureInfo.InvariantCulture, out var width)
                    && float.TryParse(match.Groups[4].Value, CultureInfo.InvariantCulture, out var height)
                   )
                {
                    attribute.SetValue(obj, new RectangleF(x, y, width, height));
                    return true;
                }
            }

            return false;
        }

        throw new Exception($"Data type '{attribute.PropertyType.Name}' not recognized nor implemented.");
    }

    public static bool SetValueFromString(this PropertyInfo attribute, object obj, object? value) =>
        attribute.SetValueFromString(obj, value?.ToString() ?? string.Empty);

    public static void CopyPropertiesTo(object src, object dest, params string[] ignoredProperties)
    {
        var srcProperties = src.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(info => info is { CanRead: true, CanWrite: true, GetMethod: not null } && !ignoredProperties.Contains(info.Name));
        var destProperties = dest.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(info => info is { CanRead: true, CanWrite: true, SetMethod: not null } && !ignoredProperties.Contains(info.Name));
        foreach (var srcProperty in srcProperties)
        {
            foreach (var destProperty in destProperties)
            {
                if (srcProperty.PropertyType != destProperty.PropertyType) continue;
                if (srcProperty.Name != destProperty.Name) continue;
                destProperty.SetValue(dest, srcProperty.GetValue(src));
            }
        }
    }

    public static PropertyInfo[] GetProperties(object obj)
    {
	    return obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
    }
}