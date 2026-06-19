using Emgu.CV;
using EmguExtensions;
using NativeCompressions;
using System.IO;
using System.IO.Compression;

namespace UVtools.Core.Compressors;

public class MatCompressorZstd : MatCompressor
{
    /// <summary>
    /// Provides a singleton instance of the <see cref="T:MatCompressorZstd" /> class for efficient reuse across the application.
    /// </summary>
    public static readonly MatCompressorZstd Instance = new();

    /// <inheritdoc />
    public override string Provider => "Cysharp";

    /// <inheritdoc />
    public override string Name  => "Zstd";

    /// <inheritdoc />
    public override int MaximumCompressionLevel { get; } = Zstandard.MaxCompressionLevel;

    /// <inheritdoc />
    static MatCompressorZstd()
    {
        AvailableCompressors.Add(Instance);
    }

    /// <inheritdoc />
    protected override int GetCompressionLevel(CompressionLevel compressionLevel)
    {
        return compressionLevel switch
        {
            CompressionLevel.NoCompression => 0,
            CompressionLevel.Fastest => 0,
            CompressionLevel.Optimal => 10,
            CompressionLevel.SmallestSize => 12,
            _ => 10
        };
    }

    /// <inheritdoc />

    protected override byte[] CompressCore(Mat src, int compressionLevel)
    {
        var options = ZstandardCompressionOptions.Default with
        {
            CompressionLevel = compressionLevel,
        };
        using var buffer = CreateCompressionBuffer(src);
        using (var compressStream = new ZstandardStream(CreateCompressionStream(buffer), options))
        {
            src.CopyTo(compressStream);
        }

        return buffer.ToArray();
    }

    /// <inheritdoc />
    protected override void DecompressCore(byte[] compressedBytes, Mat dst)
    {
        using var compressedStream = new MemoryStream(compressedBytes, writable: false);
        using var decompressStream = new ZstandardStream(compressedStream, CompressionMode.Decompress, leaveOpen: true);
        decompressStream.ReadExactly(dst.GetSpanOfBytes());
    }
}