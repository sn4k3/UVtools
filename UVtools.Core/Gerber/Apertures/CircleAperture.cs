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
using System.Drawing;
using UVtools.Core.Extensions;

namespace UVtools.Core.Gerber.Apertures;

public class CircleAperture : Aperture
{
    #region Properties
    public double Diameter { get; set; }
    public double HoleDiameter { get; set; }
    #endregion

    #region Constructor
    public CircleAperture(GerberFormat document) : base(document, "Circle") { }

    public CircleAperture(GerberFormat document, int index, double diameter, double holeDiameter = 0) : base(document, index, "Circle")
    {
        Diameter = document.GetMillimeters(diameter);
        if (holeDiameter > 0) HoleDiameter = document.GetMillimeters(holeDiameter);
    }
    #endregion

    public override void DrawFlashD3(Mat mat, PointF at, MCvScalar color, LineType lineType = LineType.EightConnected)
    {
        var location = Document.PositionMmToPx(at);
        CvInvoke.Ellipse(mat, location, Document.SizeMmToPx(Diameter / 2.0, Diameter / 2.0), 0, 0, 360, color, -1, lineType);
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