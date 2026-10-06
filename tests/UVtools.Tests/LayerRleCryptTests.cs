using System;
using UVtools.Core.FileFormats;
using Xunit;

namespace UVtools.Tests;

public class LayerRleCryptTests
{
    // The original byte by byte algorithms, the word based implementation must keep producing the same stream
    private static byte[] ReferenceChitubox(uint seed, uint layerIndex, byte[] input)
    {
        var init = seed * 0x2d83cdac + 0xd8a83423;
        var key = (layerIndex * 0x1e1530cd + 0xec3d47cd) * init;
        return Reference(key, init, input);
    }

    private static byte[] ReferenceFdg(uint seed, uint layerIndex, byte[] input)
    {
        var init = (seed - 0x1dcb76c3) ^ 0x257e2431;
        var key = init * 0x82391efd * (layerIndex ^ 0x110bdacd);
        return Reference(key, init, input);
    }

    private static byte[] ReferencePhz(uint seed, uint layerIndex, byte[] input)
    {
        seed %= 0x4324;
        var init = seed * 0x34a32231;
        var key = (layerIndex ^ 0x3fad2212) * seed * 0x4910913d;
        return Reference(key, init, input);
    }

    private static byte[] Reference(uint key, uint init, byte[] input)
    {
        var result = (byte[])input.Clone();
        var index = 0;
        for (var i = 0; i < result.Length; i++)
        {
            var k = (byte)(key >> (8 * index));
            index++;
            if ((index & 3) == 0)
            {
                key += init;
                index = 0;
            }

            result[i] ^= k;
        }

        return result;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(1021)]
    [InlineData(65536)]
    public void MatchesReferenceImplementation(int length)
    {
        var random = new Random(length);
        var data = new byte[length];
        random.NextBytes(data);

        foreach (var (seed, layerIndex) in new[] { (1u, 0u), (0xdeadbeefu, 17u), (123456789u, 4000u) })
        {
            var chitubox = (byte[])data.Clone();
            ChituboxFile.LayerRleCryptBuffer(seed, layerIndex, chitubox);
            Assert.Equal(ReferenceChitubox(seed, layerIndex, data), chitubox);
            ChituboxFile.LayerRleCryptBuffer(seed, layerIndex, chitubox);
            Assert.Equal(data, chitubox);

            var fdg = (byte[])data.Clone();
            FDGFile.LayerRleCryptBuffer(seed, layerIndex, fdg);
            Assert.Equal(ReferenceFdg(seed, layerIndex, data), fdg);

            var phz = (byte[])data.Clone();
            PHZFile.LayerRleCryptBuffer(seed, layerIndex, phz);
            Assert.Equal(ReferencePhz(seed, layerIndex, data), phz);
        }
    }
}
