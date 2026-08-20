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
using Emgu.CV;
using Emgu.CV.CvEnum;
using EmguExtensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.PixelEditor;
using Xunit;

namespace UVtools.Tests;

public class PixelStrokeTests
{
    [Fact]
    public void ConsecutiveDuplicatePointsAreNotStoredTwice()
    {
        var stroke = new PixelStroke();
        Assert.True(stroke.AddPoint(new Point(10, 10)));
        Assert.False(stroke.AddPoint(new Point(10, 10)));
        Assert.Single(stroke.Points);
    }

    [Fact]
    public void DistinctConsecutivePointsAreStoredInOrder()
    {
        var stroke = new PixelStroke();
        stroke.AddPoint(new Point(10, 10));
        stroke.AddPoint(new Point(11, 10));
        stroke.AddPoint(new Point(12, 12));

        Assert.Equal(3, stroke.Points.Count);
        Assert.Equal(new Point(10, 10), stroke.Points[0]);
        Assert.Equal(new Point(11, 10), stroke.Points[1]);
        Assert.Equal(new Point(12, 12), stroke.Points[2]);
    }

    [Fact]
    public void OperationTypeIsStroke()
    {
        Assert.Equal(PixelOperation.PixelOperationType.Stroke, new PixelStroke().OperationType);
    }

    [Fact]
    public void DefaultLayerRangeIsSingleLayer()
    {
        var stroke = new PixelStroke(42, [], LineType.AntiAlias,
            PixelDrawing.BrushShapeType.Square, 0, 1, -1, 30, 200, true);
        Assert.Equal(0u, stroke.LayersBelow);
        Assert.Equal(0u, stroke.LayersAbove);
        Assert.Equal(42u, stroke.LayerIndex);
    }

    [Fact]
    public void BrushSettingsAreStored()
    {
        var stroke = new PixelStroke
        {
            BrushShape = PixelDrawing.BrushShapeType.Circle,
            BrushSize = 7,
            RotationAngle = 45,
            Thickness = 2,
            PixelBrightness = 200,
            RemovePixelBrightness = 30
        };

        Assert.Equal(PixelDrawing.BrushShapeType.Circle, stroke.BrushShape);
        Assert.Equal(7, stroke.BrushSize);
        Assert.Equal(45, stroke.RotationAngle);
        Assert.Equal(2, stroke.Thickness);
        Assert.Equal(200, stroke.PixelBrightness);
        Assert.Equal(30, stroke.RemovePixelBrightness);
    }

    [Fact]
    public void AddStrokeBrightnessIsPixelBrightness()
    {
        var stroke = new PixelStroke
        {
            IsAdd = true,
            PixelBrightness = 210,
            RemovePixelBrightness = 10
        };

        Assert.Equal(210, stroke.Brightness);
    }

    [Fact]
    public void RemoveStrokeBrightnessIsRemovePixelBrightness()
    {
        var stroke = new PixelStroke
        {
            IsAdd = false,
            PixelBrightness = 210,
            RemovePixelBrightness = 10
        };

        Assert.Equal(10, stroke.Brightness);
    }

    [Fact]
    public void StrokeWithSinglePointIsNotEmpty()
    {
        var stroke = new PixelStroke();
        stroke.AddPoint(new Point(5, 5));
        Assert.False(stroke.IsEmpty);
    }

    [Fact]
    public void StrokeWithoutPointsIsEmpty()
    {
        Assert.True(new PixelStroke().IsEmpty);
    }

    [Fact]
    public void RenderAppliesStrokeToEachLayerInRange()
    {
        using var file = CreateFileWithLayers(5);
        var stroke = new PixelStroke(2, [new Point(10, 10), new Point(10, 11)], LineType.AntiAlias,
            PixelDrawing.BrushShapeType.Square, 0, 1, -1, 30, 200, true)
        {
            LayersBelow = 1,
            LayersAbove = 1
        };

        file.DrawModifications([stroke]);

        for (uint layer = 1; layer <= 3; layer++)
        {
            using var mat = file[layer].LayerMat;
            Assert.Equal(200, mat.GetByte(10, 10));
            Assert.Equal(200, mat.GetByte(10, 11));
        }

        using (var layer0 = file[0].LayerMat)
        {
            Assert.Equal(0, layer0.GetByte(10, 10));
        }

        using (var layer4 = file[4].LayerMat)
        {
            Assert.Equal(0, layer4.GetByte(10, 10));
        }
    }

    [Fact]
    public void RenderInterpolatesGapsBetweenDistantPoints()
    {
        using var file = CreateFileWithLayers(1);
        var stroke = new PixelStroke(0, [new Point(10, 10), new Point(16, 10)], LineType.AntiAlias,
            PixelDrawing.BrushShapeType.Square, 0, 1, -1, 30, 200, true);

        file.DrawModifications([stroke]);

        using var mat = file[0].LayerMat;
        for (var x = 10; x <= 16; x++)
        {
            Assert.Equal(200, mat.GetByte(x, 10));
        }
    }

    [Fact]
    public void RenderSinglePixelBrushUsesFastPath()
    {
        using var file = CreateFileWithLayers(1);
        var stroke = new PixelStroke(0, [new Point(25, 25)], LineType.AntiAlias,
            PixelDrawing.BrushShapeType.Square, 0, 1, -1, 30, 200, true);

        file.DrawModifications([stroke]);

        using var mat = file[0].LayerMat;
        Assert.Equal(200, mat.GetByte(25, 25));
        Assert.Equal(0, mat.GetByte(25, 26));
    }

    [Fact]
    public void RenderRemoveStrokeErasesPixels()
    {
        using var file = CreateFileWithLayers(1, setPixel: new Point(10, 10));
        var stroke = new PixelStroke(0, [new Point(10, 10)], LineType.AntiAlias,
            PixelDrawing.BrushShapeType.Square, 0, 1, -1, 30, 200, false);

        file.DrawModifications([stroke]);

        using var mat = file[0].LayerMat;
        Assert.Equal(30, mat.GetByte(10, 10));
    }

    private static FileFormat CreateFileWithLayers(int count, Point? setPixel = null)
    {
        var file = PcbFixtures.CreateSlicerFile();
        var layers = new Layer[count];
        for (var i = 0; i < count; i++)
        {
            using var mat = file.CreateMat();
            if (setPixel is { } p)
            {
                mat.SetByte(p.X, p.Y, 200);
            }

            layers[i] = new Layer((uint)i, mat, file);
        }

        file.Layers = layers;
        return file;
    }
}