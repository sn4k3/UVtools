using System;
using System.Buffers;
using System.IO.Compression;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using K4os.Compression.LZ4;
using UVtools.Core.Extensions;

namespace UVtools.Core.EmguCV;

public abstract class MatCompressor
{
    /// <summary>
    /// Compresses the <see cref="Mat"/> into a byte array.
    /// </summary>
    /// <param name="src"></param>
    /// <param name="tag"></param>
    /// <returns></returns>
    public abstract byte[] Compress(Mat src, object? tag = null);

    /// <summary>
    /// Decompresses the <see cref="Mat"/> from a byte array.
    /// </summary>
    /// <param name="compressedBytes"></param>
    /// <param name="dst"></param>
    /// <param name="tag"></param>
    public abstract void Decompress(byte[] compressedBytes, Mat dst, object? tag = null);

    /// <summary>
    /// Compresses the <see cref="Mat"/> into a byte array.
    /// </summary>
    /// <param name="src"></param>
    /// <param name="tag"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<byte[]> CompressAsync(Mat src, object? tag = null, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Compress(src, tag), cancellationToken);
    }

    /// <summary>
    /// Decompresses the <see cref="Mat"/> from a byte array.
    /// </summary>
    /// <param name="compressedBytes"></param>
    /// <param name="dst"></param>
    /// <param name="tag"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task DecompressAsync(byte[] compressedBytes, Mat dst, object? tag = null, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Decompress(compressedBytes, dst, tag), cancellationToken);
    }
}


#region PNG
public sealed class MatCompressorPng : MatCompressor
{
    /// <summary>
    /// Instance of <see cref="MatCompressorPng"/>.
    /// </summary>
    public static readonly MatCompressorPng Instance = new();

    private MatCompressorPng()
    {
    }

    /// <inheritdoc />
    public override byte[] Compress(Mat src, object? tag = null)
    {
        return src.GetPngByes();
    }

    /// <inheritdoc />
    public override void Decompress(byte[] compressedBytes, Mat dst, object? tag = null)
    {
        CvInvoke.Imdecode(compressedBytes, ImreadModes.AnyColor, dst);
    }
}

public sealed class MatCompressorPngGreyScale : MatCompressor
{
    /// <summary>
    /// Instance of <see cref="MatCompressorPng"/>.
    /// </summary>
    public static readonly MatCompressorPngGreyScale Instance = new();

    private MatCompressorPngGreyScale()
    {
    }

    /// <inheritdoc />
    public override byte[] Compress(Mat src, object? tag = null)
    {
        return src.GetPngByes();
    }

    /// <inheritdoc />
    public override void Decompress(byte[] compressedBytes, Mat dst, object? tag = null)
    {
        CvInvoke.Imdecode(compressedBytes, ImreadModes.Grayscale, dst);
    }
}
#endregion

#region Deflate
public sealed class MatCompressorDeflate : MatCompressor
{
    /// <summary>
    /// Instance of <see cref="MatCompressorDeflate"/>.
    /// </summary>
    public static readonly MatCompressorDeflate Instance = new();

    private MatCompressorDeflate()
    {
    }

/// <inheritdoc />
public override byte[] Compress(Mat src, object? tag = null)
    {
        UnmanagedMemoryStream srcStream;
        if (src.IsContinuous)
        {
            srcStream = src.GetUnmanagedMemoryStream(FileAccess.Read);
        }
        else
        {
            var bytes = src.GetBytes(); // Need to copy the submatrix to get the full data in a contiguous block
            unsafe
            {
                fixed (byte* p = bytes)
                {
                    srcStream = new UnmanagedMemoryStream(p, bytes.Length);
                }
            }
        }
        

        using var compressedStream = StreamExtensions.RecyclableMemoryStreamManager.GetStream();
        using (var gzipStream = new DeflateStream(compressedStream, CompressionLevel.Fastest, true))
        {
            srcStream.CopyTo(gzipStream);
        }

        srcStream.Dispose();
        
        return compressedStream.ToArray();
    }

    /// <inheritdoc />
    public override void Decompress(byte[] compressedBytes, Mat dst, object? tag = null)
    {
        unsafe
        {
            fixed (byte* pBuffer = compressedBytes)
            {
                using var compressedStream = new UnmanagedMemoryStream(pBuffer, compressedBytes.Length);
                using var matStream = dst.GetUnmanagedMemoryStream(FileAccess.Write);
                using var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
                deflateStream.CopyTo(matStream);
            }
        }
    }
}
#endregion

#region GZip
public sealed class MatCompressorGZip : MatCompressor
{
    /// <summary>
    /// Instance of <see cref="MatCompressorGZip"/>.
    /// </summary>
    public static readonly MatCompressorGZip Instance = new();

    private MatCompressorGZip()
    {
    }

    /// <inheritdoc />
    public override byte[] Compress(Mat src, object? tag = null)
    {
        UnmanagedMemoryStream srcStream;
        if (src.IsContinuous)
        {
            srcStream = src.GetUnmanagedMemoryStream(FileAccess.Read);
        }
        else
        {
            var bytes = src.GetBytes(); // Need to copy the submatrix to get the full data in a contiguous block
            unsafe
            {
                fixed (byte* p = bytes)
                {
                    srcStream = new UnmanagedMemoryStream(p, bytes.Length);
                }
            }
        }


        using var compressedStream = StreamExtensions.RecyclableMemoryStreamManager.GetStream();
        using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Fastest, true))
        {
            srcStream.CopyTo(gzipStream);
        }
        
        srcStream.Dispose();

        return compressedStream.ToArray();
    }

    /// <inheritdoc />
    public override void Decompress(byte[] compressedBytes, Mat dst, object? tag = null)
    {
        unsafe
        {
            fixed (byte* pBuffer = compressedBytes)
            {
                using var compressedStream = new UnmanagedMemoryStream(pBuffer, compressedBytes.Length);
                using var matStream = dst.GetUnmanagedMemoryStream(FileAccess.Write);
                using var deflateStream = new GZipStream(compressedStream, CompressionMode.Decompress);
                deflateStream.CopyTo(matStream);
            }
        }
    }
}
#endregion

#region LZ4
public sealed class MatCompressorLz4 : MatCompressor
{
    /// <summary>
    /// Instance of <see cref="MatCompressorLz4"/>.
    /// </summary>
    public static readonly MatCompressorLz4 Instance = new();

    private MatCompressorLz4()
    {
    }

    /// <inheritdoc />
    public override byte[] Compress(Mat src, object? tag = null)
    {
        Span<byte> srcSpan;
        if (src.IsContinuous)
        {
            srcSpan = src.GetDataByteSpan();
        }
        else
        {
            var bytes = src.GetBytes(); // Need to copy the submatrix to get the full data in a contiguous block
            srcSpan = bytes.AsSpan();
        }


        var rent = ArrayPool<byte>.Shared.Rent(srcSpan.Length);
        var rentSpan = rent.AsSpan();
        
        var encodedLength = LZ4Codec.Encode(srcSpan, rentSpan);
        var target = rentSpan[..encodedLength].ToArray();

        ArrayPool<byte>.Shared.Return(rent);

        return target;
    }

    /// <inheritdoc />
    public override void Decompress(byte[] compressedBytes, Mat dst, object? tag = null)
    {
        LZ4Codec.Decode(compressedBytes.AsSpan(), dst.GetDataByteSpan());
    }
}
#endregion
