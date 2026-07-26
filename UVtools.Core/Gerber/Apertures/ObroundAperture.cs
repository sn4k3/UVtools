/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Drawing;

namespace UVtools.Core.Gerber.Apertures;

public class ObroundAperture : Aperture
{
    #region Properties
    public SizeF Axes { get; set; }
    public double HoleDiameter { get; set; }
    #endregion

    #region Constructor
    public ObroundAperture(GerberFormat document) : base(document, "Obround") { }

    public ObroundAperture(GerberFormat document, int index, float width, float height, double holeDiameter = 0) : this(document, index, new SizeF(width, height), holeDiameter)
    {

    }

    public ObroundAperture(GerberFormat document, int index, SizeF axes, double holeDiameter = 0) : base(document, index, "Obround")
    {
        Axes = document.GetMillimeters(axes);
        if (holeDiameter > 0) HoleDiameter = document.GetMillimeters(holeDiameter);
    }
    #endregion

    public override void DrawFlashD3(Mat mat, PointF at, MCvScalar color, LineType lineType = LineType.EightConnected)
    {
        if (Axes.Width <= 0 || Axes.Height <= 0) return;

        var location = Document.PositionMmToPx(at);

        // full (width,height) in pixels
        var fullPx = Document.SizeMmToPx(Axes.Width, Axes.Height);

        // half sizes (halfWidth, halfHeight) in pixels
        var halfPx = Document.SizeMmToPx(Axes.Width / 2.0, Axes.Height / 2.0);

        // "circle" radii derived from half-height and half-width (square sizes)
        var halfFromHeight = Document.SizeMmToPx(Axes.Height / 2.0, Axes.Height / 2.0); // (halfHeightPx, halfHeightPx)
        var halfFromWidth = Document.SizeMmToPx(Axes.Width / 2.0, Axes.Width / 2.0); // (halfWidthPx, halfWidthPx)

        // If wider than tall -> left/right semicircles + horizontal rectangle
        if (fullPx.Width >= fullPx.Height)
        {
            var leftCenterX = location.X - halfPx.Width + halfFromHeight.Width;
            var rightCenterX = location.X + halfPx.Width - halfFromHeight.Width;

            var leftCenter = new Point(leftCenterX, location.Y);
            var rightCenter = new Point(rightCenterX, location.Y);

            // draw end-caps (full circles) and the connecting rectangle
            CvInvoke.Ellipse(mat, leftCenter, halfFromHeight, 0, 0, 360, color, -1, lineType);
            CvInvoke.Ellipse(mat, rightCenter, halfFromHeight, 0, 0, 360, color, -1, lineType);

            var rectX = leftCenter.X;
            var rectY = location.Y - halfFromHeight.Height;
            var rectW = (int)Math.Round(fullPx.Width - 2.0 * halfFromHeight.Width, MidpointRounding.AwayFromZero); // width - 2 * halfHeightPx
            var rectH = (int)Math.Round(2.0 * halfFromHeight.Height, MidpointRounding.AwayFromZero);

            if (rectW > 0 && rectH > 0)
                CvInvoke.Rectangle(mat, new Rectangle(rectX, rectY, rectW, rectH), color, -1, lineType);
        }
        else
        {
            // Taller than wide -> top/bottom end-caps + vertical rectangle
            var topCenterY = location.Y - halfPx.Height + halfFromWidth.Height;
            var bottomCenterY = location.Y + halfPx.Height - halfFromWidth.Height;

            var topCenter = new Point(location.X, topCenterY);
            var bottomCenter = new Point(location.X, bottomCenterY);

            CvInvoke.Ellipse(mat, topCenter, halfFromWidth, 0, 0, 360, color, -1, lineType);
            CvInvoke.Ellipse(mat, bottomCenter, halfFromWidth, 0, 0, 360, color, -1, lineType);

            var rectX = location.X - halfFromWidth.Width;
            var rectY = topCenter.Y;
            var rectW = (int)Math.Round(2.0 * halfFromWidth.Width, MidpointRounding.AwayFromZero);
            var rectH = (int)Math.Round(fullPx.Height - 2.0 * halfFromWidth.Height, MidpointRounding.AwayFromZero); // height - 2 * halfWidthPx

            if (rectW > 0 && rectH > 0)
                CvInvoke.Rectangle(mat, new Rectangle(rectX, rectY, rectW, rectH), color, -1, lineType);
        }

        // hole (unchanged)
        if (HoleDiameter > 0)
        {
            CvInvoke.Ellipse(mat,
                location,
                Document.SizeMmToPx(HoleDiameter / 2.0, HoleDiameter / 2.0),
                0, 0, 360, InvertColor(color), -1, lineType);
        }
    }
}
