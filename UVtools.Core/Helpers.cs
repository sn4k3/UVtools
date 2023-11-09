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
    public static BinarySerializer Serializer { get; } = new() {Endianness = Endianness.Little };

    public static MemoryStream Serialize(object value)
    {
        MemoryStream stream = new();
        Serializer.Serialize(stream, value);
        return stream;
    }

    public static T Deserialize<T>(Stream stream)
    {
        return Serializer.Deserialize<T>(stream);
    }

    public static uint SerializeWriteFileStream(FileStream fs, object value, int offset = 0)
    {
        using var stream = Serialize(value);
        return fs.WriteStream(stream, offset);
    }

    public static void SwapVariables<T>(ref T var1, ref T var2)
    {
        (var1, var2) = (var2, var1);
    }

    public static float BrightnessToPercent(byte brightness, byte roundPlates = 2) => (float)Math.Round(brightness * 100 / 255.0, roundPlates);
}