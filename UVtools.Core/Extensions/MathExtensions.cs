/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Globalization;

namespace UVtools.Core.Extensions;

public static class MathExtensions
{
    /*public static byte Clamp(this byte value, byte min, byte max)
    {
        return value <= min ? min : value >= max ? max : value;
    }

    public static sbyte Clamp(this sbyte value, sbyte min, sbyte max)
    {
        return value <= min ? min : value >= max ? max : value;
    }

    public static ushort Clamp(this ushort value, ushort min, ushort max)
    {
        return value <= min ? min : value >= max ? max : value;
    }

    public static short Clamp(this short value, short min, short max)
    {
        return value <= min ? min : value >= max ? max : value;
    }

    public static uint Clamp(this uint value, uint min, uint max)
    {
        return value <= min ? min : value >= max ? max : value;
    }

    public static int Clamp(this int value, int min, int max)
    {
        return value <= min ? min : value >= max ? max : value;
    }

    public static ulong Clamp(this ulong value, ulong min, ulong max)
    {
        return value <= min ? min : value >= max ? max : value;
    }

    public static long Clamp(this long value, long min, long max)
    {
        return value <= min ? min : value >= max ? max : value;
    }

    public static float Clamp(this float value, float min, float max)
    {
        return value <= min ? min : value >= max ? max : value;
    }

    public static double Clamp(this double value, double min, double max)
    {
        return value <= min ? min : value >= max ? max : value;
    }

    public static decimal Clamp(this decimal value, decimal min, decimal max)
    {
        return value <= min ? min : value >= max ? max : value;
    }

    public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0)
            return min;
        if (value.CompareTo(max) > 0)
            return max;

        return value;
    }*/

    public static uint DecimalDigits(this float val)
    {
        var valStr = val.ToString(CultureInfo.InvariantCulture).TrimEnd('0');
        if (string.IsNullOrEmpty(valStr)) return 0;

        var index = valStr.IndexOf('.');
        if (index < 0) return 0;

        return (uint) (valStr[index..].Length - 1);
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

    public static int GCD(int a, int b)
    {
        while (a != 0 && b != 0)
        {
            if (a > b)
                a %= b;
            else
                b %= a;
        }

        return a | b;
    }

    public static uint GCD(uint a, uint b)
    {
        while (a != 0 && b != 0)
        {
            if (a > b)
                a %= b;
            else
                b %= a;
        }

        return a | b;
    }

    public static ulong GCD(ulong a, ulong b)
    {
        while (a != 0 && b != 0)
        {
            if (a > b)
                a %= b;
            else
                b %= a;
        }

        return a | b;
    }

}