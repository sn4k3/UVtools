/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using Emgu.CV.CvEnum;
using System;
using System.Drawing;

namespace UVtools.Core.PixelEditor;

#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public class PixelSupport : PixelOperation, IEquatable<PixelSupport>
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
{
    private byte _tipDiameter = 19;
    private byte _pillarDiameter = 32;
    private byte _baseDiameter = 60;
    public override PixelOperationType OperationType => PixelOperationType.Supports;

    public byte TipDiameter
    {
        get => _tipDiameter;
        set => RaiseAndSetIfChanged(ref _tipDiameter, value);
    }

    public byte PillarDiameter
    {
        get => _pillarDiameter;
        set => RaiseAndSetIfChanged(ref _pillarDiameter, value);
    }

    public byte BaseDiameter
    {
        get => _baseDiameter;
        set => RaiseAndSetIfChanged(ref _baseDiameter, value);
    }

    public PixelSupport(){}

    public PixelSupport(uint layerIndex, Point location, byte tipDiameter, byte pillarDiameter, byte baseDiameter, byte pixelBrightness) : base(layerIndex, location, LineType.AntiAlias, pixelBrightness)
    {
        TipDiameter = tipDiameter;
        PillarDiameter = pillarDiameter;
        BaseDiameter = baseDiameter;
        Size = new Size(TipDiameter, TipDiameter);
    }

    public override void CopyTo(PixelOperation operation)
    {
        base.CopyTo(operation);
        if (operation is not PixelSupport support) throw new TypeAccessException($"Expecting PixelSupport but got {operation.GetType().Name}");
        support.TipDiameter = TipDiameter;
        support.PillarDiameter = PillarDiameter;
        support.BaseDiameter = BaseDiameter;
    }

    public override string ToString()
    {
        return $"{_tipDiameter}px, {_pillarDiameter}px, {_baseDiameter}px, {_pixelBrightness}☼";
    }

    public bool Equals(PixelSupport? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return base.Equals(other) && _tipDiameter == other._tipDiameter && _pillarDiameter == other._pillarDiameter && _baseDiameter == other._baseDiameter;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((PixelSupport)obj);
    }

    public static bool operator ==(PixelSupport? left, PixelSupport? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(PixelSupport? left, PixelSupport? right)
    {
        return !Equals(left, right);
    }
}