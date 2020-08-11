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

        public PixelEraser(uint layerIndex, Point location) : base(PixelOperationType.Eraser, layerIndex, location)
        {
            Size = new Size(Diameter, Diameter);
        }
    }
}
