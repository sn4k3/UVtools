/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using EmguExtensions;
using UVtools.Core.Extensions;
using UVtools.Core.Gerber.Apertures;
using UVtools.Core.Operations;

namespace UVtools.Core.Gerber;

/// <summary>
/// https://www.ucamco.com/files/downloads/file_en/456/gerber-layer-format-specification-revision-2022-02_en.pdf?ac97011bf6bce9aaf0b1aac43d84b05f
/// </summary>
public partial class GerberFormat
{
    public GerberFormat()
    {
    }

    public GerberFormat(string filePath)
    {
    }

    public static void ParseAndDraw(GerberFormat document, string filePath, Mat mat, bool enableAntiAliasing = false)
    {
        using var file = new StreamReader(filePath);

        double currentX = 0;
        double currentY = 0;
        var currentOperation = 2;
        Aperture? currentAperture = null;
        var regionPoints = new List<Point>();
        var insideRegion = false;
        var lineType = enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected;

        foreach (var line in ReadCommands(file))
        {
            if (line.StartsWith("M02")) break; // End-of-File

            if (line.StartsWith("%MO", StringComparison.Ordinal) && line.Length >= 7)
            {
                if (line[3] == 'M' && line[4] == 'M') document.UnitType = GerberUnitType.Millimeter;
                else if (line[3] == 'I' && line[4] == 'N') document.UnitType = GerberUnitType.Inch;
                continue;
            }

            if (line.StartsWith("G70"))
            {
                document.UnitType = GerberUnitType.Inch;
                continue;
            }


            if (line.StartsWith("G71"))
            {
                document.UnitType = GerberUnitType.Millimeter;
                continue;
            }

            if (line.StartsWith("G90"))
            {
                document.PositionType = GerberPositionType.Absolute;
                continue;
            }


            if (line.StartsWith("G91"))
            {
                document.PositionType = GerberPositionType.Relative;
                continue;
            }

            if (line.StartsWith("%FS", StringComparison.Ordinal) && line.Length >= 13)
            {
                // %FSLAX34Y34*%
                // 0123456789
                document.ZerosSuppressionType = line[3] switch
                {
                    'L' => GerberZerosSuppressionType.Leading,
                    'T' => GerberZerosSuppressionType.Trail,
                    _ => document.ZerosSuppressionType
                };
                document.PositionType = line[4] switch
                {
                    'A' => GerberPositionType.Absolute,
                    'I' => GerberPositionType.Relative,
                    _ => document.PositionType
                };
                if (line[5] != 'X') continue;
                if (char.IsAsciiDigit(line[6])) document.CoordinateXIntegers = (byte)(line[6] - '0');
                if (char.IsAsciiDigit(line[7])) document.CoordinateXFractionalDigits = (byte)(line[7] - '0');
                if (line[8] != 'Y') continue;
                if (char.IsAsciiDigit(line[9])) document.CoordinateYIntegers = (byte)(line[9] - '0');
                if (char.IsAsciiDigit(line[10])) document.CoordinateYFractionalDigits = (byte)(line[10] - '0');
                continue;
            }

            if (line.StartsWith("%LP", StringComparison.Ordinal) && line.Length >= 6)
            {
                document.Polarity = line[3] switch
                {
                    'D' => GerberPolarityType.Dark,
                    'C' => GerberPolarityType.Clear,
                    _ => document.Polarity
                };

                continue;
            }

            if (line.StartsWith("G04")) // Comment
            {
                continue;
            }

            if (line.StartsWith("G01"))
            {
                document.MoveType = GerberMoveType.Linear;
                if (line.Length == 4) continue; // G01*
            }

            if (line.StartsWith("G02"))
            {
                document.MoveType = GerberMoveType.Arc;
                if (line.Length == 4) continue; // G02*
            }

            if (line.StartsWith("G03"))
            {
                document.MoveType = GerberMoveType.ArcCounterClockwise;
                if (line.Length == 4) continue; // G03*
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
                FillRegion(mat, regionPoints, document.PolarityColor, lineType);
                regionPoints.Clear();
                continue;
            }

            if (line.StartsWith("G74"))
            {
                document.QuadrantMode = GerberQuadrantMode.SingleQuadrant;
                continue;
            }

            if (line.StartsWith("G75"))
            {
                document.QuadrantMode = GerberQuadrantMode.MultiQuadrant;
                continue;
            }

            if (line.StartsWith("%AM"))
            {
                var split = line.Split(['*', '%'],
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                var macro = Macro.Parse(document, split[0]);
                if (macro is null) continue;
                document.Macros[macro.Name] = macro;

                for (var index = 1; index < split.Length; index++)
                {
                    macro.ParsePrimitive(split[index]);
                }

                continue;
            }

            if (line.StartsWith("%ADD"))
            {
                var aperture = Aperture.Parse(line, document);
                if (aperture is null) continue;
                currentAperture = aperture;
                document.Apertures[aperture.Index] = aperture;
                continue;
            }

            var matchX = LineXParse().Match(line);
            var matchY = LineYParse().Match(line);
            var matchD = LineDParse().Match(line);
            var hasCoordinate = matchX.Success || matchY.Success;

            if (matchD.Success &&
                int.TryParse(matchD.Groups[1].Value, NumberStyles.None,
                    CultureInfo.InvariantCulture, out var parsedD))
            {
                if (parsedD >= 10)
                {
                    document.Apertures.TryGetValue(parsedD, out currentAperture);
                    if (!hasCoordinate) continue;
                }
                else if (parsedD is >= 1 and <= 3)
                {
                    currentOperation = parsedD;
                }
            }

            if (!hasCoordinate)
            {
                if (!matchD.Success && !line.StartsWith('%'))
                {
                    Debug.WriteLine($"Not recognized command: {line}");
                }

                continue;
            }

            var nowX = currentX;
            var nowY = currentY;
            if (matchX.Success)
            {
                if (!document.TryParseCoordinate(matchX.Groups[1].Value,
                        document.CoordinateXIntegers, document.CoordinateXFractionalDigits, out nowX))
                {
                    continue;
                }

                if (document.PositionType == GerberPositionType.Relative) nowX += currentX;
            }

            if (matchY.Success)
            {
                if (!document.TryParseCoordinate(matchY.Groups[1].Value,
                        document.CoordinateYIntegers, document.CoordinateYFractionalDigits, out nowY))
                {
                    continue;
                }

                if (document.PositionType == GerberPositionType.Relative) nowY += currentY;
            }

            Point[]? arcPoints = null;
            if (currentOperation == 1 &&
                document.MoveType is GerberMoveType.Arc or GerberMoveType.ArcCounterClockwise &&
                !document.TryBuildArcPoints(line, currentX, currentY, nowX, nowY, out arcPoints))
            {
                continue;
            }

            // Track the plotted extents for auto-centering. Operation 2 is a pen-up move, so it only counts
            // when it becomes the start point of the line a following operation 1 draws
            if (currentOperation == 1) document.PlotBounds(currentX, currentY);
            if (currentOperation is 1 or 3 || insideRegion) document.PlotBounds(nowX, nowY);

            if (insideRegion)
            {
                if (currentOperation == 2)
                {
                    FillRegion(mat, regionPoints, document.PolarityColor, lineType);
                    regionPoints.Clear();
                    AddUnique(regionPoints, document.PositionMmToPx(nowX, nowY));
                }
                else if (currentOperation == 1)
                {
                    if (regionPoints.Count == 0)
                    {
                        AddUnique(regionPoints, document.PositionMmToPx(currentX, currentY));
                    }

                    if (arcPoints is null)
                    {
                        AddUnique(regionPoints, document.PositionMmToPx(nowX, nowY));
                    }
                    else
                    {
                        for (var index = 1; index < arcPoints.Length; index++)
                        {
                            AddUnique(regionPoints, arcPoints[index]);
                        }
                    }
                }
            }
            else if (currentAperture is not null)
            {
                if (currentOperation == 1 && currentAperture is CircleAperture circleAperture)
                {
                    var thickness = EmguCvExtensions.CorrectThickness(
                        document.SizeMmToPx(circleAperture.Diameter));
                    if (arcPoints is null)
                    {
                        CvInvoke.Line(mat,
                            document.PositionMmToPx(currentX, currentY),
                            document.PositionMmToPx(nowX, nowY),
                            document.PolarityColor, thickness, lineType);
                    }
                    else
                    {
                        CvInvoke.Polylines(mat, arcPoints, false,
                            document.PolarityColor, thickness, lineType);
                    }
                }
                else if (currentOperation == 3)
                {
                    currentAperture.DrawFlashD3(mat,
                        new PointF((float)nowX, (float)nowY),
                        document.PolarityColor, lineType);
                }
            }

            currentX = nowX;
            currentY = nowY;
        }
    }

    private static IEnumerable<string> ReadCommands(TextReader reader)
    {
        var command = new StringBuilder();
        var extended = false;
        while (reader.Read() is var character and >= 0)
        {
            var value = (char)character;
            if (value is '\r' or '\n') continue;
            if (command.Length == 0 && char.IsWhiteSpace(value)) continue;

            command.Append(value);
            if (value == '%')
            {
                if (!extended)
                {
                    extended = true;
                }
                else
                {
                    var result = command.ToString().Trim();
                    if (result.Length > 0) yield return result;
                    command.Clear();
                    extended = false;
                }
            }
            else if (value == '*' && !extended)
            {
                var result = command.ToString().Trim();
                if (result.Length > 0) yield return result;
                command.Clear();
            }
        }

        var remaining = command.ToString().Trim();
        if (remaining.Length > 0) yield return remaining;
    }

    private bool TryParseCoordinate(string text, byte integerDigits, byte fractionalDigits,
        out double value)
    {
        value = 0;
        if (text.Length == 0) return false;

        var negative = text[0] == '-';
        var digits = negative ? text.AsSpan(1) : text.AsSpan();
        var expectedLength = integerDigits + fractionalDigits;
        if (digits.Length == 0 || digits.Length > expectedLength ||
            !long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var rawValue))
        {
            return false;
        }

        if (ZerosSuppressionType == GerberZerosSuppressionType.Trail)
        {
            rawValue *= (long)Math.Pow(10, expectedLength - digits.Length);
        }

        value = rawValue / Math.Pow(10, fractionalDigits);
        if (negative) value = -value;
        value = GetMillimeters(value);
        return double.IsFinite(value);
    }

    private bool TryBuildArcPoints(string line, double startX, double startY,
        double endX, double endY, out Point[] points)
    {
        points = [];
        var matchI = LineIParse().Match(line);
        var matchJ = LineJParse().Match(line);
        if (!matchI.Success && !matchJ.Success) return false;

        var offsetX = 0.0;
        var offsetY = 0.0;
        if (matchI.Success &&
            !TryParseCoordinate(matchI.Groups[1].Value,
                CoordinateXIntegers, CoordinateXFractionalDigits, out offsetX))
        {
            return false;
        }

        if (matchJ.Success &&
            !TryParseCoordinate(matchJ.Groups[1].Value,
                CoordinateYIntegers, CoordinateYFractionalDigits, out offsetY))
        {
            return false;
        }

        var counterClockwise = MoveType == GerberMoveType.ArcCounterClockwise;
        if (!TryResolveArc(startX, startY, endX, endY, offsetX, offsetY,
                counterClockwise, out var centerX, out var centerY, out var startAngle,
                out var sweep, out var radius))
        {
            return false;
        }

        var pixelRadius = radius * Math.Max(XYppmm.Width, XYppmm.Height);
        if (!double.IsFinite(pixelRadius) || pixelRadius <= 0) return false;

        var maximumStep = pixelRadius <= 0.25
            ? Math.PI / 4
            : 2 * Math.Acos(Math.Clamp(1 - 0.25 / pixelRadius, -1, 1));
        maximumStep = Math.Clamp(maximumStep, Math.PI / 180, Math.PI / 18);
        var segmentCount = Math.Max(1, (int)Math.Ceiling(Math.Abs(sweep) / maximumStep));
        points = new Point[segmentCount + 1];
        points[0] = PositionMmToPx(startX, startY);
        for (var index = 1; index < segmentCount; index++)
        {
            var angle = startAngle + sweep * index / segmentCount;
            points[index] = PositionMmToPx(
                centerX + radius * Math.Cos(angle),
                centerY + radius * Math.Sin(angle));
        }

        points[^1] = PositionMmToPx(endX, endY);
        return true;
    }

    private bool TryResolveArc(double startX, double startY, double endX, double endY,
        double offsetX, double offsetY, bool counterClockwise, out double centerX,
        out double centerY, out double startAngle, out double sweep, out double radius)
    {
        centerX = centerY = startAngle = sweep = radius = 0;
        if (QuadrantMode == GerberQuadrantMode.MultiQuadrant)
        {
            centerX = startX + offsetX;
            centerY = startY + offsetY;
            return TryCalculateArc(startX, startY, endX, endY, centerX, centerY,
                counterClockwise, out startAngle, out sweep, out radius);
        }

        var absoluteX = Math.Abs(offsetX);
        var absoluteY = Math.Abs(offsetY);
        var bestError = double.MaxValue;
        foreach (var xSign in new[] { -1, 1 })
        foreach (var ySign in new[] { -1, 1 })
        {
            var candidateX = startX + absoluteX * xSign;
            var candidateY = startY + absoluteY * ySign;
            if (!TryCalculateArc(startX, startY, endX, endY, candidateX, candidateY,
                    counterClockwise, out var candidateStart, out var candidateSweep,
                    out var candidateRadius) ||
                Math.Abs(candidateSweep) > Math.PI / 2 + 1e-6)
            {
                continue;
            }

            var endRadius = MathExtensions.Hypot(endX - candidateX, endY - candidateY);
            var error = Math.Abs(candidateRadius - endRadius);
            if (error >= bestError) continue;

            bestError = error;
            centerX = candidateX;
            centerY = candidateY;
            startAngle = candidateStart;
            sweep = candidateSweep;
            radius = candidateRadius;
        }

        return bestError <= Math.Max(1e-6, radius * 0.001);
    }

    private static bool TryCalculateArc(double startX, double startY, double endX,
        double endY, double centerX, double centerY, bool counterClockwise,
        out double startAngle, out double sweep, out double radius)
    {
        var startVectorX = startX - centerX;
        var startVectorY = startY - centerY;
        radius = MathExtensions.Hypot(startVectorX, startVectorY);
        var endRadius = MathExtensions.Hypot(endX - centerX, endY - centerY);
        startAngle = Math.Atan2(startVectorY, startVectorX);
        var endAngle = Math.Atan2(endY - centerY, endX - centerX);
        if (!double.IsFinite(radius) || radius <= 0 ||
            Math.Abs(radius - endRadius) > Math.Max(1e-6, radius * 0.001))
        {
            sweep = 0;
            return false;
        }

        sweep = endAngle - startAngle;
        if (MathExtensions.Hypot(endX - startX, endY - startY) <= 1e-9)
        {
            sweep = counterClockwise ? Math.Tau : -Math.Tau;
        }
        else if (counterClockwise)
        {
            while (sweep <= 0) sweep += Math.Tau;
        }
        else
        {
            while (sweep >= 0) sweep -= Math.Tau;
        }

        return true;
    }

    private static void AddUnique(List<Point> points, Point point)
    {
        if (points.Count == 0 || points[^1] != point) points.Add(point);
    }

    private static void FillRegion(Mat mat, List<Point> points, MCvScalar color,
        LineType lineType)
    {
        if (points.Count < 3) return;
        using var vector = new VectorOfPoint(points.ToArray());
        CvInvoke.FillPoly(mat, vector, color, lineType);
    }

    public static GerberFormat ParseAndDraw(OperationPCBExposure.PCBExposureFile file, Mat mat, SizeF xyPpmm,
        MidpointRoundingType sizeMidpointRounding = MidpointRoundingType.AwayFromZero, SizeF offset = default,
        bool enableAntiAliasing = false)
    {
        var document = new GerberFormat
        {
            SizeMidpointRounding = sizeMidpointRounding,
            XYppmm = xyPpmm,
            OffsetX = offset.Width,
            OffsetY = offset.Height,
            InversePolarity = file.InvertPolarity,
            SizeScale = file.SizeScale
        };

        ParseAndDraw(document, file.FilePath, mat, enableAntiAliasing);

        return document;
    }

    public static GerberFormat ParseAndDraw(string filePath, Mat mat, SizeF xyPpmm,
        MidpointRoundingType sizeMidpointRounding = MidpointRoundingType.AwayFromZero, SizeF offset = default,
        bool enableAntiAliasing = false)
    {
        var document = new GerberFormat
        {
            SizeMidpointRounding = sizeMidpointRounding,
            XYppmm = xyPpmm,
            OffsetX = offset.Width,
            OffsetY = offset.Height
        };

        ParseAndDraw(document, filePath, mat, enableAntiAliasing);

        return document;
    }

    public MCvScalar GetPolarityColor(GerberPolarityType polarity)
    {
        return polarity == GerberPolarityType.Dark
            ? !InversePolarity ? EmguCvExtensions.WhiteColor : EmguCvExtensions.BlackColor
            : !InversePolarity
                ? EmguCvExtensions.BlackColor
                : EmguCvExtensions.WhiteColor;
    }

    public MCvScalar GetPolarityColor(bool polarity)
    {
        return polarity
            ? !InversePolarity ? EmguCvExtensions.WhiteColor : EmguCvExtensions.BlackColor
            : !InversePolarity
                ? EmguCvExtensions.BlackColor
                : EmguCvExtensions.WhiteColor;
    }

    public MCvScalar GetPolarityColor(int polarity)
    {
        return polarity > 0
            ? !InversePolarity ? EmguCvExtensions.WhiteColor : EmguCvExtensions.BlackColor
            : !InversePolarity
                ? EmguCvExtensions.BlackColor
                : EmguCvExtensions.WhiteColor;
    }

    public float GetMillimeters(float size)
    {
        if (UnitType == GerberUnitType.Millimeter) return size;
        return size * (float)UnitExtensions.InchToMillimeter;
    }

    public double GetMillimeters(double size)
    {
        if (UnitType == GerberUnitType.Millimeter) return size;
        return size * UnitExtensions.InchToMillimeter;
    }

    public SizeF GetMillimeters(SizeF size)
    {
        if (UnitType == GerberUnitType.Millimeter) return size;
        return new SizeF(size.Width * (float)UnitExtensions.InchToMillimeter,
            size.Height * (float)UnitExtensions.InchToMillimeter);
    }

    public PointF GetMillimeters(PointF point)
    {
        if (UnitType == GerberUnitType.Millimeter) return point;
        return new PointF(point.X * (float)UnitExtensions.InchToMillimeter,
            point.Y * (float)UnitExtensions.InchToMillimeter);
    }

    public Point PositionMmToPx(PointF atMm)
    {
        return new Point((int)Math.Round((atMm.X + OffsetX) * XYppmm.Width, MidpointRounding.AwayFromZero),
            (int)Math.Round((atMm.Y + OffsetY) * XYppmm.Height, MidpointRounding.AwayFromZero));
    }

    public Point PositionMmToPx(double atXmm, double atYmm)
    {
        return new Point((int)Math.Round((atXmm + OffsetX) * XYppmm.Width, MidpointRounding.AwayFromZero),
            (int)Math.Round((atYmm + OffsetY) * XYppmm.Height, MidpointRounding.AwayFromZero));
    }

    public Point PositionMmToPx(float atXmm, float atYmm)
    {
        return new Point((int)Math.Round((atXmm + OffsetX) * XYppmm.Width, MidpointRounding.AwayFromZero),
            (int)Math.Round((atYmm + OffsetY) * XYppmm.Height, MidpointRounding.AwayFromZero));
    }

    public Size SizeMmToPx(SizeF sizeMm)
    {
        return new Size(
            (int)Math.Max(1,
                Math.Round(sizeMm.Width * XYppmm.Width * SizeScale, (MidpointRounding)SizeMidpointRounding)),
            (int)Math.Max(1,
                Math.Round(sizeMm.Height * XYppmm.Height * SizeScale, (MidpointRounding)SizeMidpointRounding)));
    }

    public Size SizeMmToPx(double sizeXmm, double sizeYmm)
    {
        return new Size(
            (int)Math.Max(1, Math.Round(sizeXmm * XYppmm.Width * SizeScale, (MidpointRounding)SizeMidpointRounding)),
            (int)Math.Max(1, Math.Round(sizeYmm * XYppmm.Height * SizeScale, (MidpointRounding)SizeMidpointRounding)));
    }

    public Size SizeMmToPx(float sizeXmm, float sizeYmm)
    {
        return new Size(
            (int)Math.Max(1, Math.Round(sizeXmm * XYppmm.Width * SizeScale, (MidpointRounding)SizeMidpointRounding)),
            (int)Math.Max(1, Math.Round(sizeYmm * XYppmm.Height * SizeScale, (MidpointRounding)SizeMidpointRounding)));
    }

    public int SizeMmToPx(float sizeMm)
    {
        return (int)Math.Max(1, Math.Round(sizeMm * XYppmm.Max() * SizeScale, (MidpointRounding)SizeMidpointRounding));
    }

    public int SizeMmToPx(double sizeMm)
    {
        return (int)Math.Max(1, Math.Round(sizeMm * XYppmm.Max() * SizeScale, (MidpointRounding)SizeMidpointRounding));
    }

    public int SizeMmToPxOverride(float sizeMm, float ppmm)
    {
        return (int)Math.Max(1, Math.Round(sizeMm * ppmm * SizeScale, (MidpointRounding)SizeMidpointRounding));
    }

    public int SizeMmToPxOverride(double sizeMm, float ppmm)
    {
        return (int)Math.Max(1, Math.Round(sizeMm * ppmm * SizeScale, (MidpointRounding)SizeMidpointRounding));
    }

    #region Regex Generators

    [GeneratedRegex(@"D([0-9]+)")]
    private static partial Regex LineDParse();

    [GeneratedRegex(@"X(-?[0-9]+)?")]
    private static partial Regex LineXParse();

    [GeneratedRegex(@"Y(-?[0-9]+)?")]
    private static partial Regex LineYParse();

    [GeneratedRegex(@"I(-?[0-9]+)")]
    private static partial Regex LineIParse();

    [GeneratedRegex(@"J(-?[0-9]+)")]
    private static partial Regex LineJParse();

    #endregion

    #region Properties

    public GerberZerosSuppressionType ZerosSuppressionType { get; set; } = GerberZerosSuppressionType.NoSuppression;
    public GerberPositionType PositionType { get; set; } = GerberPositionType.Absolute;
    public GerberUnitType UnitType { get; set; } = GerberUnitType.Millimeter;
    public GerberPolarityType Polarity { get; set; } = GerberPolarityType.Dark;
    public GerberMoveType MoveType { get; set; } = GerberMoveType.Linear;
    public GerberQuadrantMode QuadrantMode { get; set; } = GerberQuadrantMode.MultiQuadrant;
    public MidpointRoundingType SizeMidpointRounding { get; set; } = MidpointRoundingType.AwayFromZero;

    public byte CoordinateXIntegers { get; set; } = 3;
    public byte CoordinateXFractionalDigits { get; set; } = 6;

    public byte CoordinateXLength => (byte)(CoordinateXIntegers + CoordinateXFractionalDigits);

    public byte CoordinateYIntegers { get; set; } = 3;
    public byte CoordinateYFractionalDigits { get; set; } = 6;

    public byte CoordinateYLength => (byte)(CoordinateYIntegers + CoordinateYFractionalDigits);

    public Dictionary<int, Aperture> Apertures { get; } = new();
    public Dictionary<string, Macro> Macros { get; } = new();

    public SizeF XYppmm { get; init; }

    /// <summary>
    /// Gets or sets the X offset for drawings in millimeters
    /// </summary>
    public float OffsetX { get; set; }

    /// <summary>
    /// Gets or sets the Y offset for drawings in millimeters
    /// </summary>
    public float OffsetY { get; set; }

    /// <summary>
    /// Gets the current polarity as <see cref="MCvScalar"/>. <see cref="InversePolarity"/> will affect the return value
    /// </summary>
    public MCvScalar PolarityColor => GetPolarityColor(Polarity);

    /// <summary>
    /// Gets or sets to inverse the polarity on drawing
    /// </summary>
    public bool InversePolarity { get; set; }

    /// <summary>
    /// Gets or sets the scale to apply to each shape drawing size.
    /// Positions and vectors aren't affected by this.
    /// </summary>
    public double SizeScale
    {
        get;
        set => field = double.IsFinite(value) && value > 0 ? value : 1;
    } = 1;

    private double _minXmm = double.MaxValue;
    private double _minYmm = double.MaxValue;
    private double _maxXmm = double.MinValue;
    private double _maxYmm = double.MinValue;

    /// <summary>
    /// Gets the bounding rectangle of everything plotted so far, in millimeters and without
    /// <see cref="OffsetX"/> and <see cref="OffsetY"/> applied, ie: the coordinates as the file declares them.
    /// <para>Measured from the path centerlines: the aperture width and the bulge of an arc are not accounted for.</para>
    /// <para>Returns null when nothing was plotted. A single flash yields a zero sized rectangle, not null.</para>
    /// </summary>
    public RectangleF? BoundsMm => _maxXmm < _minXmm || _maxYmm < _minYmm
        ? null
        : new RectangleF((float)_minXmm, (float)_minYmm, (float)(_maxXmm - _minXmm), (float)(_maxYmm - _minYmm));

    /// <summary>
    /// Expands <see cref="BoundsMm"/> to include the given coordinate.
    /// </summary>
    /// <param name="atXmm">X coordinate in millimeters, without the offset applied</param>
    /// <param name="atYmm">Y coordinate in millimeters, without the offset applied</param>
    public void PlotBounds(double atXmm, double atYmm)
    {
        if (atXmm < _minXmm) _minXmm = atXmm;
        if (atXmm > _maxXmm) _maxXmm = atXmm;
        if (atYmm < _minYmm) _minYmm = atYmm;
        if (atYmm > _maxYmm) _maxYmm = atYmm;
    }

    #endregion
}

/* KIDCAD
        var document = File.ReadAllLines(@"D:\Tiago\Desktop\kisample\kisample.kicad_pcb");
        System.Drawing.PointF location = PointF.Empty;

        using var mat = EmguCvExtensions.InitMat(new System.Drawing.Size(2440, 1440));
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
                location = new PointF(float.Parse(split[0], CultureInfo.InvariantCulture), float.Parse(split[1], CultureInfo.InvariantCulture));
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
                var endXf = new PointF(float.Parse(endMatch.Groups[1].Value, CultureInfo.InvariantCulture), float.Parse(endMatch.Groups[2].Value, CultureInfo.InvariantCulture));
                var widthf = float.Parse(widthMatch.Groups[1].Value, CultureInfo.InvariantCulture);

                var startX = new System.Drawing.Point((int)(startXf.X * pixelsPerMm), (int)(startXf.Y * pixelsPerMm));
                var endX = new System.Drawing.Point((int)(endXf.X * pixelsPerMm), (int)(endXf.Y * pixelsPerMm));
                var width = (int) (widthf * pixelsPerMm);

                CvInvoke.Line(mat, startX, endX, EmguCvExtensions.WhiteColor, width);

                continue;
            }

            if (parseLine.StartsWith("(via "))
            {
                var layerMatches = Regex.Matches(parseLine, @"\S.Cu");
                if (layerMatches.Count < 1) continue;

                var atMatch = Regex.Match(parseLine, @"\(at\s+(\S+)\s+(\S+)\)");
                if (!atMatch.Success || atMatch.Groups.Count < 3) continue;

                var drillMatch = Regex.Match(parseLine, @"\(drill\s+(\S+)\)");


                var atf = new PointF(float.Parse(atMatch.Groups[1].Value, CultureInfo.InvariantCulture), float.Parse(atMatch.Groups[2].Value, CultureInfo.InvariantCulture));
                //var sizef = new SizeF(float.Parse(sizeMatch.Groups[1].Value, CultureInfo.InvariantCulture), float.Parse(sizeMatch.Groups[2].Value, CultureInfo.InvariantCulture));

                var at = new System.Drawing.Point((int)(atf.X * pixelsPerMm), (int)(atf.Y * pixelsPerMm));
                if (!drillMatch.Success || drillMatch.Groups.Count < 2) continue;
                var drillf = float.Parse(drillMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                var drill = (int) (drillf * pixelsPerMm / 2);

                CvInvoke.Circle(mat, at, drill, EmguCvExtensions.WhiteColor, -1);


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

                var atf = new PointF(float.Parse(atMatch.Groups[1].Value, CultureInfo.InvariantCulture), float.Parse(atMatch.Groups[2].Value, CultureInfo.InvariantCulture));
                var at = new System.Drawing.Point((int)(atf.X * pixelsPerMm), (int)(atf.Y * pixelsPerMm));
                var endf = new PointF(float.Parse(endMatch.Groups[1].Value, CultureInfo.InvariantCulture), float.Parse(endMatch.Groups[2].Value, CultureInfo.InvariantCulture));
                var radius = (int)(Math.Max(Math.Abs(atf.X - endf.X), Math.Abs(atf.Y - endf.Y)) * pixelsPerMm);
                var widthf = float.Parse(widthMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                var width = (int)(widthf * pixelsPerMm);

                CvInvoke.Circle(mat, at, radius, EmguCvExtensions.WhiteColor, width);
                if (parseLine.Contains("fill solid"))
                {
                    CvInvoke.Circle(mat, at, radius, EmguCvExtensions.WhiteColor, -1);
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

                var startf = new PointF(float.Parse(startMatch.Groups[1].Value, CultureInfo.InvariantCulture), float.Parse(startMatch.Groups[2].Value, CultureInfo.InvariantCulture));
                var endf = new PointF(float.Parse(endMatch.Groups[1].Value, CultureInfo.InvariantCulture), float.Parse(endMatch.Groups[2].Value, CultureInfo.InvariantCulture));
                var widthf = float.Parse(widthMatch.Groups[1].Value, CultureInfo.InvariantCulture);

                var start = new System.Drawing.Point((int)(startf.X * pixelsPerMm), (int)(startf.Y * pixelsPerMm));
                var end = new System.Drawing.Point((int)(endf.X * pixelsPerMm), (int)(endf.Y * pixelsPerMm));
                var width = (int)(widthf * pixelsPerMm);

                CvInvoke.Rectangle(mat, new Rectangle(start, new System.Drawing.Size(end.X - start.X, end.Y - start.Y)), EmguCvExtensions.WhiteColor, width);
                if (parseLine.Contains("fill solid"))
                {
                    CvInvoke.Rectangle(mat, new Rectangle(start, new System.Drawing.Size(end.X - start.X, end.Y - start.Y)), EmguCvExtensions.WhiteColor, -1);
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


                var atf = new PointF(float.Parse(atMatch.Groups[1].Value, CultureInfo.InvariantCulture), float.Parse(atMatch.Groups[2].Value, CultureInfo.InvariantCulture));
                var sizef = new SizeF(float.Parse(sizeMatch.Groups[1].Value, CultureInfo.InvariantCulture), float.Parse(sizeMatch.Groups[2].Value, CultureInfo.InvariantCulture));

                var at = new System.Drawing.Point((int)(location.X * pixelsPerMm + atf.X * pixelsPerMm), (int)(location.Y * pixelsPerMm + atf.Y * pixelsPerMm));

                if (parseLine.Contains(" rect ") || parseLine.Contains(" roundrect "))
                {
                    var size = new System.Drawing.Size((int)(sizef.Width * pixelsPerMm), (int)(sizef.Height * pixelsPerMm));
                    var rect = new Rectangle(at, size);
                    rect.Offset(-size.Width / 2, -size.Height / 2);
                    CvInvoke.Rectangle(mat, rect, EmguCvExtensions.WhiteColor, -1);
                }
                else if (parseLine.Contains(" oval ") || parseLine.Contains(" circle "))
                {
                    var size = new System.Drawing.Size((int)(sizef.Width / 2 * pixelsPerMm), (int)(sizef.Height / 2 * pixelsPerMm));
                    CvInvoke.Ellipse(mat, at, size, 0, 0, 360, EmguCvExtensions.WhiteColor, -1);
                }

                if (drillMatch.Success && drillMatch.Groups.Count >= 2)
                {
                    var drillf = float.Parse(drillMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                    var drill = (int)(drillf * pixelsPerMm / 2);

                    drillPoints.Add(new KeyValuePair<Point, int>(at, drill));
                }


                continue;
            }
        }

        foreach (var pair in drillPoints)
        {
            CvInvoke.Circle(mat, pair.Key, pair.Value, EmguCvExtensions.BlackColor, -1);
        }

        CvInvoke.Imshow("asd", mat);
        CvInvoke.WaitKey();
        return;
        */