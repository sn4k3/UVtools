using System;
using System.Drawing;

namespace UVtools.Core.Objects;

[Serializable]
public struct GreyStride : IEquatable<GreyStride>
{
    private int _startIndex;
    private int _startX;
    private int _startY;
    private uint _length;
    private byte _grey;

    /// <summary>
    /// Gets the starting pixel index
    /// </summary>
    public int StartIndex
    {
        readonly get => _startIndex;
        set => _startIndex = value;
    }

    /// <summary>
    /// Gets the starting X coordinate
    /// </summary>
    public int StartX
    {
        readonly get => _startX;
        set => _startX = value;
    }

    /// <summary>
    /// Gets the starting Y coordinate
    /// </summary>
    public int StartY
    {
        readonly get => _startY;
        set => _startY = value;
    }

    /// <summary>
    /// Gets the length of the stride
    /// </summary>
    public uint Length
    {
        readonly get => _length;
        set => _length = value;
    }

    /// <summary>
    /// Gets the grey brightness
    /// </summary>
    public byte Grey
    {
        readonly get => _grey;
        set => _grey = value;
    }

    /// <summary>
    /// Gets the starting pixel location
    /// </summary>
    public Point StartLocation
    {
        get => new(StartX, StartY);
        set
        {
            _startX = value.X;
            _startY = value.Y;
        }
    }

    public int EndIndex => (int)(_startIndex + _length);

    /// <summary>
    /// Gets the ending pixel location, use only this with break on rows
    /// </summary>
    //public Point EndLocation => StartLocation with { X = StartLocation.X + (int)Length - 1 };


    public GreyStride(int startIndex, int startX, int startY, uint length, byte grey)
    {
        _startIndex = startIndex;
        _startX = startX;
        _startY = startY;
        _length = length;
        _grey = grey;
    }

    public GreyStride(int startIndex, Point location, uint length, byte grey) : this(startIndex, location.X, location.Y, length, grey)
    {}


    public override string ToString()
    {
        return $"{nameof(StartIndex)}: {StartIndex}, {nameof(StartX)}: {StartX}, {nameof(StartY)}: {StartY}, {nameof(Length)}: {Length}, {nameof(Grey)}: {Grey}";
    }

    public bool Equals(GreyStride other)
    {
        return _startIndex == other._startIndex && _startX == other._startX && _startY == other._startY && _length == other._length && _grey == other._grey;
    }

    public override bool Equals(object? obj)
    {
        return obj is GreyStride other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_startIndex, _startX, _startY, _length, _grey);
    }

    public static bool operator ==(GreyStride left, GreyStride right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GreyStride left, GreyStride right)
    {
        return !left.Equals(right);
    }
}