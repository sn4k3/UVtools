/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System.Drawing;

namespace UVtools.Core.PixelEditor
{
    public class PixelEraser : PixelOperation
    {
        public const byte Diameter = 4;

        public override PixelOperationType OperationType => PixelOperationType.Eraser;

        public PixelEraser()
        {
        }

        public PixelEraser(uint layerIndex, Point location) : base(layerIndex, location)
        {
            Size = new Size(Diameter, Diameter);
        }
    }
}
