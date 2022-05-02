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

public class RectangleAperture : Aperture
{
    #region Properties
    public SizeF Size { get; set; }
    #endregion

    #region Constructor
    public RectangleAperture() : base("Rectangle") { }

    public RectangleAperture(int index, float width, float height) : base(index, "Rectangle")
    {
        Size = new SizeF(width, height);
    }

    public RectangleAperture(int index, SizeF size) : base(index, "Rectangle")
    {
        Size = size;
    }
    #endregion

    public override void DrawFlashD3(Mat mat, SizeF xyPpmm, Point at, MCvScalar color,
        LineType lineType = LineType.EightConnected)
    {
        var size = new Size((int) (Size.Width * xyPpmm.Width), (int) (Size.Height * xyPpmm.Height));
        at.Offset(-size.Width / 2, -size.Height / 2);
        CvInvoke.Rectangle(mat, new Rectangle(at, size), color, -1, lineType);
    }
}