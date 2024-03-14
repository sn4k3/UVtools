/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Drawing;

namespace UVtools.Core.PixelEditor;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
public class PixelDrainHole : PixelOperation, IEquatable<PixelDrainHole>
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    private ushort _diameter = 50;
    public override PixelOperationType OperationType => PixelOperationType.DrainHole;

    public ushort Diameter
    {
        get => _diameter;
        set => RaiseAndSetIfChanged(ref _diameter, value);
    }

    public PixelDrainHole(){ _pixelBrightness = 0; }

    public PixelDrainHole(uint layerIndex, Point location, ushort diameter) : base(layerIndex, location)
    {
        Diameter = diameter;
        Size = new Size(diameter, diameter);
    }

    public override void CopyTo(PixelOperation operation)
    {
        base.CopyTo(operation);
        if (operation is not PixelDrainHole drainHole) throw new TypeAccessException($"Expecting PixelDrainHole but got {operation.GetType().Name}"); 
        drainHole.Diameter = Diameter;
    }

    public override string ToString()
    {
        return $"{_diameter}px";
    }

    public bool Equals(PixelDrainHole? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return base.Equals(other) && _diameter == other._diameter;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((PixelDrainHole)obj);
    }

    public static bool operator ==(PixelDrainHole? left, PixelDrainHole? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(PixelDrainHole? left, PixelDrainHole? right)
    {
        return !Equals(left, right);
    }
}