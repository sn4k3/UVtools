/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

namespace UVtools.Core.EmguCV;

public enum EmptyRoiBehaviour
{
    /// <summary>
    /// Allows the mat to be created with an empty roi.
    /// </summary>
    Continue,

    /// <summary>
    /// Sets the roi to the source mat size.
    /// </summary>
    CaptureSource,
}