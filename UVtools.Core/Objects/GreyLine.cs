using System;
using System.Drawing;
using UVtools.Core.Extensions;

namespace UVtools.Core.Objects;

/// <summary>
/// Structure containing start and end point with a grey value
/// </summary>
[Serializable]
public struct GreyLine : IEquatable<GreyLine>
{
    private int _startX;
    private int _startY;
    private int _endX;
    private int _endY;
    private byte _grey;

    public int StartX
    {
        readonly get => _startX;
        set => _startX = value;
    }

    public int StartY
    {
        readonly get => _startY;
        set => _startY = value;
    }

    public int EndX
    {
        readonly get => _endX;
        set => _endX = value;
    }

    public int EndY
    {
        readonly get => _endY;
        set => _endY = value;
    }

    public byte Grey
    {
        readonly get => _grey;
        set => _grey = value;
    }

    public Point StartLocation
    {
        get => new(_startX, _startY);
        set
        {
            _startX = value.X;
            _startY = value.Y;
        }
    }

    public Point EndLocation
    {
        get => new(_endX, _endY);
        set
        {
            _endX = value.X;
            _endY = value.Y;
        }
    }

    public double Length => PointExtensions.FindLength(StartLocation, EndLocation);

    public GreyLine(int startX, int startY, int endX, int endY, byte grey)
    {
        _startX = startX;
        _startY = startY;
        _endX = endX;
        _endY = endY;
        _grey = grey;
    }

    public override string ToString()
    {
        return $"{nameof(StartX)}: {StartX}, {nameof(StartY)}: {StartY}, {nameof(EndX)}: {EndX}, {nameof(EndY)}: {EndY}, {nameof(Grey)}: {Grey}";
    }

    public bool Equals(GreyLine other)
    {
        return _startX == other._startX && _startY == other._startY && _endX == other._endX && _endY == other._endY && _grey == other._grey;
    }

    public override bool Equals(object? obj)
    {
        return obj is GreyLine other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_startX, _startY, _endX, _endY, _grey);
    }

    public static bool operator ==(GreyLine left, GreyLine right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GreyLine left, GreyLine right)
    {
        return !left.Equals(right);
    }
}