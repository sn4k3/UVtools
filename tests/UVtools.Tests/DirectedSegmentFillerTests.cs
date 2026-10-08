/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UVtools.Core.Objects;
using Xunit;

namespace UVtools.Tests;

/// <summary>
/// The non-zero winding rule fill of unordered directed segments, used by the Anycubic PWSZ layers.
/// </summary>
public class DirectedSegmentFillerTests
{
    private const int Width = 16;
    private const int Height = 12;

    /// <summary>
    /// A rectangle as 4 directed segments, clockwise in a Y down image or the opposite way.
    /// </summary>
    private static DirectedSegmentF[] Rectangle(float x0, float y0, float x1, float y1, bool clockwise = true)
    {
        DirectedSegmentF[] segments =
        [
            new(x0, y0, x1, y0),
            new(x1, y0, x1, y1),
            new(x1, y1, x0, y1),
            new(x0, y1, x0, y0)
        ];

        return clockwise
            ? segments
            : segments.Select(s => new DirectedSegmentF(s.EndX, s.EndY, s.StartX, s.StartY)).Reverse().ToArray();
    }

    private static string Render(IEnumerable<DirectedSegmentF> segments, int width = Width, int height = Height, int stride = 0)
    {
        if (stride == 0) stride = width;
        var pixels = new byte[stride * height];
        DirectedSegmentFiller.Fill(segments.ToArray(), pixels, width, height, stride);

        var sb = new StringBuilder();
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++) sb.Append(pixels[y * stride + x] != 0 ? '#' : '.');
            if (y < height - 1) sb.Append('\n');
        }
        return sb.ToString();
    }

    private static string Grid(params string[] rows) => string.Join('\n', rows);

    private static string[] Blank(int count) => Enumerable.Repeat(new string('.', Width), count).ToArray();

    [Fact]
    public void SingleSquareOnPixelBoundariesFillsExactlyThoseRows()
    {
        // Edges exactly on the pixel grid lines: the pixels 2..5 are inside, the last row and column are not
        var expected = Grid(
            [
                ..Blank(2),
                "..####..........",
                "..####..........",
                "..####..........",
                "..####..........",
                ..Blank(6)
            ]);

        Assert.Equal(expected, Render(Rectangle(2, 2, 6, 6)));
    }

    [Fact]
    public void EdgesOnPixelCentersAreHalfOpen()
    {
        // The centers of the pixels are at x.5, an edge exactly on a center includes it on the left/top side only
        var expected = Grid(
            [
                ..Blank(2),
                "..####..........",
                "..####..........",
                "..####..........",
                "..####..........",
                ..Blank(6)
            ]);

        Assert.Equal(expected, Render(Rectangle(2.5f, 2.5f, 6.5f, 6.5f)));
    }

    [Fact]
    public void EdgesBetweenPixelCentersDoNotLightThePixel()
    {
        // 2.4 to 6.4 covers the centers 2.5 .. 5.5 only
        Assert.Equal(Render(Rectangle(2, 2, 6, 6)), Render(Rectangle(2.4f, 2.4f, 6.4f, 6.4f)));

        // Smaller than a pixel center spacing: nothing is lit
        Assert.DoesNotContain('#', Render(Rectangle(2.6f, 2.6f, 3.4f, 3.4f)));
    }

    [Fact]
    public void OverlappingSquaresWithTheSameOrientationFillTheUnion()
    {
        var segments = Rectangle(1, 1, 7, 7).Concat(Rectangle(4, 4, 11, 10));

        var expected = Grid(
            [
                "................",
                ".######.........",
                ".######.........",
                ".######.........",
                ".##########.....",
                ".##########.....",
                ".##########.....",
                "....#######.....",
                "....#######.....",
                "....#######.....",
                "................",
                "................",
            ]);

        // The overlap [4,7) x [4,7) is not a hole, as the even-odd rule would make it
        Assert.Equal(expected, Render(segments));
        Assert.Equal(RenderUnion(), Render(segments));
    }
    /// <summary>
    /// The union of two rectangles built pixel by pixel
    /// </summary>
    private static string RenderUnion()
    {
        var sb = new StringBuilder();
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var inA = x is >= 1 and < 7 && y is >= 1 and < 7;
                var inB = x is >= 4 and < 11 && y is >= 4 and < 10;
                sb.Append(inA || inB ? '#' : '.');
            }
            if (y < Height - 1) sb.Append('\n');
        }
        return sb.ToString();
    }

    [Fact]
    public void OverlappingSquaresWithOppositeOrientationCancelInTheOverlap()
    {
        // Clockwise and counterclockwise shapes cancel each other where they overlap, the rest is still filled
        var segments = Rectangle(1, 1, 7, 7).Concat(Rectangle(4, 4, 11, 10, false));
        var rows = Render(segments).Split('\n');

        Assert.Equal(".######.........", rows[2]);
        // The overlap [4,7) x [4,7) has a winding of 0
        Assert.Equal(".###...####.....", rows[5]);
        Assert.Equal("....#######.....", rows[8]);
    }

    [Fact]
    public void InnerSquareWithTheOppositeOrientationIsAHole()
    {
        var segments = Rectangle(1, 1, 11, 11).Concat(Rectangle(4, 4, 8, 8, false));

        var expected = Grid(
            [
                "................",
                ".##########.....",
                ".##########.....",
                ".##########.....",
                ".###....###.....",
                ".###....###.....",
                ".###....###.....",
                ".###....###.....",
                ".##########.....",
                ".##########.....",
                ".##########.....",
                "................",
            ]);

        Assert.Equal(expected, Render(segments));
    }

    [Fact]
    public void InnerSquareWithTheSameOrientationIsFilled()
    {
        var segments = Rectangle(1, 1, 11, 11).Concat(Rectangle(4, 4, 8, 8));
        Assert.Equal(Render(Rectangle(1, 1, 11, 11)), Render(segments));
    }

    [Fact]
    public void SeparateSquaresStayApart()
    {
        var segments = Rectangle(1, 1, 4, 4).Concat(Rectangle(8, 6, 13, 10));

        var expected = Grid(
            [
                "................",
                ".###............",
                ".###............",
                ".###............",
                "................",
                "................",
                "........#####...",
                "........#####...",
                "........#####...",
                "........#####...",
                "................",
                "................",
            ]);

        Assert.Equal(expected, Render(segments));
    }

    [Fact]
    public void TouchingSquaresShareTheEdgeWithoutAGap()
    {
        // The shared edge at x = 6 runs both ways and cancels, so the pixels around it are filled
        var segments = Rectangle(2, 2, 6, 6).Concat(Rectangle(6, 2, 10, 6));
        Assert.Equal(Render(Rectangle(2, 2, 10, 6)), Render(segments));
    }

    [Fact]
    public void TouchingSquaresOnARowBoundaryShareTheEdgeWithoutAGap()
    {
        var segments = Rectangle(2, 2, 6, 5).Concat(Rectangle(2, 5, 6, 9));
        Assert.Equal(Render(Rectangle(2, 2, 6, 9)), Render(segments));
    }

    [Fact]
    public void SegmentsOrderDoesNotMatter()
    {
        var segments = Rectangle(1, 1, 7, 7)
            .Concat(Rectangle(4, 4, 11, 10))
            .Concat(Rectangle(2, 2, 3, 3, false))
            .Concat(Rectangle(12, 1, 15, 4))
            .Concat([new(0, 5, 3.5f, 5), new(float.NaN, 0, 1, 1)])
            .ToList();

        var expected = Render(segments);
        var random = new Random(970);
        for (var i = 0; i < 20; i++)
        {
            var shuffled = segments.OrderBy(_ => random.Next()).ToList();
            Assert.Equal(expected, Render(shuffled));
        }
    }

    [Fact]
    public void HorizontalAndNonFiniteSegmentsAreIgnored()
    {
        var segments = new DirectedSegmentF[]
        {
            new(0, 3.2f, 10, 3.2f),
            new(float.NaN, 0, 4, 8),
            new(0, float.PositiveInfinity, 4, 8),
        };

        Assert.DoesNotContain('#', Render(segments));
    }

    [Fact]
    public void FilledAreaIsClippedToTheImage()
    {
        var segments = Rectangle(-5, -5, 3, 3).Concat(Rectangle(13, 9, 40, 40));

        var rows = Render(segments).Split('\n');
        Assert.Equal("###.............", rows[0]);
        Assert.Equal("###.............", rows[2]);
        Assert.Equal("................", rows[3]);
        Assert.Equal("................", rows[8]);
        Assert.Equal(".............###", rows[9]);
        Assert.Equal(".............###", rows[11]);
    }

    [Fact]
    public void StrideLargerThanWidthIsRespectedAndThePaddingIsUntouched()
    {
        const int stride = Width + 5;
        var pixels = new byte[stride * Height];
        DirectedSegmentFiller.Fill(Rectangle(0, 0, 20, 20), pixels, Width, Height, stride, 200);

        for (var y = 0; y < Height; y++)
        {
            Assert.All(pixels.AsSpan(y * stride, Width).ToArray(), v => Assert.Equal(200, v));
            if (y < Height - 1) Assert.All(pixels.AsSpan(y * stride + Width, stride - Width).ToArray(), v => Assert.Equal(0, v));
        }
    }

    [Fact]
    public void ThrowsWhenThePixelsSpanIsTooSmall()
    {
        Assert.Throws<ArgumentException>(() => DirectedSegmentFiller.Fill(Rectangle(1, 1, 3, 3), new byte[Width * Height - 1], Width, Height, Width));
    }

    [Fact]
    public void NoSegmentsDoNothing()
    {
        Assert.DoesNotContain('#', Render([]));
    }
}
