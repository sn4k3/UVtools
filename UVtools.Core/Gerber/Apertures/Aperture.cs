/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.Extensions;

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

    public GerberDocument Document { get; set; } = null!;

    #endregion

    protected Aperture() { }

    protected Aperture(GerberDocument document, int index)
    {
        Document = document;
        Index = index;
    }

    protected Aperture(GerberDocument document, string name)
    {
        Document = document;
        Name = name;
    }
    protected Aperture(GerberDocument document, int index, string name) : this(document, index) { Name = name; }

    public abstract void DrawFlashD3(Mat mat, PointF at, MCvScalar color, LineType lineType = LineType.EightConnected);

    public static Aperture? Parse(string line, GerberDocument document)
    {
        var match = Regex.Match(line, @"\%ADD(\d+)(\w+),?(\S+)?\*\%");
        if (!match.Success || match.Groups.Count < 3) return null;

        if (!int.TryParse(match.Groups[1].Value, out var index)) return null;
        //if (!char.TryParse(match.Groups[2].Value, out var type)) return null;

        switch (match.Groups[2].Value)
        {
            case "C":
            {
                if (match.Groups.Count < 4) return null;
                if (!double.TryParse(match.Groups[3].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var diameter)) return null;
                return new CircleAperture(document, index, diameter);
            }
            case "O":
            {
                if (match.Groups.Count < 4) return null;
                var split = match.Groups[3].Value.Split('X', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (split.Length < 2) return null;
                if (!float.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var width)) return null;
                if (!float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var height)) return null;

                return new EllipseAperture(document, index, width, height);
            }
            case "R":
            {
                if (match.Groups.Count < 4) return null;
                var split = match.Groups[3].Value.Split('X', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (split.Length < 2) return null;
                if (!float.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var width)) return null;
                if (!float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var height)) return null;

                return new RectangleAperture(document, index, width, height);
                }
            case "P":
            {
                if (match.Groups.Count < 4) return null;
                var split = match.Groups[3].Value.Split('X', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (split.Length < 2) return null;
                if (!double.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var diameter)) return null;
                if (!ushort.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var vertices)) return null;

                return new PolygonAperture(document, index, diameter, vertices);
            }
            default: // macro
            {
                if (!document.Macros.TryGetValue(match.Groups[2].Value, out var macro)) return null;
                macro = macro.Clone();
                //var parseLine = line.TrimEnd('%', '*');
                //var commaIndex = parseLine.IndexOf(',')+1;
                //parseLine = parseLine[commaIndex..];
                string[] args = { "0" };
                if (match.Groups.Count >= 4)
                {
                    args = args.Concat(match.Groups[3].Value.Split('X', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).ToArray();
                }
                
                foreach (var primitive in macro)
                {
                    primitive.ParseExpressions(args);
                }

                return new MacroAperture(document, index, macro);
            }
        }
    }
}