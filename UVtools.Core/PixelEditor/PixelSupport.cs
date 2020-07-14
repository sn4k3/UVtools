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
    public class PixelSupport : PixelOperation
    {
        public byte TipDiameter { get; }

        public byte PillarDiameter { get; }

        public byte BaseDiameter { get; }

        public PixelSupport(uint layerIndex, Point location, byte tipDiameter, byte pillarDiameter, byte baseDiameter) : base(PixelOperationType.Supports, layerIndex, location)
        {
            TipDiameter = tipDiameter;
            PillarDiameter = pillarDiameter;
            BaseDiameter = baseDiameter;
        }
    }
}
