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
using Emgu.CV.Util;
using System;
using System.Data;
using System.Drawing;
using UVtools.Core.Gerber.Apertures;

namespace UVtools.Core.Gerber.Primitives;

/// <summary>
/// A vector line is a rectangle defined by its line width, start and end points. The line ends are rectangular.
/// </summary>
public class CenterLinePrimitive : Primitive
{
    #region Constants
    public const byte Code = 21;
    #endregion

    #region Properties
    public override string Name => "CenterLine";

    /// <summary>
    /// Exposure off/on (0/1)
    /// 1
    /// </summary>
    public string ExposureExpression { get; set; } = "1";
    public byte Exposure { get; set; } = 1;

    /// <summary>
    /// Width ≥ 0
    /// 2
    /// </summary>
    public string WidthExpression { get; set; } = "0";
    public float Width { get; set; }

    /// <summary>
    /// Height ≥ 0
    /// 3
    /// </summary>
    public string HeightExpression { get; set; } = "0";
    public float Height { get; set; }

    /// <summary>
    /// Center point X coordinate
    /// 4
    /// </summary>
    public string CenterXExpression { get; set; } = "0";

    public float CenterX { get; set; }

    /// <summary>
    /// Center point Y coordinate
    /// 5
    /// </summary>
    public string CenterYExpression { get; set; } = "0";

    public float CenterY { get; set; }

    /// <summary>
    /// Rotation angle, in degrees counterclockwise, a decimal.
    /// The primitive is rotated around the origin of the macro definition, i.e. the (0, 0) point of macro coordinates.
    /// 6
    /// </summary>
    public string RotationExpression { get; set; } = "0";
    public float Rotation { get; set; } = 0;
    #endregion

    protected CenterLinePrimitive(GerberFormat document) : base(document) { }

    public CenterLinePrimitive(GerberFormat document, string exposureExpression, string widthExpression = "0", string heightExpression = "0", string centerXExpression = "0", string centerYExpression = "0", string rotationExpression = "0") : base(document)
    {
        ExposureExpression = exposureExpression;
        WidthExpression = widthExpression.Replace("X", "*", StringComparison.OrdinalIgnoreCase);
        HeightExpression = heightExpression.Replace("X", "*", StringComparison.OrdinalIgnoreCase);
        CenterXExpression = centerXExpression.Replace("X", "*", StringComparison.OrdinalIgnoreCase);
        CenterYExpression = centerYExpression.Replace("X", "*", StringComparison.OrdinalIgnoreCase);
        RotationExpression = rotationExpression.Replace("X", "*", StringComparison.OrdinalIgnoreCase);
    }


    public override bool IsExposureOff => IsParsed && Exposure == 0;

    public override void DrawFlashD3(Mat mat, PointF at, MCvScalar color, LineType lineType = LineType.EightConnected)
    {
        if (!IsParsed) return;
        if (Width <= 0 || Height <= 0) return;

        color = GetExposureColor(Exposure, color);
        var rotation = Rotation % 360;
        if (rotation % 90 == 0)
        {
            // Axis aligned, drawn as a rectangle to cover the same pixels as a rectangle aperture of that size
            var center = RotateAroundMacroOrigin(CenterX, CenterY, rotation);
            var location = Document.PositionMmToPx(at.X + center.X, at.Y + center.Y);
            var size = Math.Abs(rotation) is 90 or 270
                ? Document.SizeMmToPx(Height, Width)
                : Document.SizeMmToPx(Width, Height);
            RectangleAperture.DrawCenteredRectangle(mat, location, size, color, lineType);
            return;
        }

        // Rotated by the exact angle, counterclockwise around the macro origin
        var corners = GetCorners();
        var points = new Point[corners.Length];
        for (var index = 0; index < corners.Length; index++)
        {
            points[index] = Document.PositionMmToPx(at.X + corners[index].X, at.Y + corners[index].Y);
        }

        using var vector = new VectorOfPoint(points);
        CvInvoke.FillPoly(mat, vector, color, lineType);
    }

    public override RectangleF GetBoundsMm()
    {
        if (!IsParsed || Width <= 0 || Height <= 0) return RectangleF.Empty;
        return GetBoundsMm(GetCorners());
    }

    /// <summary>
    /// Gets the rotated corners in millimeters relative to the macro origin, with the size scaled.
    /// </summary>
    private PointF[] GetCorners()
    {
        var halfWidth = (float)(Width * Document.SizeScale / 2);
        var halfHeight = (float)(Height * Document.SizeScale / 2);
        return
        [
            RotateAroundMacroOrigin(CenterX - halfWidth, CenterY - halfHeight, Rotation),
            RotateAroundMacroOrigin(CenterX + halfWidth, CenterY - halfHeight, Rotation),
            RotateAroundMacroOrigin(CenterX + halfWidth, CenterY + halfHeight, Rotation),
            RotateAroundMacroOrigin(CenterX - halfWidth, CenterY + halfHeight, Rotation)
        ];
    }

    public override void ParseExpressions(params string[] args)
    {
        IsParsed = false;
        using var evaluator = new DataTable();
        if (!TryEvaluateByte(evaluator, ExposureExpression, args, 0, 1, out var exposure) ||
            !TryEvaluateLength(evaluator, WidthExpression, args, out var width) ||
            !TryEvaluateLength(evaluator, HeightExpression, args, out var height) ||
            !TryEvaluateLength(evaluator, CenterXExpression, args, out var centerX) ||
            !TryEvaluateLength(evaluator, CenterYExpression, args, out var centerY) ||
            !TryEvaluateExpression(evaluator, RotationExpression, args, out var rotation) ||
            rotation is < float.MinValue or > float.MaxValue)
        {
            return;
        }

        Exposure = exposure;
        Width = width;
        Height = height;
        CenterX = centerX;
        CenterY = centerY;
        Rotation = (float)rotation;
        IsParsed = true;
    }
}
