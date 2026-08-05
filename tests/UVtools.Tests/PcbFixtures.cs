/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.IO;
using Emgu.CV;
using EmguExtensions;
using UVtools.Core.FileFormats;

namespace UVtools.Tests;

/// <summary>
/// Synthetic gerber and drill files, plus the plate they are measured against.
/// </summary>
/// <remarks>
/// Every board here is plotted with negative Y, the way a CAD tool exports a design placed below the page
/// origin, because that is the case the coordinate handling used to get wrong.
/// </remarks>
internal static class PcbFixtures
{
    /// <summary>
    /// Pixels per millimeter of <see cref="CreateSlicerFile"/>: a 1000px plate over 100mm.
    /// </summary>
    public const float Ppmm = 10f;

    /// <summary>
    /// Plate size in pixels, square to keep the vertical flip arithmetic obvious.
    /// </summary>
    public const int PlatePixels = 1000;

    /// <summary>
    /// A 20x20mm square outline at X 10..30mm, Y -20..-40mm, drawn with a 0.1mm circular aperture.
    /// </summary>
    public const string NegativeYBoard =
        """
        %FSLAX46Y46*%
        %MOMM*%
        %ADD10C,0.100000*%
        G01*
        D10*
        X10000000Y-20000000D02*
        X30000000Y-20000000D01*
        X30000000Y-40000000D01*
        X10000000Y-40000000D01*
        X10000000Y-20000000D01*
        M02*
        """;

    /// <summary>
    /// The same board mirrored onto positive Y, at X 10..30mm, Y 20..40mm. Regression control.
    /// </summary>
    public const string PositiveYBoard =
        """
        %FSLAX46Y46*%
        %MOMM*%
        %ADD10C,0.100000*%
        G01*
        D10*
        X10000000Y20000000D02*
        X30000000Y20000000D01*
        X30000000Y40000000D01*
        X10000000Y40000000D01*
        X10000000Y20000000D01*
        M02*
        """;

    /// <summary>
    /// <see cref="NegativeYBoard"/> with a 1.8mm square pad flashed at its centre (X20, Y-30) and a 1.8mm
    /// round pad at (X15, Y-25). The round pad is the control: it exercises a different aperture class.
    /// </summary>
    public const string NegativeYBoardWithPads =
        """
        %FSLAX46Y46*%
        %MOMM*%
        %ADD10C,0.100000*%
        %ADD14R,1.800000X1.800000*%
        %ADD15C,1.800000*%
        G01*
        D10*
        X10000000Y-20000000D02*
        X30000000Y-20000000D01*
        X30000000Y-40000000D01*
        X10000000Y-40000000D01*
        X10000000Y-20000000D01*
        D14*
        X20000000Y-30000000D03*
        D15*
        X15000000Y-25000000D03*
        M02*
        """;

    /// <summary>
    /// Two 1mm holes at (X12, Y-22) and (X28, Y-38), inside <see cref="NegativeYBoard"/>.
    /// Coordinates carry an explicit decimal point.
    /// </summary>
    public const string NegativeYDrill =
        """
        M48
        FMAT,2
        METRIC
        T1C1.000
        %
        G90
        G05
        T1
        X12.0Y-22.0
        X28.0Y-38.0
        M30
        """;

    /// <summary>
    /// Two 1mm holes through the centres of the pads of <see cref="NegativeYBoardWithPads"/>.
    /// </summary>
    public const string NegativeYPadDrill =
        """
        M48
        FMAT,2
        METRIC
        T1C1.000
        %
        G90
        G05
        T1
        X20.0Y-30.0
        X15.0Y-25.0
        M30
        """;

    /// <summary>
    /// One 1mm hole at X 15.000mm, Y -25.000mm, written with an implied decimal point:
    /// 4 integer and 3 fraction digits, trailing zeros included.
    /// </summary>
    public const string ImpliedDecimalDrill =
        """
        M48
        FMAT,2
        METRIC,TZ
        ;FILE_FORMAT=4:3
        T1C1.000
        %
        G90
        G05
        T1
        X0015000Y-0025000
        M30
        """;

    /// <summary>
    /// Creates a square 1000px / 100mm plate, ie <see cref="Ppmm"/> pixels per millimeter.
    /// </summary>
    public static FileFormat CreateSlicerFile() => new ChituboxFile
    {
        Resolution = new Size(PlatePixels, PlatePixels),
        DisplayWidth = PlatePixels / Ppmm,
        DisplayHeight = PlatePixels / Ppmm,
        LayerHeight = 0.05f,
        BottomExposureTime = 5
    };

    /// <summary>
    /// Counts the lit pixels of a square window centred on where the given millimeter coordinate lands,
    /// accounting for the drawing offset and the vertical flip.
    /// </summary>
    /// <param name="mat">Drawn plate</param>
    /// <param name="atMm">Coordinate as the gerber declares it</param>
    /// <param name="offsetMm">Offset the plate was drawn with</param>
    /// <param name="flipY">Whether the plate was flipped vertically</param>
    /// <param name="size">Window side in pixels, keep it well inside the feature being sampled</param>
    public static int CountAt(Mat mat, PointF atMm, SizeF offsetMm, bool flipY = true, int size = 8)
    {
        var x = (int)Math.Round((atMm.X + offsetMm.Width) * Ppmm);
        var y = (int)Math.Round((atMm.Y + offsetMm.Height) * Ppmm);
        if (flipY) y = mat.Height - 1 - y;

        using var window = mat.Roi(new Rectangle(x - size / 2, y - size / 2, size, size));
        return CvInvoke.CountNonZero(window);
    }
}

/// <summary>
/// Writes content to a temporary file and removes it on dispose.
/// </summary>
internal sealed class TempFile : IDisposable
{
    /// <summary>
    /// Gets the full path of the written file
    /// </summary>
    public string Path { get; }

    public TempFile(string content, string extension = ".gbr")
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"uvtools-test-{Guid.NewGuid():N}{extension}");
        File.WriteAllText(Path, content);
    }

    public void Dispose()
    {
        try
        {
            File.Delete(Path);
        }
        catch (IOException)
        {
            // A leftover temp file is not worth failing a test over
        }
    }
}
