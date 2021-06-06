/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;

namespace UVtools.Core.PixelEditor
{
    public class PixelText : PixelOperation
    {
        private FontFace _font;
        private double _fontScale = 1;
        private ushort _thickness = 1;
        private string _text;
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

        public bool IsAdd { get; }

        public byte Brightness => IsAdd ? _pixelBrightness : _removePixelBrightness;

        public Rectangle Rectangle { get; }

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

        
    }
}
