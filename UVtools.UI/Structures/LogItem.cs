/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace UVtools.UI.Structures;

public sealed partial class LogItem : ObservableObject
{
    [ObservableProperty]
    public partial int Index { get; set; }

    [ObservableProperty]
    public partial string StartTime { get; set; }

    public double ElapsedTime
    {
        get;
        set => SetProperty(ref field, Math.Round(value, 2));
    }

    [ObservableProperty]
    public partial string Description { get; set; }

    public LogItem(int index, string description, double elapsedTime = 0)
    {
        Index = index;
        Description = description;
        ElapsedTime = elapsedTime;
        StartTime = DateTime.Now.ToString("HH:mm:ss");
    }

    public LogItem(string description = "", uint elapsedTime = 0) : this(0, description, elapsedTime)
    { }

    public override string ToString()
    {
        return Description;
    }
}