/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using CommunityToolkit.Mvvm.ComponentModel;
using Emgu.CV.CvEnum;
using System;
using System.Drawing;
using System.Xml.Serialization;
using EmguExtensions;

namespace UVtools.Core.PixelEditor;

#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public partial class PixelText : PixelOperation, IEquatable<PixelText>
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
{
    public override PixelOperationType OperationType => PixelOperationType.Text;

    public static FontFace[] FontFaces => (FontFace[]) Enum.GetValues(typeof(FontFace));

    [ObservableProperty]
    public partial FontFace Font { get; set; }

    public double FontScale
    {
        get;
        set => SetProperty(ref field, Math.Round(value, 2));
    }

    [ObservableProperty]
    public partial ushort Thickness { get; set; } = 1;

    [ObservableProperty]
    public partial string Text { get; set; } = null!;

    [ObservableProperty]
    public partial bool Mirror { get; set; }

    [ObservableProperty]
    public partial PutTextLineAlignment LineAlignment { get; set; } = PutTextLineAlignment.Left;

    [ObservableProperty]
    public partial double Angle { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RemovePixelBrightnessPercent))]
    public partial byte RemovePixelBrightness { get; set; }

    public decimal RemovePixelBrightnessPercent => Math.Round(RemovePixelBrightness * 100M / 255M, 2);

    [XmlIgnore]
    public bool IsAdd { get; private set; }

    public byte Brightness => IsAdd ? PixelBrightness : RemovePixelBrightness;

    [XmlIgnore]
    public Rectangle Rectangle { get; private set; }

    public PixelText(){}

    public PixelText(uint layerIndex, Point location, LineType lineType, FontFace font, double fontScale, ushort thickness, string text, bool mirror, PutTextLineAlignment lineAlignment, double angle, byte removePixelBrightness, byte pixelBrightness, bool isAdd) : base(layerIndex, location, lineType, pixelBrightness)
    {
        Font = font;
        FontScale = fontScale;
        Thickness = thickness;
        Text = text;
        Mirror = mirror;
        LineAlignment = lineAlignment;
        Angle = angle;
        IsAdd = isAdd;
        RemovePixelBrightness = removePixelBrightness;

        int baseLine = 0;
        Size = EmguCvExtensions.GetTextSizeExtended(text, font, fontScale, thickness, ref baseLine, lineAlignment);
        Rectangle = new Rectangle(location, Size);
    }

    public override void CopyTo(PixelOperation operation)
    {
        base.CopyTo(operation);
        if (operation is not PixelText text) throw new TypeAccessException($"Expecting PixelText but got {operation.GetType().Name}");
        text.Font = Font;
        text.FontScale = FontScale;
        text.Thickness = Thickness;
        text.Text = Text;
        text.Mirror = Mirror;
        text.LineAlignment = LineAlignment;
        text.Angle = Angle;
        text.RemovePixelBrightness = RemovePixelBrightness;
        text.IsAdd = IsAdd;
        text.Rectangle = Rectangle;
    }

    public override string ToString()
    {
        return $"{LineType}, {Font}, {FontScale}px/{Thickness}px, "
               + (string.IsNullOrWhiteSpace(Text) ? string.Empty : $"{Text}, ")
               + $"{LineAlignment}, {Angle}º, Layers: {LayersBelow}/{LayersAbove}";
    }

    public bool Equals(PixelText? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return base.Equals(other) && Font == other.Font && FontScale.Equals(other.FontScale) && Thickness == other.Thickness && Text == other.Text && Mirror == other.Mirror && LineAlignment == other.LineAlignment && Angle.Equals(other.Angle) && RemovePixelBrightness == other.RemovePixelBrightness && IsAdd == other.IsAdd && Rectangle.Equals(other.Rectangle);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((PixelText)obj);
    }

    public static bool operator ==(PixelText? left, PixelText? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(PixelText? left, PixelText? right)
    {
        return !Equals(left, right);
    }
}
