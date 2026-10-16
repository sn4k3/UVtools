/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.FileFormats;
using Xunit;

namespace UVtools.Tests;

/// <summary>
/// Formats list their thumbnails in different orders (largest first or smallest first), the thumbnails must be
/// paired with the target slots by size instead of by order when converting (#1121).
/// </summary>
public class ThumbnailSizeRankMatchingTests
{
    private static readonly MCvScalar SmallColor = new(10, 20, 200);
    private static readonly MCvScalar LargeColor = new(200, 20, 10);

    private static Mat CreateFilled(Size size, MCvScalar color)
    {
        var mat = new Mat(size.Height, size.Width, DepthType.Cv8U, 3);
        mat.SetTo(color);
        return mat;
    }

    private static void AssertSlot(Mat thumbnail, Size expectedSize, MCvScalar expectedColor)
    {
        Assert.Equal(expectedSize, thumbnail.Size);
        var mean = CvInvoke.Mean(thumbnail); // the images are filled with a single color
        Assert.Equal(expectedColor.V0, mean.V0, 1);
        Assert.Equal(expectedColor.V1, mean.V1, 1);
        Assert.Equal(expectedColor.V2, mean.V2, 1);
    }

    [Fact]
    public void LargestFirstSourceIsMatchedToSmallestFirstTarget()
    {
        // Chitubox ZIP order (largest first) into GR1 (smallest first)
        using var large = CreateFilled(new Size(954, 850), LargeColor);
        using var small = CreateFilled(new Size(168, 150), SmallColor);
        var file = new GR1File();
        Assert.Equal(new Size(116, 116), file.ThumbnailsOriginalSize[0]);
        Assert.Equal(new Size(290, 290), file.ThumbnailsOriginalSize[1]);

        Assert.True(file.ReplaceThumbnails([large, small]));

        Assert.Equal(2, file.ThumbnailsCount);
        AssertSlot(file.Thumbnails[0], file.ThumbnailsOriginalSize[0], SmallColor);
        AssertSlot(file.Thumbnails[1], file.ThumbnailsOriginalSize[1], LargeColor);
    }

    [Fact]
    public void SourceAlreadyInTargetOrderIsKept()
    {
        using var small = CreateFilled(new Size(168, 150), SmallColor);
        using var large = CreateFilled(new Size(954, 850), LargeColor);
        var file = new GR1File();

        Assert.True(file.ReplaceThumbnails([small, large]));

        Assert.Equal(2, file.ThumbnailsCount);
        AssertSlot(file.Thumbnails[0], file.ThumbnailsOriginalSize[0], SmallColor);
        AssertSlot(file.Thumbnails[1], file.ThumbnailsOriginalSize[1], LargeColor);
    }

    [Fact]
    public void SmallestFirstSourceIsMatchedToLargestFirstTarget()
    {
        // GR1 order (smallest first) into Chitubox CTB (largest first)
        using var small = CreateFilled(new Size(116, 116), SmallColor);
        using var large = CreateFilled(new Size(290, 290), LargeColor);
        var file = new ChituboxFile();
        Assert.True(file.ThumbnailsOriginalSize[0].Width > file.ThumbnailsOriginalSize[1].Width);

        Assert.True(file.ReplaceThumbnails([small, large]));

        Assert.Equal(2, file.ThumbnailsCount);
        AssertSlot(file.Thumbnails[0], file.ThumbnailsOriginalSize[0], LargeColor);
        AssertSlot(file.Thumbnails[1], file.ThumbnailsOriginalSize[1], SmallColor);
    }
}
