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
using System.Xml.Serialization;
using UVtools.Core.Extensions;

namespace UVtools.Core.PixelEditor;

#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public class PixelText : PixelOperation, IEquatable<PixelText>
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
{
    private FontFace _font;
    private double _fontScale = 1;
    private ushort _thickness = 1;
    private string _text = null!;
    private bool _mirror;
    private EmguExtensions.PutTextLineAlignment _lineAlignment = EmguExtensions.PutTextLineAlignment.Left;
    private double _angle;
    private byte _removePixelBrightness;
    public override PixelOperationType OperationType => PixelOperationType.Text;

    public static FontFace[] FontFaces => (FontFace[]) Enum.GetValues(typeof(FontFace));

    public FontFace Font
    {
        get => _font;
        set => RaiseAndSetIfChanged(ref _font, value);
    }

    public double FontScale
    {
        get => _fontScale;
        set => RaiseAndSetIfChanged(ref _fontScale, Math.Round(value, 2));
    }

    public ushort Thickness
    {
        get => _thickness;
        set => RaiseAndSetIfChanged(ref _thickness, value);
    }

    public string Text
    {
        get => _text;
        set => RaiseAndSetIfChanged(ref _text, value);
    }

    public bool Mirror
    {
        get => _mirror;
        set => RaiseAndSetIfChanged(ref _mirror, value);
    }

    public EmguExtensions.PutTextLineAlignment LineAlignment
    {
        get => _lineAlignment;
        set => RaiseAndSetIfChanged(ref _lineAlignment, value);
    }

    public double Angle
    {
        get => _angle;
        set => RaiseAndSetIfChanged(ref _angle, value);
    }

    public byte RemovePixelBrightness
    {
        get => _removePixelBrightness;
        set
        {
            if (!RaiseAndSetIfChanged(ref _removePixelBrightness, value)) return;
            RaisePropertyChanged(nameof(RemovePixelBrightnessPercent));
        }
    }

    public decimal RemovePixelBrightnessPercent => Math.Round(_removePixelBrightness * 100M / 255M, 2);

    [XmlIgnore]
    public bool IsAdd { get; private set; }

    public byte Brightness => IsAdd ? _pixelBrightness : _removePixelBrightness;

    [XmlIgnore]
    public Rectangle Rectangle { get; private set; }

    public PixelText(){}

    public PixelText(uint layerIndex, Point location, LineType lineType, FontFace font, double fontScale, ushort thickness, string text, bool mirror, EmguExtensions.PutTextLineAlignment lineAlignment, double angle, byte removePixelBrightness, byte pixelBrightness, bool isAdd) : base(layerIndex, location, lineType, pixelBrightness)
    {
        _font = font;
        _fontScale = fontScale;
        _thickness = thickness;
        _text = text;
        _mirror = mirror;
        _lineAlignment = lineAlignment;
        _angle = angle;
        IsAdd = isAdd;
        _removePixelBrightness = removePixelBrightness;

        int baseLine = 0;
        Size = EmguExtensions.GetTextSizeExtended(text, font, fontScale, thickness, ref baseLine, lineAlignment);
        Rectangle = new Rectangle(location, Size);
    }

    public override void CopyTo(PixelOperation operation)
    {
        base.CopyTo(operation);
        if (operation is not PixelText text) throw new TypeAccessException($"Expecting PixelText but got {operation.GetType().Name}");
        text.Font = _font;
        text.FontScale = _fontScale;
        text.Thickness = _thickness;
        text.Text = _text;
        text.Mirror = _mirror;
        text.LineAlignment = _lineAlignment;
        text.Angle = _angle;
        text.RemovePixelBrightness = _removePixelBrightness;
        text.IsAdd = IsAdd;
        text.Rectangle = Rectangle;
    }

    public override string ToString()
    {
        return $"{LineType}, {_font}, {_fontScale}px/{_thickness}px, "
               + (string.IsNullOrWhiteSpace(_text) ? string.Empty : $"{_text}, ")
               + $"{_lineAlignment}, {_angle}º, Layers: {_layersBelow}/{_layersAbove}";
    }

    public bool Equals(PixelText? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return base.Equals(other) && _font == other._font && _fontScale.Equals(other._fontScale) && _thickness == other._thickness && _text == other._text && _mirror == other._mirror && _lineAlignment == other._lineAlignment && _angle.Equals(other._angle) && _removePixelBrightness == other._removePixelBrightness && IsAdd == other.IsAdd && Rectangle.Equals(other.Rectangle);
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