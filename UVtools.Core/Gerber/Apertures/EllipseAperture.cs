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
    public EllipseAperture() : base("Ellipse") { }

    public EllipseAperture(int index, float width, float height) : base(index, "Ellipse")
    {
        Axes = new SizeF(width, height);
    }

    public EllipseAperture(int index, SizeF axes) : base(index, "Ellipse")
    {
        Axes = axes;
    }
    #endregion

    public override void DrawFlashD3(Mat mat, SizeF xyPpmm, Point at, MCvScalar color,
        LineType lineType = LineType.EightConnected)
    {
        var axes = new Size((int) (Axes.Width * xyPpmm.Width / 2), (int) (Axes.Height * xyPpmm.Height / 2));
        CvInvoke.Ellipse(mat, at, axes, 0, 0, 360, color, -1, lineType);
    }
}