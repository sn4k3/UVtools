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
using System.Linq;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using EmguExtensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Objects;
using UVtools.Core.Operations;
using Xunit;

namespace UVtools.Tests;

public class OperationsImageTests
{
    private static FileFormat CreateFile(params Action<Mat>[] drawLayers)
    {
        var slicerFile = PcbFixtures.CreateSlicerFile();
        var layers = new Layer[drawLayers.Length];
        for (var i = 0; i < layers.Length; i++)
        {
            using var mat = new Mat(slicerFile.Resolution, DepthType.Cv8U, 1);
            mat.SetTo(new MCvScalar(0));
            drawLayers[i](mat);
            layers[i] = new Layer((uint)i, mat, slicerFile);
        }

        slicerFile.Init(layers);
        return slicerFile;
    }

    /// <summary>
    /// A layer with solid and grey shapes, a hole, noise and content touching the borders, all away from the middle.
    /// </summary>
    private static void DrawShapes(Mat mat)
    {
        CvInvoke.Rectangle(mat, new Rectangle(0, 0, 60, 40), new MCvScalar(255), -1);
        CvInvoke.Rectangle(mat, new Rectangle(300, 200, 250, 180), new MCvScalar(255), -1);
        CvInvoke.Rectangle(mat, new Rectangle(350, 250, 40, 30), new MCvScalar(0), -1);
        CvInvoke.Circle(mat, new Point(700, 300), 70, new MCvScalar(190), -1, LineType.AntiAlias);
        CvInvoke.Circle(mat, new Point(990, 995), 30, new MCvScalar(255), -1);
        var random = new Random(11);
        for (var i = 0; i < 200; i++)
        {
            mat.SetByte(random.Next(280, 600), random.Next(180, 420), (byte)random.Next(1, 256));
        }
    }

    private static Mat GetShapes()
    {
        var mat = new Mat(PcbFixtures.CreateSlicerFile().Resolution, DepthType.Cv8U, 1);
        mat.SetTo(new MCvScalar(0));
        DrawShapes(mat);
        return mat;
    }

    private static void AssertSame(Mat expected, Mat actual)
    {
        using var difference = new Mat();
        CvInvoke.AbsDiff(expected, actual, difference);
        Assert.Equal(0, CvInvoke.CountNonZero(difference));
    }

    [Theory]
    [InlineData(OperationBlur.BlurAlgorithm.BoxBlur, 3)]
    [InlineData(OperationBlur.BlurAlgorithm.BoxBlur, 21)]
    [InlineData(OperationBlur.BlurAlgorithm.MedianBlur, 3)]
    [InlineData(OperationBlur.BlurAlgorithm.MedianBlur, 9)]
    [InlineData(OperationBlur.BlurAlgorithm.GaussianBlur, 9)]
    [InlineData(OperationBlur.BlurAlgorithm.Pyramid, 1)]
    public void BlurOverTheContentAreaMatchesTheWholeLayer(OperationBlur.BlurAlgorithm algorithm, uint size)
    {
        using var slicerFile = CreateFile(DrawShapes, _ => { });
        using var expected = GetShapes();
        var kernelSize = new Size((int)size, (int)size);
        switch (algorithm)
        {
            case OperationBlur.BlurAlgorithm.BoxBlur:
                CvInvoke.Blur(expected, expected, kernelSize, new Point(-1, -1));
                break;
            case OperationBlur.BlurAlgorithm.MedianBlur:
                CvInvoke.MedianBlur(expected, expected, (int)size);
                break;
            case OperationBlur.BlurAlgorithm.GaussianBlur:
                CvInvoke.GaussianBlur(expected, expected, kernelSize, 0);
                break;
            case OperationBlur.BlurAlgorithm.Pyramid:
                CvInvoke.PyrDown(expected, expected);
                CvInvoke.PyrUp(expected, expected);
                break;
        }

        Assert.True(new OperationBlur(slicerFile) { BlurOperation = algorithm, Size = size }.Execute());

        using var actual = slicerFile[0].LayerMat;
        AssertSame(expected, actual);
        Assert.True(slicerFile[1].IsEmpty);
    }

    [Fact]
    public void StackBlurGivesTheSameResultEveryTime()
    {
        // The OpenCV stack blur is not thread safe, the layers were being blurred at the same time
        var files = Enumerable.Range(0, 4)
            .Select(_ => CreateFile(Enumerable.Repeat<Action<Mat>>(DrawShapes, 12).ToArray()))
            .ToArray();
        try
        {
            foreach (var file in files)
            {
                Assert.True(new OperationBlur(file) { BlurOperation = OperationBlur.BlurAlgorithm.StackBlur, Size = 9 }
                    .Execute());
            }

            using var reference = files[0][0].LayerMat;
            foreach (var file in files)
            {
                foreach (var layer in file)
                {
                    using var mat = layer.LayerMat;
                    AssertSame(reference, mat);
                }
            }
        }
        finally
        {
            foreach (var file in files) file.Dispose();
        }
    }

    [Theory]
    [InlineData(OperationMorph.MorphOperations.Erode, 1)]
    [InlineData(OperationMorph.MorphOperations.Erode, 7)]
    [InlineData(OperationMorph.MorphOperations.Dilate, 3)]
    [InlineData(OperationMorph.MorphOperations.Open, 2)]
    [InlineData(OperationMorph.MorphOperations.Close, 4)]
    [InlineData(OperationMorph.MorphOperations.Gradient, 2)]
    [InlineData(OperationMorph.MorphOperations.WhiteTopHat, 3)]
    [InlineData(OperationMorph.MorphOperations.BlackTopHat, 3)]
    public void MorphOverTheContentAreaMatchesTheWholeLayer(OperationMorph.MorphOperations morph, uint iterations)
    {
        using var slicerFile = CreateFile(DrawShapes, _ => { });
        using var expected = GetShapes();
        var operation = new OperationMorph(slicerFile) { MorphOperation = morph, Iterations = iterations };
        var kernel = operation.Kernel.GetKernel();
        CvInvoke.MorphologyEx(expected, expected, operation.MorphOperationOpenCV, kernel, operation.Kernel.Anchor,
            (int)iterations, BorderType.Reflect101, default);

        Assert.True(operation.Execute());

        using var actual = slicerFile[0].LayerMat;
        AssertSame(expected, actual);
        Assert.True(slicerFile[1].IsEmpty);
    }

    [Fact]
    public void SolidifyFillsTheHolesAndSkipsTheLayersWithoutThem()
    {
        using var slicerFile = CreateFile(
            DrawShapes,
            mat => CvInvoke.Rectangle(mat, new Rectangle(100, 100, 200, 200), new MCvScalar(255), -1),
            _ => { });
        foreach (var layer in slicerFile) layer.IsModified = false;

        Assert.True(new OperationSolidify(slicerFile).Execute());

        using (var mat = slicerFile[0].LayerMat)
        {
            Assert.Equal(255, mat.GetByte(370, 260)); // The hole was filled
        }

        Assert.True(slicerFile[0].IsModified);
        Assert.False(slicerFile[1].IsModified); // Nothing to fill
        Assert.False(slicerFile[2].IsModified);
    }

    [Fact]
    public void ApplyMaskAcceptsFullSizeLayersWithARoiAndAMask()
    {
        using var slicerFile = CreateFile(mat => CvInvoke.Rectangle(mat, new Rectangle(100, 100, 600, 600), new MCvScalar(255), -1));
        var operation = new OperationThreshold(slicerFile)
        {
            ROI = new Rectangle(200, 200, 400, 400),
            MaskPoints = [[new Point(250, 250), new Point(550, 250), new Point(550, 550), new Point(250, 550)]]
        };

        using var original = slicerFile[0].LayerMat;
        using var result = original.Clone();
        result.SetTo(new MCvScalar(0));

        operation.ApplyMask(original, result); // Both full, the mask is built by the operation and cropped by the ROI

        Assert.Equal(0, result.GetByte(300, 300)); // Inside of the mask: keeps the result
        Assert.Equal(255, result.GetByte(210, 210)); // Inside of the ROI but outside of the mask: the original is restored
        Assert.Equal(0, result.GetByte(120, 120)); // Outside of the ROI: not touched
    }

    [Fact]
    public void LayerArithmeticWorksWithARoiAndAMask()
    {
        using var slicerFile = CreateFile(
            mat => CvInvoke.Rectangle(mat, new Rectangle(100, 100, 500, 500), new MCvScalar(100), -1),
            mat => CvInvoke.Rectangle(mat, new Rectangle(100, 100, 500, 500), new MCvScalar(50), -1),
            _ => { });
        var operation = new OperationLayerArithmetic(slicerFile)
        {
            Sentence = "2 = 0+1",
            ROI = new Rectangle(150, 150, 300, 300),
            MaskPoints = [[new Point(200, 200), new Point(400, 200), new Point(400, 400), new Point(200, 400)]]
        };

        Assert.True(operation.Execute());

        using var mat = slicerFile[2].LayerMat;
        Assert.Equal(150, mat.GetByte(300, 300)); // Inside of the mask
        Assert.Equal(0, mat.GetByte(170, 170)); // Inside of the ROI, outside of the mask
        Assert.Equal(0, mat.GetByte(500, 500)); // Outside of the ROI
    }

    [Fact]
    public void InfillWithAMaskOnlyChangesInsideOfTheMask()
    {
        // The mask is as big as the layer while the infill works over the model area
        using var slicerFile = CreateFile(
            mat => CvInvoke.Rectangle(mat, new Rectangle(100, 100, 700, 700), new MCvScalar(255), -1),
            mat => CvInvoke.Rectangle(mat, new Rectangle(100, 100, 700, 700), new MCvScalar(255), -1));
        var operation = new OperationInfill(slicerFile)
        {
            InfillType = OperationInfill.InfillAlgorithm.Honeycomb,
            FloorCeilThickness = 0,
            WallThickness = 10,
            InfillThickness = 20,
            InfillSpacing = 80,
            InfillBrightness = 0,
            MaskPoints = [[new Point(300, 300), new Point(600, 300), new Point(600, 600), new Point(300, 600)]]
        };

        Assert.True(operation.Execute());

        using var mat = slicerFile[0].LayerMat;
        Assert.Equal(255, mat.GetByte(150, 150)); // Outside of the mask: kept solid
        Assert.Equal(255, mat.GetByte(750, 750));
        using var maskArea = new Mat(mat, new Rectangle(300, 300, 300, 300));
        Assert.True(CvInvoke.CountNonZero(maskArea) < maskArea.Width * maskArea.Height); // Infilled inside
    }

    [Fact]
    public void RemovingManyLayersKeepsTheOthersAndTheirHeights()
    {
        var layers = Enumerable.Range(0, 12)
            .Select<int, Action<Mat>>(i => mat => CvInvoke.Rectangle(mat, new Rectangle(10 * i, 10, 5, 5), new MCvScalar(255), -1))
            .ToArray();
        using var slicerFile = CreateFile(layers);

        Assert.True(new OperationLayerRemove(slicerFile) { LayerIndexStart = 3, LayerIndexEnd = 8 }.Execute());

        Assert.Equal(6u, slicerFile.LayerCount);
        var expectedOrder = new[] { 0, 1, 2, 9, 10, 11 };
        for (var i = 0; i < expectedOrder.Length; i++)
        {
            using var mat = slicerFile[i].LayerMat;
            Assert.Equal(255, mat.GetByte(10 * expectedOrder[i] + 1, 11));
            Assert.Equal((i + 1) * 0.05f, slicerFile[i].PositionZ, 3);
        }
    }

    [Fact]
    public void StripedAccumulatorAddsLikeASequentialAdd()
    {
        var random = new Random(3);
        using var target = new Mat(new Size(300, 201), DepthType.Cv32S, 1);
        target.SetTo(new MCvScalar(5));
        using var expected = target.Clone();
        using var mask = new Mat(target.Size, DepthType.Cv8U, 1);
        mask.SetTo(new MCvScalar(0));
        CvInvoke.Rectangle(mask, new Rectangle(20, 30, 200, 120), new MCvScalar(255), -1);

        var sources = Enumerable.Range(0, 40).Select(_ =>
        {
            using var bytes = new Mat(target.Size, DepthType.Cv8U, 1);
            var data = new byte[bytes.Width * bytes.Height];
            random.NextBytes(data);
            System.Runtime.InteropServices.Marshal.Copy(data, 0, bytes.DataPointer, data.Length);
            var converted = new Mat();
            bytes.ConvertTo(converted, DepthType.Cv32S);
            return converted;
        }).ToArray();

        foreach (var source in sources)
        {
            CvInvoke.Add(expected, source, expected, mask);
        }

        var accumulator = new StripedAccumulator(target, mask, 7);
        Parallel.ForEach(sources, source => accumulator.Add(source));

        AssertSame(expected, target);
        foreach (var source in sources) source.Dispose();
    }

    [Fact]
    public void LayerRoiMatMatchesTheFullLayerArea()
    {
        using var slicerFile = CreateFile(
            mat => CvInvoke.Rectangle(mat, new Rectangle(120, 80, 300, 200), new MCvScalar(255), -1),
            _ => { });
        var layer = slicerFile[0];
        var bounds = layer.BoundingRectangle;

        foreach (var roi in new[]
                 {
                     bounds, // Exactly the stored area
                     new Rectangle(bounds.X - 10, bounds.Y - 10, bounds.Width + 30, bounds.Height + 30), // Bigger
                     new Rectangle(bounds.X + 20, bounds.Y + 15, 100, 60), // Inside
                     new Rectangle(bounds.X - 40, bounds.Y + 50, 100, 100), // Overlapping a corner
                     new Rectangle(800, 800, 100, 100), // Away from the content
                     new Rectangle(0, 0, 1000, 1000) // The whole layer
                 })
        {
            using var full = layer.LayerMat;
            using var expected = full.Roi(roi);
            using var actual = layer.GetRoiMat(roi);
            Assert.Equal(roi.Size, actual.Size);
            AssertSame(expected, actual);
        }

        // An empty layer
        using var emptyRoi = slicerFile[1].GetRoiMat(new Rectangle(10, 10, 50, 50));
        Assert.Equal(new Size(50, 50), emptyRoi.Size);
        Assert.Equal(0, CvInvoke.CountNonZero(emptyRoi));
    }

    [Fact]
    public void LayerImportMergeSkipsEmptyImagesAndCompletesTheProgress()
    {
        var scratch = Path.Combine(Path.GetTempPath(), $"layer-import-{Guid.NewGuid()}");
        Directory.CreateDirectory(scratch);
        try
        {
            using var slicerFile = CreateFile(
                mat => CvInvoke.Rectangle(mat, new Rectangle(100, 100, 50, 50), new MCvScalar(255), -1),
                mat => CvInvoke.Rectangle(mat, new Rectangle(100, 100, 50, 50), new MCvScalar(255), -1),
                mat => CvInvoke.Rectangle(mat, new Rectangle(100, 100, 50, 50), new MCvScalar(255), -1));

            var imageRectangle = new Rectangle(300, 300, 40, 40);
            var imageNames = new[] { "1.png", "2.png", "3.png" };
            for (var i = 0; i < imageNames.Length; i++)
            {
                using var image = new Mat(slicerFile.Resolution, DepthType.Cv8U, 1);
                image.SetTo(new MCvScalar(0));
                if (i == 1) CvInvoke.Rectangle(image, imageRectangle, new MCvScalar(200), -1);
                image.Save(Path.Combine(scratch, imageNames[i]));
            }

            var operation = new OperationLayerImport(slicerFile)
            {
                ImportType = OperationLayerImport.ImportTypes.MergeSum,
                StartLayerIndex = 1,
                ExtendBeyondLayerCount = false
            };
            foreach (var name in imageNames) operation.AddFile(Path.Combine(scratch, name));

            var progress = new OperationProgress();
            Assert.True(operation.Execute(progress));

            // The images go to layers 1, 2 and 3, the last one does not exist
            Assert.Equal(3u, slicerFile.LayerCount);
            Assert.Equal(3u, progress.ItemCount);
            Assert.Equal(3u, progress.ProcessedItems);

            using var layer1 = slicerFile[1].LayerMat; // Blank image, same as before
            Assert.Equal(255, layer1.GetByte(110, 110));
            Assert.Equal(0, layer1.GetByte(310, 310));

            using var layer2 = slicerFile[2].LayerMat; // Image with the rectangle
            Assert.Equal(255, layer2.GetByte(110, 110));
            Assert.Equal(200, layer2.GetByte(310, 310));
        }
        finally
        {
            Directory.Delete(scratch, true);
        }
    }
}