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
using System.Data;
using System.Drawing;
using Emgu.CV.Util;

namespace UVtools.Core.Gerber.Primitives;

/// <summary>
/// A vector line is a rectangle defined by its line width, start and end points. The line ends are rectangular.
/// </summary>
public class VectorLinePrimitive : Primitive
{
    #region Constants
    public const byte Code = 20;
    #endregion

    #region Properties
    public override string Name => "VectorLine";

    /// <summary>
    /// Exposure off/on (0/1)
    /// 1
    /// </summary>
    public string ExposureExpression { get; set; } = "1";
    public byte Exposure { get; set; } = 1;

    /// <summary>
    /// Width of the line ≥ 0
    /// 2
    /// </summary>
    public string LineWidthExpression { get; set; } = "0";
    public float LineWidth { get; set; }

    /// <summary>
    /// Start point X coordinate
    /// 3
    /// </summary>
    public string StartXExpression { get; set; } = "0";

    public float StartX { get; set; }

    /// <summary>
    /// Start point Y coordinate
    /// 4
    /// </summary>
    public string StartYExpression { get; set; } = "0";

    public float StartY { get; set; }

    /// <summary>
    /// End point X coordinate
    /// 5
    /// </summary>
    public string EndXExpression { get; set; } = "0";

    public float EndX { get; set; }

    /// <summary>
    /// Start point Y coordinate
    /// 6
    /// </summary>
    public string EndYExpression { get; set; } = "0";

    public float EndY { get; set; }

    /// <summary>
    /// Rotation angle, in degrees counterclockwise, a decimal.
    /// The primitive is rotated around the origin of the macro definition, i.e. the (0, 0) point of macro coordinates.
    /// 7
    /// </summary>
    public string RotationExpression { get; set; } = "0";
    public float Rotation { get; set; } = 0;
    #endregion

    protected VectorLinePrimitive(GerberFormat document) : base(document) { }

    public VectorLinePrimitive(GerberFormat document, string exposureExpression, string lineWidthExpression, string startXExpression, string startYExpression, string endXExpression, string endYExpression, string rotationExpression = "0") : base(document)
    {
        ExposureExpression = exposureExpression;
        LineWidthExpression = lineWidthExpression.Replace("X", "*", StringComparison.OrdinalIgnoreCase); ;
        StartXExpression = startXExpression;
        StartYExpression = startYExpression;
        EndXExpression = endXExpression;
        EndYExpression = endYExpression;
        RotationExpression = rotationExpression.Replace("X", "*", StringComparison.OrdinalIgnoreCase); ;
    }

    public override bool IsExposureOff => IsParsed && Exposure == 0;

    public override void DrawFlashD3(Mat mat, PointF at, MCvScalar color, LineType lineType = LineType.EightConnected)
    {
        if (!IsParsed) return;
        if (LineWidth <= 0) return;

        var dx = (double)EndX - StartX;
        var dy = (double)EndY - StartY;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length == 0) return;

        // Vector lines have square ends. Construct the rectangle in millimeters so rotation and
        // unequal pixel pitches cannot change its physical width.
        var halfWidth = LineWidth * Document.SizeScale / 2;
        var nx = (float)(-dy / length * halfWidth);
        var ny = (float)(dx / length * halfWidth);
        ReadOnlySpan<PointF> corners = stackalloc PointF[4]
        {
            RotateAroundMacroOrigin(StartX + nx, StartY + ny, Rotation),
            RotateAroundMacroOrigin(EndX + nx, EndY + ny, Rotation),
            RotateAroundMacroOrigin(EndX - nx, EndY - ny, Rotation),
            RotateAroundMacroOrigin(StartX - nx, StartY - ny, Rotation)
        };
        var points = new Point[corners.Length];
        for (var index = 0; index < corners.Length; index++)
        {
            points[index] = Document.PositionMmToPx(at.X + corners[index].X, at.Y + corners[index].Y);
        }

        using var polygon = new VectorOfPoint(points);
        CvInvoke.FillConvexPoly(mat, polygon, GetExposureColor(Exposure, color), lineType);
    }

    public override RectangleF GetBoundsMm()
    {
        if (!IsParsed || LineWidth <= 0) return RectangleF.Empty;
        // The full width on each side also covers the corners of a slanted line
        return GetBoundsMm([RotateAroundMacroOrigin(StartX, StartY, Rotation), RotateAroundMacroOrigin(EndX, EndY, Rotation)],
            (float)(LineWidth * Document.SizeScale));
    }

    public override void ParseExpressions(params string[] args)
    {
        IsParsed = false;
        using var evaluator = new DataTable();
        if (!TryEvaluateByte(evaluator, ExposureExpression, args, 0, 1, out var exposure) ||
            !TryEvaluateLength(evaluator, LineWidthExpression, args, out var lineWidth) ||
            !TryEvaluateLength(evaluator, StartXExpression, args, out var startX) ||
            !TryEvaluateLength(evaluator, StartYExpression, args, out var startY) ||
            !TryEvaluateLength(evaluator, EndXExpression, args, out var endX) ||
            !TryEvaluateLength(evaluator, EndYExpression, args, out var endY) ||
            !TryEvaluateExpression(evaluator, RotationExpression, args, out var rotation) ||
            rotation is < float.MinValue or > float.MaxValue)
        {
            return;
        }

        Exposure = exposure;
        LineWidth = lineWidth;
        StartX = startX;
        StartY = startY;
        EndX = endX;
        EndY = endY;
        Rotation = (float)rotation;
        IsParsed = true;
    }
}
