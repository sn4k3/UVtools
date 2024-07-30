/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV.CvEnum;
using System;
using System.ComponentModel;

namespace UVtools.Core;

/// <summary>
/// Enumeration of m manufacturing processes
/// </summary>
public enum PrinterManufacturingProcess : byte
{
    mSLA,
    DLP,
    SLA
}

/// <summary>
/// Gets index start number, if starts on 0 or 1
/// </summary>
public enum IndexStartNumber : byte
{
    Zero,
    One
}

/// <summary>
/// Gets the possible layer groups in the file
/// </summary>
public enum LayerGroup : byte
{
    [Description("Bottom layers")]
    Bottom,
    [Description("Normal layers")]
    Normal,
}

public enum LayerRangeSelection : byte
{
    None,
    All,
    Current,
    Bottom,
    Normal,
    First,
    Last
}

/// <summary>
/// Flip direction, same as <see cref="FlipType"/>, but add None which must be taken care/ignored on code
/// </summary>
public enum FlipDirection : sbyte
{
    None = sbyte.MinValue,
    Horizontally = FlipType.Horizontal,
    Vertically = FlipType.Vertical,
    Both = FlipType.Both,
}

/// <summary>
/// Rotate direction, same as <see cref="RotateFlags"/>, but add None which must be taken care/ignored on code
/// </summary>
public enum RotateDirection : sbyte
{
    [Description("None")]
    None = -1,
    /// <summary>Rotate 90 degrees clockwise (0)</summary>
    [Description("Rotate 90º CW")]
    Rotate90Clockwise = RotateFlags.Rotate90Clockwise,
    /// <summary>Rotate 180 degrees clockwise (1)</summary>
    [Description("Rotate 180º")]
    Rotate180 = RotateFlags.Rotate180,
    /// <summary>Rotate 270 degrees clockwise (2)</summary>
    [Description("Rotate 90º CCW")]
    Rotate90CounterClockwise = RotateFlags.Rotate90CounterClockwise,
}

public enum Anchor : byte
{
    TopLeft, TopCenter, TopRight,
    MiddleLeft, MiddleCenter, MiddleRight,
    BottomLeft, BottomCenter, BottomRight,
    None
}

public enum LightOffDelaySetMode : byte
{
    [Description("Set the light-off with an extra delay")]
    UpdateWithExtraDelay,

    [Description("Set the light-off without an extra delay")]
    UpdateWithoutExtraDelay,

    [Description("Set the light-off to zero")]
    SetToZero,

    [Description("Disabled")]
    NoAction
}

public enum SpeedUnit : byte
{
    /// <summary>
    /// mm/s
    /// </summary>
    MillimetersPerSecond,
    /// <summary>
    /// mm/m
    /// </summary>
    MillimetersPerMinute,
    /// <summary>
    /// cm/m
    /// </summary>
    CentimetersPerMinute,
}

public enum TimeUnits : byte
{
    /// <summary>
    /// ms
    /// </summary>
    Milliseconds,
    /// <summary>
    /// s
    /// </summary>
    Seconds
}

public enum MidpointRoundingType
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

public enum RemoveSourceFileAction : byte
{
    [Description("Keep the source file")]
    No,

    [Description("Remove the source file")]
    Yes,

    [Description("Prompt before remove the source file")]
    Prompt
}

/// <summary>
/// Default order of issues to show on the UI list
/// </summary>
public enum IssuesOrderBy : byte
{
    [Description("Type (↑ASC) » Layer (↑ASC) » Area (↓DESC)")]
    TypeAscLayerAscAreaDesc,

    [Description("Type (↑ASC) » Area (↓DESC) » Layer (↑ASC)")]
    TypeAscAreaDescLayerAsc,

    [Description("Area (↓DESC) » Layer (↑ASC) » Type (↑ASC)")]
    AreaDescLayerIndexAscTypeAsc
}

/*
public static class Enumerations
{
    public static FlipType ToOpenCVFlipType(FlipDirection flip)
    {
        return flip switch
        {
            FlipDirection.None => throw new NotSupportedException($"Flip type: {flip} is not supported by OpenCV."),
            FlipDirection.Horizontally => FlipType.Horizontal,
            FlipDirection.Vertically => FlipType.Vertical,
            FlipDirection.Both => FlipType.Both,
            _ => throw new ArgumentOutOfRangeException(nameof(flip), flip, null)
        };
    }

    public static RotateFlags ToOpenCVRotateFlags(RotateDirection rotate)
    {
        return rotate switch
        {
            RotateDirection.None => throw new NotSupportedException($"Rotate direction: {rotate} is not supported by OpenCV."),
            RotateDirection.Rotate90Clockwise => RotateFlags.Rotate90Clockwise,
            RotateDirection.Rotate90CounterClockwise => RotateFlags.Rotate90CounterClockwise,
            RotateDirection.Rotate180 => RotateFlags.Rotate180,
            _ => throw new ArgumentOutOfRangeException(nameof(rotate), rotate, null)
        };
    }
}*/