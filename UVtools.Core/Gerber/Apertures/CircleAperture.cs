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
using UVtools.Core.Extensions;

namespace UVtools.Core.Gerber.Apertures;

public class CircleAperture : Aperture
{
    #region Properties
    public double Diameter { get; set; }
    #endregion

    #region Constructor
    public CircleAperture() : base("Circle") { }

    public CircleAperture(int index, double diameter) : base(index, "Circle")
    {
        Diameter = diameter;
    }
    #endregion

    public override void DrawFlashD3(Mat mat, SizeF xyPpmm, PointF at, MCvScalar color, LineType lineType = LineType.EightConnected)
    {
        CvInvoke.Circle(mat, 
            GerberDocument.PositionMmToPx(at, xyPpmm),
            GerberDocument.SizeMmToPx(Diameter / 2, xyPpmm.Max()), color, -1, lineType);
    }
}