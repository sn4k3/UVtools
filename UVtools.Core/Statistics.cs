/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Collections.Generic;
using System.Diagnostics;
using ZLinq;

namespace UVtools.Core;

public class Statistics
{
    #region Properties

    public List<string> ImplementedKeys { get; } = [];
    public List<string> MissingKeys { get; } = [];
    public ushort TotalKeys => (ushort)(ImplementedKeys.Count + MissingKeys.Count);

    public Stopwatch ExecutionTime { get; } = new();
    #endregion

    #region Overrides
    public override string ToString()
    {
        string message = $"{nameof(ImplementedKeys)}: {ImplementedKeys.Count}, {nameof(MissingKeys)}: {MissingKeys.Count}, {nameof(TotalKeys)}: {TotalKeys}, {nameof(ExecutionTime)}: {ExecutionTime.ElapsedMilliseconds}ms";
        message = MissingKeys.AsValueEnumerable().Aggregate(message, (current, missingKey) => current + ("\n" + missingKey));
        return message;
    }

    #endregion

    #region Methods

    public void Clear()
    {
        ImplementedKeys.Clear();
        MissingKeys.Clear();
        ExecutionTime.Reset();
    }
    #endregion
}