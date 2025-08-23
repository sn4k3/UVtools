/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Microsoft.IO;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace UVtools.Core.Extensions;

public static class StreamExtensions
{
    public static readonly RecyclableMemoryStreamManager RecyclableMemoryStreamManager = new();

    //public const int DefaultCopyBufferSize = 81920; // 81.92 kilobytes, .NET default
    //public const int DefaultCopyBufferSize = 512000; // 512 kilobytes
    public const int DefaultCopyBufferSize = 1048576; // 1 MB

    /// <summary>
    /// Converts stream into byte array
    /// </summary>
    /// <param name="stream">Input</param>
    /// <returns>Byte array data</returns>
    public static byte[] ToArray(this Stream stream)
    {
        if (stream is MemoryStream isMemoryStream)
        {
            return isMemoryStream.TryGetBuffer(out var segment)
                ? segment.AsSpan().ToArray()
                : isMemoryStream.ToArray();
        }


        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var length = stream.Length;
            if (length == 0)
                return [];

            if (length > int.MaxValue)
                throw new InvalidOperationException("Stream too large to fit in a single byte array.");

            var buffer = GC.AllocateUninitializedArray<byte>((int)length);
            stream.ReadExactly(buffer);
            return buffer;
        }

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    public static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize, IProgress<long>? progress = null, CancellationToken cancellationToken = default)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (!source.CanRead)
            throw new ArgumentException("Has to be readable", nameof(source));

        if (destination is null)
            throw new ArgumentNullException(nameof(destination));
        if (!destination.CanWrite)
            throw new ArgumentException("Has to be writable", nameof(destination));

        if (bufferSize < 0)
            throw new ArgumentOutOfRangeException(nameof(bufferSize));

        if (bufferSize == 0)
            bufferSize = DefaultCopyBufferSize;

        // ✅ Skip zeroing the buffer
        var buffer = GC.AllocateUninitializedArray<byte>(bufferSize);
        var bufferMemory = buffer.AsMemory();

        long totalBytesRead = 0;

        while (true)
        {
            int bytesRead = await source
                .ReadAsync(bufferMemory, cancellationToken)
                .ConfigureAwait(false);

            if (bytesRead == 0)
                break;

            await destination
                .WriteAsync(bufferMemory[..bytesRead], cancellationToken)
                .ConfigureAwait(false);

            totalBytesRead += bytesRead;
            progress?.Report(totalBytesRead);
        }
    }

    public static async Task CopyToAsync(this Stream source, Stream destination, IProgress<long>? progress = null, CancellationToken cancellationToken = default)
    {
        await CopyToAsync(source, destination, DefaultCopyBufferSize, progress, cancellationToken).ConfigureAwait(false);
    }

    public static MemoryStream ToStream(this byte[] arr)
    {
        return new MemoryStream(arr);
    }
}