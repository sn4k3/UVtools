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

    public abstract void DrawFlashD3(Mat mat, PointF at, MCvScalar color, LineType lineType = LineType.EightConnected);

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

    protected static MCvScalar InvertColor(MCvScalar color)
        => color.Equals(EmguCvExtensions.BlackColor)
            ? EmguCvExtensions.WhiteColor
            : EmguCvExtensions.BlackColor;
}
