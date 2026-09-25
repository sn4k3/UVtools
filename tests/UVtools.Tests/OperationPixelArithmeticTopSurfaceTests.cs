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
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using EmguExtensions;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Operations;
using Xunit;

namespace UVtools.Tests;

public class OperationPixelArithmeticTopSurfaceTests
{
    [Fact]
    public void TopSurfaceChangesExposedPixelsButNotCoveredPixelsOrBackground()
    {
        using var file = CreateFile(
            (new Rectangle(2, 2, 12, 12), 255),
            (new Rectangle(4, 4, 8, 8), 255));
        var operation = CreateOperation(file, OperationPixelArithmetic.PixelArithmeticApplyMethod.ModelTopSurface, 0, 0);

        Assert.True(operation.Execute());

        Assert.Equal(128, Pixel(file[0], 3, 8));
        Assert.Equal(255, Pixel(file[0], 5, 8));
        Assert.Equal(0, Pixel(file[0], 0, 0));
        Assert.Equal(255, Pixel(file[1], 5, 8));
    }

    [Fact]
    public void OccupiedNextLayerCoversPixelRegardlessOfBrightness()
    {
        using var file = CreateFile(
            (new Rectangle(2, 2, 12, 12), 255),
            (new Rectangle(2, 2, 12, 12), 1));
        var operation = CreateOperation(file, OperationPixelArithmetic.PixelArithmeticApplyMethod.ModelTopSurface, 0, 0);

        Assert.True(operation.Execute());

        Assert.Equal(255, Pixel(file[0], 5, 8));
    }

    [Fact]
    public void TopSurfaceInsetExcludesWallMarginAndCoveredPixels()
    {
        using var file = CreateFile(
            (new Rectangle(2, 2, 12, 12), 255),
            (new Rectangle(6, 6, 4, 4), 255));
        var operation = CreateOperation(file, OperationPixelArithmetic.PixelArithmeticApplyMethod.ModelTopSurfaceAndInset, 0, 0);
        operation.WallThickness = 2;

        Assert.True(operation.IsWallSettingVisible);
        Assert.True(operation.Execute());

        Assert.Equal(255, Pixel(file[0], 3, 8));
        Assert.Equal(128, Pixel(file[0], 4, 8));
        Assert.Equal(255, Pixel(file[0], 7, 8));
        Assert.Equal(128, Pixel(file[0], 11, 8));
        Assert.Equal(0, Pixel(file[0], 1, 8));
    }

    [Fact]
    public void TopSurfaceInsetErodesFinalLayer()
    {
        using var file = CreateFile((new Rectangle(2, 2, 12, 12), 255));
        var operation = CreateOperation(file, OperationPixelArithmetic.PixelArithmeticApplyMethod.ModelTopSurfaceAndInset, 0, 0);
        operation.WallThickness = 2;

        Assert.True(operation.Execute());

        Assert.Equal(255, Pixel(file[0], 3, 8));
        Assert.Equal(128, Pixel(file[0], 4, 8));
    }

    [Fact]
    public void ZeroTopSurfaceInsetMatchesTopSurface()
    {
        using var file = CreateFile(
            (new Rectangle(2, 2, 12, 12), 255),
            (new Rectangle(6, 6, 4, 4), 255));
        var operation = CreateOperation(file, OperationPixelArithmetic.PixelArithmeticApplyMethod.ModelTopSurfaceAndInset, 0, 0);
        operation.WallThickness = 0;

        Assert.True(operation.Execute());

        Assert.Equal(128, Pixel(file[0], 2, 8));
        Assert.Equal(255, Pixel(file[0], 7, 8));
    }

    [Fact]
    public void FinalLayerTreatsAllModelPixelsAsTop()
    {
        using var file = CreateFile(
            (new Rectangle(2, 2, 12, 12), 255),
            (new Rectangle(4, 4, 8, 8), 255));
        var operation = CreateOperation(file, OperationPixelArithmetic.PixelArithmeticApplyMethod.ModelTopSurface, 1, 1);

        Assert.True(operation.Execute());

        Assert.Equal(255, Pixel(file[0], 5, 8));
        Assert.Equal(128, Pixel(file[1], 5, 8));
        Assert.Equal(0, Pixel(file[1], 0, 0));
    }

    [Fact]
    public void EditingMultipleLayersUsesOriginalNextLayerOccupancy()
    {
        using var file = CreateFile(
            (new Rectangle(2, 2, 12, 12), 255),
            (new Rectangle(2, 2, 12, 12), 255),
            (new Rectangle(2, 2, 12, 12), 255));
        var operation = CreateOperation(file, OperationPixelArithmetic.PixelArithmeticApplyMethod.ModelTopSurface, 0, 2);
        operation.Value = 0;

        Assert.True(operation.Execute());

        Assert.Equal(255, Pixel(file[0], 5, 8));
        Assert.Equal(255, Pixel(file[1], 5, 8));
        Assert.Equal(0, Pixel(file[2], 5, 8));
    }

    [Theory]
    [InlineData(OperationPixelArithmetic.PixelArithmeticApplyMethod.ModelTopSurface)]
    [InlineData(OperationPixelArithmetic.PixelArithmeticApplyMethod.ModelTopSurfaceAndInset)]
    [InlineData(OperationPixelArithmetic.PixelArithmeticApplyMethod.ModelSurface)]
    [InlineData(OperationPixelArithmetic.PixelArithmeticApplyMethod.ModelSurfaceAndInset)]
    public void EditingAcrossBatchesUsesOriginalNextLayers(
        OperationPixelArithmetic.PixelArithmeticApplyMethod applyMethod)
    {
        var layers = new (Rectangle Bounds, byte Brightness)[10];
        Array.Fill(layers, (new Rectangle(2, 2, 12, 12), (byte)255));
        using var file = CreateFile(layers);
        var operation = CreateOperation(file, applyMethod, 0, 9);
        operation.Value = 0;
        operation.WallThickness = 1;

        Assert.True(operation.Execute());

        for (var layerIndex = 0; layerIndex < 9; layerIndex++)
            Assert.Equal(255, Pixel(file[layerIndex], 8, 8));
        Assert.Equal(0, Pixel(file[9], 8, 8));
    }

    private static OperationPixelArithmetic CreateOperation(
        FileFormat file,
        OperationPixelArithmetic.PixelArithmeticApplyMethod applyMethod,
        uint firstLayer,
        uint lastLayer)
    {
        return new OperationPixelArithmetic(file)
        {
            Operator = OperationPixelArithmetic.PixelArithmeticOperators.Set,
            ApplyMethod = applyMethod,
            Value = 128,
            LayerIndexStart = firstLayer,
            LayerIndexEnd = lastLayer
        };
    }

    private static ChituboxFile CreateFile(params (Rectangle Bounds, byte Brightness)[] layers)
    {
        var file = new ChituboxFile { Resolution = new Size(16, 16), Version = 4 };
        file.HeaderSettings.Magic = ChituboxFile.MAGIC_CTBv4;
        var result = new Layer[layers.Length];
        for (var i = 0; i < layers.Length; i++)
        {
            using var mat = new Mat(file.Resolution, DepthType.Cv8U, 1);
            mat.SetTo(new MCvScalar(0));
            CvInvoke.Rectangle(mat, layers[i].Bounds, new MCvScalar(layers[i].Brightness), -1);
            result[i] = new Layer((uint)i, mat, file);
        }

        file.Init(result);
        return file;
    }

    private static byte Pixel(Layer layer, int x, int y)
    {
        using var mat = layer.LayerMat;
        return mat.GetSpanOfBytes()[y * mat.Cols + x];
    }
}
