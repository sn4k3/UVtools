using Emgu.CV;
using EmguExtensions;
using NativeCompressions;
using System;
using System.IO;
using System.IO.Compression;

namespace UVtools.Core.Compressors;

public class MatCompressorLz4 : MatCompressor
{
    /// <summary>
    /// Provides a singleton instance of the <see cref="T:MatCompressorLz4" /> class for efficient reuse across the application.
    /// </summary>
    public static readonly MatCompressorLz4 Instance = new();

    /// <inheritdoc />
    public override string Provider => "Cysharp";

    /// <inheritdoc />
    public override string Name => "LZ4";

    /// <inheritdoc />
    public override int MaximumCompressionLevel { get; } = LZ4.MaxCompressionLevel; // 12

    static MatCompressorLz4()
    {
        AvailableCompressors.Add(Instance);
    }

    /// <inheritdoc />
    private MatCompressorLz4()
    {
    }

    /// <inheritdoc />
    protected override int GetCompressionLevel(CompressionLevel compressionLevel)
    {
        return compressionLevel switch
        {
            CompressionLevel.NoCompression => 0,
            CompressionLevel.Fastest => 1,
            CompressionLevel.Optimal => 10,
            CompressionLevel.SmallestSize => 12,
            _ => throw new ArgumentException("Invalid CompressionLevel value.", nameof(compressionLevel))
        };
    }


    /// <inheritdoc />
    protected override byte[] CompressCore(Mat src, int compressionLevel)
    {
        var options = LZ4CompressionOptions.Default with
        {
            CompressionLevel = compressionLevel,
            ContentSize = (ulong)src.ByteCountInt64,
            FavorDecompressionSpeed = 1 // Optimize for decompression
        };

        using var buffer = CreateCompressionBuffer(src);
        using (var compressStream = new LZ4Stream(CreateCompressionStream(buffer), options))
        {
            src.CopyTo(compressStream);
        }

        return buffer.ToArray();
    }

    /// <inheritdoc />
    protected override void DecompressCore(byte[] compressedBytes, Mat dst)
    {
        using var compressedStream = new MemoryStream(compressedBytes, false);
        using var decompressStream = new LZ4Stream(compressedStream, CompressionMode.Decompress, true);
        decompressStream.ReadExactly(dst.GetSpanOfBytes());
    }
}