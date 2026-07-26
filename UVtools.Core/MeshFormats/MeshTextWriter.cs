/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Buffers;
using System.Buffers.Text;
using System.IO;

namespace UVtools.Core.MeshFormats;

/// <summary>
/// A writer for mesh text data that writes to a provided buffer.
/// </summary>
internal ref struct MeshTextWriter
{
    private readonly Span<byte> _buffer;
    private int _position;

    public MeshTextWriter(Span<byte> buffer)
    {
        _buffer = buffer;
        _position = 0;
    }

    public void Append(ReadOnlySpan<byte> value)
    {
        if (value.Length > _buffer.Length - _position)
        {
            ThrowBufferTooSmall();
        }

        value.CopyTo(_buffer[_position..]);
        _position += value.Length;
    }

    public void Append(byte value)
    {
        if (_position >= _buffer.Length)
        {
            ThrowBufferTooSmall();
        }

        _buffer[_position++] = value;
    }

    public void Append(float value, StandardFormat format)
    {
        if (!Utf8Formatter.TryFormat(value, _buffer[_position..], out var bytesWritten, format))
        {
            ThrowBufferTooSmall();
        }

        _position += bytesWritten;
    }

    public void Append(uint value)
    {
        if (!Utf8Formatter.TryFormat(value, _buffer[_position..], out var bytesWritten))
        {
            ThrowBufferTooSmall();
        }

        _position += bytesWritten;
    }

    public readonly void CopyTo(Stream stream)
    {
        stream.Write(_buffer[.._position]);
    }

    private static void ThrowBufferTooSmall()
    {
        throw new InvalidOperationException("The mesh text buffer is too small.");
    }
}