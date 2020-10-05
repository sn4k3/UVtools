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

namespace UVtools.Core.PixelEditor
{
    public class PixelDrawing : PixelOperation
    {
        private BrushShapeType _brushShape = BrushShapeType.Rectangle;
        private ushort _brushSize = 1;
        private short _thickness = -1;
        public const byte MinRectangleBrush = 1;
        public const byte MinCircleBrush = 7;
        public enum BrushShapeType : byte
        {
            Rectangle = 0,
            Circle
        }

        public override PixelOperationType OperationType => PixelOperationType.Drawing;

        public static BrushShapeType[] BrushShapeTypes => (BrushShapeType[])Enum.GetValues(typeof(BrushShapeType));

        public BrushShapeType BrushShape
        {
            get => _brushShape;
            set
            {
                if (!RaiseAndSetIfChanged(ref _brushShape, value)) return;
                if (_brushShape == BrushShapeType.Circle)
                {
                    BrushSize = Math.Max(MinCircleBrush, BrushSize);
                }
            }
        }

        public ushort BrushSize
        {
            get => _brushSize;
            set => RaiseAndSetIfChanged(ref _brushSize, value);
        }

        public short Thickness
        {
            get => _thickness;
            set => RaiseAndSetIfChanged(ref _thickness, value);
        }

        public bool IsAdd { get;  }

        public byte Color { get; }

        public Rectangle Rectangle { get; }

        public PixelDrawing() 
        {

        }

        public PixelDrawing(uint layerIndex, Point location, LineType lineType, BrushShapeType brushShape, ushort brushSize, short thickness,  bool isAdd) : base(layerIndex, location, lineType)
        {
            BrushShape = brushShape;
            BrushSize = brushSize;
            Thickness = thickness;
            IsAdd = isAdd;
            

            Color = (byte) (isAdd ? 255 : 0);

            int shiftPos = brushSize / 2;
            Rectangle = new Rectangle(Math.Max(0, location.X - shiftPos), Math.Max(0, location.Y - shiftPos), brushSize-1, brushSize-1);
            Size = new Size(BrushSize, BrushSize);
        }

        
    }
}
