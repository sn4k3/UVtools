/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using UVtools.Core.Objects;

namespace UVtools.UI.Structures;

public sealed class LogItem : BindableBase
{
    private int _index;
    private string _startTime;
    private double _elapsedTime;
    private string _description;

    public int Index
    {
        get => _index;
        set => RaiseAndSetIfChanged(ref _index, value);
    }

    public string StartTime
    {
        get => _startTime;
        set => RaiseAndSetIfChanged(ref _startTime, value);
    }

    public double ElapsedTime
    {
        get => _elapsedTime;
        set => RaiseAndSetIfChanged(ref _elapsedTime, Math.Round(value, 2));
    }

    public string Description
    {
        get => _description;
        set => RaiseAndSetIfChanged(ref _description, value);
    }

    public LogItem(int index, string description, double elapsedTime = 0)
    {
        _index = index;
        _description = description;
        ElapsedTime = elapsedTime;
        _startTime = DateTime.Now.ToString("HH:mm:ss");
    }

    public LogItem(string description = "", uint elapsedTime = 0) : this(0, description, elapsedTime)
    { }

    public override string ToString()
    {
        return Description;
    }
}