/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.ComponentModel;

namespace UVtools.Core.Gerber;

public enum GerberZerosSuppressionType : byte
{
    /// <summary>
    /// Do not omit zeros
    /// </summary>
    NoSuppression,
    /// <summary>
    /// Omit left zeros
    /// </summary>
    Leading,
    /// <summary>
    /// Omit right zeros
    /// </summary>
    Trail
}

public enum GerberPositionType : byte
{
    Absolute,
    Relative
}

public enum GerberUnitType : byte
{
    Millimeter,
    Inch
}

public enum GerberPolarityType : byte
{
    Dark,
    Clear
}


public enum GerberMoveType : byte
{
    // G01
    Linear,
    // G02
    Arc,
    // G03
    ArcCounterClockwise
}

public enum GerberMidpointRounding
{
    [Description("To even: The strategy of rounding to the nearest number, and when a number is halfway between two others, it's rounded toward the nearest even number.")]
    ToEven = MidpointRounding.ToEven,

    [Description("Away from zero: The strategy of rounding to the nearest number, and when a number is halfway between two others, it's rounded toward the nearest number that's away from zero.")]
    AwayFromZero = MidpointRounding.AwayFromZero,

    [Description("To zero: The strategy of directed rounding toward zero, with the result closest to and no greater in magnitude than the infinitely precise result.")]
    ToZero = MidpointRounding.ToZero,

    [Description("To negative inifity: The strategy of downwards-directed rounding, with the result closest to and no greater than the infinitely precise result.")]
    ToNegativeInfinity = MidpointRounding.ToNegativeInfinity,

    [Description("To positive inifity: The strategy of upwards-directed rounding, with the result closest to and no less than the infinitely precise result.")]
    ToPositiveInfinity = MidpointRounding.ToPositiveInfinity
}