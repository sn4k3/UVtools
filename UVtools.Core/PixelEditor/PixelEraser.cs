/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System.Drawing;
using Emgu.CV.CvEnum;

namespace UVtools.Core.PixelEditor
{
    public class PixelEraser : PixelOperation
    {
        public const byte Diameter = 4;

        public override PixelOperationType OperationType => PixelOperationType.Eraser;

        public PixelEraser()
        {
            _pixelBrightness = 0;
        }

        public PixelEraser(uint layerIndex, Point location, byte pixelBrightness) : base(layerIndex, location, LineType.AntiAlias, pixelBrightness)
        {
            Size = new Size(Diameter, Diameter);
        }
    }
}
