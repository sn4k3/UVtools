/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Drawing;
using System.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using Xunit;

namespace UVtools.Tests;

public class LayerTests
{
    private static FileFormat CreateFile(int layerCount)
    {
        var slicerFile = PcbFixtures.CreateSlicerFile();
        var layers = new Layer[layerCount];
        for (var i = 0; i < layers.Length; i++)
        {
            using var mat = new Mat(slicerFile.Resolution, DepthType.Cv8U, 1);
            mat.SetTo(new MCvScalar(0));
            CvInvoke.Rectangle(mat, new Rectangle(100, 100, 50, 50), new MCvScalar(255), -1);
            layers[i] = new Layer((uint)i, mat, slicerFile);
        }

        slicerFile.Init(layers);
        return slicerFile;
    }

    private static MainIssue CreateIsland(Layer layer) => new(MainIssue.IssueType.Island,
        new IssueOfPoints(layer, [new Point(10, 10), new Point(11, 10)], new Rectangle(10, 10, 2, 1)));

    [Fact]
    public void SingleLayerIssueGroupHasTheAreaOfItsLayer()
    {
        using var slicerFile = CreateFile(3);
        var trap = new IssueOfContours(slicerFile[1], [[new Point(0, 0), new Point(9, 0), new Point(9, 9)]],
            new Rectangle(0, 0, 10, 10), 123);

        var single = new MainIssue(MainIssue.IssueType.ResinTrap, [trap]);

        Assert.Equal(123, single.Area);
        Assert.Equal('²', single.AreaChar);
    }

    [Fact]
    public void IslandsWithTheSameFootprintOnOtherLayersAreDifferent()
    {
        using var slicerFile = CreateFile(3);

        Assert.Equal(CreateIsland(slicerFile[1]), CreateIsland(slicerFile[1]));
        Assert.NotEqual(CreateIsland(slicerFile[1]), CreateIsland(slicerFile[2]));
        Assert.Equal(CreateIsland(slicerFile[1]).GetHashCode(), CreateIsland(slicerFile[1]).GetHashCode());
    }

    [Fact]
    public void ClonedLayerHasItsOwnLock()
    {
        using var slicerFile = CreateFile(1);
        var layer = slicerFile[0];

        var clone = layer.Clone();

        // Locked by this thread, the clone must still be free for another one
        var cloneLockAcquired = false;
        layer.Mutex.Enter();
        try
        {
            var thread = new Thread(() =>
            {
                if (!clone.Mutex.TryEnter()) return;
                cloneLockAcquired = true;
                clone.Mutex.Exit();
            });
            thread.Start();
            thread.Join();
        }
        finally
        {
            layer.Mutex.Exit();
        }

        Assert.True(cloneLockAcquired);
        Assert.True(clone == layer);
        Assert.False(clone == null);
        Assert.False(null == clone);
    }
}
