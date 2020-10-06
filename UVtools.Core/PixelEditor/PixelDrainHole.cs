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
        private byte _diameter = 50;
        public override PixelOperationType OperationType => PixelOperationType.DrainHole;

        public byte Diameter
        {
            get => _diameter;
            set => RaiseAndSetIfChanged(ref _diameter, value);
        }

        public PixelDrainHole(){}

        public PixelDrainHole(uint layerIndex, Point location, byte diameter) : base(layerIndex, location)
        {
            Diameter = diameter;
            Size = new Size(diameter, diameter);
        }
    }
}
