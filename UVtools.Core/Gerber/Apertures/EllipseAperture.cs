/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace UVtools.Core.Gerber.Apertures;

public class EllipseAperture : Aperture
{
    #region Properties
    public SizeF Axes { get; set; }
    #endregion

    #region Constructor
    public EllipseAperture(GerberDocument document) : base(document, "Ellipse") { }

    public EllipseAperture(GerberDocument document, int index, float width, float height) : this(document, index, new SizeF(width, height))
    {

    }

    public EllipseAperture(GerberDocument document, int index, SizeF axes) : base(document, index, "Ellipse")
    {
        Axes = document.GetMillimeters(axes);
    }
    #endregion

    public override void DrawFlashD3(Mat mat, SizeF xyPpmm, PointF at, MCvScalar color, LineType lineType = LineType.EightConnected)
    {
        CvInvoke.Ellipse(mat, 
            GerberDocument.PositionMmToPx(at, xyPpmm), 
            GerberDocument.SizeMmToPx(Axes.Width / 2, Axes.Height / 2, xyPpmm),
            0, 0, 360, color, -1, lineType);
    }
}