/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Drawing;
using System.IO;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Operations;
using Xunit;

namespace UVtools.Tests;

public class OperationDynamicLiftsTests
{
    [Fact]
    public void InitializesRetractionFromFileSettings()
    {
        using var slicerFile = CreateFile(4);
        slicerFile.BottomRetractSpeed = 180;
        slicerFile.BottomRetractHeight2 = 2;
        slicerFile.BottomRetractSpeed2 = 60;
        slicerFile.RetractSpeed = 150;
        slicerFile.RetractHeight2 = 2;
        slicerFile.RetractSpeed2 = 50;

        var operation = new OperationDynamicLifts(slicerFile);

        Assert.Equal(180, operation.BottomRetractSpeed);
        Assert.Equal(20, operation.BottomRetractHeight2Percentage);
        Assert.Equal(60, operation.BottomRetractSpeed2);
        Assert.Equal(150, operation.RetractSpeed);
        Assert.Equal(25, operation.RetractHeight2Percentage);
        Assert.Equal(50, operation.RetractSpeed2);
    }

    [Fact]
    public void ExplicitZeroPercentageIsPreservedWhenFileIsAssigned()
    {
        using var slicerFile = CreateFile(4);
        slicerFile.BottomRetractHeight2 = 2;
        slicerFile.RetractHeight2 = 2;
        var operation = new OperationDynamicLifts
        {
            BottomRetractHeight2Percentage = 0,
            RetractHeight2Percentage = 0,
            SlicerFile = slicerFile
        };

        Assert.Equal(0, operation.BottomRetractHeight2Percentage);
        Assert.Equal(0, operation.RetractHeight2Percentage);
    }

    [Fact]
    public void RetractionSettingsRoundTripThroughXmlProfile()
    {
        var expected = new OperationDynamicLifts
        {
            BottomRetractSpeed = 180,
            BottomRetractHeight2Percentage = 0,
            BottomRetractSpeed2 = 60,
            RetractSpeed = 150,
            RetractHeight2Percentage = 25,
            RetractSpeed2 = 50
        };
        var serializer = new XmlSerializer(typeof(OperationDynamicLifts));
        using var writer = new StringWriter();
        serializer.Serialize(writer, expected);
        using var reader = new StringReader(writer.ToString());

        var actual = Assert.IsType<OperationDynamicLifts>(serializer.Deserialize(reader));

        Assert.Equal(expected.BottomRetractSpeed, actual.BottomRetractSpeed);
        Assert.Equal(expected.BottomRetractHeight2Percentage, actual.BottomRetractHeight2Percentage);
        Assert.Equal(expected.BottomRetractSpeed2, actual.BottomRetractSpeed2);
        Assert.Equal(expected.RetractSpeed, actual.RetractSpeed);
        Assert.Equal(expected.RetractHeight2Percentage, actual.RetractHeight2Percentage);
        Assert.Equal(expected.RetractSpeed2, actual.RetractSpeed2);
    }

    [Fact]
    public void AppliesIndependentBottomAndNormalRetractionSettings()
    {
        using var slicerFile = CreateFile(4);
        var operation = CreateOperation(slicerFile);
        operation.BottomRetractSpeed = 180;
        operation.BottomRetractHeight2Percentage = 20;
        operation.BottomRetractSpeed2 = 60;
        operation.RetractSpeed = 150;
        operation.RetractHeight2Percentage = 25;
        operation.RetractSpeed2 = 50;

        Assert.True(operation.Execute());

        Assert.Collection(slicerFile,
            layer => AssertRetraction(layer, 10, 180, 2, 60),
            layer => AssertRetraction(layer, 10, 180, 2, 60),
            layer => AssertRetraction(layer, 8, 150, 2, 50),
            layer => AssertRetraction(layer, 8, 150, 2, 50));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(25, 2)]
    [InlineData(100, 8)]
    public void AppliesSecondRetractionPercentage(float percentage, float expectedHeight)
    {
        using var slicerFile = CreateFile(4);
        var operation = CreateOperation(slicerFile);
        operation.RetractHeight2Percentage = percentage;
        operation.RetractSpeed2 = 50;

        Assert.True(operation.Execute());

        Assert.Equal(expectedHeight, slicerFile[2].RetractHeight2);
        Assert.Equal(expectedHeight, slicerFile[3].RetractHeight2);
    }

    [Fact]
    public void AppliesFirstRetractionSpeedWithoutTsmcSupport()
    {
        using var slicerFile = CreateFile(3);
        var operation = CreateOperation(slicerFile);
        operation.RetractSpeed = 150;
        operation.RetractHeight2Percentage = 25;
        operation.RetractSpeed2 = 50;

        Assert.True(slicerFile.CanUseLayerRetractSpeed);
        Assert.False(slicerFile.CanUseLayerRetractHeight2);
        Assert.True(operation.Execute());

        Assert.Equal(150, slicerFile[2].RetractSpeed);
        Assert.Equal(0, slicerFile[2].RetractHeight2);
    }

    private static OperationDynamicLifts CreateOperation(FileFormat slicerFile)
    {
        return new OperationDynamicLifts(slicerFile)
        {
            LayerIndexStart = 0,
            LayerIndexEnd = slicerFile.LastLayerIndex,
            SmallestBottomLiftHeight = 10,
            LargestBottomLiftHeight = 10,
            SlowestBottomLiftSpeed = 40,
            FastestBottomLiftSpeed = 80,
            SmallestLiftHeight = 8,
            LargestLiftHeight = 8,
            SlowestLiftSpeed = 50,
            FastestLiftSpeed = 100
        };
    }

    private static ChituboxFile CreateFile(uint version)
    {
        var slicerFile = new ChituboxFile
        {
            Resolution = new Size(32, 32),
            Version = version,
            BottomLayerCount = 2,
            BottomLiftHeight = 10,
            LiftHeight = 8,
            BottomLiftSpeed = 40,
            LiftSpeed = 50
        };
        slicerFile.HeaderSettings.Magic = ChituboxFile.MAGIC_CTBv4;

        var layers = new Layer[4];
        for (var i = 0; i < layers.Length; i++)
        {
            using var mat = new Mat(slicerFile.Resolution, DepthType.Cv8U, 1);
            mat.SetTo(new MCvScalar(0));
            var side = 4 + i * 2;
            CvInvoke.Rectangle(mat, new Rectangle(0, 0, side, side), new MCvScalar(255), -1);
            layers[i] = new Layer((uint)i, mat, slicerFile);
        }

        slicerFile.Init(layers);
        return slicerFile;
    }

    private static void AssertRetraction(Layer layer, float height, float speed, float height2, float speed2)
    {
        Assert.Equal(height, layer.LiftHeightTotal);
        Assert.Equal(speed, layer.RetractSpeed);
        Assert.Equal(height2, layer.RetractHeight2);
        Assert.Equal(speed2, layer.RetractSpeed2);
    }
}
