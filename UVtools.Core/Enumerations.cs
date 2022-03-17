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

public class Enumerations
{
    /// <summary>
    /// Gets index start number
    /// </summary>
    public enum IndexStartNumber : byte
    {
        Zero,
        One
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

    public enum FlipDirection : byte
    {
        None,
        Horizontally,
        Vertically,
        Both,
    }

    public enum RotateDirection : sbyte
    {
        [Description("None")]
        None = -1,
        /// <summary>Rotate 90 degrees clockwise (0)</summary>
        [Description("Rotate 90º CW")]
        Rotate90Clockwise = 0,
        /// <summary>Rotate 180 degrees clockwise (1)</summary>
        [Description("Rotate 180º")]
        Rotate180 = 1,
        /// <summary>Rotate 270 degrees clockwise (2)</summary>
        [Description("Rotate 90º CCW")]
        Rotate90CounterClockwise = 2,
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
}