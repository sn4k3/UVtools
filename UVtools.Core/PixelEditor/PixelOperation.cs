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
    public class PixelOperation
    {
        public enum PixelOperationType : byte
        {
            Drawing,
            Supports,
            DrainHole,
        }

        public PixelOperationType OperationType { get; }
        public uint LayerIndex { get; }

        public Point Location { get; }


        public PixelOperation(PixelOperationType operationType, uint layerIndex, Point location)
        {
            OperationType = operationType;
            Location = location;
            LayerIndex = layerIndex;
        }
    }
}
