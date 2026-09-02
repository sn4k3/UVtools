using System;
using System.Drawing;
using System.IO;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Operations;
using Xunit;

namespace UVtools.Tests;

public class SL1RoundTripTests
{
    [Fact]
    public void LayerHeightSurvivesEncodeAndDecode()
    {
        // config.ini and prusaslicer.ini both carry the layer height under different keys, and the decoder
        // writes either onto every settings object that has the member. A file written from scratch used
        // to have 0 in prusaslicer.ini, which overwrote the real value from config.ini on decode, so every
        // layer sat at Z = 0 and island and overhang detection silently skipped the whole file.
        var path = Path.Combine(Path.GetTempPath(), $"uvtools-roundtrip-{Guid.NewGuid():N}.sl1");
        try
        {
            using (var slicerFile = FileFormat.FindByExtensionOrFilePath("sl1", true)!)
            {
                slicerFile.Resolution = new Size(200, 300);
                slicerFile.DisplayWidth = 20;
                slicerFile.DisplayHeight = 30;
                slicerFile.LayerHeight = 0.05f;
                slicerFile.BottomLayerCount = 1;

                var layers = new Layer[3];
                for (var i = 0; i < layers.Length; i++)
                {
                    using var mat = new Mat(slicerFile.Resolution, DepthType.Cv8U, 1);
                    mat.SetTo(new MCvScalar(0));
                    CvInvoke.Rectangle(mat, new Rectangle(50, 50, 100, 100), new MCvScalar(255), -1);
                    layers[i] = new Layer((uint)i, mat, slicerFile);
                }

                slicerFile.Init(layers);
                slicerFile.SetThumbnails(slicerFile[0].LayerMat);
                slicerFile.Encode(path, new OperationProgress());
            }

            using var reopened = FileFormat.Open(path, FileFormat.FileDecodeType.Full, new OperationProgress());
            Assert.NotNull(reopened);
            Assert.Equal(0.05f, reopened.LayerHeight, 3);
            Assert.Equal(3u, reopened.LayerCount);
            Assert.Equal(0.15f, reopened.PrintHeight, 3);
            Assert.Equal(0.15f, reopened[2].PositionZ, 3);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
