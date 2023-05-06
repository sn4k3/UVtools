/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using BinarySerialization;
using UVtools.Core.Extensions;

namespace UVtools.Core.Objects;

/// <summary>
/// A string that always end with 0x00 if not null
/// It contains the string length as uint
/// </summary>
public sealed class UInt24BigEndian
{
    [FieldOrder(0)] [FieldCount(3)] public byte[] Bytes { get; set; } = new byte[3];


    [Ignore]
    public uint Value
    {
        get => BitExtensions.ToUIntBigEndian(0, Bytes[0], Bytes[1], Bytes[2]);
        set
        {
            var bytes = BitExtensions.ToBytesBigEndian(value);
            Bytes[0] = bytes[1];
            Bytes[1] = bytes[2];
            Bytes[2] = bytes[3];
        }
    }

    public UInt24BigEndian() { }

    public UInt24BigEndian(uint value)
    {
        Value = value;
    }

    public override string ToString() => Value.ToString();

    private bool Equals(UInt24BigEndian other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is UInt24BigEndian other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)Value;
    }


    public static uint operator +(UInt24BigEndian a, UInt24BigEndian b) => a.Value + b.Value;

    public static uint operator -(UInt24BigEndian a, UInt24BigEndian b) => a.Value - b.Value;

    public static uint operator *(UInt24BigEndian a, UInt24BigEndian b) => a.Value * b.Value;

    public static uint operator /(UInt24BigEndian a, UInt24BigEndian b) => a.Value / b.Value;

    public static implicit operator UInt24BigEndian(uint value) => new(value);
}