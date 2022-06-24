/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using UVtools.Core.Extensions;
using UVtools.Core.Gerber.Apertures;

namespace UVtools.Core.Gerber;

public class GerberDocument
{
    #region Properties

    public GerberPositionType PositionType { get; set; } = GerberPositionType.Absolute;
    public GerberUnitType UnitType { get; set; } = GerberUnitType.Millimeter;
    public GerberPolarityType Polarity { get; set; } = GerberPolarityType.Dark;
    public GerberMoveType MoveType { get; set; } = GerberMoveType.Linear;

    public bool LeadingZeroOmitted { get; set; } = true;

    public byte CoordinateXIntegers { get; set; } = 3;
    public byte CoordinateXFractionalDigits { get; set; } = 6;

    public byte CoordinateYIntegers { get; set; } = 3;
    public byte CoordinateYFractionalDigits { get; set; } = 6;

    public Dictionary<int, Aperture> Apertures { get; } = new();
    public Dictionary<string, Macro> Macros { get; } = new();

    #endregion


    public GerberDocument()
    {
    }

    public GerberDocument(string filePath)
    {
    }

    public static GerberDocument ParseAndDraw(string filePath, Mat mat, SizeF xyPpmm, bool enableAntialiasing = false)
    {
        using var file = new StreamReader(filePath);
        var document = new GerberDocument();

        int FSlength = "%FSLAX46Y46*%".Length;
        int MOlength = "%MOMM*%".Length;
        int LPlength = "%LPD*%".Length;

        double currentX = 0;
        double currentY = 0;
        Aperture? currentAperture = null;
        Macro? currentMacro = null;
        var regionPoints = new List<Point>();
        bool insideRegion = false;
        string? line;
        while ((line = file.ReadLine()) is not null)
        {
            line = line.Trim();
            if(line == string.Empty) continue;
            if (line.StartsWith("M02")) break;

            var accumulatedLine = line;
            while (!accumulatedLine.Contains('*') && (line = file.ReadLine()) is not null)
            {
                line = line.Trim();
                if (line == string.Empty) continue;
                accumulatedLine += line;
            }

            line = accumulatedLine;

            if(currentMacro is not null)
            {
                currentMacro.ParsePrimitive(line);
                if (line[^1] == '%') currentMacro = null;
                continue;
            }

            if (line.StartsWith("%MO") && line.Length >= MOlength)
            {
                if(line[3] == 'M' && line[4] == 'M') document.UnitType = GerberUnitType.Millimeter;
                else if(line[3] == 'I' && line[4] == 'N') document.UnitType = GerberUnitType.Inch;
                continue;
            }

            if (line.StartsWith("%FS") && line.Length >= FSlength) 
            {
                // %FSLAX34Y34*%
                // 0123456789
                document.LeadingZeroOmitted = line[3] switch
                {
                    'L' => true,
                    'T' => false,
                    _ => document.LeadingZeroOmitted
                };
                document.PositionType = line[4] switch
                {
                    'A' => GerberPositionType.Absolute,
                    'I' => GerberPositionType.Relative,
                    _ => document.PositionType
                };
                if (line[5] != 'X') continue;
                if (byte.TryParse(line[6].ToString(), out var x1)) document.CoordinateXIntegers = x1;
                if (byte.TryParse(line[7].ToString(), out var x2)) document.CoordinateXFractionalDigits = x2;
                if (line[8] != 'Y') continue;
                if (byte.TryParse(line[9].ToString(), out var y1)) document.CoordinateYIntegers = y1;
                if (byte.TryParse(line[10].ToString(), out var y2)) document.CoordinateYFractionalDigits = y2;
                continue;
            }

            if (line.StartsWith("%LP") && line.Length >= LPlength)
            {
                document.Polarity = line[3] switch
                {
                    'D' => GerberPolarityType.Dark,
                    'C' => GerberPolarityType.Clear,
                    _ => document.Polarity
                };

                continue;
            }

            if (line.StartsWith("G01"))
            {
                document.MoveType = GerberMoveType.Linear;
                continue;
            }

            if (line.StartsWith("G02"))
            {
                document.MoveType = GerberMoveType.Arc;
                continue;
            }

            if (line.StartsWith("G03"))
            {
                document.MoveType = GerberMoveType.ArcCounterClockwise;
                continue;
            }

            if (line.StartsWith("G36"))
            {
                insideRegion = true;
                regionPoints.Clear();
                continue;
            }

            if (line.StartsWith("G37"))
            {
                insideRegion = false;
                if (regionPoints.Count > 0)
                {
                    using var vec = new VectorOfPoint(regionPoints.ToArray());
                    CvInvoke.FillPoly(mat, vec, document.Polarity == GerberPolarityType.Dark ? EmguExtensions.WhiteColor : EmguExtensions.BlackColor, enableAntialiasing ? LineType.AntiAlias : LineType.EightConnected);
                }
                //CvInvoke.Imshow("G37", mat);
                //CvInvoke.WaitKey();
                regionPoints.Clear();
                continue;
            }

            if (line.StartsWith("%AM"))
            {
                currentMacro = Macro.Parse(line);
                if (currentMacro is null) continue;
                document.Macros.Add(currentMacro.Name, currentMacro);
                //document.Apertures.Add(aperture.Index, aperture);
                continue;
            }

            if (line.StartsWith("%ADD"))
            {
                var aperture = Aperture.Parse(line, document);
                if (aperture is null) continue;
                currentAperture = aperture;
                document.Apertures.Add(aperture.Index, aperture);
                continue;
            }

            if (line[0] == 'D')
            {
                var matchD = Regex.Match(line, @"D(\d+)");
                if (!matchD.Success || matchD.Groups.Count < 2) continue;

                if (!int.TryParse(matchD.Groups[1].Value, out var d)) continue;

                if (!document.Apertures.TryGetValue(d, out currentAperture)) continue;

                continue;
            }

            if (line[0] == 'X' || line[0] == 'Y')
            {
                var matchX = Regex.Match(line, @"X-?(\d+)");
                var matchY = Regex.Match(line, @"Y-?(\d+)");
                var matchD = Regex.Match(line, @"D(\d+)");

                double nowX = 0;
                double nowY = 0;

                if (!matchD.Success || matchD.Groups.Count < 2) continue;
                if (!int.TryParse(matchD.Groups[1].Value, out var d)) continue;

                if (matchX.Success && matchX.Groups.Count >= 2)
                {
                    if (double.TryParse(matchX.Groups[1].Value, out nowX))
                    {
                        if (nowX != 0)
                        {
                            if (document.CoordinateXFractionalDigits > matchX.Groups[1].Value.Length) // Leading zero omitted
                            {
                                double.TryParse($"0.{matchX.Groups[1].Value}", out nowX);
                            }
                            else
                            {
                                var integers = matchX.Groups[1].Value[..^document.CoordinateXFractionalDigits];
                                var fraction = matchX.Groups[1].Value.Substring(matchX.Groups[1].Value.Length - document.CoordinateXFractionalDigits, document.CoordinateXFractionalDigits);
                                double.TryParse($"{integers}.{fraction}", out nowX);
                            }

                            nowX = document.GetMillimeters(nowX);
                        }
                    }
                }
                else
                {
                    nowX = currentX;
                }

                if (matchY.Success && matchY.Groups.Count >= 2)
                {
                    if (double.TryParse(matchY.Groups[1].Value, out nowY))
                    {
                        if (nowY != 0) 
                        {
                            if (document.CoordinateYFractionalDigits > matchY.Groups[1].Value.Length)  // Leading zero omitted
                            {
                                double.TryParse($"0.{matchY.Groups[1].Value}", out nowY);
                            }
                            else
                            {
                                var integers = matchY.Groups[1].Value[..^document.CoordinateYFractionalDigits];
                                var fraction = matchY.Groups[1].Value.Substring(matchY.Groups[1].Value.Length - document.CoordinateYFractionalDigits, document.CoordinateYFractionalDigits);
                                double.TryParse($"{integers}.{fraction}", out nowY);
                            }
                            nowY = document.GetMillimeters(nowY);
                        }
                    }
                }
                else
                {
                    nowY = currentY;
                }

                if (insideRegion)
                {
                    if (d == 2)
                    {
                        if (regionPoints.Count > 0)
                        {
                            using var vec = new VectorOfPoint(regionPoints.ToArray());
                            CvInvoke.FillPoly(mat, vec, document.Polarity == GerberPolarityType.Dark ? EmguExtensions.WhiteColor : EmguExtensions.BlackColor, enableAntialiasing ? LineType.AntiAlias : LineType.EightConnected);
                        }
                        regionPoints.Clear();
                    }

                    var pt = PositionMmToPx(nowX, nowY, xyPpmm);
                    if (regionPoints.Count == 0 || (regionPoints.Count > 0 && regionPoints[^1] != pt)) regionPoints.Add(pt);
                }
                else if(currentAperture is not null)
                {
                    if (d == 1)
                    {
                        if (currentAperture is CircleAperture circleAperture)
                        {
                            if (document.MoveType is GerberMoveType.Arc or GerberMoveType.ArcCounterClockwise)
                            {
                                var matchI = Regex.Match(line, @"I(-?\d+)");
                                var matchJ = Regex.Match(line, @"J(-?\d+)");
                                if(!matchI.Success || !matchJ.Success || matchI.Groups.Count < 2 || matchJ.Groups.Count < 2) continue;


                                if (double.TryParse(matchI.Groups[1].Value, out var xOffset))
                                {
                                    if (xOffset != 0)
                                    {
                                        if (document.CoordinateXFractionalDigits > matchI.Groups[1].Value.Length) // Leading zero omitted
                                        {
                                            double.TryParse($"0.{matchI.Groups[1].Value}", out xOffset);
                                        }
                                        else
                                        {
                                            var integers = matchI.Groups[1].Value[..^document.CoordinateXFractionalDigits];
                                            var fraction = matchI.Groups[1].Value.Substring(matchI.Groups[1].Value.Length - document.CoordinateXFractionalDigits, document.CoordinateXFractionalDigits);
                                            double.TryParse($"{integers}.{fraction}", out xOffset);
                                        }
                                        xOffset = document.GetMillimeters(xOffset);
                                    }
                                }

                                if (double.TryParse(matchJ.Groups[1].Value, out var yOffset))
                                {
                                    if(yOffset != 0)
                                    {
                                        if (document.CoordinateYFractionalDigits > matchJ.Groups[1].Value.Length) // Leading zero omitted
                                        {
                                            double.TryParse($"0.{matchJ.Groups[1].Value}", out yOffset);
                                        }
                                        else
                                        {
                                            var integers = matchJ.Groups[1].Value[..^document.CoordinateYFractionalDigits];
                                            var fraction = matchJ.Groups[1].Value.Substring(matchJ.Groups[1].Value.Length - document.CoordinateYFractionalDigits, document.CoordinateYFractionalDigits);
                                            double.TryParse($"{integers}.{fraction}", out yOffset);
                                        }
                                        yOffset = document.GetMillimeters(yOffset);
                                    }
                                }

                                if (currentX == nowX && currentY == nowY) // Closed circle
                                {
                                    CvInvoke.Ellipse(mat,
                                        PositionMmToPx(nowX + xOffset, nowY + yOffset, xyPpmm),
                                        SizeMmToPx(Math.Abs(xOffset), Math.Abs(xOffset), xyPpmm),
                                        0, 0, 360.0, document.Polarity == GerberPolarityType.Dark ? EmguExtensions.WhiteColor : EmguExtensions.BlackColor,
                                        SizeMmToPx(circleAperture.Diameter, xyPpmm.Max()),
                                        enableAntialiasing ? LineType.AntiAlias : LineType.EightConnected
                                    );
                                }
                                else
                                {
                                    // TODO: Fix this
                                    throw new NotImplementedException("Partial arcs are not yet implemented (G03)\nTo be fixed in the future...");
                                    /*CvInvoke.Ellipse(mat, new Point((int)((nowX + xOffset) * xyPpmm.Width), (int)((currentY) * xyPpmm.Height)),
                                        new Size((int)(Math.Abs(xOffset) * xyPpmm.Width), (int)(Math.Abs(yOffset) * xyPpmm.Width)),
                                        0, Math.Abs(currentY - nowY), 360.0 / Math.Abs(currentX - nowX), document.Polarity == GerberPolarityType.Dark ? EmguExtensions.WhiteColor : EmguExtensions.BlackColor,
                                        (int)(circleAperture.Diameter * xyPpmm.Max()),
                                        enableAntialiasing ? LineType.AntiAlias : LineType.EightConnected
                                    );*/
                                }
                                    
                            }
                            else
                            {
                                CvInvoke.Line(mat,
                                    PositionMmToPx(currentX, currentY, xyPpmm),
                                    PositionMmToPx(nowX, nowY, xyPpmm),
                                    document.Polarity == GerberPolarityType.Dark ? EmguExtensions.WhiteColor : EmguExtensions.BlackColor,
                                    SizeMmToPx(circleAperture.Diameter, xyPpmm.Max()),
                                    enableAntialiasing ? LineType.AntiAlias : LineType.EightConnected);
                            }
                                
                        }
                    }
                    else if (d == 3)
                    {
                        currentAperture.DrawFlashD3(mat, xyPpmm, new PointF((float) nowX, (float) nowY), 
                            document.Polarity == GerberPolarityType.Dark ? EmguExtensions.WhiteColor : EmguExtensions.BlackColor, enableAntialiasing ? LineType.AntiAlias : LineType.EightConnected);
                        //CvInvoke.Imshow("G37", mat);
                        //CvInvoke.WaitKey();
                    }
                }

                currentX = nowX;
                currentY = nowY;
                continue;
            }
        }

        return document;
    }

    public float GetMillimeters(float size)
    {
        if (UnitType == GerberUnitType.Millimeter) return size;
        return size * 25.4f;
    }

    public double GetMillimeters(double size)
    {
        if (UnitType == GerberUnitType.Millimeter) return size;
        return size * 25.4;
    }

    public SizeF GetMillimeters(SizeF size)
    {
        if (UnitType == GerberUnitType.Millimeter) return size;
        return new SizeF(size.Width * 25.4f, size.Height * 25.4f);
    }

    public PointF GetMillimeters(PointF point)
    {
        if (UnitType == GerberUnitType.Millimeter) return point;
        return new PointF(point.X * 25.4f, point.Y * 25.4f);
    }

    public static Point PositionMmToPx(PointF atMm, SizeF xyPpmm)
        => new((int)Math.Round(atMm.X * xyPpmm.Width, MidpointRounding.AwayFromZero), (int)Math.Round(atMm.Y * xyPpmm.Height, MidpointRounding.AwayFromZero));

    public static Point PositionMmToPx(double atXmm, double atYmm, SizeF xyPpmm)
        => new((int)Math.Round(atXmm * xyPpmm.Width, MidpointRounding.AwayFromZero), (int)Math.Round(atYmm * xyPpmm.Height, MidpointRounding.AwayFromZero));

    public static Point PositionMmToPx(float atXmm, float atYmm, SizeF xyPpmm)
        => new((int)Math.Round(atXmm * xyPpmm.Width, MidpointRounding.AwayFromZero), (int)Math.Round(atYmm * xyPpmm.Height, MidpointRounding.AwayFromZero));

    public static Size SizeMmToPx(SizeF sizeMm, SizeF xyPpmm)
        => new((int)Math.Max(1, Math.Round(sizeMm.Width * xyPpmm.Width, MidpointRounding.AwayFromZero)),
            (int)Math.Max(1, Math.Round(sizeMm.Height * xyPpmm.Height, MidpointRounding.AwayFromZero)));

    public static Size SizeMmToPx(double sizeXmm, double sizeYmm, SizeF xyPpmm)
        => new((int)Math.Max(1, Math.Round(sizeXmm * xyPpmm.Width, MidpointRounding.AwayFromZero)),
            (int)Math.Max(1, Math.Round(sizeYmm * xyPpmm.Height, MidpointRounding.AwayFromZero)));

    public static Size SizeMmToPx(float sizeXmm, float sizeYmm, SizeF xyPpmm)
        => new((int)Math.Max(1, Math.Round(sizeXmm * xyPpmm.Width, MidpointRounding.AwayFromZero)),
            (int)Math.Max(1, Math.Round(sizeYmm * xyPpmm.Height, MidpointRounding.AwayFromZero)));

    public static int SizeMmToPx(float sizeMm, float ppmm)
        => (int) Math.Max(1, Math.Round(sizeMm * ppmm, MidpointRounding.AwayFromZero));

    public static int SizeMmToPx(double sizeMm, float ppmm)
        => (int)Math.Max(1, Math.Round(sizeMm * ppmm, MidpointRounding.AwayFromZero));
}


/* KIDCAD
        var document = File.ReadAllLines(@"D:\Tiago\Desktop\kisample\kisample.kicad_pcb");
        System.Drawing.PointF location = PointF.Empty;

        using var mat = EmguExtensions.InitMat(new System.Drawing.Size(2440, 1440));
        const byte pixelsPerMm = 20;

        var drillPoints = new List<KeyValuePair<Point, int>>();

        foreach (var line in document)
        {
            var parseLine = line.Trim();
            if (parseLine.StartsWith("(footprint "))
            {
                location = PointF.Empty;
                continue;
            }
            if (location.IsEmpty && parseLine.StartsWith("(at "))
            {
                parseLine = parseLine.Substring(4, parseLine.Length-5);
                var split = parseLine.Split(' ');
                location = new PointF(float.Parse(split[0]), float.Parse(split[1]));
                continue;
            }
            if (parseLine.StartsWith("(segment ") || parseLine.StartsWith("(gr_line "))
            {
                var layerMatch = Regex.Match(parseLine, @"\S.Cu");
                if (!layerMatch.Success || layerMatch.Groups.Count < 1) continue;

                var startMatch = Regex.Match(parseLine, @"\(start\s+(\S+)\s+(\S+)\)");
                if(!startMatch.Success || startMatch.Groups.Count < 3) continue;

                var endMatch = Regex.Match(parseLine, @"\(end\s+(\S+)\s+(\S+)\)");
                if (!endMatch.Success || endMatch.Groups.Count < 3) continue;

                var widthMatch = Regex.Match(parseLine, @"\(width\s+(\S+)\)");
                if (!widthMatch.Success || widthMatch.Groups.Count < 2) continue;

                var startXf = new PointF(float.Parse(startMatch.Groups[1].Value), float.Parse(startMatch.Groups[2].Value));
                var endXf = new PointF(float.Parse(endMatch.Groups[1].Value), float.Parse(endMatch.Groups[2].Value));
                var widthf = float.Parse(widthMatch.Groups[1].Value);

                var startX = new System.Drawing.Point((int)(startXf.X * pixelsPerMm), (int)(startXf.Y * pixelsPerMm));
                var endX = new System.Drawing.Point((int)(endXf.X * pixelsPerMm), (int)(endXf.Y * pixelsPerMm));
                var width = (int) (widthf * pixelsPerMm);

                CvInvoke.Line(mat, startX, endX, EmguExtensions.WhiteColor, width);

                continue;
            }

            if (parseLine.StartsWith("(via "))
            {
                var layerMatches = Regex.Matches(parseLine, @"\S.Cu");
                if (layerMatches.Count < 1) continue;
                
                var atMatch = Regex.Match(parseLine, @"\(at\s+(\S+)\s+(\S+)\)");
                if (!atMatch.Success || atMatch.Groups.Count < 3) continue;

                var drillMatch = Regex.Match(parseLine, @"\(drill\s+(\S+)\)");


                var atf = new PointF(float.Parse(atMatch.Groups[1].Value), float.Parse(atMatch.Groups[2].Value));
                //var sizef = new SizeF(float.Parse(sizeMatch.Groups[1].Value), float.Parse(sizeMatch.Groups[2].Value));

                var at = new System.Drawing.Point((int)(atf.X * pixelsPerMm), (int)(atf.Y * pixelsPerMm));
                if (!drillMatch.Success || drillMatch.Groups.Count < 2) continue;
                var drillf = float.Parse(drillMatch.Groups[1].Value);
                var drill = (int) (drillf * pixelsPerMm / 2);

                CvInvoke.Circle(mat, at, drill, EmguExtensions.WhiteColor, -1);


                continue;
            }

            if (parseLine.StartsWith("(gr_circle "))
            {
                var layerMatch = Regex.Match(parseLine, @"\S.Cu");
                if (!layerMatch.Success || layerMatch.Groups.Count < 1) continue;

                var atMatch = Regex.Match(parseLine, @"\(center\s+(\S+)\s+(\S+)\)");
                if (!atMatch.Success || atMatch.Groups.Count < 3) continue;

                var endMatch = Regex.Match(parseLine, @"\(end\s+(\S+)\s+(\S+)\)");
                if (!endMatch.Success || endMatch.Groups.Count < 3) continue;

                var widthMatch = Regex.Match(parseLine, @"\(width\s+(\S+)\)");
                if (!widthMatch.Success || widthMatch.Groups.Count < 2) continue;

                var atf = new PointF(float.Parse(atMatch.Groups[1].Value), float.Parse(atMatch.Groups[2].Value));
                var at = new System.Drawing.Point((int)(atf.X * pixelsPerMm), (int)(atf.Y * pixelsPerMm));
                var endf = new PointF(float.Parse(endMatch.Groups[1].Value), float.Parse(endMatch.Groups[2].Value));
                var radius = (int)(Math.Max(Math.Abs(atf.X - endf.X), Math.Abs(atf.Y - endf.Y)) * pixelsPerMm);
                var widthf = float.Parse(widthMatch.Groups[1].Value);
                var width = (int)(widthf * pixelsPerMm);

                CvInvoke.Circle(mat, at, radius, EmguExtensions.WhiteColor, width);
                if (parseLine.Contains("fill solid"))
                {
                    CvInvoke.Circle(mat, at, radius, EmguExtensions.WhiteColor, -1);
                }


                continue;
            }
            if (parseLine.StartsWith("(gr_rect "))
            {
                var layerMatch = Regex.Match(parseLine, @"\S.Cu");
                if (!layerMatch.Success || layerMatch.Groups.Count < 1) continue;

                var startMatch = Regex.Match(parseLine, @"\(start\s+(\S+)\s+(\S+)\)");
                if (!startMatch.Success || startMatch.Groups.Count < 3) continue;

                var endMatch = Regex.Match(parseLine, @"\(end\s+(\S+)\s+(\S+)\)");
                if (!endMatch.Success || endMatch.Groups.Count < 3) continue;

                var widthMatch = Regex.Match(parseLine, @"\(width\s+(\S+)\)");
                if (!widthMatch.Success || widthMatch.Groups.Count < 2) continue;

                var startf = new PointF(float.Parse(startMatch.Groups[1].Value), float.Parse(startMatch.Groups[2].Value));
                var endf = new PointF(float.Parse(endMatch.Groups[1].Value), float.Parse(endMatch.Groups[2].Value));
                var widthf = float.Parse(widthMatch.Groups[1].Value);

                var start = new System.Drawing.Point((int)(startf.X * pixelsPerMm), (int)(startf.Y * pixelsPerMm));
                var end = new System.Drawing.Point((int)(endf.X * pixelsPerMm), (int)(endf.Y * pixelsPerMm));
                var width = (int)(widthf * pixelsPerMm);

                CvInvoke.Rectangle(mat, new Rectangle(start, new System.Drawing.Size(end.X - start.X, end.Y - start.Y)), EmguExtensions.WhiteColor, width);
                if (parseLine.Contains("fill solid"))
                {
                    CvInvoke.Rectangle(mat, new Rectangle(start, new System.Drawing.Size(end.X - start.X, end.Y - start.Y)), EmguExtensions.WhiteColor, -1);
                }

                continue;
            }

            if (location.IsEmpty) continue;

            if (parseLine.StartsWith("(pad "))
            {
                var layerMatch = Regex.Match(parseLine, @"\S.Cu");
                if (!layerMatch.Success || layerMatch.Groups.Count < 1) continue;

                var atMatch = Regex.Match(parseLine, @"\(at\s+(\S+)\s+(\S+)\)");
                if (!atMatch.Success || atMatch.Groups.Count < 3) continue;
                
                var sizeMatch = Regex.Match(parseLine, @"\(size\s+(\S+)\s+(\S+)\)");
                if (!sizeMatch.Success || sizeMatch.Groups.Count < 3) continue;

                var drillMatch = Regex.Match(parseLine, @"\(drill\s+(\S+)\)");


                var atf = new PointF(float.Parse(atMatch.Groups[1].Value), float.Parse(atMatch.Groups[2].Value));
                var sizef = new SizeF(float.Parse(sizeMatch.Groups[1].Value), float.Parse(sizeMatch.Groups[2].Value));

                var at = new System.Drawing.Point((int)(location.X * pixelsPerMm + atf.X * pixelsPerMm), (int)(location.Y * pixelsPerMm + atf.Y * pixelsPerMm));

                if (parseLine.Contains(" rect ") || parseLine.Contains(" roundrect "))
                {
                    var size = new System.Drawing.Size((int)(sizef.Width * pixelsPerMm), (int)(sizef.Height * pixelsPerMm));
                    var rect = new Rectangle(at, size);
                    rect.Offset(-size.Width / 2, -size.Height / 2);
                    CvInvoke.Rectangle(mat, rect, EmguExtensions.WhiteColor, -1);
                }
                else if (parseLine.Contains(" oval ") || parseLine.Contains(" circle "))
                {
                    var size = new System.Drawing.Size((int)(sizef.Width / 2 * pixelsPerMm), (int)(sizef.Height / 2 * pixelsPerMm));
                    CvInvoke.Ellipse(mat, at, size, 0, 0, 360, EmguExtensions.WhiteColor, -1);
                }
                
                if (drillMatch.Success && drillMatch.Groups.Count >= 2)
                {
                    var drillf = float.Parse(drillMatch.Groups[1].Value);
                    var drill = (int)(drillf * pixelsPerMm / 2);

                    drillPoints.Add(new KeyValuePair<Point, int>(at, drill));
                }


                continue;
            }
        }

        foreach (var pair in drillPoints)
        {
            CvInvoke.Circle(mat, pair.Key, pair.Value, EmguExtensions.BlackColor, -1);
        }

        CvInvoke.Imshow("asd", mat);
        CvInvoke.WaitKey();
        return;
        */