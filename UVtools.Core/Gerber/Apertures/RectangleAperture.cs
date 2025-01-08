/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Microsoft.CodeAnalysis;
using System;
using System.Drawing;
using UVtools.Core.Extensions;

namespace UVtools.Core.Gerber.Apertures;

public class RectangleAperture : Aperture
{
    #region Properties
    public SizeF Size { get; set; }
    public double HoleDiameter { get; set; }
    #endregion

    #region Constructor
    public RectangleAperture(GerberFormat document) : base(document, "Rectangle") { }

    public RectangleAperture(GerberFormat document, int index, float width, float height, double holeDiameter = 0) : this(document, index, new SizeF(width, height), holeDiameter)
    {
    }

    public RectangleAperture(GerberFormat document, int index, SizeF size, double holeDiameter = 0) : base(document, index, "Rectangle")
    {
        Size = document.GetMillimeters(size);
        if (holeDiameter > 0) HoleDiameter = document.GetMillimeters(holeDiameter);
    }
    #endregion

    public override void DrawFlashD3(Mat mat, PointF at, MCvScalar color, LineType lineType = LineType.EightConnected)
    {
        var location = Document.PositionMmToPx(at);

        at = new PointF(Math.Max(0, at.X - Size.Width / 2), Math.Max(0, at.Y - Size.Height / 2));
        CvInvoke.Rectangle(mat, new Rectangle(Document.PositionMmToPx(at), Document.SizeMmToPx(Size)), color, -1, lineType);

        if (HoleDiameter > 0)
        {
            var invertColor = color.Equals(EmguExtensions.BlackColor) ? EmguExtensions.WhiteColor : EmguExtensions.BlackColor;
            CvInvoke.Ellipse(mat,
                location,
                Document.SizeMmToPx(HoleDiameter / 2.0, HoleDiameter / 2.0),
                0, 0, 360, invertColor, -1, lineType);
        }
    }
}