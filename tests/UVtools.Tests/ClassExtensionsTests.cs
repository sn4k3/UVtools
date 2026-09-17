/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2020-2026 PTRTECH
 */

using System.Collections.Generic;
using UVtools.Core.Extensions;
using Xunit;

namespace UVtools.Tests;

public class ClassExtensionsTests
{
    /// <summary>
    /// Mimics the shape of UserSettings: a container whose properties are nested types declared inside it
    /// (which CopyValuesFrom must recurse into, preserving identity) plus a plain leaf-typed property and
    /// a collection property (both of which are declared outside the container, so CopyValuesFrom must
    /// overwrite them directly instead of recursing).
    /// </summary>
    private class Container
    {
        public sealed class Nested
        {
            public int Value { get; set; }
        }

        public Nested Sub { get; set; } = new();
        public int Leaf { get; set; }
        public List<int> Items { get; set; } = [];
    }

    [Fact]
    public void CopyValuesFromOverwritesLeafPropertiesInPlace()
    {
        var target = new Container { Leaf = 1 };
        var source = new Container { Leaf = 2 };

        target.CopyValuesFrom(source);

        Assert.Equal(2, target.Leaf);
    }

    [Fact]
    public void CopyValuesFromRecursesIntoNestedPropertiesPreservingTargetIdentity()
    {
        var target = new Container();
        var originalSub = target.Sub;
        originalSub.Value = 1;

        var source = new Container();
        source.Sub.Value = 2;

        target.CopyValuesFrom(source);

        Assert.Same(originalSub, target.Sub);
        Assert.Equal(2, target.Sub.Value);
        Assert.NotSame(source.Sub, target.Sub);
    }

    [Fact]
    public void CopyValuesFromReplacesCollectionPropertiesDeclaredOutsideTheContainer()
    {
        var target = new Container();
        var source = new Container { Items = [1, 2, 3] };

        target.CopyValuesFrom(source);

        Assert.Equal([1, 2, 3], target.Items);
    }
}
