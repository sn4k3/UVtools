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

    protected override bool HasClearAreas => HoleDiameter > 0;

    protected override RectangleF GetBoundsMm() =>
        Size.Width > 0 && Size.Height > 0 ? GetCenteredBoundsMm(Size.Width, Size.Height) : RectangleF.Empty;

    protected override void DrawShape(Mat mat, PointF at, MCvScalar color, LineType lineType)
    {
        if (Size.Width <= 0 || Size.Height <= 0) return;

        var location = Document.PositionMmToPx(at);

        // Centered on the scaled size, a corner taken from the unscaled size grew the pad to one side only
        DrawCenteredRectangle(mat, location, Document.SizeMmToPx(Size), color, lineType);

        if (HoleDiameter > 0)
        {
            CvInvoke.Ellipse(mat,
                location,
                Document.SizeMmToPx(HoleDiameter / 2.0, HoleDiameter / 2.0),
                0, 0, 360, InvertColor(color), -1, lineType);
        }
    }

    /// <summary>
    /// Fills a rectangle of exactly <paramref name="size"/> pixels centered on a pixel. The filled rectangle includes
    /// its right and bottom edges, so a <see cref="Rectangle"/> of that size would be one pixel larger on each axis.
    /// </summary>
    internal static void DrawCenteredRectangle(Mat mat, Point center, Size size, MCvScalar color, LineType lineType)
    {
        var left = center.X - size.Width / 2;
        var top = center.Y - size.Height / 2;
        CvInvoke.Rectangle(mat, new Rectangle(left, top, size.Width - 1, size.Height - 1), color, -1, lineType);
    }
}
