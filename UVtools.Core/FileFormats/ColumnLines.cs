/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using Emgu.CV;
using EmguExtensions;
using UVtools.Core.Extensions;

namespace UVtools.Core.FileFormats;

/// <summary>
/// Helpers for the formats that store layers as vertical lines (one line per run of pixels on a column), as the
/// Creality CXDLP, Makerbase MDLP and GR1 Workshop do.
/// </summary>
/// <remarks>
/// The images are row major, so walking a column pixel by pixel jumps a whole image row per step. These helpers
/// work on a transposed copy, where every column is a contiguous span that can be scanned with vectorized searches.
/// </remarks>
internal static class ColumnLines
{
    /// <summary>
    /// Creates a transposed copy of a region of an image. Each row of the result is a column of the region, so
    /// the result has <see cref="Rectangle.Width"/> rows of <see cref="Rectangle.Height"/> pixels.
    /// </summary>
    public static Mat TransposeRoi(Mat mat, Rectangle roi)
    {
        var result = new Mat();
        if (roi.IsEmpty) return result;

        using var roiMat = mat.Roi(roi);
        CvInvoke.Transpose(roiMat, result);
        return result;
    }

    /// <summary>
    /// Gets the next run of equal non-zero pixels of a column, black pixels are skipped.
    /// </summary>
    /// <param name="column">The column pixels</param>
    /// <param name="index">The position to continue from, it is advanced past the run</param>
    /// <param name="start">The position of the first pixel of the run</param>
    /// <param name="length">The number of pixels on the run</param>
    /// <param name="color">The color of the run</param>
    /// <returns>True if a run was found, otherwise false</returns>
    public static bool TryGetNextColorRun(ReadOnlySpan<byte> column, ref int index, out int start, out int length, out byte color)
    {
        while (index < column.Length)
        {
            color = column[index];
            start = index;
            length = FileFormat.GetRunLength(column, index);
            index += length;
            if (color != 0) return true;
        }

        start = length = 0;
        color = 0;
        return false;
    }

    /// <summary>
    /// Gets the next run of white pixels (<paramref name="threshold"/> or above) of a column.
    /// </summary>
    /// <param name="column">The column pixels</param>
    /// <param name="threshold">The gray value from which a pixel is white</param>
    /// <param name="index">The position to continue from, it is advanced past the run</param>
    /// <param name="start">The position of the first pixel of the run</param>
    /// <param name="length">The number of pixels on the run</param>
    /// <returns>True if a run was found, otherwise false</returns>
    public static bool TryGetNextWhiteRun(ReadOnlySpan<byte> column, byte threshold, ref int index, out int start, out int length)
    {
        while (index < column.Length)
        {
            var white = column[index] >= threshold;
            start = index;
            length = BitPlaneRle.GetRunLength(column, index, threshold);
            index += length;
            if (white) return true;
        }

        start = length = 0;
        return false;
    }

    /// <summary>
    /// Creates a zeroed image to draw vertical lines on, to be converted into the final image with <see cref="ToImage"/>.
    /// </summary>
    public static Mat CreateColumns(Size resolution)
        => resolution.IsEmpty ? new Mat() : EmguCvExtensions.InitMat(new Size(resolution.Height, resolution.Width));

    /// <summary>
    /// Converts an image of columns created with <see cref="CreateColumns"/> into the final image.
    /// </summary>
    public static Mat ToImage(Mat columns)
    {
        var result = new Mat();
        if (columns.IsEmpty) return result;

        CvInvoke.Transpose(columns, result);
        return result;
    }

    /// <summary>
    /// Draws vertical lines on an image of columns, lines are clipped to the image like OpenCV does.
    /// </summary>
    public ref struct Canvas
    {
        private readonly Span<byte> _columns;
        private readonly int _width;
        private readonly int _height;

        /// <param name="columns">An image created with <see cref="CreateColumns"/></param>
        /// <param name="resolution">The resolution of the final image</param>
        public Canvas(Mat columns, Size resolution)
        {
            _columns = columns.IsEmpty ? default : columns.GetSpanOfBytes();
            _width = resolution.Width;
            _height = resolution.Height;
        }

        /// <summary>
        /// Draws a vertical line from <paramref name="startY"/> to <paramref name="endY"/>, both inclusive.
        /// </summary>
        public void Line(int x, int startY, int endY, byte color)
        {
            if ((uint)x >= (uint)_width) return;

            if (startY > endY) (startY, endY) = (endY, startY);
            if (endY < 0 || startY >= _height) return;

            startY = Math.Max(startY, 0);
            endY = Math.Min(endY, _height - 1);

            _columns.Slice(x * _height + startY, endY - startY + 1).Fill(color);
        }
    }
}
