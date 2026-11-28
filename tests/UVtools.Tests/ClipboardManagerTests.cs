using System.Drawing;
using System.Numerics;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using EmguExtensions;
using UVtools.Core;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Managers;
using UVtools.Core.Operations;
using UVtools.Core.Voxel;
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

    [Theory]
    [InlineData(VoxelObjectEdit.Move, 2)]
    [InlineData(VoxelObjectEdit.Duplicate, 10)]
    [InlineData(VoxelObjectEdit.Delete, 0)]
    public void ObjectEditsUndoAndRedoOriginalGrayscalePixels(VoxelObjectEdit edit, int dx)
    {
        using var file = CreateTestFile(2);
        file.DisplayMirror = FlipDirection.None;
        for (var index = 0; index < 2; index++)
        {
            using var image = file[index].LayerMat;
            image.SetByte(20, 20, 255);
            image.SetByte(21, 20, 200);
            image.SetByte(22, 20, 80);
            image.SetByte(40, 20, 150);
            file[index].LayerMat = image;
        }

        using var mesh = VoxelPreviewMeshBuilder.Build(file,
            VoxelPreviewMeshOptions.FromQuality(VoxelPreviewQuality.Detailed));
        var selection = Assert.IsType<VoxelObjectSelection>(VoxelObjectSelection.Find(file, mesh,
            new Vector3(20.5f * mesh.PixelWidth, 20.5f * mesh.PixelHeight, file[1].PositionZ), Vector3.UnitZ));

        Assert.Equal(new Rectangle(20, 20, 3, 1), selection.PixelBounds);

        var history = ClipboardManager.Instance;
        history.Init(file);
        try
        {
            using var operation = new OperationEditVoxelObject(file, selection, edit, new Point(dx, 0));
            history.Snapshot();
            Assert.True(operation.Execute());
            Assert.NotNull(history.Clip(operation));
            Assert.False(selection.IsCurrentFor(file));
            AssertPixels(edited: true);

            history.Undo();
            AssertPixels(edited: false);
            history.Redo();
            AssertPixels(edited: true);
        }
        finally
        {
            history.Reset();
        }

        void AssertPixels(bool edited)
        {
            for (var index = 0; index < 2; index++)
            {
                using var image = file[index].LayerMat;
                var expectedSource = !edited || edit == VoxelObjectEdit.Duplicate;
                var expectedTarget = edited && edit != VoxelObjectEdit.Delete;
                Assert.Equal(expectedSource ? 255 : 0, image.GetByte(20, 20));
                Assert.Equal(expectedSource ? 200 : 0, image.GetByte(21, 20));
                if (expectedTarget)
                {
                    Assert.Equal(255, image.GetByte(20 + dx, 20));
                    Assert.Equal(200, image.GetByte(21 + dx, 20));
                    Assert.Equal(80, image.GetByte(22 + dx, 20));
                }
                if (!edited || edit == VoxelObjectEdit.Duplicate)
                    Assert.Equal(80, image.GetByte(22, 20));
                if (edited && edit == VoxelObjectEdit.Delete)
                    Assert.Equal(0, image.GetByte(22, 20));
                Assert.Equal(150, image.GetByte(40, 20));
            }
        }
    }

    [Fact]
    public void ObjectSelectionUsesThePlatePositionOfEachObject()
    {
        using var file = CreateTestFile(2);
        file.DisplayMirror = FlipDirection.None; // The preview mirrors Y
        var objects = new[] { new Rectangle(10, 10, 4, 3), new Rectangle(30, 40, 5, 6) };
        for (var index = 0; index < 2; index++)
        {
            using var image = file[index].LayerMat;
            foreach (var rectangle in objects)
            {
                using var area = new Mat(image, rectangle);
                area.SetTo(new MCvScalar(255));
            }
            file[index].LayerMat = image;
        }

        using var mesh = VoxelPreviewMeshBuilder.Build(file,
            VoxelPreviewMeshOptions.FromQuality(VoxelPreviewQuality.Detailed));
        var height = (float)file.ResolutionY;
        foreach (var expected in objects)
        {
            // Mirrored across the whole plate, not across the area of both objects.
            var center = new Vector3((expected.X + expected.Width / 2f) * mesh.PixelWidth,
                (height - expected.Y - expected.Height / 2f) * mesh.PixelHeight, file[1].PositionZ);
            var selection = Assert.IsType<VoxelObjectSelection>(
                VoxelObjectSelection.Find(file, mesh, center, Vector3.UnitZ));
            Assert.Equal(expected, selection.PixelBounds);

            var (min, max) = selection.GetPreviewBounds(mesh);
            Assert.Equal(expected.Left * mesh.PixelWidth, min.X, 3);
            Assert.Equal(expected.Right * mesh.PixelWidth, max.X, 3);
            Assert.Equal((height - expected.Bottom) * mesh.PixelHeight, min.Y, 3);
            Assert.Equal((height - expected.Top) * mesh.PixelHeight, max.Y, 3);
        }
    }

    [Fact]
    public void ObjectSelectionToleratesTheRoundingOfTheRayHit()
    {
        using var file = CreateTestFile(3);
        file.DisplayMirror = FlipDirection.None;
        var expected = new Rectangle(30, 40, 5, 6);
        // The top layer stays empty, a hit just above the top face must not land in it.
        for (var index = 0; index < 2; index++)
        {
            using var image = file[index].LayerMat;
            using (var area = new Mat(image, expected)) area.SetTo(new MCvScalar(255));
            file[index].LayerMat = image;
        }

        using var mesh = VoxelPreviewMeshBuilder.Build(file,
            VoxelPreviewMeshOptions.FromQuality(VoxelPreviewQuality.Detailed));
        var height = (float)file.ResolutionY;
        var centerX = (expected.X + expected.Width / 2f) * mesh.PixelWidth;
        var centerY = (height - expected.Y - expected.Height / 2f) * mesh.PixelHeight;

        var top = new Vector3(centerX, centerY, file[1].PositionZ + 0.0005f);
        Assert.Equal(expected, VoxelObjectSelection.Find(file, mesh, top, Vector3.UnitZ)?.PixelBounds);

        var leftWall = new Vector3(expected.Left * mesh.PixelWidth - 0.002f, centerY, file[1].PositionZ - 0.5f * file.LayerHeight);
        Assert.Equal(expected, VoxelObjectSelection.Find(file, mesh, leftWall, -Vector3.UnitX)?.PixelBounds);
    }

    [Theory]
    [InlineData(VoxelObjectEdit.Move)]
    [InlineData(VoxelObjectEdit.Duplicate)]
    public void EditedObjectCanBeSelectedAgainFromItsAnchor(VoxelObjectEdit edit)
    {
        using var file = CreateTestFile(2);
        file.DisplayMirror = FlipDirection.None;
        var source = new Rectangle(10, 10, 4, 3);
        for (var index = 0; index < 2; index++)
        {
            using var image = file[index].LayerMat;
            using (var area = new Mat(image, source)) area.SetTo(new MCvScalar(255));
            file[index].LayerMat = image;
        }

        var options = VoxelPreviewMeshOptions.FromQuality(VoxelPreviewQuality.Detailed);
        VoxelObjectSelection selection;
        using (var mesh = VoxelPreviewMeshBuilder.Build(file, options))
        {
            selection = Assert.IsType<VoxelObjectSelection>(VoxelObjectSelection.FindAtPixel(file, mesh, 1,
                new Point(11, 11)));
        }
        Assert.Equal(source, selection.PixelBounds);

        var offset = new Point(20, 15);
        var (layerIndex, anchor) = selection.Anchor;
        anchor.Offset(offset);
        using (var operation = new OperationEditVoxelObject(file, selection, edit, offset))
        {
            Assert.True(operation.Execute());
        }

        using var editedMesh = VoxelPreviewMeshBuilder.Build(file, options);
        var edited = Assert.IsType<VoxelObjectSelection>(
            VoxelObjectSelection.FindAtPixel(file, editedMesh, layerIndex, anchor));
        var expected = source;
        expected.Offset(offset);
        Assert.Equal(expected, edited.PixelBounds);
    }

    [Fact]
    public void DuplicateRejectsCollisionAcrossExposuresAtTheSameHeight()
    {
        using var file = CreateTestFile(2);
        file.DisplayMirror = FlipDirection.None;
        file[1].PositionZ = file[0].PositionZ;
        using (var image = file[0].LayerMat)
        {
            image.SetByte(20, 20, 255);
            file[0].LayerMat = image;
        }
        using (var image = file[1].LayerMat)
        {
            image.SetByte(30, 20, 255);
            file[1].LayerMat = image;
        }

        using var mesh = VoxelPreviewMeshBuilder.Build(file,
            VoxelPreviewMeshOptions.FromQuality(VoxelPreviewQuality.Detailed));
        var selection = Assert.IsType<VoxelObjectSelection>(VoxelObjectSelection.Find(file, mesh,
            new Vector3(20.5f * mesh.PixelWidth, 20.5f * mesh.PixelHeight, file[0].PositionZ), Vector3.UnitZ));
        using var operation = new OperationEditVoxelObject(file, selection, VoxelObjectEdit.Duplicate, new Point(10, 0));

        Assert.False(operation.Execute());
        Assert.True(selection.IsCurrentFor(file));
        using var source = file[0].LayerMat;
        using var destination = file[1].LayerMat;
        Assert.Equal(255, source.GetByte(20, 20));
        Assert.Equal(0, source.GetByte(30, 20));
        Assert.Equal(255, destination.GetByte(30, 20));
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

    [Fact]
    public void GetLayerAtClip_ReturnsTheLayerAsItWasWithoutRestoring()
    {
        var file = CreateTestFile(5);
        var cm = ClipboardManager.Instance;
        cm.Init(file);

        cm.Snapshot();
        using (var mat1 = file[1].LayerMat)
        {
            CvInvoke.Rectangle(mat1, new Rectangle(10, 10, 50, 50), new MCvScalar(255), -1);
            file[1].LayerMat = mat1;
        }
        cm.Clip("Edit 1");

        cm.Snapshot();
        using (var mat2 = file[2].LayerMat)
        {
            CvInvoke.Rectangle(mat2, new Rectangle(20, 20, 50, 50), new MCvScalar(255), -1);
            file[2].LayerMat = mat2;
        }
        cm.Clip("Edit 2");

        // Edit 2 (index 0) is the current state, Edit 1 (index 1) is before it and Original (index 2) is the oldest
        Assert.Equal(0, cm.CurrentIndex);
        Assert.True(cm.GetLayerAtClip(0, 2)!.NonZeroPixelCount > 0);
        Assert.Equal(0u, cm.GetLayerAtClip(1, 2)!.NonZeroPixelCount);
        Assert.True(cm.GetLayerAtClip(1, 1)!.NonZeroPixelCount > 0);
        Assert.Equal(0u, cm.GetLayerAtClip(2, 1)!.NonZeroPixelCount);
        Assert.Null(cm.GetLayerAtClip(3, 1));
        Assert.Null(cm.GetLayerAtClip(0, 99));

        // Nothing was restored
        Assert.Equal(0, cm.CurrentIndex);
        Assert.True(file[2].NonZeroPixelCount > 0);
    }
}
