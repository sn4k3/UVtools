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
    public class PixelDrainHole : PixelOperation
    {
        public byte Diameter { get; }

        public PixelDrainHole(uint layerIndex, Point location, byte diameter) : base(PixelOperationType.DrainHole, layerIndex, location)
        {
            Diameter = diameter;
        }
    }
}
