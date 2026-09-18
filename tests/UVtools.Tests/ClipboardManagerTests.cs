using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Managers;
using Xunit;

namespace UVtools.Tests;

public class ClipboardManagerTests
{
    private static FileFormat CreateTestFile(int count = 5)
    {
        var slicerFile = PcbFixtures.CreateSlicerFile();
        var layers = new Layer[count];
        for (var i = 0; i < layers.Length; i++)
        {
            using var mat = new Mat(slicerFile.Resolution, DepthType.Cv8U, 1);
            mat.SetTo(new MCvScalar(0));
            layers[i] = new Layer((uint)i, mat, slicerFile);
        }

        slicerFile.Init(layers);
        return slicerFile;
    }

    [Fact]
    public void UndoRedo_WithMultipleDifferentialEdits_CorrectlyRestoresLayers()
    {
        var file = CreateTestFile(5);
        var cm = ClipboardManager.Instance;
        cm.Init(file);

        // Edit 1: Draw on layer 1
        cm.Snapshot();
        using (var mat1 = file[1].LayerMat)
        {
            CvInvoke.Rectangle(mat1, new Rectangle(10, 10, 50, 50), new MCvScalar(255), -1);
            file[1].LayerMat = mat1;
        }
        cm.Clip("Edit 1");

        // Edit 2: Draw on layer 2
        cm.Snapshot();
        using (var mat2 = file[2].LayerMat)
        {
            CvInvoke.Rectangle(mat2, new Rectangle(20, 20, 50, 50), new MCvScalar(255), -1);
            file[2].LayerMat = mat2;
        }
        cm.Clip("Edit 2");

        // Edit 3: Draw on layer 3
        cm.Snapshot();
        using (var mat3 = file[3].LayerMat)
        {
            CvInvoke.Rectangle(mat3, new Rectangle(30, 30, 50, 50), new MCvScalar(255), -1);
            file[3].LayerMat = mat3;
        }
        cm.Clip("Edit 3");

        Assert.True(file[1].NonZeroPixelCount > 0);
        Assert.True(file[2].NonZeroPixelCount > 0);
        Assert.True(file[3].NonZeroPixelCount > 0);

        // Undo 1: Reverts Edit 3 -> Layer 3 should now be empty, Layers 1 and 2 still have pixels
        cm.Undo();
        Assert.Equal(1, cm.CurrentIndex);
        Assert.True(file[1].NonZeroPixelCount > 0);
        Assert.True(file[2].NonZeroPixelCount > 0);
        Assert.Equal(0u, file[3].NonZeroPixelCount);

        // Undo 2: Reverts Edit 2 -> Layer 2 should now be empty, Layer 1 still has pixels
        cm.Undo();
        Assert.Equal(2, cm.CurrentIndex);
        Assert.True(file[1].NonZeroPixelCount > 0);
        Assert.Equal(0u, file[2].NonZeroPixelCount);
        Assert.Equal(0u, file[3].NonZeroPixelCount);

        // Undo 3: Reverts Edit 1 -> All layers empty (back to original)
        cm.Undo();
        Assert.Equal(3, cm.CurrentIndex);
        Assert.Equal(0u, file[1].NonZeroPixelCount);
        Assert.Equal(0u, file[2].NonZeroPixelCount);
        Assert.Equal(0u, file[3].NonZeroPixelCount);

        // Redo 1: Re-applies Edit 1
        cm.Redo();
        Assert.Equal(2, cm.CurrentIndex);
        Assert.True(file[1].NonZeroPixelCount > 0);
        Assert.Equal(0u, file[2].NonZeroPixelCount);
        Assert.Equal(0u, file[3].NonZeroPixelCount);

        // Redo 2: Re-applies Edit 2
        cm.Redo();
        Assert.Equal(1, cm.CurrentIndex);
        Assert.True(file[1].NonZeroPixelCount > 0);
        Assert.True(file[2].NonZeroPixelCount > 0);
        Assert.Equal(0u, file[3].NonZeroPixelCount);

        // Redo 3: Re-applies Edit 3
        cm.Redo();
        Assert.Equal(0, cm.CurrentIndex);
        Assert.True(file[1].NonZeroPixelCount > 0);
        Assert.True(file[2].NonZeroPixelCount > 0);
        Assert.True(file[3].NonZeroPixelCount > 0);
    }

    [Fact]
    public void JumpIndex_CorrectlyRestoresTargetState()
    {
        var file = CreateTestFile(5);
        var cm = ClipboardManager.Instance;
        cm.Init(file);

        // Edit 1: Draw on layer 1
        cm.Snapshot();
        using (var mat1 = file[1].LayerMat)
        {
            CvInvoke.Rectangle(mat1, new Rectangle(10, 10, 50, 50), new MCvScalar(255), -1);
            file[1].LayerMat = mat1;
        }
        cm.Clip("Edit 1");

        // Edit 2: Draw on layer 2
        cm.Snapshot();
        using (var mat2 = file[2].LayerMat)
        {
            CvInvoke.Rectangle(mat2, new Rectangle(20, 20, 50, 50), new MCvScalar(255), -1);
            file[2].LayerMat = mat2;
        }
        cm.Clip("Edit 2");

        // Jump directly to Original (index 2)
        cm.CurrentIndex = 2;
        Assert.Equal(0u, file[1].NonZeroPixelCount);
        Assert.Equal(0u, file[2].NonZeroPixelCount);

        // Jump directly to Edit 2 (index 0)
        cm.CurrentIndex = 0;
        Assert.True(file[1].NonZeroPixelCount > 0);
        Assert.True(file[2].NonZeroPixelCount > 0);

        // Jump directly to Edit 1 (index 1)
        cm.CurrentIndex = 1;
        Assert.True(file[1].NonZeroPixelCount > 0);
        Assert.Equal(0u, file[2].NonZeroPixelCount);
    }
}
