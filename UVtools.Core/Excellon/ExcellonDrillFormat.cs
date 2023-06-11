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
using System.IO;
using System.Text.RegularExpressions;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;

namespace UVtools.Core.Excellon;

/// <summary>
/// <para>The Excellon drill format is a subset of RS274D and is used by the drilling and routing machines made by the Excellon corporation.
/// Because of Excellon's long history and dominance of the PCB drilling business for many years their format is a defacto industry standard.</para>
/// <para>Almost every PCB layout software can produce this format.However we have noticed that many PCB layout tools do not take
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
            return $"T{Index}C{nameof(Diameter)}";
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
            return $"X{Position.X}Y{Position.Y}";
        }
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

    public static readonly string[] Extensions = {"drl", "xln"};

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

    public List<Drill> Drills { get; init; } = new();

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
    public double SizeScale { get; set; } = 1;

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

        bool endOfHeader = false;
        bool drillMode = true;
        uint selectedToolIndex = 0;

        float x = 0, y = 0;

        while ((line = tr.ReadLine()?.Trim()) is not null)
        {
            if (line.Length == 0) continue;

            if (line is "M30") break; // End

            if (line.StartsWith("FMAT,"))
            {
                var split = line.Split(',', StringSplitOptions.TrimEntries);
                FormatVersion = uint.Parse(split[1]);
                continue;
            }

            if (line.StartsWith("METRIC") || line.StartsWith("INCH"))
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

                continue;
            }

            if (line is "ICI" or "ICI,ON")
            {
                throw new NotImplementedException("ICI (Incremental input of program coordinates) is not yet implemented, please use absolute coordinate system.");
            }

            if (line is "%" or "M95")
            {
                endOfHeader = true;
                continue;
            }

            if (line is "G81" or "G05")
            {
                drillMode = true;
                continue;
            }

            // Tool or select tool
            if (line[0] == 'T')
            {
                if (!endOfHeader)
                {
                    var match = Regex.Match(line, @"^T([0-9]+)C(([0-9]*[.])?[0-9]+)");
                    if (match is
                        {
                            Success: true,
                            Groups.Count: >= 4
                        })
                    {
                        var index = uint.Parse(match.Groups[1].Value);
                        var diameter = float.Parse(match.Groups[2].Value);
                        var tool = new Tool(index, diameter);
                        Tools.Add(index, tool);
                    }
                }
                else
                {
                    selectedToolIndex = uint.Parse(line[1..]);
                }


                continue;
            }

            // Drill coordinate
            if (line[0] == 'X' || line[0] == 'Y')
            {
                if(!drillMode) continue;

                var match = Regex.Match(line, @"^X-?(([0-9]*[.])?[0-9]+)");
                if (match is
                    {
                        Success: true,
                        Groups.Count: >= 2
                    })
                {
                    if (match.Groups[1].Value.Contains('.') || ZerosIncludeType == ExcellonDrillZerosIncludeType.None)
                    {
                        x = float.Parse(match.Groups[1].Value);
                    }
                    else
                    {
                        switch (ZerosIncludeType)
                        {
                            case ExcellonDrillZerosIncludeType.Leading:
                                x = ValueToCoordinate(float.Parse(match.Groups[1].Value.PadRight(PaddingZeros, '0')));
                                break;
                            case ExcellonDrillZerosIncludeType.Trail:
                                x = ValueToCoordinate(float.Parse(match.Groups[1].Value.PadLeft(PaddingZeros, '0')));
                                break;
                        }
                    }
                    
                }

                match = Regex.Match(line, @"Y-?(([0-9]*[.])?[0-9]+)");
                if (match is
                    {
                        Success: true,
                        Groups.Count: >= 2
                    })
                {
                    if (match.Groups[1].Value.Contains('.') || ZerosIncludeType == ExcellonDrillZerosIncludeType.None)
                    {
                        y = float.Parse(match.Groups[1].Value);
                    }
                    else
                    {
                        switch (ZerosIncludeType)
                        {
                            case ExcellonDrillZerosIncludeType.Leading:
                                y = ValueToCoordinate(float.Parse(match.Groups[1].Value.PadRight(PaddingZeros, '0')));
                                break;
                            case ExcellonDrillZerosIncludeType.Trail:
                                y = ValueToCoordinate(float.Parse(match.Groups[1].Value.PadLeft(PaddingZeros, '0')));
                                break;
                        }
                    }

                }

                var drill = new Drill(Tools[selectedToolIndex], new PointF(x, y));
                Drills.Add(drill);

                continue;
            }
        }
    }
   
    public float ValueToCoordinate(float value) =>
        UnitType switch
        {
            ExcellonDrillUnitType.Millimeter => (float) Math.Round(value / MillimeterResolution, PaddingZeros),
            ExcellonDrillUnitType.Inch => (float) Math.Round(value / InchResolution, PaddingZeros),
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
    #endregion

    #region Static methods
    public static void ParseAndDraw(ExcellonDrillFormat document, string filePath, Mat mat, bool enableAntiAliasing = false)
    {
        document.Load(filePath);

        foreach (var drill in document.Drills)
        {
            var position = document.PositionMmToPx(document.GetMillimeters(drill.Position));
            var radius = document.SizeMmToPx(document.GetMillimeters(drill.Diameter / 2));
            CvInvoke.Circle(mat, position, radius,
                document.InversePolarity ? EmguExtensions.WhiteColor : EmguExtensions.BlackColor,
                -1, 
                enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
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