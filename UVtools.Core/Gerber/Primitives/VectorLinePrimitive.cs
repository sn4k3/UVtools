/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Data;
using System.Drawing;
using EmguExtensions;

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

    public override void DrawFlashD3(Mat mat, PointF at, LineType lineType = LineType.EightConnected)
    {
        if (!IsParsed) return;
        if (LineWidth <= 0) return;

        var start = RotateAroundMacroOrigin(StartX, StartY, Rotation);
        var end = RotateAroundMacroOrigin(EndX, EndY, Rotation);
        var pt1 = Document.PositionMmToPx(at.X + start.X, at.Y + start.Y);
        var pt2 = Document.PositionMmToPx(at.X + end.X, at.Y + end.Y);
        CvInvoke.Line(mat, pt1, pt2, Document.GetPolarityColor(Exposure), EmguCvExtensions.CorrectThickness(Document.SizeMmToPxOverride(LineWidth, Document.XYppmm.Height)), lineType);
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
