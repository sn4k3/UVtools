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
using System;
using System.Drawing;
using System.Globalization;
using EmguExtensions;

namespace UVtools.Core.Gerber.Apertures;

public abstract class Aperture
{
    #region Properties
    /// <summary>
    /// Gets the index of this aperture
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets the aperture name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    public GerberFormat Document { get; set; } = null!;

    #endregion

    protected Aperture() { }

    protected Aperture(GerberFormat document, int index)
    {
        Document = document;
        Index = index;
    }

    protected Aperture(GerberFormat document, string name)
    {
        Document = document;
        Name = name;
    }
    protected Aperture(GerberFormat document, int index, string name) : this(document, index) { Name = name; }

    /// <summary>
    /// Gets whether the aperture has areas that are not exposed, a hole or a macro primitive with the exposure off.
    /// They are see-through: the image under them is kept rather than painted with the opposite color.
    /// </summary>
    protected virtual bool HasClearAreas => false;

    /// <summary>
    /// Flashes the aperture at a position with the current polarity color.
    /// </summary>
    /// <param name="mat">Image to draw on</param>
    /// <param name="at">Position in millimeters</param>
    /// <param name="color">Polarity color, white to add and black to clear</param>
    /// <param name="lineType">Line type</param>
    public void DrawFlashD3(Mat mat, PointF at, MCvScalar color, LineType lineType = LineType.EightConnected)
    {
        if (!HasClearAreas)
        {
            DrawShape(mat, at, color, lineType);
            return;
        }

        // The shape is drawn as a mask of its exposed area, then only that area takes the polarity color,
        // so the holes don't erase what is under them nor add copper on a clear flash
        var bounds = GetBoundsMm();
        if (bounds.IsEmpty) return;

        var topLeft = Document.PositionMmToPx(at.X + bounds.Left, at.Y + bounds.Top);
        var bottomRight = Document.PositionMmToPx(at.X + bounds.Right, at.Y + bounds.Bottom);
        var rectangle = Rectangle.FromLTRB(topLeft.X - 2, topLeft.Y - 2, bottomRight.X + 3, bottomRight.Y + 3);
        rectangle.Intersect(new Rectangle(Point.Empty, mat.Size));
        if (rectangle.Width <= 0 || rectangle.Height <= 0) return;

        using var mask = new Mat(rectangle.Size, DepthType.Cv8U, 1);
        mask.SetTo(EmguCvExtensions.BlackColor);
        Document.DrawOrigin = rectangle.Location;
        try
        {
            DrawShape(mask, at, EmguCvExtensions.WhiteColor, lineType);
        }
        finally
        {
            Document.DrawOrigin = Point.Empty;
        }

        using var roi = mat.Roi(rectangle);
        if (color.Equals(EmguCvExtensions.BlackColor))
        {
            CvInvoke.BitwiseNot(mask, mask);
            CvInvoke.Min(roi, mask, roi);
        }
        else
        {
            CvInvoke.Max(roi, mask, roi);
        }
    }

    /// <summary>
    /// Draws the aperture at a position, the exposed areas with <paramref name="color"/> and the clear areas with the
    /// opposite color.
    /// </summary>
    protected abstract void DrawShape(Mat mat, PointF at, MCvScalar color, LineType lineType);

    /// <summary>
    /// Gets the area the aperture covers in millimeters, relative to its flash position and with
    /// <see cref="GerberFormat.SizeScale"/> applied, or an empty rectangle when it draws nothing.
    /// </summary>
    protected abstract RectangleF GetBoundsMm();

    /// <summary>
    /// Gets a rectangle centered on the flash position, in millimeters.
    /// </summary>
    protected RectangleF GetCenteredBoundsMm(double width, double height)
    {
        var scaledWidth = (float)(width * Document.SizeScale);
        var scaledHeight = (float)(height * Document.SizeScale);
        return new RectangleF(-scaledWidth / 2, -scaledHeight / 2, scaledWidth, scaledHeight);
    }

    public static Aperture? Parse(string line, GerberFormat document)
    {
        line = line.Trim();
        if (!line.StartsWith("%ADD", StringComparison.Ordinal) ||
            !line.EndsWith("*%", StringComparison.Ordinal) ||
            line.Length <= 6)
        {
            return null;
        }

        var definition = line.AsSpan(4, line.Length - 6);
        var indexLength = 0;
        while (indexLength < definition.Length && char.IsAsciiDigit(definition[indexLength]))
        {
            indexLength++;
        }

        if (indexLength == 0 ||
            !int.TryParse(definition[..indexLength], NumberStyles.None,
                CultureInfo.InvariantCulture, out var index))
        {
            return null;
        }

        var apertureDefinition = definition[indexLength..];
        var separatorIndex = apertureDefinition.IndexOf(',');
        var template = (separatorIndex < 0
            ? apertureDefinition
            : apertureDefinition[..separatorIndex]).Trim().ToString();
        var modifiers = separatorIndex < 0
            ? string.Empty
            : apertureDefinition[(separatorIndex + 1)..].Trim().ToString();
        if (template.Length == 0) return null;

        var split = modifiers.Length == 0
            ? []
            : modifiers.Split(['X', 'x'], StringSplitOptions.TrimEntries);
        switch (template)
        {
            case "C":
            {
                if (split.Length is < 1 or > 2 ||
                    !double.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var diameter) ||
                    !double.IsFinite(diameter) || diameter <= 0)
                {
                    return null;
                }

                var holeDiameter = 0.0;
                if (split.Length > 1 &&
                    (!double.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out holeDiameter) ||
                     !double.IsFinite(holeDiameter) || holeDiameter < 0))
                {
                    return null;
                }

                return new CircleAperture(document, index, diameter, holeDiameter);
            }
            case "O": // OBround
            {
                if (split.Length is < 2 or > 3 ||
                    !float.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var width) ||
                    !float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var height) ||
                    !float.IsFinite(width) || !float.IsFinite(height) || width <= 0 || height <= 0)
                {
                    return null;
                }

                var holeDiameter = 0.0;
                if (split.Length > 2 &&
                    (!double.TryParse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture, out holeDiameter) ||
                     !double.IsFinite(holeDiameter) || holeDiameter < 0))
                {
                    return null;
                }

                return new ObroundAperture(document, index, width, height, holeDiameter);
            }
            case "R":
            {
                if (split.Length is < 2 or > 3 ||
                    !float.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var width) ||
                    !float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var height) ||
                    !float.IsFinite(width) || !float.IsFinite(height) || width <= 0 || height <= 0)
                {
                    return null;
                }

                var holeDiameter = 0.0;
                if (split.Length > 2 &&
                    (!double.TryParse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture, out holeDiameter) ||
                     !double.IsFinite(holeDiameter) || holeDiameter < 0))
                {
                    return null;
                }

                return new RectangleAperture(document, index, width, height, holeDiameter);
            }
            case "P":
            {
                if (split.Length is < 2 or > 4 ||
                    !double.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var diameter) ||
                    !ushort.TryParse(split[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var vertices) ||
                    !double.IsFinite(diameter) || diameter <= 0 || vertices is < 3 or > 12)
                {
                    return null;
                }

                var rotation = 0.0;
                if (split.Length > 2 &&
                    (!double.TryParse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture, out rotation) ||
                     !double.IsFinite(rotation)))
                {
                    return null;
                }

                var holeDiameter = 0.0;
                if (split.Length > 3 &&
                    (!double.TryParse(split[3], NumberStyles.Float, CultureInfo.InvariantCulture, out holeDiameter) ||
                     !double.IsFinite(holeDiameter) || holeDiameter < 0))
                {
                    return null;
                }

                return new PolygonAperture(document, index, diameter, vertices, rotation, holeDiameter);
            }
            default: // macro
            {
                if (!document.Macros.TryGetValue(template, out var macro)) return null;
                macro = macro.Clone();
                var args = new string[split.Length + 1];
                args[0] = "0";
                for (var argumentIndex = 0; argumentIndex < split.Length; argumentIndex++)
                {
                    if (!double.TryParse(split[argumentIndex], NumberStyles.Float,
                            CultureInfo.InvariantCulture, out var argument) ||
                        !double.IsFinite(argument))
                    {
                        return null;
                    }

                    args[argumentIndex + 1] =
                        argument.ToString("R", CultureInfo.InvariantCulture);
                }

                if (!macro.ParseExpressions(args)) return null;

                return new MacroAperture(document, index, macro);
            }
        }
    }

    protected static MCvScalar InvertColor(MCvScalar color) => GerberFormat.InvertColor(color);
}
