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

        /// <summary>
        /// Gets or sets the index number to show on GUI
        /// </summary>
        public uint Index { get; set; }

        /// <summary>
        /// Gets the <see cref="PixelOperationType"/>
        /// </summary>
        public PixelOperationType OperationType { get; }

        /// <summary>
        /// Gets the layer index
        /// </summary>
        public uint LayerIndex { get; }

        /// <summary>
        /// Gets the location of the operation
        /// </summary>
        public Point Location { get; }

        /// <summary>
        /// Gets the total size of the operation
        /// </summary>
        public Size Size { get; private protected set; } = Size.Empty;


        public PixelOperation(PixelOperationType operationType, uint layerIndex, Point location)
        {
            OperationType = operationType;
            Location = location;
            LayerIndex = layerIndex;
        }
    }
}
