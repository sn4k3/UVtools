/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
namespace UVtools.Core.Gerber;

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