/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

namespace UVtools.Core.Scripting;

public class ScriptNumericalInput<T> : ScriptBaseInput<T>
{
    /// <summary>
    /// Gets the minimum for this input
    /// </summary>
    public T? Minimum { get; set; }

    /// <summary>
    /// Gets the minimum for this input
    /// </summary>
    public T? Maximum { get; set; }

    /// <summary>
    /// Gets the increment value for this
    /// </summary>
    public T? Increment { get; set; }

    /// <summary>
    /// Gets the number of decimal plates to round the value
    /// </summary>
    public byte DecimalPlates { get; set; } = 2;
}