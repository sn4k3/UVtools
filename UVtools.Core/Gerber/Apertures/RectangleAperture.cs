/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace UVtools.Core.Gerber.Apertures;

public class RectangleAperture : Aperture
{
    #region Properties
    public SizeF Size { get; set; }
    #endregion

    #region Constructor
    public RectangleAperture(GerberDocument document) : base(document, "Rectangle") { }

    public RectangleAperture(GerberDocument document, int index, float width, float height) : this(document, index, new SizeF(width, height))
    { }

    public RectangleAperture(GerberDocument document, int index, SizeF size) : base(document, index, "Rectangle")
    {
        Size = document.GetMillimeters(size);
    }
    #endregion

    public override void DrawFlashD3(Mat mat, SizeF xyPpmm, PointF at, MCvScalar color,
        LineType lineType = LineType.EightConnected)
    {
        at = new PointF(Math.Max(0, at.X - Size.Width / 2), Math.Max(0, at.Y - Size.Height / 2));
        CvInvoke.Rectangle(mat, new Rectangle(GerberDocument.PositionMmToPx(at, xyPpmm), GerberDocument.SizeMmToPx(Size, xyPpmm)), color, -1, lineType);
    }
}