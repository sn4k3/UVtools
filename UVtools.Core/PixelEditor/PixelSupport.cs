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
        public override PixelOperationType OperationType => PixelOperationType.Supports;

        public byte TipDiameter { get; set; }

        public byte PillarDiameter { get; set; }

        public byte BaseDiameter { get; set; }

        public PixelSupport(){}

        public PixelSupport(uint layerIndex, Point location, byte tipDiameter, byte pillarDiameter, byte baseDiameter) : base(layerIndex, location)
        {
            TipDiameter = tipDiameter;
            PillarDiameter = pillarDiameter;
            BaseDiameter = baseDiameter;
            Size = new Size(TipDiameter, TipDiameter);
        }
    }
}
