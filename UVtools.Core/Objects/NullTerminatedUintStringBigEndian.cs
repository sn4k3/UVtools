/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using BinarySerialization;

namespace UVtools.Core.Objects;

/// <summary>
/// A string that always end with 0x00 if not null
/// It contains the string length as uint
/// </summary>
public sealed class NullTerminatedUintStringBigEndian
{
    [FieldOrder(0)] [FieldEndianness(Endianness.Big)]
    public uint SerializedLength { get; set; }

    [FieldOrder(1)] [FieldLength(nameof(SerializedLength))] [SerializeAs(SerializedType.TerminatedString)]
    public string? SerializedValue { get; set; }

    [Ignore]
    public string? Value
    {
        get => SerializedValue?.TrimEnd(char.MinValue);
        set => SerializedValue = value is null ? null : $"{value}{char.MinValue}";
    }

    [Ignore]
    public string ValueNotNull
    {
        get => SerializedValue?.TrimEnd(char.MinValue) ?? string.Empty;
        set => SerializedValue = value is null ? null : $"{value}{char.MinValue}";
    }

    public NullTerminatedUintStringBigEndian() { }

    public NullTerminatedUintStringBigEndian(string value)
    {
        Value = value;
    }

    public override string? ToString() => Value;
        
    private bool Equals(NullTerminatedUintStringBigEndian other)
    {
        return SerializedValue == other.SerializedValue;
    }

    public bool Equals(string value)
    {
        return Value == value;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is NullTerminatedUintStringBigEndian other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (SerializedValue != null ? SerializedValue.GetHashCode() : 0);
    }

    public static implicit operator NullTerminatedUintStringBigEndian(string value) => new(value);

    //public static implicit operator string(NullTerminatedUintStringBigEndian value) => new(value.Value);
}