using System;
using System.IO;
using System.IO.Compression;
using Emgu.CV;
using EmguExtensions;
using NativeCompressions;

namespace UVtools.Core.Compressors;

public class MatCompressorLz4 : MatCompressor
{
    /// <summary>
    /// Provides a singleton instance of the <see cref="T:MatCompressorLz4" /> class for efficient reuse across the application.
    /// </summary>
    public static readonly MatCompressorLz4 Instance = new();

    static MatCompressorLz4()
    {
        AvailableCompressors.Add(Instance);
    }

    /// <inheritdoc />
    private MatCompressorLz4() { }

    /// <inheritdoc />
    public override string Provider => "Cysharp";

    /// <inheritdoc />
    public override string Name => "K4os";

    /// <inheritdoc />
    public override int MaximumCompressionLevel { get; } = LZ4.MaxCompressionLevel;

    /// <inheritdoc />
    protected override int GetCompressionLevel(CompressionLevel compressionLevel)
    {
        return compressionLevel switch
        {
            CompressionLevel.NoCompression => 0,
            CompressionLevel.Fastest => 1,
            CompressionLevel.Optimal => 10,
            CompressionLevel.SmallestSize => 12,
            _ => throw new ArgumentException(
                "Invalid CompressionLevel value.",
                nameof(compressionLevel)
            ),
        };
    }

    /// <inheritdoc />
    protected override byte[] CompressCore(Mat src, int compressionLevel)
    {
        var options = LZ4CompressionOptions.Default with { CompressionLevel = compressionLevel };
        // The compressed length is unknown; keep sparse streaming to avoid a worst-case output allocation.
        using var buffer = CreateCompressionBuffer(src);
        using (var compressStream = new LZ4Stream(CreateCompressionStream(buffer), options, false))
        {
            src.CopyTo(compressStream);
        }

        return buffer.ToArray();
    }

    /// <inheritdoc />
    protected override void DecompressCore(byte[] compressedBytes, Mat dst)
    {
        var destination = dst.GetSpanOfBytes();
        var bytesWritten = LZ4.Decompress(compressedBytes, destination);
        if (bytesWritten != destination.Length)
        {
            throw new InvalidDataException(
                $"The LZ4 frame contains {bytesWritten} bytes, but the destination Mat requires {destination.Length}."
            );
        }
    }
}
