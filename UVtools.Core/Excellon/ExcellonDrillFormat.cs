/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using EmguExtensions;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;

namespace UVtools.Core.Excellon;

/// <summary>
/// <para>The Excellon drill format is a subset of RS274D and is used by the drilling and routing machines made by the Excellon corporation.
/// Because of Excellon's long history and dominance of the PCB drilling business for many years their format is a defacto industry standard.</para>
/// <para>Almost every PCB layout software can produce this format. However we have noticed that many PCB layout tools do not take
/// full advantage of the header information which makes reading the drill file more difficult than it should be.</para>
/// <para>https://www.artwork.com/gerber/drl2laser/excellon/index.htm</para>
/// <para>https://gist.github.com/katyo/5692b935abc085b1037e</para>
/// </summary>
public class ExcellonDrillFormat
{
    #region Sub classes

    /// <summary>
    /// <para>Defines tool as having a diameter.</para>
    /// <para>For each tool used in the data the diameter should be defined here.</para>
    /// <para>There are additional parameters but if you are a PCB designer it is not up to you to specify feed rates and such.</para>
    /// </summary>
    public class Tool
    {
        public uint Index { get; init; }

        public float Diameter { get; init; }

        public Tool(uint index, float diameter)
        {
            Index = index;
            Diameter = diameter;
        }

        public override string ToString()
        {
            return FormattableString.Invariant($"T{Index}C{Diameter}");
        }
    }

    public class Drill
    {
        public Tool Tool { get; init; }

        public PointF Position { get; init; }

        public float Diameter => Tool.Diameter;

        public Drill(Tool tool, PointF position)
        {
            Tool = tool;
            Position = position;
        }

        public override string ToString()
        {
            return FormattableString.Invariant($"X{Position.X}Y{Position.Y}");
        }
    }

    public class Slot
    {
        public Tool Tool { get; init; }
        public PointF Start { get; init; }
        public PointF End { get; init; }
        public float Diameter => Tool.Diameter;

        public Slot(Tool tool, PointF start, PointF end)
        {
            Tool = tool;
            Start = start;
            End = end;
        }

        public override string ToString()
            => FormattableString.Invariant(
                $"X{Start.X}Y{Start.Y}G85X{End.X}Y{End.Y}");
    }

    #endregion

    #region Enums
    public enum ExcellonDrillUnitType : byte
    {
        Millimeter,
        Inch
    }

    public enum ExcellonDrillZerosIncludeType : byte
    {
        /// <summary>
        /// Use float system
        /// </summary>
        None,
        /// <summary>
        /// Include left zeros
        /// </summary>
        Leading,
        /// <summary>
        /// Include right zeros
        /// </summary>
        Trail,

    }

    #endregion

    #region Constants

    public static readonly string[] Extensions = ["drl", "xln"];

    /// <summary>
    /// Indicates the start of the header. should always be the first line in the header
    /// </summary>
    public const string CommandM48 = "M48";

    /// <summary>
    /// Number of padding zeros on coordinate system
    /// </summary>
    public const byte PaddingZeros = 6;

    public const int InchResolution = 10000;
    public const int MillimeterResolution = 1000;
    #endregion

    #region Properties

    /// <summary>
    /// Use Format 2 commands; alternative would be FMAT,1
    /// </summary>
    public uint FormatVersion { get; set; } = 2;
    public ExcellonDrillUnitType UnitType { get; set; } = ExcellonDrillUnitType.Millimeter;
    public ExcellonDrillZerosIncludeType ZerosIncludeType { get; set; } = ExcellonDrillZerosIncludeType.Leading;

    public Dictionary<uint, Tool> Tools { get; init; } = new();

    public List<Drill> Drills { get; init; } = [];
    public List<Slot> Slots { get; init; } = [];

    private SizeF XYppmm { get; set; }

    /// <summary>
    /// Gets or sets the X offset for drawings in millimeters
    /// </summary>
    public float OffsetX { get; set; }

    /// <summary>
    /// Gets or sets the Y offset for drawings in millimeters
    /// </summary>
    public float OffsetY { get; set; }

    /// <summary>
    /// Gets or sets to inverse the polarity on drawing
    /// </summary>
    public bool InversePolarity { get; set; }

    /// <summary>
    /// Gets or sets the scale to apply to each shape drawing size.
    /// Positions and vectors aren't affected by this.
    /// </summary>
    private double _sizeScale = 1;
    public double SizeScale
    {
        get => _sizeScale;
        set => _sizeScale = double.IsFinite(value) && value > 0 ? value : 1;
    }

    public MidpointRoundingType SizeMidpointRounding { get; set; } = MidpointRoundingType.AwayFromZero;

    #endregion

    #region Constructor

    public ExcellonDrillFormat()
    {
    }

    public ExcellonDrillFormat(string filePath)
    {
        Load(filePath);
    }
    #endregion

    #region Methods
    private void Load(string filePath)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException("File not found.", filePath);
        using var tr = new StreamReader(filePath);
        var line = tr.ReadLine()?.Trim();

        if(string.IsNullOrWhiteSpace(line) || line != CommandM48) throw new InvalidDataException("Invalid Excellon Drill file, should start with M48.");

        Tools.Clear();
        Drills.Clear();
        Slots.Clear();
        FormatVersion = 2;
        UnitType = ExcellonDrillUnitType.Millimeter;
        ZerosIncludeType = ExcellonDrillZerosIncludeType.Leading;

        var endOfHeader = false;
        var drillMode = true;
        var routeDraw = false;
        var incrementalCoordinates = false;
        uint? selectedToolIndex = null;

        float x = 0, y = 0;
        var integerDigits = 0;
        var fractionDigits = 0;

        while ((line = tr.ReadLine()?.Trim()) is not null)
        {
            if (line.Length == 0) continue;

            if (line is "M30") break; // End

            if (line.StartsWith("FMAT,", StringComparison.Ordinal))
            {
                var split = line.Split(',', StringSplitOptions.TrimEntries);
                if (split.Length > 1 &&
                    uint.TryParse(split[1], NumberStyles.None,
                        CultureInfo.InvariantCulture, out var formatVersion))
                {
                    FormatVersion = formatVersion;
                }
                continue;
            }

            if (line.StartsWith("METRIC", StringComparison.Ordinal) ||
                line.StartsWith("INCH", StringComparison.Ordinal))
            {
                var split = line.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                UnitType = split[0] == "METRIC" ? ExcellonDrillUnitType.Millimeter : ExcellonDrillUnitType.Inch;

                if (split.Length >= 2)
                {
                    ZerosIncludeType = split[1] switch
                    {
                        "LZ" => ExcellonDrillZerosIncludeType.Leading,
                        "TZ" => ExcellonDrillZerosIncludeType.Trail,
                        _ => ExcellonDrillZerosIncludeType.None
                    };
                }
                else
                {
                    ZerosIncludeType = ExcellonDrillZerosIncludeType.None;
                }

                if (split.Length >= 3 && integerDigits == 0)
                {
                    var decimalIndex = split[2].IndexOf('.');
                    if (decimalIndex > 0)
                    {
                        integerDigits = decimalIndex;
                        fractionDigits = split[2].Length - decimalIndex - 1;
                    }
                }

                continue;
            }

            if (line is "M71" or "M72")
            {
                UnitType = line == "M71"
                    ? ExcellonDrillUnitType.Millimeter
                    : ExcellonDrillUnitType.Inch;
                continue;
            }

            if (integerDigits == 0 && line.StartsWith(";FILE_FORMAT="))
            {
                line = line[13..];
                var split = line.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (split.Length < 2) continue;
                int.TryParse(split[0], NumberStyles.None,
                    CultureInfo.InvariantCulture, out integerDigits);
                int.TryParse(split[1], NumberStyles.None,
                    CultureInfo.InvariantCulture, out fractionDigits);
                continue;
            }

            if (line is "ICI" or "ICI,ON" or "G91")
            {
                incrementalCoordinates = true;
                continue;
            }

            if (line is "ICI,OFF" or "G90")
            {
                incrementalCoordinates = false;
                continue;
            }

            if (line is "%" or "M95")
            {
                endOfHeader = true;
                continue;
            }

            if (line is "G81" or "G05")
            {
                drillMode = true;
                routeDraw = false;
                continue;
            }

            if (line is "M15" or "M16")
            {
                routeDraw = line == "M15";
                continue;
            }

            // Tool or select tool
            if (line[0] == 'T')
            {
                if (!endOfHeader)
                {
                    if (TryParseTool(line, out var tool))
                    {
                        Tools[tool.Index] = tool;
                    }
                }
                else
                {
                    if (TryParseTool(line, out var inlineTool))
                    {
                        Tools[inlineTool.Index] = inlineTool;
                        selectedToolIndex = inlineTool.Index;
                        continue;
                    }

                    var indexEnd = 1;
                    while (indexEnd < line.Length && char.IsAsciiDigit(line[indexEnd]))
                    {
                        indexEnd++;
                    }

                    if (indexEnd > 1 &&
                        uint.TryParse(line.AsSpan(1, indexEnd - 1), NumberStyles.None,
                            CultureInfo.InvariantCulture, out var toolIndex))
                    {
                        selectedToolIndex = toolIndex;
                    }
                }

                continue;
            }

            if (line[0] == ';') continue;

            var slotCommandIndex = line.IndexOf("G85", StringComparison.Ordinal);
            if (slotCommandIndex >= 0)
            {
                var current = new PointF(x, y);
                var startCommand = line[..slotCommandIndex];
                var endCommand = line[(slotCommandIndex + 3)..];
                if (!TryParsePosition(startCommand, current, incrementalCoordinates,
                        integerDigits, fractionDigits, out var start, out var hasStart) ||
                    !TryParsePosition(endCommand, hasStart ? start : current,
                        incrementalCoordinates, integerDigits, fractionDigits,
                        out var end, out var hasEnd) ||
                    !hasEnd)
                {
                    continue;
                }

                start = hasStart ? start : current;
                if (selectedToolIndex is { } slotToolIndex &&
                    Tools.TryGetValue(slotToolIndex, out var slotTool) &&
                    start != end)
                {
                    Slots.Add(new Slot(slotTool, start, end));
                }

                x = end.X;
                y = end.Y;
                continue;
            }

            if (line[0] == 'R' &&
                TryParseRepeat(line, integerDigits, fractionDigits, out var repeatCount,
                    out var repeatOffset) &&
                selectedToolIndex is { } repeatToolIndex &&
                Tools.TryGetValue(repeatToolIndex, out var repeatTool))
            {
                for (var repeat = 0; repeat < repeatCount; repeat++)
                {
                    x += repeatOffset.X;
                    y += repeatOffset.Y;
                    Drills.Add(new Drill(repeatTool, new PointF(x, y)));
                }
                continue;
            }

            var coordinateCommand = line;
            if (line.StartsWith("G00", StringComparison.Ordinal))
            {
                drillMode = false;
                routeDraw = false;
                coordinateCommand = line[3..];
            }
            else if (line.StartsWith("G01", StringComparison.Ordinal))
            {
                drillMode = false;
                routeDraw = true;
                coordinateCommand = line[3..];
            }
            else if (line.StartsWith("G02", StringComparison.Ordinal) ||
                     line.StartsWith("G03", StringComparison.Ordinal))
            {
                drillMode = false;
                routeDraw = false;
                coordinateCommand = line[3..];
            }
            else if (line.StartsWith("G05", StringComparison.Ordinal) ||
                     line.StartsWith("G81", StringComparison.Ordinal))
            {
                drillMode = true;
                routeDraw = false;
                coordinateCommand = line[3..];
            }

            var previous = new PointF(x, y);
            if (!TryParsePosition(coordinateCommand, previous, incrementalCoordinates,
                    integerDigits, fractionDigits, out var position, out var hasCoordinate) ||
                !hasCoordinate)
            {
                continue;
            }

            if (selectedToolIndex is { } coordinateToolIndex &&
                Tools.TryGetValue(coordinateToolIndex, out var coordinateTool))
            {
                if (drillMode)
                {
                    Drills.Add(new Drill(coordinateTool, position));
                }
                else if (routeDraw && previous != position)
                {
                    Slots.Add(new Slot(coordinateTool, previous, position));
                }
            }

            x = position.X;
            y = position.Y;
        }
    }

    private static bool TryParseTool(string line, out Tool tool)
    {
        tool = null!;
        var indexEnd = 1;
        while (indexEnd < line.Length && char.IsAsciiDigit(line[indexEnd]))
        {
            indexEnd++;
        }

        var diameterIndex = line.IndexOf('C', indexEnd);
        if (indexEnd == 1 || diameterIndex < 0 ||
            !uint.TryParse(line.AsSpan(1, indexEnd - 1), NumberStyles.None,
                CultureInfo.InvariantCulture, out var index))
        {
            return false;
        }

        var diameterEnd = diameterIndex + 1;
        while (diameterEnd < line.Length &&
               (char.IsAsciiDigit(line[diameterEnd]) || line[diameterEnd] is '.' or '-' or '+'))
        {
            diameterEnd++;
        }

        if (diameterEnd == diameterIndex + 1 ||
            !float.TryParse(line.AsSpan(diameterIndex + 1,
                    diameterEnd - diameterIndex - 1), NumberStyles.Float,
                CultureInfo.InvariantCulture, out var diameter) ||
            !float.IsFinite(diameter) || diameter <= 0)
        {
            return false;
        }

        tool = new Tool(index, diameter);
        return true;
    }

    private bool TryParsePosition(string line, PointF current, bool incremental,
        int integerDigits, int fractionDigits, out PointF position,
        out bool hasCoordinate)
    {
        position = current;
        if (!TryReadCoordinate(line, 'X', integerDigits, fractionDigits,
                out var parsedX, out var hasX) ||
            !TryReadCoordinate(line, 'Y', integerDigits, fractionDigits,
                out var parsedY, out var hasY))
        {
            hasCoordinate = false;
            return false;
        }

        hasCoordinate = hasX || hasY;
        if (!hasCoordinate) return true;

        position = incremental
            ? new PointF(current.X + (hasX ? parsedX : 0),
                current.Y + (hasY ? parsedY : 0))
            : new PointF(hasX ? parsedX : current.X,
                hasY ? parsedY : current.Y);
        return float.IsFinite(position.X) && float.IsFinite(position.Y);
    }

    private bool TryReadCoordinate(string line, char axis, int integerDigits,
        int fractionDigits, out float coordinate, out bool found)
    {
        coordinate = 0;
        var axisIndex = line.IndexOf(axis);
        found = axisIndex >= 0;
        if (!found) return true;

        var valueStart = axisIndex + 1;
        var valueEnd = valueStart;
        if (valueEnd < line.Length && line[valueEnd] is '-' or '+') valueEnd++;
        while (valueEnd < line.Length &&
               (char.IsAsciiDigit(line[valueEnd]) || line[valueEnd] == '.'))
        {
            valueEnd++;
        }

        if (valueEnd == valueStart ||
            (valueEnd == valueStart + 1 && line[valueStart] is '-' or '+'))
        {
            return false;
        }

        var value = line.AsSpan(valueStart, valueEnd - valueStart);
        if (value.Contains('.'))
        {
            return float.TryParse(value, NumberStyles.Float,
                       CultureInfo.InvariantCulture, out coordinate) &&
                   float.IsFinite(coordinate);
        }

        var negative = value[0] == '-';
        var positive = value[0] == '+';
        var digits = negative || positive ? value[1..] : value;
        var declaredFormat = integerDigits > 0 && fractionDigits > 0;
        var totalDigits = declaredFormat
            ? integerDigits + fractionDigits
            : PaddingZeros;
        var decimals = declaredFormat
            ? fractionDigits
            : UnitType == ExcellonDrillUnitType.Millimeter ? 3 : 4;
        if (digits.Length == 0 || digits.Length > totalDigits ||
            !long.TryParse(digits, NumberStyles.None,
                CultureInfo.InvariantCulture, out var rawValue))
        {
            return false;
        }

        if (ZerosIncludeType is ExcellonDrillZerosIncludeType.None
            or ExcellonDrillZerosIncludeType.Leading)
        {
            rawValue *= (long)Math.Pow(10, totalDigits - digits.Length);
        }

        var decoded = rawValue / Math.Pow(10, decimals);
        if (negative) decoded = -decoded;
        coordinate = (float)decoded;
        return float.IsFinite(coordinate);
    }

    private bool TryParseRepeat(string line, int integerDigits, int fractionDigits,
        out int count, out PointF offset)
    {
        count = 0;
        offset = PointF.Empty;
        var countEnd = 1;
        while (countEnd < line.Length && char.IsAsciiDigit(line[countEnd]))
        {
            countEnd++;
        }

        return countEnd > 1 &&
               int.TryParse(line.AsSpan(1, countEnd - 1), NumberStyles.None,
                   CultureInfo.InvariantCulture, out count) &&
               count is > 0 and <= 1_000_000 &&
               TryParsePosition(line[countEnd..], PointF.Empty, true,
                   integerDigits, fractionDigits, out offset, out var hasCoordinate) &&
               hasCoordinate;
    }

    public float ValueToCoordinate(float value) =>
        UnitType switch
        {
            ExcellonDrillUnitType.Millimeter => MathF.Round(value / MillimeterResolution, PaddingZeros),
            ExcellonDrillUnitType.Inch => MathF.Round(value / InchResolution, PaddingZeros),
            _ => throw new ArgumentOutOfRangeException()
        };

    public float ValueToCoordinate(int value) =>
        ValueToCoordinate((float)value);

    public float GetMillimeters(float size)
    {
        if (UnitType == ExcellonDrillUnitType.Millimeter) return size;
        return size * (float)UnitExtensions.InchToMillimeter;
    }

    public double GetMillimeters(double size)
    {
        if (UnitType == ExcellonDrillUnitType.Millimeter) return size;
        return size * UnitExtensions.InchToMillimeter;
    }


    public SizeF GetMillimeters(SizeF size)
    {
        if (UnitType == ExcellonDrillUnitType.Millimeter) return size;
        return new SizeF(size.Width * (float)UnitExtensions.InchToMillimeter, size.Height * (float)UnitExtensions.InchToMillimeter);
    }

    public PointF GetMillimeters(PointF point)
    {
        if (UnitType == ExcellonDrillUnitType.Millimeter) return point;
        return new PointF(point.X * (float)UnitExtensions.InchToMillimeter, point.Y * (float)UnitExtensions.InchToMillimeter);
    }

    public Point PositionMmToPx(PointF atMm)
        => new((int)Math.Round((atMm.X + OffsetX) * XYppmm.Width, MidpointRounding.AwayFromZero), (int)Math.Round((atMm.Y + OffsetY) * XYppmm.Height, MidpointRounding.AwayFromZero));

    public int SizeMmToPx(float sizeMm)
        => (int)Math.Max(1, Math.Round(sizeMm * XYppmm.Max() * SizeScale, (MidpointRounding)SizeMidpointRounding));

    public Size SizeMmToPx(float sizeMmX, float sizeMmY)
        => new ((int)Math.Max(1, Math.Round(sizeMmX * XYppmm.Width * SizeScale, (MidpointRounding)SizeMidpointRounding)),
            (int)Math.Max(1, Math.Round(sizeMmY * XYppmm.Height * SizeScale, (MidpointRounding)SizeMidpointRounding)));
    #endregion

    #region Static methods
    public static void ParseAndDraw(ExcellonDrillFormat document, string filePath, Mat mat, bool enableAntiAliasing = false)
    {
        document.Load(filePath);

        var color = document.InversePolarity
            ? EmguCvExtensions.WhiteColor
            : EmguCvExtensions.BlackColor;
        var lineType = enableAntiAliasing
            ? LineType.AntiAlias
            : LineType.EightConnected;

        foreach (var slot in document.Slots)
        {
            var diameterMillimeters = document.GetMillimeters(slot.Diameter);
            var radius = document.SizeMmToPx(
                diameterMillimeters / 2, diameterMillimeters / 2);
            var start = document.PositionMmToPx(document.GetMillimeters(slot.Start));
            var end = document.PositionMmToPx(document.GetMillimeters(slot.End));
            CvInvoke.Line(mat, start, end, color,
                EmguCvExtensions.CorrectThickness(
                    document.SizeMmToPx(diameterMillimeters)),
                lineType);
            mat.DrawCircle(start, radius, color, -1, lineType);
            mat.DrawCircle(end, radius, color, -1, lineType);
        }

        foreach (var drill in document.Drills)
        {
            var radiusMillimeters = document.GetMillimeters(drill.Diameter / 2);
            var position = document.PositionMmToPx(document.GetMillimeters(drill.Position));
            var radius = document.SizeMmToPx(radiusMillimeters, radiusMillimeters);
            mat.DrawCircle(position, radius, color, -1, lineType);
        }
    }

    public static ExcellonDrillFormat ParseAndDraw(OperationPCBExposure.PCBExposureFile file, Mat mat, SizeF xyPpmm,
        MidpointRoundingType sizeMidpointRounding = MidpointRoundingType.AwayFromZero, SizeF offset = default, bool enableAntiAliasing = false)
    {
        var document = new ExcellonDrillFormat
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
    #endregion
}
