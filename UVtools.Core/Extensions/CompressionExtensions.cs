/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using CommunityToolkit.HighPerformance;
using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace UVtools.Core.Extensions;

public static class CompressionExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetGzipUncompressedLength(ReadOnlyMemory<byte> compressedData)
    {
        return BitConverter.ToInt32(compressedData.Slice(compressedData.Length - 4, 4).Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetGzipUncompressedLength(Stream stream)
    {
        Span<byte> uncompressedLength = stackalloc byte[4];
        stream.Position = stream.Length - 4;
        stream.Read(uncompressedLength);
        stream.Seek(0, SeekOrigin.Begin);
        return BitConverter.ToInt32(uncompressedLength);
    }

    public static MemoryStream GZipCompress(Stream inputStream, CompressionLevel compressionLevel, bool leaveStreamOpen = false)
    {
        if (inputStream.Position == inputStream.Length) { inputStream.Seek(0, SeekOrigin.Begin); }

        var compressedStream = new MemoryStream();
        using (var gzipStream = new GZipStream(compressedStream, compressionLevel, true))
        {
            inputStream.CopyTo(gzipStream);
        }
        if (!leaveStreamOpen) { inputStream.Close(); }

        compressedStream.Seek(0, SeekOrigin.Begin);
        return compressedStream;
    }

    public static MemoryStream GZipCompress(byte[] inputStream, CompressionLevel compressionLevel) =>
        GZipCompress(new ReadOnlyMemory<byte>(inputStream).AsStream(), compressionLevel);

    public static byte[] GZipCompressToBytes(byte[] inputStream, CompressionLevel compressionLevel)
    {
        using var ms = GZipCompress(new ReadOnlyMemory<byte>(inputStream).AsStream(), compressionLevel);
        return ms.ToArray();
    }
            

    public static MemoryStream GZipDecompress(Stream compressedStream, bool leaveStreamOpen = false)
    {
        if (compressedStream.Position == compressedStream.Length) { compressedStream.Seek(0, SeekOrigin.Begin); }

        var uncompressedStream = new MemoryStream(GetGzipUncompressedLength(compressedStream));
        using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, leaveStreamOpen);
        gzipStream.CopyTo(uncompressedStream);

        return uncompressedStream;
    }

    public static ArraySegment<byte> GZipDecompress(ReadOnlyMemory<byte> compressedData)
    {
        using var uncompressedStream = new MemoryStream(GetGzipUncompressedLength(compressedData));
        using (var gzipStream = new GZipStream(compressedData.AsStream(), CompressionMode.Decompress, false))
        {
            gzipStream.CopyTo(uncompressedStream);
        }

        return uncompressedStream.TryGetBuffer(out var buffer)
            ? buffer
            : uncompressedStream.ToArray();
    }

    public static MemoryStream DeflateCompress(Stream inputStream, CompressionLevel compressionLevel, bool leaveStreamOpen = false)
    {
        if (inputStream.Position == inputStream.Length) { inputStream.Seek(0, SeekOrigin.Begin); }

        var compressedStream = new MemoryStream();
        using (var gzipStream = new DeflateStream(compressedStream, compressionLevel, true))
        {
            inputStream.CopyTo(gzipStream);
        }
        if (!leaveStreamOpen) { inputStream.Close(); }

        compressedStream.Seek(0, SeekOrigin.Begin);
        return compressedStream;
    }

    public static MemoryStream DeflateCompress(byte[] inputStream, CompressionLevel compressionLevel) =>
        DeflateCompress(new ReadOnlyMemory<byte>(inputStream).AsStream(), compressionLevel);

    public static byte[] DeflateCompressToBytes(byte[] inputStream, CompressionLevel compressionLevel)
    {
        using var ms = DeflateCompress(new ReadOnlyMemory<byte>(inputStream).AsStream(), compressionLevel);
        return ms.ToArray();
    }

    public static MemoryStream DeflateDecompress(Stream compressedStream, bool leaveStreamOpen = false)
    {
        if (compressedStream.Position == compressedStream.Length) { compressedStream.Seek(0, SeekOrigin.Begin); }

        var uncompressedStream = new MemoryStream();
        using var gzipStream = new DeflateStream(compressedStream, CompressionMode.Decompress, leaveStreamOpen);
        gzipStream.CopyTo(uncompressedStream);

        return uncompressedStream;
    }

    public static ArraySegment<byte> DeflateDecompress(ReadOnlyMemory<byte> compressedData)
    {
        using var uncompressedStream = new MemoryStream();
        using (var gzipStream = new DeflateStream(compressedData.AsStream(), CompressionMode.Decompress, false))
        {
            gzipStream.CopyTo(uncompressedStream);
        }

        return uncompressedStream.TryGetBuffer(out var buffer)
            ? buffer
            : uncompressedStream.ToArray();
    }
}