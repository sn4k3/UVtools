/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */


using System;

namespace UVtools.Core.Converters;

public static class SpeedConverter
{
    /// <summary>
    /// Converts a speed from one unit to another
    /// </summary>
    /// <param name="value"></param>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="rounding"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static float Convert(float value, SpeedUnit from, SpeedUnit to, byte rounding = 2)
    {
        if (from == to) return value;

        return from switch
        {
            SpeedUnit.MillimetersPerSecond => to switch
            {
                SpeedUnit.MillimetersPerSecond => value,
                SpeedUnit.MillimetersPerMinute => (float) Math.Round(value * 60, rounding),
                SpeedUnit.CentimetersPerMinute => (float) Math.Round(value * 6, rounding),
                _ => throw new ArgumentOutOfRangeException(nameof(to), to, null)
            },
            SpeedUnit.MillimetersPerMinute => to switch
            {
                SpeedUnit.MillimetersPerSecond => (float) Math.Round(value / 60, rounding),
                SpeedUnit.MillimetersPerMinute => value,
                SpeedUnit.CentimetersPerMinute => (float) Math.Round(value / 10, rounding),
                _ => throw new ArgumentOutOfRangeException(nameof(to), to, null)
            },
            SpeedUnit.CentimetersPerMinute => to switch
            {
                SpeedUnit.MillimetersPerSecond => (float) Math.Round(value * (1.0/6.0), rounding),
                SpeedUnit.MillimetersPerMinute => (float)Math.Round(value * 10, rounding),
                SpeedUnit.CentimetersPerMinute => value,
                _ => throw new ArgumentOutOfRangeException(nameof(to), to, null)
            },
            _ => throw new ArgumentOutOfRangeException(nameof(from), from, null)
        };
    }

}