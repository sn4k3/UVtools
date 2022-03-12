/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace UVtools.Core.Extensions;

public static class StreamExtensions
{
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
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    public static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize, IProgress<long>? progress = null, CancellationToken cancellationToken = default)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (!source.CanRead)
            throw new ArgumentException("Has to be readable", nameof(source));
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));
        if (!destination.CanWrite)
            throw new ArgumentException("Has to be writable", nameof(destination));
        if (bufferSize < 0)
            throw new ArgumentOutOfRangeException(nameof(bufferSize));
        if (bufferSize == 0) bufferSize = DefaultCopyBufferSize;

        var buffer = new byte[bufferSize];
        long totalBytesRead = 0;
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) != 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
            totalBytesRead += bytesRead;
            progress?.Report(totalBytesRead);
        }
    }

    public static async Task CopyToAsync(this Stream source, Stream destination, IProgress<long>? progress = null, CancellationToken cancellationToken = default)
    {
        await CopyToAsync(source, destination, DefaultCopyBufferSize, progress, cancellationToken);
    }
}