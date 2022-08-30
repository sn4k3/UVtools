/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using UVtools.Core.Objects;

namespace UVtools.WPF.Structures;

public class SlicerProperty : BindableBase
{
    private string _name;
    private string _value;
    private string _group;

    public string Name
    {
        get => _name;
        set => RaiseAndSetIfChanged(ref _name, value);
    }

    public string Value
    {
        get => _value;
        set => RaiseAndSetIfChanged(ref _value, value);
    }

    public string Group
    {
        get => _group;
        set => RaiseAndSetIfChanged(ref _group, value);
    }

    public SlicerProperty(string name, string value, string group = null)
    {
        _name = name;
        _value = value;
        _group = group;
    }
}