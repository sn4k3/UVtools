/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Globalization;
using System;

namespace UVtools.Core.Extensions;

public static class NumberExtensions
{
    /// <summary>
    /// Convert bool to byte (0/1)
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static byte ToByte(this bool value) => (byte)(value ? 1 : 0);

    /// <summary>
    /// Gets the number of digits this number haves
    /// </summary>
    /// <param name="n"></param>
    /// <returns>Digit count</returns>
    public static int DigitCount(this sbyte n)
    {
        if (n >= 0)
        {
            return n switch
            {
                < 10 => 1,
                < 100 => 2,
                _ => 3
            };
        }

        return n switch
        {
            > -10 => 1,
            > -100 => 2,
            _ => 3
        };
    }

    /// <summary>
    /// Gets the number of digits this number haves
    /// </summary>
    /// <param name="n"></param>
    /// <returns>Digit count</returns>
    public static int DigitCount(this byte n)
    {
        return n switch
        {
            < 10 => 1,
            < 100 => 2,
            _ => 3
        };
    }

    /// <summary>
    /// Gets the number of digits this number haves
    /// </summary>
    /// <param name="n"></param>
    /// <returns>Digit count</returns>
    public static int DigitCount(this short n)
    {
        if (n >= 0)
        {
            return n switch
            {
                < 10 => 1,
                < 100 => 2,
                < 1000 => 3,
                _ => 4
            };
        }

        return n switch
        {
            > -10 => 1,
            > -100 => 2,
            > -1000 => 3,
            _ => 4
        };
    }

    /// <summary>
    /// Gets the number of digits this number haves
    /// </summary>
    /// <param name="n"></param>
    /// <returns>Digit count</returns>
    public static int DigitCount(this ushort n)
    {
        return n switch
        {
            < 10 => 1,
            < 100 => 2,
            < 1000 => 3,
            _ => 4
        };
    }

    /// <summary>
    /// Gets the number of digits this number haves
    /// </summary>
    /// <param name="n"></param>
    /// <returns>Digit count</returns>
    public static int DigitCount(this int n)
    {
        if (n >= 0)
        {
            return n switch
            {
                < 10 => 1,
                < 100 => 2,
                < 1000 => 3,
                < 10000 => 4,
                < 100000 => 5,
                < 1000000 => 6,
                < 10000000 => 7,
                < 100000000 => 8,
                < 1000000000 => 9,
                _ => 10
            };
        }

        return n switch
        {
            > -10 => 1,
            > -100 => 2,
            > -1000 => 3,
            > -10000 => 4,
            > -100000 => 5,
            > -1000000 => 6,
            > -10000000 => 7,
            > -100000000 => 8,
            > -1000000000 => 9,
            _ => 10
        };
    }

    /// <summary>
    /// Gets the number of digits this number haves
    /// </summary>
    /// <param name="n"></param>
    /// <returns>Digit count</returns>
    public static int DigitCount(this uint n)
    {
        return n switch
        {
            < 10U => 1,
            < 100U => 2,
            < 1000U => 3,
            < 10000U => 4,
            < 100000U => 5,
            < 1000000U => 6,
            < 10000000U => 7,
            < 100000000U => 8,
            < 1000000000U => 9,
            _ => 10
        };
    }

    /// <summary>
    /// Gets the number of digits this number haves
    /// </summary>
    /// <param name="n"></param>
    /// <returns>Digit count</returns>
    public static int DigitCount(this long n)
    {
        if (n >= 0)
        {
            return n switch
            {
                < 10L => 1,
                < 100L => 2,
                < 1000L => 3,
                < 10000L => 4,
                < 100000L => 5,
                < 1000000L => 6,
                < 10000000L => 7,
                < 100000000L => 8,
                < 1000000000L => 9,
                < 10000000000L => 10,
                < 100000000000L => 11,
                < 1000000000000L => 12,
                < 10000000000000L => 13,
                < 100000000000000L => 14,
                < 1000000000000000L => 15,
                < 10000000000000000L => 16,
                < 100000000000000000L => 17,
                < 1000000000000000000L => 18,
                _ => 19
            };
        }

        return n switch
        {
            > -10L => 1,
            > -100L => 2,
            > -1000L => 3,
            > -10000L => 4,
            > -100000L => 5,
            > -1000000L => 6,
            > -10000000L => 7,
            > -100000000L => 8,
            > -1000000000L => 9,
            > -10000000000L => 10,
            > -100000000000L => 11,
            > -1000000000000L => 12,
            > -10000000000000L => 13,
            > -100000000000000L => 14,
            > -1000000000000000L => 15,
            > -10000000000000000L => 16,
            > -100000000000000000L => 17,
            > -1000000000000000000L => 18,
            _ => 19
        };
    }

    /// <summary>
    /// Gets the number of digits this number haves
    /// </summary>
    /// <param name="n"></param>
    /// <returns>Digit count</returns>
    public static int DigitCount(this ulong n)
    {
        return n switch
        {
            < 10UL => 1,
            < 100UL => 2,
            < 1000UL => 3,
            < 10000UL => 4,
            < 100000UL => 5,
            < 1000000UL => 6,
            < 10000000UL => 7,
            < 100000000UL => 8,
            < 1000000000UL => 9,
            < 10000000000UL => 10,
            < 100000000000UL => 11,
            < 1000000000000UL => 12,
            < 10000000000000UL => 13,
            < 100000000000000UL => 14,
            < 1000000000000000UL => 15,
            < 10000000000000000UL => 16,
            < 100000000000000000UL => 17,
            < 1000000000000000000UL => 18,
            < 10000000000000000000UL => 19,
            _ => 20
        };
    }

    public static uint DecimalDigits(this float val)
    {
        var valStr = val.ToString(CultureInfo.InvariantCulture).TrimEnd('0');
        if (string.IsNullOrEmpty(valStr)) return 0;

        var index = valStr.IndexOf('.');
        if (index < 0) return 0;

        return (uint)(valStr[index..].Length - 1);
    }

    public static uint DecimalDigits(this double val)
    {
        var valStr = val.ToString(CultureInfo.InvariantCulture).TrimEnd('0');
        if (string.IsNullOrEmpty(valStr)) return 0;

        var index = valStr.IndexOf('.');
        if (index < 0) return 0;

        return (uint)(valStr[index..].Length - 1);
    }

    public static uint DecimalDigits(this decimal val)
    {
        var valStr = val.ToString(CultureInfo.InvariantCulture).TrimEnd('0');
        if (string.IsNullOrEmpty(valStr) || valStr[^1] == '.') return 0;

        var index = valStr.IndexOf('.');
        if (index < 0) return 0;

        return (uint)(valStr[index..].Length - 1);
    }

    public static bool IsInteger(this float val, float tolerance = 0.0001f) => Math.Abs(val - Math.Floor(val)) < tolerance;
    public static bool IsInteger(this double val, double tolerance = 0.0001) => Math.Abs(val - Math.Floor(val)) < tolerance;
    public static bool IsInteger(this decimal val) => val == Math.Floor(val);
}