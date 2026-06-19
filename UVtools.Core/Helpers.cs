/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using BinarySerialization;
using System;
using System.IO;
using UVtools.Core.Extensions;

namespace UVtools.Core;

/// <summary>
/// A helper class with utilities
/// </summary>
public static class Helpers
{
    /// <summary>
    /// Gets the <see cref="BinarySerializer"/> instance
    /// </summary>
    public static BinarySerializer Serializer { get; } = new() { Endianness = Endianness.Little };

    public static BinarySerializer SerializerBigEndianness { get; } = new() { Endianness = Endianness.Big };

    public static MemoryStream Serialize(object value)
    {
        MemoryStream stream = new();
        Serializer.Serialize(stream, value);
        return stream;
    }

    public static MemoryStream Serialize(object value, Endianness endianness)
    {
        MemoryStream stream = new();
        switch (endianness)
        {
            case Endianness.Big:
                SerializerBigEndianness.Serialize(stream, value);
                break;
            default:
                Serializer.Serialize(stream, value);
                break;
        }

        return stream;
    }

    public static T Deserialize<T>(Stream stream)
    {
        return Serializer.Deserialize<T>(stream);
    }


    public static T Deserialize<T>(Stream stream, Endianness endianness)
    {
        switch (endianness)
        {
            case Endianness.Big:
                return SerializerBigEndianness.Deserialize<T>(stream);
                break;
            default:
                return Serializer.Deserialize<T>(stream);
                break;
        }
    }

    public static uint SerializeWriteFileStream(FileStream fs, object value, int offset = 0,
        Endianness endianness = Endianness.Little)
    {
        using var stream = Serialize(value, endianness);
        return fs.WriteStream(stream, offset);
    }

    public static uint SerializeWriteFileStream(FileStream fs, object value, Endianness endianness)
    {
        using var stream = Serialize(value, endianness);
        return fs.WriteStream(stream);
    }

    public static void SwapVariables<T>(ref T var1, ref T var2)
    {
        (var1, var2) = (var2, var1);
    }

    public static float BrightnessToPercent(byte brightness, byte roundPlates = 2)
    {
        return MathF.Round(brightness * 100 / 255.0f, roundPlates);
    }
}