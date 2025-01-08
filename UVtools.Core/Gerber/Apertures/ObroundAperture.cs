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
        var location = Document.PositionMmToPx(at);
        // Calculate radii of the semicircles
        var radius = Document.SizeMmToPx(Axes.Width / 2, Axes.Height / 2);
        var radiusFromHeight = Document.SizeMmToPx(Axes.Height / 2, Axes.Height / 2);
        var diameter = Document.SizeMmToPx(Axes.Width, Axes.Height);

        // Calculate centers for the semicircles
        var leftCircleCenter = location with { X = location.X - radius.Width + radiusFromHeight.Width };
        var rightCircleCenter = location with { X = location.X + radius.Width - radiusFromHeight.Width };

        // Draw the two semicircles
        CvInvoke.Ellipse(mat, leftCircleCenter, radiusFromHeight, 0, 90, 270, color, -1, lineType);
        CvInvoke.Ellipse(mat, rightCircleCenter, radiusFromHeight, 0, -90, 90, color, -1, lineType);

        /*CvInvoke.Ellipse(mat,
            location,
            radius,
            0, 0, 360, color, -1, lineType);*/

        // Draw the rectangle connecting the semicircles
        var rect = new Rectangle(leftCircleCenter with { Y = location.Y - radius.Height },
            diameter with { Width = diameter.Width - diameter.Height });
        CvInvoke.Rectangle(mat, rect, color, -1, lineType);

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