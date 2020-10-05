/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using Emgu.CV.CvEnum;
using UVtools.Core.Objects;

namespace UVtools.Core.PixelEditor
{
    public abstract class PixelOperation : BindableBase
    {
        private uint _index;

        public enum PixelOperationType : byte
        {
            Drawing,
            Text,
            Eraser,
            Supports,
            DrainHole,
        }

        /// <summary>
        /// Gets or sets the index number to show on GUI
        /// </summary>
        public uint Index
        {
            get => _index;
            set => RaiseAndSetIfChanged(ref _index, value);
        }

        /// <summary>
        /// Gets the <see cref="PixelOperationType"/>
        /// </summary>
        public abstract PixelOperationType OperationType { get; }

        /// <summary>
        /// Gets the layer index
        /// </summary>
        public uint LayerIndex { get; }

        /// <summary>
        /// Gets the location of the operation
        /// </summary>
        public Point Location { get; }

        /// <summary>
        /// Gets the <see cref="LineType"/> for the draw operation
        /// </summary>
        public LineType LineType { get; set; } = LineType.AntiAlias;

        public LineType[] LineTypes => new[]
        {
            LineType.FourConnected,
            LineType.EightConnected,
            LineType.AntiAlias
        };

        public uint LayersBelow { get; set; }

        public uint LayersAbove { get; set; }

        /// <summary>
        /// Gets the total size of the operation
        /// </summary>
        public Size Size { get; private protected set; } = Size.Empty;

        protected PixelOperation()
        {
        }

        protected PixelOperation(uint layerIndex, Point location, LineType lineType = LineType.AntiAlias)
        {
            Location = location;
            LayerIndex = layerIndex;
            LineType = lineType;
        }

        public PixelOperation Clone()
        {
            return (PixelOperation) MemberwiseClone();
        }
    }
}
