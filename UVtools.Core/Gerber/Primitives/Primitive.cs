/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Text;

namespace UVtools.Core.Gerber.Primitives;

public abstract class Primitive
{
    #region Properties
    public abstract string Name { get; }

    public bool IsParsed { get; protected set; } = false;

    public GerberFormat Document { get; init; }

    #endregion

    //protected Primitive() { }

    protected Primitive(GerberFormat document)
    {
        Document = document;
    }

    /// <summary>
    /// Gets whether the primitive is drawn with the exposure off, clearing the primitives before it in the macro.
    /// </summary>
    public virtual bool IsExposureOff => false;

    /// <summary>
    /// Draws the primitive of a macro flashed at a position.
    /// </summary>
    /// <param name="mat">Image to draw on</param>
    /// <param name="at">Flash position in millimeters</param>
    /// <param name="color">Polarity color, drawn when the exposure is on, the opposite color when it is off</param>
    /// <param name="lineType">Line type</param>
    public abstract void DrawFlashD3(Mat mat, PointF at, MCvScalar color, LineType lineType = LineType.EightConnected);

    /// <summary>
    /// Gets the area the primitive covers in millimeters, relative to the macro origin and with
    /// <see cref="GerberFormat.SizeScale"/> applied to the sizes, or an empty rectangle when it draws nothing.
    /// </summary>
    public abstract RectangleF GetBoundsMm();

    /// <summary>
    /// Gets the color for an exposure, the polarity color when on and the opposite color when off.
    /// </summary>
    protected static MCvScalar GetExposureColor(byte exposure, MCvScalar color)
        => exposure > 0 ? color : GerberFormat.InvertColor(color);

    /// <summary>
    /// Gets a square centered on a point in millimeters.
    /// </summary>
    protected static RectangleF GetCenteredBoundsMm(PointF center, double size)
    {
        var half = (float)size / 2;
        return new RectangleF(center.X - half, center.Y - half, half * 2, half * 2);
    }

    /// <summary>
    /// Gets the rectangle enclosing points in millimeters, grown by <paramref name="margin"/> on each side.
    /// </summary>
    protected static RectangleF GetBoundsMm(ReadOnlySpan<PointF> points, float margin = 0)
    {
        if (points.IsEmpty) return RectangleF.Empty;

        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (var point in points)
        {
            minX = MathF.Min(minX, point.X);
            minY = MathF.Min(minY, point.Y);
            maxX = MathF.Max(maxX, point.X);
            maxY = MathF.Max(maxY, point.Y);
        }

        return RectangleF.FromLTRB(minX - margin, minY - margin, maxX + margin, maxY + margin);
    }

    public abstract void ParseExpressions(params string[] args);

    public virtual Primitive Clone() => (Primitive)MemberwiseClone();

    internal static bool TryEvaluateExpression(DataTable evaluator, string expression,
        IReadOnlyList<string> arguments, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(expression)) return false;

        if (double.TryParse(expression, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return double.IsFinite(value);
        }

        var normalized = new StringBuilder(expression.Length);
        for (var index = 0; index < expression.Length; index++)
        {
            var character = expression[index];
            if (character is 'X' or 'x')
            {
                normalized.Append('*');
                continue;
            }

            if (character != '$')
            {
                normalized.Append(character);
                continue;
            }

            var argumentStart = index + 1;
            var argumentEnd = argumentStart;
            while (argumentEnd < expression.Length && char.IsAsciiDigit(expression[argumentEnd]))
            {
                argumentEnd++;
            }

            if (argumentStart == argumentEnd ||
                !int.TryParse(expression.AsSpan(argumentStart, argumentEnd - argumentStart),
                    NumberStyles.None, CultureInfo.InvariantCulture, out var argumentIndex) ||
                argumentIndex >= arguments.Count ||
                !double.TryParse(arguments[argumentIndex], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var argumentValue) ||
                !double.IsFinite(argumentValue))
            {
                return false;
            }

            normalized.Append(argumentValue.ToString("R", CultureInfo.InvariantCulture));
            index = argumentEnd - 1;
        }

        try
        {
            var result = evaluator.Compute(normalized.ToString(), null);
            if (result is null or DBNull) return false;
            value = Convert.ToDouble(result, CultureInfo.InvariantCulture);
            return double.IsFinite(value);
        }
        catch (Exception exception) when (exception is EvaluateException or SyntaxErrorException
                                               or FormatException or InvalidCastException or OverflowException)
        {
            return false;
        }
    }

    protected static bool TryEvaluateByte(DataTable evaluator, string expression,
        IReadOnlyList<string> arguments, byte minimum, byte maximum, out byte value)
    {
        value = 0;
        if (!TryEvaluateExpression(evaluator, expression, arguments, out var result) ||
            result < minimum || result > maximum || result != Math.Truncate(result))
        {
            return false;
        }

        value = (byte)Math.Round(result, MidpointRounding.AwayFromZero);
        return value >= minimum && value <= maximum;
    }

    protected bool TryEvaluateLength(DataTable evaluator, string expression,
        IReadOnlyList<string> arguments, out float value)
    {
        value = 0;
        if (!TryEvaluateExpression(evaluator, expression, arguments, out var result) ||
            result is < float.MinValue or > float.MaxValue)
        {
            return false;
        }

        value = Document.GetMillimeters((float)result);
        return float.IsFinite(value);
    }

    /// <summary>
    /// Rotates a point counterclockwise around the macro origin. The image is drawn with the file coordinates as they
    /// are, the Y axis is only flipped afterward for the whole board, so this is the standard rotation.
    /// </summary>
    protected static PointF RotateAroundMacroOrigin(float x, float y, float rotation)
    {
        rotation %= 360;
        if (rotation % 360 == 0) return new PointF(x, y);

        var radians = rotation * Math.PI / 180;
        var cosine = Math.Cos(radians);
        var sine = Math.Sin(radians);
        return new PointF(
            (float)(x * cosine - y * sine),
            (float)(x * sine + y * cosine));
    }
}
