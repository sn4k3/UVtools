/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

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

    #endregion

    protected Aperture() { }

    protected Aperture(int index) { Index = index; }
    protected Aperture(string name) { Name = name; }
    protected Aperture(int index, string name) : this(index) { Name = name; }

    public abstract void DrawFlashD3(Mat mat, SizeF xyPpmm, Point at, MCvScalar color, LineType lineType = LineType.EightConnected);

    public static Aperture? Parse(string line, GerberDocument document)
    {
        var match = Regex.Match(line, @"\%ADD(\d+)(\S+),(\S+)\*\%");
        if (!match.Success || match.Groups.Count < 4) return null;

        if (!int.TryParse(match.Groups[1].Value, out var index)) return null;
        //if (!char.TryParse(match.Groups[2].Value, out var type)) return null;


        switch (match.Groups[2].Value)
        {
            case "C":
            {
                if (!double.TryParse(match.Groups[3].Value, out var diameter)) return null;
                return new CircleAperture(index, diameter);
            }
            case "O":
            {
                var split = match.Groups[3].Value.Split('X', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (split.Length < 2) return null;
                if (!float.TryParse(split[0], out var width)) return null;
                if (!float.TryParse(split[1], out var height)) return null;

                return new EllipseAperture(index, width, height);
            }
            case "R":
            {
                var split = match.Groups[3].Value.Split('X', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (split.Length < 2) return null;
                if (!float.TryParse(split[0], out var width)) return null;
                if (!float.TryParse(split[1], out var height)) return null;

                return new RectangleAperture(index, width, height);
                }
            case "P":
            {
                var split = match.Groups[3].Value.Split('X', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (split.Length < 2) return null;
                if (!double.TryParse(split[0], out var diameter)) return null;
                if (!ushort.TryParse(split[1], out var vertices)) return null;

                return new PolygonAperture(index, diameter, vertices);
            }
            default: // macro
            {
                if (!document.Macros.TryGetValue(match.Groups[2].Value, out var macro)) return null;
                var parseLine = line.TrimEnd('%', '*');
                var commaIndex = parseLine.IndexOf(',')+1;
                if (commaIndex == 0) return null;
                parseLine = parseLine[commaIndex..];
                var args = new[] {"0"}.Concat(parseLine.Split('X', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).ToArray();
                foreach (var primitive in macro)
                {
                    primitive.ParseExpressions(args);
                }

                return new MacroAperture(index, macro);
            }
        }
    }
}